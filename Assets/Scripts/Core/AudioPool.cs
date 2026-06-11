using UnityEngine;
using System.Collections.Generic;

public class AudioPool : MonoBehaviour
{
    [System.Serializable]
    public class AudioPoolEntry
    {
        public string key;
        public AudioClip clip;
        public int initialSize = 5;
        public int maxSize = 20;
        public float volume = 1f;
        public bool loop = false;
    }

    [SerializeField] private List<AudioPoolEntry> poolEntries = new List<AudioPoolEntry>();
    private Dictionary<string, Queue<AudioSource>> _audioPools = new Dictionary<string, Queue<AudioSource>>();
    private Dictionary<string, AudioPoolEntry> _entryDict = new Dictionary<string, AudioPoolEntry>();
    // 维护运行时激活对象列表，用于正确计数
    private Dictionary<string, List<AudioSource>> _activeSources = new Dictionary<string, List<AudioSource>>();
    private Transform _poolRoot;

    private void Awake()
    {
        _poolRoot = transform;
        InitializePools();
    }

    private void InitializePools()
    {
        foreach (var entry in poolEntries)
        {
            if (string.IsNullOrEmpty(entry.key) || entry.clip == null) continue;

            _entryDict[entry.key] = entry;
            _audioPools[entry.key] = new Queue<AudioSource>();
            _activeSources[entry.key] = new List<AudioSource>();

            for (int i = 0; i < entry.initialSize; i++)
            {
                CreateAudioSource(entry.key);
            }
        }
    }

    private AudioSource CreateAudioSource(string key)
    {
        var entry = _entryDict[key];
        var audioObj = new GameObject($"Audio_{key}");
        audioObj.transform.SetParent(_poolRoot);

        var source = audioObj.AddComponent<AudioSource>();
        source.clip = entry.clip;
        source.volume = entry.volume;
        source.loop = entry.loop;
        source.playOnAwake = false;
        audioObj.SetActive(false);

        // 将创建的AudioSource添加到池中
        _audioPools[key].Enqueue(source);
        return source;
    }

    public AudioSource Play(string key, Vector3 position, float volumeScale = 1f)
    {
        if (!_audioPools.ContainsKey(key))
        {
            Debug.LogWarning($"[AudioPool] Key '{key}' not found in pool.");
            return null;
        }

        AudioSource source = GetAudioSource(key);
        if (source == null) return null;

        var entry = _entryDict[key];
        source.transform.position = position;
        source.volume = entry.volume * volumeScale;
        source.gameObject.SetActive(true);
        source.Play();

        // 将激活的AudioSource添加到激活列表
        if (!_activeSources.ContainsKey(key))
            _activeSources[key] = new List<AudioSource>();
        _activeSources[key].Add(source);

        if (!source.loop)
        {
            StartCoroutine(ReturnToPoolAfterPlay(source, key));
        }

        return source;
    }

    private AudioSource GetAudioSource(string key)
    {
        var pool = _audioPools[key];
        var entry = _entryDict[key];

        if (pool.Count > 0)
        {
            return pool.Dequeue();
        }

        // 检查是否超过最大大小
        int activeCount = CountActiveSources(key);
        if (activeCount < entry.maxSize)
        {
            return CreateAudioSource(key);
        }

        Debug.LogWarning($"[AudioPool] Pool '{key}' at max capacity ({entry.maxSize}). Skipping playback.");
        return null;
    }

    private int CountActiveSources(string key)
    {
        if (!_activeSources.ContainsKey(key)) return 0;
        return _activeSources[key].Count;
    }

    private System.Collections.IEnumerator ReturnToPoolAfterPlay(AudioSource source, string key)
    {
        yield return new WaitWhile(() => source.isPlaying);
        source.gameObject.SetActive(false);

        // 从激活列表中移除
        if (_activeSources.ContainsKey(key))
            _activeSources[key].Remove(source);

        _audioPools[key].Enqueue(source);
    }

    public void StopAll(string key)
    {
        if (!_audioPools.ContainsKey(key)) return;

        // 停止所有激活的AudioSource
        if (_activeSources.ContainsKey(key))
        {
            foreach (var source in new List<AudioSource>(_activeSources[key]))
            {
                source.Stop();
                source.gameObject.SetActive(false);
                _audioPools[key].Enqueue(source);
            }
            _activeSources[key].Clear();
        }

        // 停止池中剩余的激活对象
        foreach (var source in _audioPools[key])
        {
            if (source.gameObject.activeSelf)
            {
                source.Stop();
                source.gameObject.SetActive(false);
            }
        }
    }
}
