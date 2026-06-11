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

        return source;
    }

    public AudioSource Play(string key, Vector3 position)
    {
        if (!_audioPools.ContainsKey(key))
        {
            DebugLogger.LogWarning($"[AudioPool] Key '{key}' not found in pool.");
            return null;
        }

        AudioSource source = GetAudioSource(key);
        if (source == null) return null;

        source.transform.position = position;
        source.gameObject.SetActive(true);
        source.Play();

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

        DebugLogger.LogWarning($"[AudioPool] Pool '{key}' at max capacity ({entry.maxSize}). Skipping playback.");
        return null;
    }

    private int CountActiveSources(string key)
    {
        int count = 0;
        foreach (var source in _audioPools[key])
        {
            if (source.gameObject.activeSelf) count++;
        }
        return count;
    }

    private System.Collections.IEnumerator ReturnToPoolAfterPlay(AudioSource source, string key)
    {
        yield return new WaitWhile(() => source.isPlaying);
        source.gameObject.SetActive(false);
        _audioPools[key].Enqueue(source);
    }

    public void StopAll(string key)
    {
        if (!_audioPools.ContainsKey(key)) return;

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
