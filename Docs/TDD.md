# 技术设计文档 (Technical Design Document)

## 项目：暗夜幸存者 (Night Survivors)

---

## 1. 技术概述

| 属性 | 说明 |
|------|------|
| **引擎** | Tuanjie (类Unity API) |
| **渲染管线** | Built-in Render Pipeline |
| **编程语言** | C# |
| **目标帧率** | 60 FPS (同屏500+敌人) |
| **数据驱动** | ScriptableObject |
| **架构模式** | 管理器模式 + 事件驱动 + 对象池 |

---

## 2. 架构设计

### 2.1 分层架构

```
┌─────────────────────────────────────┐
│         表现层 (Presentation)        │  ← UI、特效、动画
├─────────────────────────────────────┤
│         游戏逻辑层 (Game Logic)      │  ← 系统与管理器
├─────────────────────────────────────┤
│         数据层 (Data)                │  ← ScriptableObject、存档
├─────────────────────────────────────┤
│         基础设施层 (Infrastructure)  │  ← 对象池、事件总线、空间分区
└─────────────────────────────────────┘
```

### 2.2 核心管理器

```
GameManager (单局生命周期)
├── PlayerManager (玩家状态与武器)
├── EnemyManager (敌人生成与管理)
├── DropManager (掉落物管理)
├── UpgradeManager (升级选择管理)
├── PoolManager (对象池总管)
├── EventManager (事件总线)
├── TimeManager (游戏时间与难度曲线)
└── UnlockManager (角色解锁管理, slot-aware)
SaveSlotManager (静态工具类, 非MonoBehaviour, PlayerPrefs-based)
GoldManager (静态工具类, 非MonoBehaviour, PlayerPrefs-based, 金币+永久升级)
StatsTracker (静态工具类, 非MonoBehaviour, PlayerPrefs-based, 统计追踪)
```

### 2.3 设计模式

| 模式 | 应用场景 |
|------|----------|
| **对象池** | 敌人、投射物、掉落物、特效的复用 |
| **观察者(事件总线)** | 系统间解耦：击杀事件、升级事件、受伤事件 |
| **策略模式** | 不同武器的攻击行为 |
| **状态机** | 游戏状态、敌人AI状态 |
| **工厂模式** | 敌人创建、武器实例化 |
| **单例** | 全局管理器（GameManager、PoolManager等） |

---

## 3. 核心系统详细设计

### 3.1 对象池系统

**性能关键：同屏500+敌人必须使用对象池，杜绝运行时 Instantiate/Destroy。**

```csharp
// 核心接口
public class ObjectPool<T> where T : Component
{
    private Queue<T> _pool;
    private Func<T> _createFunc;
    private Action<T> _resetAction;
    private int _maxSize;

    T Get();                          // 获取对象
    void Return(T obj);              // 归还对象
    void Prewarm(int count);          // 预热池
}

// 池管理器
public class PoolManager : MonoBehaviour
{
    Dictionary<string, object> _pools;  // 按Prefab名索引
    
    T Get<T>(string key);
    void Return<T>(string key, T obj);
}
```

**池化对象列表：**

| 对象 | 预加载数量 | 最大容量 | 说明 |
|------|-----------|----------|------|
| 骷髅兵 | 100 | 300 | 最常见敌人 |
| 蝙蝠 | 80 | 200 | 高频生成 |
| 其他敌人 | 30 | 100 | 按需扩展 |
| 投射物 | 50 | 200 | 各类弹丸 |
| 经验宝石 | 200 | 500 | 大量掉落 |
| 特效 | 30 | 100 | 命中/死亡特效 |

### 3.2 空间分区系统

**目的：替代 `Physics.OverlapSphere`，O(1) 查询最近敌人，避免N²遍历。**

```csharp
public class SpatialGrid
{
    private Dictionary<Vector2Int, List<EnemyBase>> _cells;
    private float _cellSize;  // 建议3.0单位

    void Insert(EnemyBase enemy);
    void Remove(EnemyBase enemy);
    void Update(EnemyBase enemy);           // 敌人移动时更新
    EnemyBase FindNearest(Vector2 pos);     // 最近敌人查询
    List<EnemyBase> FindInRange(Vector2 center, float radius);
    List<EnemyBase> FindNearestN(Vector2 pos, int count);  // 最近N个敌人
}
```

**网格参数：**
- 单元格大小：3.0 单位
- 活跃区域：以玩家为中心 30×30 单位
- 超出范围的敌人不加入网格（休眠状态）

### 3.3 事件总线系统

```csharp
public static class GameEvents
{
    // 玩家事件
    static event Action<int> OnPlayerDamaged;      // 受伤
    static event Action OnPlayerDied;               // 死亡
    static event Action<int> OnPlayerLevelUp;      // 升级
    
    // 敌人事件
    static event Action<EnemyBase> OnEnemyDied;     // 击杀
    static event Action<EnemyBase> OnEnemySpawned;  // 生成
    
    // 掉落事件
    static event Action<DropBase> OnDropCollected;   // 拾取
    
    // 武器事件
    static event Action<WeaponBase> OnWeaponEvolved; // 进化
}
```

### 3.4 角色系统

```
Player (GameObject)
├── PlayerController (MonoBehaviour)   ← 输入处理、移动
├── PlayerStats (MonoBehaviour)        ← 属性管理、等级、经验
├── PlayerWeaponManager (MonoBehaviour) ← 武器装备管理
├── PlayerHitbox (MonoBehaviour)        ← 碰撞检测（受击判定）
├── SpriteRenderer
├── Rigidbody2D (Kinematic)
├── CircleCollider2D (Trigger)          ← 拾取范围
└── Animator
```

**PlayerController 移动逻辑：**
```csharp
void Update()
{
    Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    Vector2 direction = input.normalized;
    _rigidbody.velocity = direction * _stats.MoveSpeed;
    
    // 通知所有武器玩家位置和朝向
    _weaponManager.OnPlayerMoved(transform.position, direction);
}
```

**PlayerStats 属性管理：**
```csharp
public class PlayerStats : MonoBehaviour
{
    // 基础属性（由角色数据决定）
    public float MaxHP;
    public float MoveSpeed;
    public int Armor;
    public float PickupRange;
    public int Luck;
    public float Regen;
    public float DamageMultiplier = 1.0f;
    public float CooldownMultiplier = 1.0f;
    public float AreaMultiplier = 1.0f;
    public int ProjectileBonus = 0;
    
    // 运行时状态
    public int Level;
    public float CurrentHP;
    public float CurrentEXP;
    public float RequiredEXP;  // => 5 + 5 * Level * Level (quadratic curve)
}
```

