using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages all audio playback: BGM with crossfade, SFX with pooled sources.
/// Subscribes to GameEvents for automatic audio triggering.
/// </summary>
public class AudioManager : Singleton<AudioManager>
{
    // ── BGM Clips ─────────────────────────────────────────────
    [Header("BGM Clips")]
    public AudioClip menuTheme;
    public AudioClip battleTheme;
    public AudioClip bossTheme;

    // ── SFX Clips ─────────────────────────────────────────────
    [Header("Weapon SFX")]
    public AudioClip weaponSword;
    public AudioClip weaponKnife;
    public AudioClip weaponShield;
    public AudioClip weaponEnergy;
    public AudioClip weaponHoly;
    public AudioClip weaponWater;

    [Header("Enemy & Environment SFX")]
    public AudioClip enemyHit;
    public AudioClip enemyDie;
    public AudioClip playerHit;
    public AudioClip expPickup;
    public AudioClip levelUp;
    public AudioClip evolve;

    // ── Settings ──────────────────────────────────────────────
    [Header("Settings")]
    [Range(0f, 1f)] public float bgmVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 0.7f;
    public float bgmFadeDuration = 1f;
    public int sfxSourceCount = 12;
    [Range(0f, 0.2f)] public float sfxThrottle = 0.05f;

    // ── BGM Internal ──────────────────────────────────────────
    private AudioSource _bgmA;
    private AudioSource _bgmB;
    private bool _bgmUsingA = true;
    private AudioClip _targetBGM;
    private float _fadeProgress;
    private bool _fading;

    // ── SFX Pool ──────────────────────────────────────────────
    private AudioSource[] _sfxPool;
    private int _sfxIndex;
    private readonly Dictionary<int, float> _lastSFXTime = new Dictionary<int, float>();

    // ── Public API ────────────────────────────────────────────

    /// <summary>Play BGM clip with optional crossfade duration.</summary>
    public void PlayBGM(AudioClip clip, float fadeTime = -1f)
    {
        if (clip == _targetBGM && !_fading) return;

        // Complete any in-progress fade first
        if (_fading) CompleteFade();

        _targetBGM = clip;
        float duration = fadeTime < 0f ? bgmFadeDuration : fadeTime;

        var incoming = _bgmUsingA ? _bgmB : _bgmA;
        var outgoing = _bgmUsingA ? _bgmA : _bgmB;

        incoming.clip = clip;
        incoming.volume = 0f;
        incoming.Play();

        if (duration <= 0f || outgoing.clip == null)
        {
            outgoing.Stop();
            outgoing.clip = null;
            incoming.volume = bgmVolume;
            _bgmUsingA = !_bgmUsingA;
            _fading = false;
        }
        else
        {
            _fadeProgress = 0f;
            _fading = true;
        }
    }

    /// <summary>Stop BGM with optional fade-out.</summary>
    public void StopBGM(float fadeTime = -1f)
    {
        _targetBGM = null;
        float duration = fadeTime < 0f ? bgmFadeDuration : fadeTime;

        if (duration <= 0f)
        {
            CurrentBGM.Stop();
            CurrentBGM.clip = null;
            _fading = false;
        }
        else
        {
            _fadeProgress = 0f;
            _fading = true;
        }
    }

    /// <summary>Play a one-shot SFX with per-clip throttling.</summary>
    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;

        // Throttle by clip instance ID
        int id = clip.GetInstanceID();
        if (sfxThrottle > 0f && _lastSFXTime.TryGetValue(id, out float last))
        {
            if (Time.unscaledTime - last < sfxThrottle) return;
        }
        _lastSFXTime[id] = Time.unscaledTime;

