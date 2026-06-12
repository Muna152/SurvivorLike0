# 暗夜幸存者 (Night Survivors)

> 玩家操控角色在黑暗中生存，武器自动攻击海量敌人，通过升级获取更强大的武器和道具，活到最后即为胜利。

## 游戏信息

| 属性 | 说明 |
|------|------|
| **类型** | Roguelite 生存动作游戏 |
| **参考** | Vampire Survivors / Brotato / Holocure |
| **视角** | 俯视角 2D |
| **核心玩法** | 移动躲避 + 自动攻击 + 升级选择 |
| **单局时长** | 15~30 分钟 |
| **美术风格** | 像素风 / 扁平化卡通 |

## 技术栈

- **引擎**: Unity 2022.3 (Tuanjie)
- **渲染管线**: URP

## 项目结构

```
Assets/
├── Animators/       # 动画控制器
├── Art/             # 美术资源
├── Audio/           # 音频（BGM / SFX）
├── Data/            # ScriptableObject 数据资产
├── Editor/          # 编辑器扩展
├── Fonts/           # 字体
├── Prefabs/         # 预制体
├── Resources/       # 运行时加载资源
├── Scenes/          # 场景
├── Scripts/         # C# 脚本
│   ├── Core/        # 核心系统（游戏状态、事件等）
│   ├── Player/      # 玩家逻辑
│   ├── Enemies/     # 敌人 AI 与生成
│   ├── Weapons/     # 武器系统
│   ├── Upgrades/    # 升级与技能选择
│   ├── Drops/       # 掉落物（经验宝石等）
│   ├── UI/          # 界面逻辑
│   ├── Rendering/   # 渲染相关
│   ├── VFX/         # 视觉特效
│   └── Data/        # 数据定义
├── Sprites/         # 2D 精灵图
└── Tests/           # 测试
```

## 快速开始

1. 克隆仓库
   ```bash
   git clone https://github.com/Muna152/SurvivorLike0.git
   ```
2. 用 Unity 2022.3 打开项目
3. 打开 `Assets/Scenes/GameLevel.unity`
4. 点击 Play 运行

## 文档

- [游戏设计文档 (GDD)](Docs/GDD.md)
- [技术设计文档 (TDD)](Docs/TDD.md)

## License

All rights reserved.