### 3.5 武器系统

**架构：ScriptableObject定义 + 运行时实例 + 策略模式行为**

```
WeaponBase (抽象基类)
├── ProjectileWeapon : WeaponBase    ← 飞剑、飞刀、能量球
├── OrbitalWeapon : WeaponBase       ← 旋转盾
├── AreaWeapon : WeaponBase          ← 圣水、圣光
└── AuxiliaryWeapon : WeaponBase     ← 护盾、磁铁力场
```

**核心数据结构：**

```csharp
// 武器定义数据 (ScriptableObject)
[CreateAssetMenu(fileName = "WeaponData", menuName = "Data/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("Basic Info")]
    public string id;                    // 唯一标识 (e.g. "flying_sword", "knife", "excalibur")
    public string weaponName;
    public string description;
    public WeaponType weaponType;        // Projectile/Orbital/Area/Auxiliary
    public GameObject projectilePrefab;  // 投射物Prefab（如有）
    public int maxLevel = 8;
    public LevelData[] levelData;        // 每级数据
    
    // 进化相关
    public bool canEvolve;
    public string requiredItemId;        // 所需被动道具ID
    public WeaponData evolvedWeapon;     // 进化后武器

    [Tooltip("If true, this weapon can only be obtained through evolution, not from the upgrade pool.")]
    public bool isEvolutionOnly;
}

[System.Serializable]
public class LevelData
{
    public int damage;
    public float cooldown;
    public float area;
    public int projectileCount;
    public int pierce;
    public float speed;
    public float duration;
}
```

**武器运行时实例：**

```csharp
public abstract class WeaponBase : MonoBehaviour
{
    protected WeaponData _data;
    protected int _currentLevel = 1;
    protected float _cooldownTimer;
    protected PlayerStats _playerStats;
    
    public virtual void Initialize(WeaponData data, PlayerStats stats);
    public virtual void OnPlayerMoved(Vector2 position, Vector2 direction);
    public virtual void Upgrade();  // 升级到下一级
    protected abstract void Attack();  // 子类实现攻击逻辑
    
    void Update()
    {
        _cooldownTimer -= Time.deltaTime;
        if (_cooldownTimer <= 0)
        {
            Attack();
            _cooldownTimer = _data.levelData[_currentLevel - 1].cooldown * _playerStats.CooldownMultiplier;
        }
    }
}
```

**ProjectileWeapon 实现（飞剑示例）：**

> **注意**：`OrbitalWeapon` 在 `_orbitalPrefab` 为空时会自动从 `WeaponData.projectilePrefab` 获取 Prefab，确保运行时创建时不会丢失引用。

```csharp
public class ProjectileWeapon : WeaponBase
{
    protected override void Attack()
    {
        var levelData = _data.levelData[_currentLevel - 1];
        int count = levelData.projectileCount + _playerStats.ProjectileBonus;
        
        for (int i = 0; i < count; i++)
        {
            // 从空间网格找最近敌人
            EnemyBase target = SpatialGrid.FindNearest(transform.position);
            if (target == null) return;
            
            Vector2 dir = (target.transform.position - transform.position).normalized;
            
            // 对象池获取投射物
            var proj = PoolManager.Get<Projectile>(_data.projectilePrefab.name);
            proj.Launch(transform.position, dir, levelData);
        }
    }
}
```

**Projectile 投射物：**

```csharp
public class Projectile : MonoBehaviour
{
    private int _damage;
    private int _pierce;
    private int _hitCount;
    private float _speed;
    
    public void Launch(Vector2 origin, Vector2 direction, LevelData data)
    {
        transform.position = origin;
        _damage = data.damage;
        _pierce = data.pierce;
        _speed = data.speed;
        _hitCount = 0;
        // 设置朝向
        transform.right = direction;
    }
    
    void Update()
    {
        transform.position += transform.right * _speed * Time.deltaTime;
        // 超出范围自动回收
        if (Vector2.Distance(transform.position, PlayerPosition) > 30f)
            PoolManager.Return(this);
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<EnemyBase>(out var enemy))
        {
            enemy.TakeDamage(_damage);
            _hitCount++;
            if (_hitCount >= _pierce)
                PoolManager.Return(this);
        }
    }
}
```

**AreaWeapon 实现（圣水/圣光）：**

> **设计要点**：AreaWeapon 区分两种行为模式——**光环(Aura)**和**水潭(Puddle)**。
> - 光环（`_followsPlayer = true`，如圣光 Holy Light）：持续跟随玩家的永久区域，每次攻击刷新 duration 和范围。
> - 水潭（`_followsPlayer = false`，如圣水 Holy Water）：在玩家当前位置放置一个固定水潭，持续 duration 秒后自然过期消失；下次 CD 完成且无水潭时，在新位置重新创建。生成间隔（cooldown）与伤害 tick 间隔（_tickInterval）独立。

```csharp
public class AreaWeapon : WeaponBase
{
    protected GameObject _currentArea;
    protected GameObject _areaPrefab;
    protected float _areaRadius;
    protected float _duration;
    protected Vector2 _areaOrigin;
    protected bool _followsPlayer;
    protected bool _isHealing;

    protected override void Attack()
    {
        if (_followsPlayer)
        {
            // 光环：首次创建，后续刷新位置/duration
            if (_currentArea == null) CreateAreaEffect();
            else RefreshAreaEffect();
        }
        else
        {
            // 水潭：仅当无水潭时创建，已有则跳过（让水潭自然过期）
            if (_currentArea == null)
                CreateAreaEffect();
        }
    }

    protected virtual void RefreshAreaEffect()
    {
        if (_currentArea == null) return;
        if (_followsPlayer)
        {
            // 光环：重置 duration（保持永久），跟随玩家
            _currentArea.transform.position = _playerPosition;
            _areaOrigin = _playerPosition;
            var ld = CurrentLevelData;
            if (ld != null) { _duration = ld.duration; _areaRadius = ld.area; }
        }
        else
        {
            // 水潭：只更新范围（处理升级），不重置 duration
            var ld = CurrentLevelData;
            if (ld != null) _areaRadius = ld.area;
        }
        SetupAreaEffect();
    }
}
```