        AudioSource src = GetSFXSource();
        src.PlayOneShot(clip, sfxVolume * volumeScale);
    }

    // Convenience methods
    public void PlayMenuBGM() => PlayBGM(menuTheme);
    public void PlayBattleBGM() => PlayBGM(battleTheme);
    public void PlayBossBGM() => PlayBGM(bossTheme);

    // ── Lifecycle ─────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        InitBGM();
        InitSFX();
    }

    private void Start()
    {
        // Play initial BGM based on current game state
        if (GameManager.HasInstance && GameManager.Instance.CurrentState == GameManager.GameState.Menu)
            PlayMenuBGM();
    }

    private void OnEnable()
    {
        GameEvents.OnBossSpawned += HandleBossSpawned;
        GameEvents.OnBossDied += HandleBossDied;
        GameEvents.OnPlayerDamaged += HandlePlayerDamaged;
        GameEvents.OnPlayerLevelUp += HandlePlayerLevelUp;
        GameEvents.OnWeaponEvolved += HandleWeaponEvolved;
        GameEvents.OnEnemyDied += HandleEnemyDied;
        GameEvents.OnEnemyHit += HandleEnemyHit;
        GameEvents.OnDropCollected += HandleDropCollected;
    }

    private void OnDisable()
    {
        GameEvents.OnBossSpawned -= HandleBossSpawned;
        GameEvents.OnBossDied -= HandleBossDied;
        GameEvents.OnPlayerDamaged -= HandlePlayerDamaged;
        GameEvents.OnPlayerLevelUp -= HandlePlayerLevelUp;
        GameEvents.OnWeaponEvolved -= HandleWeaponEvolved;
        GameEvents.OnEnemyDied -= HandleEnemyDied;
        GameEvents.OnEnemyHit -= HandleEnemyHit;
        GameEvents.OnDropCollected -= HandleDropCollected;
    }

    private void Update()
    {
        if (!_fading) return;

        var incoming = _bgmUsingA ? _bgmB : _bgmA;
        var outgoing = _bgmUsingA ? _bgmA : _bgmB;

        _fadeProgress += Time.unscaledDeltaTime / bgmFadeDuration;
        float t = Mathf.Clamp01(_fadeProgress);

        if (_targetBGM != null)
        {
            incoming.volume = Mathf.Lerp(0f, bgmVolume, t);
            outgoing.volume = Mathf.Lerp(bgmVolume, 0f, t);
        }
        else
        {
            outgoing.volume = Mathf.Lerp(bgmVolume, 0f, t);
        }

        if (t >= 1f)
        {
            _fading = false;
            outgoing.Stop();
            outgoing.clip = null;

            if (_targetBGM != null)
            {
                _bgmUsingA = !_bgmUsingA;
                incoming.volume = bgmVolume;
            }
        }
    }

    // ── BGM Implementation ────────────────────────────────────

    private AudioSource CurrentBGM => _bgmUsingA ? _bgmA : _bgmB;

    private void InitBGM()
    {
        _bgmA = gameObject.AddComponent<AudioSource>();
        _bgmB = gameObject.AddComponent<AudioSource>();

        foreach (var src in new[] { _bgmA, _bgmB })
        {
            src.loop = true;
            src.playOnAwake = false;
            src.priority = 0;
            src.volume = 0f;
        }
    }

    private void CompleteFade()
    {
        var incoming = _bgmUsingA ? _bgmB : _bgmA;
        var outgoing = _bgmUsingA ? _bgmA : _bgmB;

        outgoing.Stop();
        outgoing.clip = null;

        if (_targetBGM != null)
        {
            _bgmUsingA = !_bgmUsingA;
            incoming.volume = bgmVolume;
        }

        _fading = false;
    }

    // ── SFX Implementation ────────────────────────────────────

    private void InitSFX()
    {
        _sfxPool = new AudioSource[sfxSourceCount];
        for (int i = 0; i < sfxSourceCount; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            _sfxPool[i] = src;
        }
    }

    private AudioSource GetSFXSource()
    {
        // Find idle source
        for (int i = 0; i < _sfxPool.Length; i++)
        {
            int idx = (_sfxIndex + i) % _sfxPool.Length;
            if (!_sfxPool[idx].isPlaying)
            {
                _sfxIndex = (idx + 1) % _sfxPool.Length;
                return _sfxPool[idx];
            }
        }
        // All busy — steal oldest (round-robin position)
        var stolen = _sfxPool[_sfxIndex];
        stolen.Stop();
        _sfxIndex = (_sfxIndex + 1) % _sfxPool.Length;
        return stolen;
    }

    // ── Event Handlers ────────────────────────────────────────

    private void HandleBossSpawned(BossEnemy _) => PlayBossBGM();
    private void HandleBossDied(BossEnemy _) => PlayBattleBGM();
    private void HandlePlayerDamaged(int _) => PlaySFX(playerHit);
    private void HandlePlayerLevelUp(int _) => PlaySFX(levelUp);
    private void HandleWeaponEvolved(WeaponBase _) => PlaySFX(evolve);
    private void HandleEnemyDied(EnemyBase _) => PlaySFX(enemyDie);
    private void HandleEnemyHit(EnemyBase _) => PlaySFX(enemyHit);
    private void HandleDropCollected(DropBase _) => PlaySFX(expPickup);
}
