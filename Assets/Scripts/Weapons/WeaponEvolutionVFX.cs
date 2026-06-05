using System.Collections;
using UnityEngine;

/// <summary>
/// Plays a visual effect when a weapon evolves.
/// Listens to GameEvents.OnWeaponEvolved and spawns a flash + ring of particles.
/// </summary>
public class WeaponEvolutionVFX : MonoBehaviour
{
    [SerializeField] private float _flashDuration = 1.5f;
    [SerializeField] private Color _flashColor = new Color(1f, 0.9f, 0.3f, 0.8f);
    [SerializeField] private int _particleCount = 12;

    private void OnEnable()
    {
        GameEvents.OnWeaponEvolved += OnWeaponEvolved;
    }

    private void OnDisable()
    {
        GameEvents.OnWeaponEvolved -= OnWeaponEvolved;
    }

    private void OnWeaponEvolved(WeaponBase evolvedWeapon)
    {
        if (evolvedWeapon == null) return;
        StartCoroutine(PlayEvolutionEffect(evolvedWeapon.transform.position));
    }

    private IEnumerator PlayEvolutionEffect(Vector3 position)
    {
        var circleSprite = GetCachedCircleSprite();
        if (circleSprite == null) yield break;

        // Create flash object
        var flashObj = new GameObject("EvolutionFlash");
        flashObj.transform.position = position;

        var sr = flashObj.AddComponent<SpriteRenderer>();
        sr.sprite = circleSprite;
        sr.color = _flashColor;
        sr.sortingOrder = 100;
        sr.drawMode = SpriteDrawMode.Sliced;

        // Spawn ring of particles that fly outward and fade
        var particles = new SpriteRenderer[_particleCount];
        var particleVelocities = new Vector2[_particleCount];
        for (int i = 0; i < _particleCount; i++)
        {
            var pObj = new GameObject($"EvoParticle_{i}");
            pObj.transform.position = position;
            pObj.transform.SetParent(flashObj.transform, false);

            var pSr = pObj.AddComponent<SpriteRenderer>();
            pSr.sprite = circleSprite;
            pSr.color = _flashColor;
            pSr.sortingOrder = 101;
            pSr.transform.localScale = Vector3.one * 0.3f;

            float angle = (360f / _particleCount) * i * Mathf.Deg2Rad;
            particleVelocities[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 4f;
            particles[i] = pSr;
        }

        float elapsed = 0f;
        float maxSize = 6f;
        float minSize = 0.5f;

        while (elapsed < _flashDuration)
        {
            float t = elapsed / _flashDuration;

            // Scale pulse: expand then fade
            float size = Mathf.Lerp(minSize, maxSize, t);
            sr.size = new Vector2(size, size);

            // Fade out flash
            Color c = _flashColor;
            c.a = Mathf.Lerp(0.8f, 0f, t);
            sr.color = c;

            // Animate particles outward with fade
            for (int i = 0; i < _particleCount; i++)
            {
                if (particles[i] != null)
                {
                    particles[i].transform.position += (Vector3)(particleVelocities[i] * Time.deltaTime);
                    Color pc = _flashColor;
                    pc.a = Mathf.Lerp(1f, 0f, t);
                    particles[i].color = pc;
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(flashObj);
    }

    // Cached circle sprite + texture to avoid leaking Texture2D every evolution
    private static Sprite _cachedCircleSprite;
    private static Texture2D _cachedCircleTex;

    private static Sprite GetCachedCircleSprite()
    {
        if (_cachedCircleSprite != null) return _cachedCircleSprite;

        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        float half = size / 2f;
        Color white = Color.white;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - half + 0.5f;
                float dy = y - half + 0.5f;
                pixels[y * size + x] = (dx * dx + dy * dy < half * half) ? white : Color.clear;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        _cachedCircleTex = tex;
        _cachedCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return _cachedCircleSprite;
    }

    /// <summary>Destroy cached sprite and texture. Call on session end.</summary>
    public static void ClearStaticCache()
    {
        if (_cachedCircleSprite != null) { DestroyHelper.Destroy(_cachedCircleSprite); _cachedCircleSprite = null; }
        if (_cachedCircleTex != null) { DestroyHelper.Destroy(_cachedCircleTex); _cachedCircleTex = null; }
    }
}