> **注意**：早期版本中 `RefreshAreaEffect()` 对所有区域武器无条件重置 `_duration`，导致圣水水潭永不过期、位置无法更新。已修复为仅对 `_followsPlayer = true` 的光环重置 duration。
>
> **视觉缩放**：Zone 预制体使用 `SpriteRenderer.drawMode = Simple` + `transform.localScale` 控制尺寸，不使用 `Sliced` 模式（因精灵资源未设置 9-slice border）。

### 3.6 敌人系统

**敌人数据定义：**

```csharp
[CreateAssetMenu(fileName = "EnemyData", menuName = "Data/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("Basic Info")]
    public string id;                    // 唯一标识 (e.g. "skeleton", "bat", "skeleton_king")
    public string enemyName;
    public GameObject prefab;
    public Sprite icon;

    [Header("Stats")]
    public float baseHP;
    public float moveSpeed;
    public float damage;

    [Header("Rewards")]
    public int expValue;
    public int goldValue;

    [Header("Spawn Settings")]
    public float spawnWeight;     // 生成权重
    public float minSpawnTime;    // 最早出现时间(秒)
}
```

**敌人基类：**

```csharp
public class EnemyBase : MonoBehaviour
{
    protected EnemyData _data;
    protected float _currentHP;
    protected float _moveSpeed;
    
    public virtual void Initialize(EnemyData data)
    {
        _data = data;
        _currentHP = data.baseHP;
        _moveSpeed = data.moveSpeed;
    }
    
    void Update()
    {
        // 追踪玩家
        Vector2 dir = ((Vector2)PlayerTransform.position - (Vector2)transform.position).normalized;
        transform.position += (Vector3)(dir * _moveSpeed * Time.deltaTime);
    }
    
    public virtual void TakeDamage(int damage)
    {
        // 护甲减伤
        int finalDamage = Mathf.Max(1, damage - PlayerStats.Armor);
        _currentHP -= finalDamage;
        
        // 受击闪烁
        StartCoroutine(HitFlash());
        
        if (_currentHP <= 0)
        {
            Die();
        }
    }
    
    protected virtual void Die()
    {
        GameEvents.OnEnemyDied?.Invoke(this);
        // 生成掉落物
        DropManager.Instance.SpawnDrops(transform.position, _data.expValue, _data.goldValue);
        // 归还对象池
        PoolManager.Return(gameObject.name, this);
    }
    
    public void ResetForReuse()
    {
        _currentHP = _data.baseHP;
        // 重置其他状态
    }
}
```

### 3.7 敌人生成器

> **数据加载**：EnemySpawner 不再使用 `[SerializeField]` 引用 EnemyData，而是通过 `EnemyDatabase.Instance` 获取所有敌人和 Boss 数据。Boss 召唤数据也通过 ID 查找，不再依赖 Inspector 赋值。

```csharp
public class EnemySpawner : MonoBehaviour
{
    // 生成参数读取 GameBalanceConfig.Instance
    // 敌人/Boss数据读取 EnemyDatabase.Instance
    
    void Start()
    {
        _enemyPool = EnemyDatabase.Instance.enemies;
    }
    
    void Update()
    {
        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0)
        {
            SpawnWave();
            // 间隔随时间递减
            float elapsedMinutes = TimeManager.ElapsedTime / 60f;
            var cfg = GameBalanceConfig.Instance;
            _spawnTimer = _baseSpawnInterval / (1 + cfg.spawnAccelRate * elapsedMinutes);
        }
    }
    
    void SpawnWave()
    {
        var cfg = GameBalanceConfig.Instance;
        if (EnemyManager.ActiveCount >= cfg.maxEnemiesOnScreen) return;
        
        // 确定本次生成的敌人类型
        var availableEnemies = GetAvailableEnemies();
        if (availableEnemies.Count == 0) return;
        
        // 计算生成数量
        int count = CalculateSpawnCount();
        
        for (int i = 0; i < count; i++)
        {
            var data = WeightedRandom.Select(availableEnemies);
            Vector2 pos = GetSpawnPosition();
            EnemyManager.SpawnEnemy(data, pos);
        }
    }
    
    // Boss 生成通过 EnemyDatabase.Instance.bosses 按 ID 查找
    void SpawnBoss(string bossId)
    {
        var bossData = EnemyDatabase.Instance.GetById(bossId);
        if (bossData != null)
            EnemyManager.SpawnEnemy(bossData, GetSpawnPosition());
    }
}
```

### 3.8 升级系统

**UpgradeUI 使用 CanvasGroup 控制显隐**（而非 SetActive），确保 Start() 始终执行以绑定事件。选择/跳过后通过 `OnUpgradeComplete` 事件驱动 Hide()，而非选择后立即隐藏（避免时序问题）。

> **数据加载**：UpgradeManager 不再使用 `[SerializeField]` 引用 WeaponData/PassiveData 数组，而是通过 `WeaponDatabase.Instance` 和 `PassiveDatabase.Instance` 获取所有可用数据。

```csharp
public class UpgradeManager : MonoBehaviour
{
    // 数据来源: WeaponDatabase.Instance / PassiveDatabase.Instance
    private PlayerWeaponManager _weaponManager;
    private PlayerStats _playerStats;
    
    public void OnPlayerLevelUp(int newLevel)
    {
        // 暂停游戏
        Time.timeScale = 0f;
        
        // 生成3个随机选项
        var options = GenerateUpgradeOptions(3);
        
        // 显示升级UI
        UpgradeUI.Show(options, OnOptionSelected);
    }
    
    List<UpgradeOption> GenerateUpgradeOptions(int count)
    {
        var pool = new List<UpgradeOption>();
        
        // 添加：已有武器升级选项
        foreach (var weapon in _weaponManager.EquippedWeapons)
        {
            if (weapon.CurrentLevel < weapon.MaxLevel)
                pool.Add(new WeaponUpgradeOption(weapon));
        }
        
        // 添加：新武器选项（最多6把武器）— 从 WeaponDatabase 获取
        if (_weaponManager.EquippedWeapons.Count < 6)
        {
            foreach (var w in WeaponDatabase.Instance.weapons)
            {
                if (w.isEvolutionOnly) continue;  // 进化武器不在普通升级池
                if (!_weaponManager.HasWeapon(w))
                    pool.Add(new NewWeaponOption(w));
            }
        }
        
        // 添加：被动道具选项 — 从 PassiveDatabase 获取
        foreach (var p in PassiveDatabase.Instance.passives)
        {
            if (!_playerStats.HasPassive(p) || _playerStats.GetPassiveLevel(p) < p.maxLevel)
                pool.Add(new PassiveUpgradeOption(p));
        }
        
        // 按权重随机选取count个
        return WeightedRandom.SelectN(pool, count);
    }
    
    void OnOptionSelected(UpgradeOption option)
    {
        option.Apply(_weaponManager, _playerStats);
        Time.timeScale = 1f;
    }
}
```

