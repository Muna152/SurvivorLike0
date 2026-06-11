using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 全局音频管理器，统一管理BGM、SFX、环境音和UI音效。
/// 单例模式，支持音量控制、循环控制、淡入淡出、节流。
/// </summary>
[DefaultExecutionOrder(-500)]
public sealed class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // 节流控制
    public float sfxThrottle = 0.1f;

    // 音量设置
    public float masterVolume = 1f;
    public float bgmVolume = 1f;
    public float sfxVolume = 1f;
    public float ambientVolume = 1f;
    public float uiVolume = 1f;

    private Dictionary<int, float> _lastSFXTime = new Dictionary<int, float>();
    private WaitForSeconds _throttleWait;

    // BGM源（单例支持循环与过渡）
    private AudioSource _bgmSource;

    // 简单SFX池（固定大小）
    private AudioSource[] _sfxSources;
    private int _sfxIndex = 0;
    public int sfxSourceCount = 12;

    // UI和Ambient单例源
    private AudioSource _uiSource;
    private AudioSource _ambientSource;

    // 音频池引用
    private AudioPool _audioPool;

    // 日志辅助（可选：可替换为更完善的日志系统）
    public static class DebugLogger
    {
        public static void Log(string msg) => Debug.Log(msg);
        public static void LogWarning(string msg) => Debug.LogWarning(msg);
        public static void LogError(string msg) => Debug.LogError(msg);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _throttleWait = new WaitForSeconds(sfxThrottle);

        SetupAudioSources();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void SetupAudioSources()
    {
        // BGM源
        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.loop = true;
        _bgmSource.playOnAwake = false;

        // SFX池
        _sfxSources = new AudioSource[sfxSourceCount];
        for (int i = 0; i < sfxSourceCount; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            _sfxSources[i] = src;
        }

        // UI和Ambient单例源
        _uiSource = gameObject.AddComponent<AudioSource>();
        _uiSource.playOnAwake = false;

        _ambientSource = gameObject.AddComponent<AudioSource>();
        _ambientSource.loop = true;
        _ambientSource.playOnAwake = false;

        // 获取音频池组件
        _audioPool = GetComponent<AudioPool>();
    }

    #region BGM控制

    public void PlayBGM(AudioClip clip, bool fadeIn = false, float fadeDuration = 1f, bool loop = true)
    {
        if (clip == null)
        {
            DebugLogger.LogWarning("[AudioManager] PlayBGM called with null clip.");
            return;
        }

        if (fadeIn)
        {
            StartCoroutine(FadeInBGM(clip, fadeDuration, loop));
        }
        else
        {
            _bgmSource.clip = clip;
            _bgmSource.loop = loop;
            _bgmSource.volume = masterVolume * bgmVolume;
            _bgmSource.Play();
        }
    }

    public void StopBGM(bool fadeOut = false, float fadeDuration = 1f)
    {
        if (fadeOut)
        {
            StartCoroutine(FadeOutBGM(fadeDuration));
        }
        else
        {
            _bgmSource.Stop();
        }
    }

    public void SetBGMVolume(float volume, bool immediate = false, float fadeDuration = 0f)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (immediate)
        {
            _bgmSource.volume = masterVolume * bgmVolume;
        }
        else if (fadeDuration > 0f)
        {
            StartCoroutine(FadeBGMVolume(bgmVolume, fadeDuration));
        }
    }

    #endregion

    #region SFX控制

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;

        // 节流逻辑
        int id = clip.GetInstanceID();
        if (sfxThrottle > 0f && _lastSFXTime.TryGetValue(id, out float last))
        {
            if (Time.unscaledTime - last < sfxThrottle) return;
        }
        _lastSFXTime[id] = Time.unscaledTime;

        // 使用音频池播放
        if (_audioPool != null)
        {
            string key = clip.name;
            _audioPool.Play(key, transform.position);
        }
        else
        {
            // 回退到原有SFX池
            AudioSource src = GetSFXSource();
            src.PlayOneShot(clip, sfxVolume * volumeScale);
        }
    }

    public void PlaySFX(AudioClip clip, Vector3 position, float volumeScale = 1f, bool spatialBlend = false)
    {
        if (clip == null) return;

        // 节流逻辑
        int id = clip.GetInstanceID();
        if (sfxThrottle > 0f && _lastSFXTime.TryGetValue(id, out float last))
        {
            if (Time.unscaledTime - last < sfxThrottle) return;
        }
        _lastSFXTime[id] = Time.unscaledTime;

        AudioSource src = GetSFXSource();
        src.transform.position = position;
        src.spatialBlend = spatialBlend ? 1.0f : 0.0f;
        src.PlayOneShot(clip, sfxVolume * volumeScale);
    }

    private AudioSource GetSFXSource()
    {
        int startIndex = _sfxIndex;
        do
        {
            _sfxIndex = (_sfxIndex + 1) % sfxSourceCount;
        } while (_sfxSources[_sfxIndex].isPlaying && _sfxIndex != startIndex);

        return _sfxSources[_sfxIndex];
    }

    #endregion

    #region UI音效控制

    public void PlayUI(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;

        _uiSource.PlayOneShot(clip, uiVolume * volumeScale);
    }

    public void SetUIVolume(float volume)
    {
        uiVolume = Mathf.Clamp01(volume);
    }

    #endregion

    #region 环境音效控制

    public void PlayAmbient(AudioClip clip, bool fadeIn = false, float fadeDuration = 1f)
    {
        if (clip == null)
        {
            DebugLogger.LogWarning("[AudioManager] PlayAmbient called with null clip.");
            return;
        }

        if (fadeIn)
        {
            StartCoroutine(FadeInAmbient(clip, fadeDuration));
        }
        else
        {
            _ambientSource.clip = clip;
            _ambientSource.volume = masterVolume * ambientVolume;
            _ambientSource.Play();
        }
    }

    public void StopAmbient(bool fadeOut = false, float fadeDuration = 1f)
    {
        if (fadeOut)
        {
            StartCoroutine(FadeOutAmbient(fadeDuration));
        }
        else
        {
            _ambientSource.Stop();
        }
    }

    public void SetAmbientVolume(float volume, bool immediate = false, float fadeDuration = 0f)
    {
        ambientVolume = Mathf.Clamp01(volume);
        if (immediate)
        {
            _ambientSource.volume = masterVolume * ambientVolume;
        }
        else if (fadeDuration > 0f)
        {
            StartCoroutine(FadeAmbientVolume(ambientVolume, fadeDuration));
        }
    }

    #endregion

    #region 主音量控制

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        _bgmSource.volume = masterVolume * bgmVolume;
        _ambientSource.volume = masterVolume * ambientVolume;
    }

    #endregion

    #region 淡入淡出协程

    private System.Collections.IEnumerator FadeInBGM(AudioClip clip, float duration, bool loop = true)
    {
        _bgmSource.clip = clip;
        _bgmSource.loop = loop;
        _bgmSource.volume = 0f;
        _bgmSource.Play();

        float elapsed = 0f;
        float startVolume = 0f;
        float targetVolume = masterVolume * bgmVolume;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _bgmSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        _bgmSource.volume = targetVolume;
    }

    private System.Collections.IEnumerator FadeOutBGM(float duration)
    {
        float elapsed = 0f;
        float startVolume = _bgmSource.volume;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        _bgmSource.Stop();
        _bgmSource.volume = 0f;
    }

    private System.Collections.IEnumerator FadeBGMVolume(float targetVolume, float duration)
    {
        float elapsed = 0f;
        float startVolume = _bgmSource.volume;
        float endVolume = masterVolume * targetVolume;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _bgmSource.volume = Mathf.Lerp(startVolume, endVolume, elapsed / duration);
            yield return null;
        }

        _bgmSource.volume = endVolume;
    }

    private System.Collections.IEnumerator FadeInAmbient(AudioClip clip, float duration)
    {
        _ambientSource.clip = clip;
        _ambientSource.volume = 0f;
        _ambientSource.Play();

        float elapsed = 0f;
        float startVolume = 0f;
        float targetVolume = masterVolume * ambientVolume;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _ambientSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        _ambientSource.volume = targetVolume;
    }

    private System.Collections.IEnumerator FadeOutAmbient(float duration)
    {
        float elapsed = 0f;
        float startVolume = _ambientSource.volume;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _ambientSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        _ambientSource.Stop();
        _ambientSource.volume = 0f;
    }

    private System.Collections.IEnumerator FadeAmbientVolume(float targetVolume, float duration)
    {
        float elapsed = 0f;
        float startVolume = _ambientSource.volume;
        float endVolume = masterVolume * targetVolume;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _ambientSource.volume = Mathf.Lerp(startVolume, endVolume, elapsed / duration);
            yield return null;
        }

        _ambientSource.volume = endVolume;
    }

    #endregion
}