### 3.9 掉落物系统

```csharp
public class DropBase : MonoBehaviour
{
    public DropType Type;
    public int Value;
    
    void Update()
    {
        // 磁铁吸引逻辑
        float dist = Vector2.Distance(transform.position, PlayerPosition);
        if (dist < PlayerStats.PickupRange)
        {
            // 加速飞向玩家
            Vector2 dir = ((Vector2)PlayerPosition - (Vector2)transform.position).normalized;
            transform.position += (Vector3)(dir * 15f * Time.deltaTime);
        }
        
        // 接触自动拾取
        if (dist < 0.5f)
        {
            Collect();
        }
    }
    
    protected virtual void Collect()
    {
        switch (Type)
        {
            case DropType.ExpGem:
                PlayerStats.AddEXP(Value);
                break;
            case DropType.Health:
                PlayerStats.Heal(Value);
                break;
            case DropType.Chest:
                // 不直接处理，触发开箱流程（暂停+动画+进化/升级）
                GameEvents.InvokeChestCollected();
                break;
            case DropType.Gold:
                GoldManager.AddGold(Value);
                break;
        }
        PoolManager.Return(this);
    }
}
```

### 3.10 宝箱开箱系统 (ChestOpenUI)

**参考 Vampire Survivors 的宝箱机制：拾取后暂停游戏 → 播放开箱动画 → 展示结果 → 恢复游戏。**

```csharp
/// <summary>
/// 宝箱开箱UI：拾取宝箱时暂停游戏，播放开箱动画，展示进化/升级结果。
/// 动画使用 unscaledDeltaTime 驱动（timeScale=0时不卡住）。
/// </summary>
public class ChestOpenUI : MonoBehaviour
{
    // 阶段枚举
    enum Phase { Idle, ChestAppear, ChestBurst, ResultShow, FadeOut }
    
    // 核心流程：
    // 1. 订阅 GameEvents.OnChestCollected
    // 2. 收到事件 → Time.timeScale = 0 → 开始 ChestAppear 阶段
    // 3. ChestAppear(0.3s): 宝箱缩放0→1 + 金色光晕
    // 4. ChestBurst(0.5s): 宝箱放大到1.3 + 白色闪光 + 粒子爆发
    // 5. 判定内容：
    //    a. 有可进化武器 → CheckAndEvolveWeapons() → 显示进化文字
    //    b. 无进化 → 随机升级一把已装备武器1级 → 显示升级文字
    //    c. 全满级+无进化 → 给少量金币保底
    // 6. ResultShow(2.0s): 显示结果文字 + 对应VFX
    // 7. FadeOut(0.3s): 遮罩淡出 → Time.timeScale = 1
}
```

**宝箱收集数据流：**

```
DropBase.Collect() [Chest类型]
    ↓
GameEvents.InvokeChestCollected()
    ↓
ChestOpenUI.OnChestCollected()
    ├── Time.timeScale = 0
    ├── 播放开箱动画 (unscaledDeltaTime)
    ├── 判定结果：
    │   ├── 有进化 → pwm.CheckAndEvolveWeapons() → GameEvents.InvokeWeaponEvolved() → 进化VFX
    │   ├── 无进化 → pwm.UpgradeRandomWeapon() → 升级文字
    │   └── 全满级 → PlayerStats.AddGold(保底金币)
    ├── 显示结果文字
    └── Time.timeScale = 1
```

**VFX 集成：**
- `VFXManager` 新增 `PlayChestOpenEffect(Vector3 position)` — 金色光爆 + 粒子
- `AudioManager` 新增 chest open SFX — 开箱音效
- `GameEvents` 新增 `OnChestCollected` 事件

**PlayerWeaponManager 新增方法：**
```csharp
/// <summary>随机升级一把未满级的已装备武器。返回被升级的武器，无可用武器返回null。</summary>
public WeaponBase UpgradeRandomWeapon()
```

---

## 4. 项目目录结构

```
Assets/
├── Scripts/
│   ├── Core/                          # 核心框架
│   │   ├── GameManager.cs             # 单局游戏生命周期
│   │   ├── PoolManager.cs             # 对象池管理器
│   │   ├── ObjectPool.cs              # 泛型对象池
│   │   ├── SpatialGrid.cs             # 空间分区网格
│   │   ├── GameEvents.cs              # 事件总线
│   │   ├── Singleton.cs               # 单例基类
│   │   ├── WeightedRandom.cs          # 加权随机工具
│   │   ├── GoldManager.cs             # 金币+永久升级管理 (静态工具类)
│   │   └── StatsTracker.cs            # 统计数据追踪 (静态工具类)
│   │
│   ├── Player/                        # 玩家系统
│   │   ├── PlayerController.cs        # 移动控制
│   │   ├── PlayerStats.cs             # 属性管理
│   │   ├── PlayerWeaponManager.cs     # 武器装备管理
│   │   ├── PlayerHitbox.cs            # 受击检测
│   │   └── CameraFollow.cs            # 正交相机跟随
│   │
│   ├── Weapons/                       # 武器系统
│   │   ├── WeaponBase.cs              # 武器抽象基类
│   │   ├── ProjectileWeapon.cs        # 投射型武器
│   │   ├── OrbitalWeapon.cs           # 轨道型武器
│   │   ├── AreaWeapon.cs              # 范围型武器
│   │   ├── AuxiliaryWeapon.cs         # 辅助型武器
│   │   ├── Projectile.cs              # 投射物
│   │   ├── OrbitalObject.cs           # 轨道物体
│   │   ├── HolyLight.cs              # 圣光武器
│   │   └── HolyWater.cs              # 圣水武器
│   │
│   ├── Enemies/                       # 敌人系统
│   │   ├── EnemyBase.cs               # 敌人基类
│   │   ├── EnemySpawner.cs            # 敌人生成器
│   │   ├── EnemyManager.cs            # 敌人管理器
│   │   ├── EliteEnemy.cs              # 精英敌人组件
│   │   ├── MageEnemy.cs               # 法师敌人组件
│   │   └── MageProjectile.cs          # 法师投射物
│   │
│   ├── Drops/                         # 掉落物系统
│   │   ├── DropBase.cs                # 掉落物基类
│   │   └── DropManager.cs             # 掉落物管理器 (读取 GameBalanceConfig)
│   │
│   ├── Upgrades/                      # 升级系统
│   │   ├── UpgradeManager.cs          # 升级管理器
│   │   ├── UpgradeOption.cs           # 升级选项基类
│   │   ├── UpgradeUI.cs              # 升级选择UI (CanvasGroup驱动显隐)
│   │   ├── UpgradeCard.cs            # 升级卡片组件
│   │   └── ChestOpenUI.cs            # 宝箱开箱UI (暂停+动画+结果展示)
│   │
│   ├── UI/                            # UI系统
│   │   ├── HUDController.cs          # 战斗HUD (含EXP文字显示)
│   │   ├── MainMenuUI.cs             # 主菜单 + 存档管理面板
│   │   ├── CharacterSelectUI.cs       # 角色选择界面
│   │   ├── PauseMenuController.cs     # 暂停菜单
│   │   ├── ResultScreen.cs           # 结算界面
│   │   ├── UpgradeShopUI.cs          # 永久升级商店 (编程式UI, CanvasGroup驱动)
│   │
│   └── Data/                          # 数据定义
│       ├── WeaponData.cs              # 武器数据SO
│       ├── EnemyData.cs               # 敌人数据SO
│       ├── CharacterData.cs           # 角色数据SO
│       ├── PassiveData.cs             # 被动道具数据SO (含StatType枚举)
│       ├── GameBalanceConfig.cs       # 全局系统配置SO (Resources单例)
│       ├── WeaponDatabase.cs         # 武器数据库SO (Resources单例)
│       ├── EnemyDatabase.cs          # 敌人数据库SO (Resources单例)
│       ├── CharacterDatabase.cs      # 角色数据库SO (Resources单例)
│       ├── PassiveDatabase.cs        # 被动道具数据库SO (Resources单例)
│       ├── SaveSlotManager.cs         # 存档栏位管理 (静态工具类)
│       └── UnlockManager.cs          # 角色解锁管理 (Singleton, slot-aware)
│
├── Resources/                         # Resources.Load 加载入口
│   ├── GameBalanceConfig.asset        # 全局系统配置 (单例SO)
│   ├── Data/                          # 数据库SO + 内容SO
│   │   ├── WeaponDatabase.asset       # 武器数据库 (引用所有WeaponData)
│   │   ├── EnemyDatabase.asset        # 敌人数据库 (引用所有EnemyData)
│   │   ├── CharacterDatabase.asset    # 角色数据库 (引用所有CharacterData)
│   │   ├── PassiveDatabase.asset      # 被动道具数据库 (引用所有PassiveData)
│   │   ├── Weapons/                   #   WeaponData (6基础+6进化)
│   │   ├── Enemies/                   #   EnemyData (6敌人)
│   │   ├── Bosses/                    #   EnemyData (3Boss)
│   │   ├── Characters/                #   CharacterData (5角色)
│   │   └── Passives/                  #   PassiveData (8被动道具)
│   └── VFX/                           # VFX 预制体 (对象池加载)
│
├── Prefabs/
│   ├── Player/
│   ├── Weapons/
│   ├── Projectiles/
│   ├── Enemies/
│   ├── Drops/
│   └── VFX/
│
├── Art/
│   ├── Sprites/
│   │   ├── Characters/
│   │   ├── Enemies/
│   │   ├── Weapons/
│   │   ├── Drops/
│   │   ├── UI/
│   │   └── Environment/
│   └── Animations/
│       ├── Characters/
│       └── Enemies/
│
├── Scenes/
│   ├── MainMenu.unity
│   └── GameLevel.unity
│
├── Audio/
│   ├── BGM/
│   └── SFX/
│
└── Fonts/
```

---

## 5. 性能优化策略

### 5.1 关键性能目标

| 指标 | 目标值 |
|------|--------|
| 目标帧率 | 60 FPS |
| 同屏敌人上限 | 500 |
| 单帧敌人更新耗时 | < 3ms |
| 内存占用（战斗中） | < 500MB |
| GC触发频率 | 尽量避免运行时GC |

### 5.2 优化手段

| 优化项 | 具体措施 | 预期收益 |
|--------|---------|----------|
| **对象池** | 所有运行时创建的GameObject全部池化 | 消除Instantiate/Destroy开销 |
| **空间分区** | GridPartition替代Physics查询 | 查询O(1) vs O(N) |
| **禁用物理** | 敌人间不互相碰撞，仅与玩家检测 | 减少Physics计算 |
| **LOD更新** | 远处敌人5帧更新一次位置 | 减少50%+的Update调用 |
| **合批渲染** | 相同SpriteRenderer材质的Sprite合批 | 减少DrawCall |
| **避免GC** | 缓存List/Array，避免LINQ，避免闭包 | 消除GC Spikes |
| **限制生成** | 超出玩家视野25单位外的敌人体眠 | 减少活跃对象数 |
| **特效优化** | 粒子数量限制，对象池管理 | 避免特效卡顿 |

### 5.3 碰撞检测优化

- 玩家 vs 敌人：使用 `Rigidbody2D` + `Collider2D` Trigger
- 投射物 vs 敌人：使用 `Rigidbody2D` + `Collider2D` Trigger
- 掉落物拾取：距离判断（不用物理），在 `Update` 中检查
- 敌人之间：**禁用碰撞**（不添加碰撞体或使用 Layer 忽略）
- 使用 Layer Matrix 减少不必要的碰撞检测对

### 5.4 帧预算分配

```
单帧预算 (16.67ms @60FPS)
├── 渲染：8ms
├── 逻辑更新：5ms
│   ├── 敌人AI/移动：2ms
│   ├── 武器攻击逻辑：1ms
│   ├── 碰撞检测：1ms
│   └── 其他逻辑：1ms
└── 物理引擎：3ms
```

---

## 6. 数据驱动设计

### 6.1 ScriptableObject 体系

所有游戏内容数据均通过 ScriptableObject 定义，策划可独立调整无需改代码：

```
CharacterData (角色)
├── string id                          # 唯一标识 (e.g. "hero", "mage")
├── string characterName
├── Sprite portrait
├── float baseHP, moveSpeed, pickupRange...
├── WeaponData startingWeapon
├── string specialPassiveId
└── UnlockCondition unlockCondition

WeaponData (武器)
├── string id                          # 唯一标识 (e.g. "flying_sword", "knife", "excalibur")
├── string weaponName, description
├── WeaponType type
├── Sprite icon
├── GameObject projectilePrefab
├── LevelData[] levelData (8级)
├── bool canEvolve
├── string requiredPassiveId
├── WeaponData evolvedWeapon
└── bool isEvolutionOnly

EnemyData (敌人)
├── string id                          # 唯一标识 (e.g. "skeleton", "bat", "skeleton_king")
├── string enemyName
├── GameObject prefab
├── Sprite icon
├── float baseHP, moveSpeed, damage
├── int expValue, goldValue
├── float spawnWeight
└── float minSpawnTime

PassiveData (被动道具)
├── string id                          # 唯一标识 (e.g. "wing", "bracer", "magnet")
├── string passiveName, description
├── Sprite icon
├── int maxLevel
├── float effectPerLevel
└── StatType affectedStat
```

### 6.2 Database 单例模式

**所有内容数据 SO 通过 Database 单例加载，与 GameBalanceConfig 采用相同的 `Resources.Load` 模式。管理器不再使用 `[SerializeField]` 引用数据数组，消除了场景依赖和引用断裂风险。**

```csharp
/// <summary>
/// 武器数据库 — 持有所有 WeaponData 引用，通过 Resources.Load 单例访问。
/// 存放路径: Resources/Data/WeaponDatabase.asset
/// </summary>
[CreateAssetMenu(fileName = "WeaponDatabase", menuName = "Data/WeaponDatabase")]
public class WeaponDatabase : ScriptableObject
{
    private static WeaponDatabase _instance;
    public static WeaponDatabase Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<WeaponDatabase>("Data/WeaponDatabase");
            return _instance;
        }
    }

    public WeaponData[] weapons;

    /// <summary>按 id 查找武器，未找到返回 null</summary>
    public WeaponData GetById(string id) { ... }
}
```

**四个 Database SO 及其存放路径：**

| Database | Resources 路径 | 持有数据 | 使用方 |
|----------|---------------|---------|--------|
| `WeaponDatabase` | `Data/WeaponDatabase` | `WeaponData[] weapons` | UpgradeManager, PlayerWeaponManager, CodexUI |
| `EnemyDatabase` | `Data/EnemyDatabase` | `EnemyData[] enemies`<br>`EnemyData[] bosses` | EnemySpawner, SkeletonKing, DarkLord |
| `CharacterDatabase` | `Data/CharacterDatabase` | `CharacterData[] characters` | UnlockManager, CharacterSelectUI, CodexUI |
| `PassiveDatabase` | `Data/PassiveDatabase` | `PassiveData[] passives` | UpgradeManager, PlayerWeaponManager, CodexUI |

**Database 加载时机**：首次访问 `*.Database.Instance` 时通过 `Resources.Load` 延迟加载，后续调用返回缓存实例。与 `GameBalanceConfig.Instance` 完全一致。

**Boss 召唤数据引用方式**：Boss 子类（SkeletonKing、DarkLord）不再使用 `[SerializeField] EnemyData` 引用召唤数据，改为通过 `summonEnemyId` 字符串字段 + `EnemyDatabase.Instance.GetById(summonEnemyId)` 查找。这消除了 Boss Prefab 对特定 EnemyData 资产的 Inspector 依赖。

**UI 数据查询方式**：CodexUI 和 PauseMenuController 不再使用 `Resources.FindObjectsOfTypeAll<T>()` 扫描全部已加载资产，改为直接使用对应 Database 实例，确保数据完整性和确定性。

### 6.3 数值配置体系

**设计原则：内容数据 vs 系统配置分离**

| 位置 | 职责 | 加载方式 | 调整场景 |
|------|------|---------|----------|
| `Assets/Resources/Data/{Category}/` | 内容定义 (每个游戏实体一个SO) | Database 单例引用 | 添加新武器/敌人/角色时 |
| `Assets/Resources/Data/{Database}.asset` | 数据库 (引用某类所有SO) | `Resources.Load` 单例 | 增删数据条目时 |
| `Assets/Resources/GameBalanceConfig.asset` | 全局系统配置 (唯一SO) | `Resources.Load` 单例 | 调整难度/掉落/缩放等数值时 |

**数据流总览：**

```
Resources.Load
    │
    ├── GameBalanceConfig.Instance    ← 系统参数 (难度/掉落/物理/武器行为)
    │       ↓
    │   DifficultyManager, EnemySpawner, DropManager, WeaponBase, PlayerStats ...
    │
    ├── WeaponDatabase.Instance      ← 武器内容 (6基础+6进化)
    │       ↓
    │   UpgradeManager (升级选项), PlayerWeaponManager (进化查询), CodexUI
    │
    ├── EnemyDatabase.Instance       ← 敌人/Boss内容 (6敌人+3Boss)
    │       ↓
    │   EnemySpawner (生成池/Boss生成), SkeletonKing/DarkLord (召唤数据), CodexUI
    │
    ├── CharacterDatabase.Instance   ← 角色内容 (5角色)
    │       ↓
    │   UnlockManager (解锁查询), CharacterSelectUI, CodexUI
    │
    └── PassiveDatabase.Instance     ← 被动道具内容 (8道具)
            ↓
        UpgradeManager (升级选项), PlayerWeaponManager (进化查询), CodexUI
```

| 分类 | 字段 | 说明 |
|------|------|------|
| **难度缩放** | hpGrowthRate, spawnAccelRate, damageGrowthRate, speedGrowthRate | 每分钟增长率 |
| **精英** | eliteHpMultiplier, eliteDamageMultiplier, eliteScaleMultiplier, eliteSpeedMultiplier, eliteExpMultiplier, eliteGoldMultiplier | 精英属性倍率 |
| **Boss** | bossScaleMultiplier, bossAttackInterval, bossAttackIntervalPhaseMultiplier, bossExpMultiplier, bossGoldMultiplier, bossPhaseTransitionDuration | Boss行为参数 |
| **生成器** | minSpawnDistance, maxSpawnDistance, maxEnemiesOnScreen, eliteWaveInterval, bossSpawnDistance, baseBatchSize, batchGrowthRate, maxBatchSize, eliteWaveMinCount, eliteWaveMaxCount, eliteWaveCap, eliteWaveBonusPerWave | 生成参数 |
| **掉落率** | expGemChance, healthDropChance, chestDropChance, chestBossDropChance, magnetDropChance | 各类掉落概率 |
| **经验分级** | expGemSmallValue, expGemMediumValue, expGemLargeValue, expGemMediumThreshold, expGemLargeThreshold | EXP宝石数值与阈值 |
| **掉落物** | healthRestoreAmount, magnetDropDuration, magnetDropPickupBoost | 回血/磁铁参数 |
| **掉落性能** | maxDropsPerFrame, dropMergeRadius | 每帧生成上限+合并半径 |
| **武器** | areaWeaponTickInterval, areaWeaponDamageRadiusBuffer, orbitalRotationSpeed, orbitalHitCooldown, projectileMaxRange, projectileSearchRadius | 各类武器行为参数 |
| **玩家** | extraLifeReviveHpPercent, regenTickInterval | 额外生命/再生参数 |
| **掉落物理** | dropAttractSpeed, dropCollectRadius, dropVacuumSpeedMultiplier, dropVacuumEaseRate, dropVacuumDelay, dropVacuumRange | 吸附/拾取物理参数 |

---

## 7. 关键技术决策

| 决策 | 选择 | 理由 |
|------|------|------|
| 是否使用ECS | 否 | Tuanjie引擎ECS支持不确定，传统MonoBehaviour更稳定 |
| 2D物理引擎 | Rigidbody2D (Kinematic) | 性能优于Dynamic，敌人不需要物理响应 |
| UI框架 | UGUI (Canvas) | 成熟稳定，Tuanjie兼容性好 |
| 数据持久化 | JSON序列化 | 简单可靠，适合存档系统 |
| 碰撞检测 | 混合（物理+距离判断） | 投射物用物理，掉落物用距离 |
| 相机 | 正交相机跟随玩家 | 俯视角2D游戏标准方案 |
| 内容数据加载 | Database SO + Resources.Load 单例 | 与 GameBalanceConfig 一致；消除 Inspector SerializeField 引用断裂风险；数据与场景解耦 |

### 7.1 数据加载架构迁移说明

**迁移前（旧模式）：**
- 各 Manager 使用 `[SerializeField]` 引用数据数组（如 `_availableWeapons`, `_enemyPool`, `_allCharacters`）
- 数据 SO 存放在 `Assets/Data/` 目录下，通过 Inspector 手动拖拽赋值
- 问题：场景/ prefab 依赖强；引用断裂时 NullReferenceException 静默崩溃（历史发生过 PassiveData 引用全部断裂事故）
- UI 查询使用 `Resources.FindObjectsOfTypeAll<T>()`（不可靠，依赖资产是否已加载）

**迁移后（新模式）：**
- 4 个 Database SO（WeaponDatabase / EnemyDatabase / CharacterDatabase / PassiveDatabase）存放在 `Resources/Data/`
- 每个 Database 持有对应类型的数组引用，通过 `Resources.Load` 单例访问
- 数据 SO 移至 `Resources/Data/{Category}/` 下，由 Database SO 引用
- Manager 启动时从 `Database.Instance` 获取数据，无需 Inspector 赋值
- Boss 召唤数据改用 `summonEnemyId` 字符串 + `EnemyDatabase.Instance.GetById(id)` 查找
- UI 查询改用 Database 实例，不再使用 `Resources.FindObjectsOfTypeAll`

**受影响的 Manager 和组件：**

| 组件 | 移除的 SerializeField | 改用的 Database 字段 |
|------|----------------------|-------------------|
| UpgradeManager | `_availableWeapons`, `_availablePassives` | `WeaponDatabase.Instance.weapons`, `PassiveDatabase.Instance.passives` |
| PlayerWeaponManager | `_allPassives` | `PassiveDatabase.Instance.passives` |
| EnemySpawner | `_enemyPool`, `_skeletonKingData`, `_darkLordData`, `_deathBossData` | `EnemyDatabase.Instance.enemies`, `EnemyDatabase.Instance.bosses` |
| UnlockManager | `_allCharacters` | `CharacterDatabase.Instance.characters` |
| SkeletonKing | `_summonData` (EnemyData) | `summonEnemyId` (string) + `EnemyDatabase.Instance.GetById()` |
| DarkLord | `_eliteSummonData` (EnemyData) | `summonEnemyId` (string) + `EnemyDatabase.Instance.GetById()` |
| CodexUI | `Resources.FindObjectsOfTypeAll` | `WeaponDatabase.Instance`, `CharacterDatabase.Instance` |
| PauseMenuController | `Resources.FindObjectsOfTypeAll` | `WeaponDatabase.Instance`, `PassiveDatabase.Instance` |

---

## 8. 存档系统设计

### 8.1 存档栏位管理 (SaveSlotManager)

**纯静态工具类（非 MonoBehaviour），基于 PlayerPrefs 管理3个存档栏位。**

```csharp
public static class SaveSlotManager
{
    public const int MAX_SLOTS = 3;
    
    // 当前激活存档 (0-2, -1=无)
    public static int ActiveSlotIndex { get; }
    public static bool HasActiveSlot { get; }
    
    // 存档信息
    public static string GetSlotName(int index);
    public static bool HasSlot(int index);
    public static bool IsDuplicateName(string name);
    public static int UsedSlotCount { get; }
    
    // 存档 CRUD
    public static int CreateSlot(string name);     // 返回栏位索引, -1=失败
    public static void DeleteSlot(int index);      // 清除该存档所有数据
    public static void SwitchSlot(int index);       // 切换激活存档, 触发 UnlockManager 重载
    public static void ClearActiveSlot();           // 取消当前选择
    
    // PlayerPrefs 键格式
    // 存档名: SaveSlot_{i}_Name
    // 激活槽: SaveSlot_Active
    // 解锁键: Save_{slotIndex}_Unlock_{characterId}
}
```

**数据隔离机制**：每个存档的解锁数据通过 `Save_{slotIndex}_Unlock_{charId}` 前缀隔离，切换存档时 `UnlockManager.ReloadUnlockState()` 自动重载。

### 8.2 角色解锁管理 (UnlockManager, slot-aware)

```csharp
public class UnlockManager : Singleton<UnlockManager>
{
    // 解锁键使用存档前缀: Save_{activeSlot}_Unlock_{charId}
    // 无激活存档时，仅 isDefaultUnlocked 角色可用
    
    public bool IsUnlocked(string characterId);
    public void SetUnlocked(string characterId);  // 仅在有激活存档时生效
    public List<CharacterData> CheckUnlocks(PlayerStats stats, float elapsedTime);
    public void ReloadUnlockState();               // 切换存档时调用
    
    // 清除指定存档的解锁数据 (SaveSlotManager.DeleteSlot 调用)
    public static void ClearSlotData(int slotIndex);
}
```

### 8.3 主菜单 (MainMenuUI)

**编程式构建UI，挂在 HUD Canvas 上。控制游戏流程入口。**

```csharp
public class MainMenuUI : MonoBehaviour
{
    // 流程: MainMenuUI.Show() → 创建/选择存档 → 开始游戏 → CharacterSelectUI.Show()
    // MainMenuUI 显示时隐藏 HUD 元素 (SetHUDEnabled)
    // "开始游戏" 需要 HasActiveSlot 才可点击
    // "存档管理" 弹出面板: 3栏位 CRUD + InputField 创建
}
```

### 8.4 金币系统 (GoldManager)

**纯静态工具类（非 MonoBehaviour），基于 PlayerPrefs 管理每存档金币余额和永久升级。**

```csharp
public static class GoldManager
{
    // 金币管理 (键: Save_{slot}_Gold)
    public static int GetGold(int slot);
    public static void AddGold(int slot, int amount);     // ResultScreen 调用
    public static bool SpendGold(int slot, int amount);   // 商店购买
    
    // 永久升级 (5种, 键: Save_{slot}_Upgrade_{type})
    public enum PermanentUpgradeType { HPBonus, MoveSpeedBonus, DamageBonus, PickupRangeBonus, ExtraLife }
    public static int GetUpgradeLevel(int slot, PermanentUpgradeType type);
    public static bool PurchaseUpgrade(int slot, PermanentUpgradeType type);  // 扣金+升级
    public static void ApplyPermanentUpgrades(PlayerStats stats);             // 游戏开始时注入
    public static void ClearSlotData(int slot);                               // 删除存档时清理
    
    // 费用表 (GDD 9.2): HP/移速 50/100/200/400/800, 伤害 100/200/400/800/1600,
    //                   拾取 30/60/120/240/480, 额外生命 500/1000/2000
}
```

**关键交互**：
- `PlayerStats.InitializeFromCharacterData()` 末尾调用 `GoldManager.ApplyPermanentUpgrades(this)` 注入永久加成
- `PlayerStats.TakeDamage()` 中 ExtraLife 在 `OnPlayerDied` 前拦截，复活回50%HP
- `ResultScreen.Show()` 中调用 `GoldManager.AddGold()` 持久化局内金币（`_persisted` 防重入）
- `HUDController` 监听 `OnGoldChanged` 事件刷新金币文字（change-detected，无每帧字符串分配）

### 8.5 统计追踪 (StatsTracker)

**纯静态工具类（非 MonoBehaviour），基于 PlayerPrefs 管理每存档累计统计。**

```csharp
public static class StatsTracker
{
    // 统计项 (键: Save_{slot}_Stat_{statName})
    public static int GetTotalKills(int slot);
    public static void AddTotalKills(int slot, int count);
    public static int GetTotalGames(int slot);
    public static void IncrementTotalGames(int slot);
    public static float GetBestSurvivalTime(int slot);
    public static void UpdateBestTime(int slot, float time);
    public static int GetTotalGoldEarned(int slot);
    public static void AddTotalGoldEarned(int slot, int amount);
    public static void ClearSlotStats(int slot);  // 删除存档时清理
}
```

**数据流**：`ResultScreen.Show()` → `StatsTracker.AddTotalKills/IncrementTotalGames/UpdateBestTime/AddTotalGoldEarned`（`_persisted` 防重入）。`SaveSlotManager.DeleteSlot()` 调用 `ClearSlotStats()`。

### 8.6 永久升级商店 (UpgradeShopUI)

**编程式UI（CanvasGroup驱动显隐），由 MainMenuUI 按需 AddComponent 创建。**

```csharp
public class UpgradeShopUI : MonoBehaviour
{
    // 5个升级行: 名称 + 描述 + 等级指示器(■□□□□) + 费用 + 购买按钮
    // 金币余额显示在顶部
    // 购买通过 GoldManager.PurchaseUpgrade(), 成功后自动刷新
    // 监听 OnPermanentUpgradePurchased 事件自动刷新
    // 关闭按钮 → CanvasGroup hide
}
```

**交互流**：MainMenuUI "🛒 商店"按钮 → `OnOpenShop()` → 创建/显示 UpgradeShopUI → 购买升级 → GoldManager 扣金+升级 → OnPermanentUpgradePurchased 事件 → UI 刷新

### 8.7 未来扩展（JSON存档）

当前实现基于 PlayerPrefs，适合小数据量。未来可扩展为 JSON 序列化：
[Serializable]
public class SaveData
{
    // 永久进度
    public int totalGold;
    public List<string> unlockedCharacters;
    public List<string> unlockedWeapons;
    public Dictionary<string, int> permanentUpgrades;  // upgradeId → level
    
    // 统计
    public int totalKills;
    public int totalGamesPlayed;
    public float bestSurvivalTime;
    
    // 设置
    public float sfxVolume;
    public float bgmVolume;
}

// 存档管理器
public class SaveManager
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");
    
    public void Save(SaveData data);
    public SaveData Load();
    public void DeleteSave();
}
```

---

*文档版本: v1.4 | 最后更新: 2026-05-12*
