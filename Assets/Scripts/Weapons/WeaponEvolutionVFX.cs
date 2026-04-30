using System.Collections;
using UnityEngine;

/// <summary>
/// Plays a visual effect when a weapon evolves.
/// Listens to GameEvents.OnWeaponEvolved and spawns a flash + scale pulse.
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
        // Create flash object
        var flashObj = new GameObject("EvolutionFlash");
        flashObj.transform.position = position;

        var sr = flashObj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite();
        sr.color = _flashColor;
        sr.sortingOrder = 100;
        sr.drawMode = SpriteDrawMode.Sliced;

        float elapsed = 0f;
        float maxSize = 6f;
        float minSize = 0.5f;

        while (elapsed < _flashDuration)
        {
            float t = elapsed / _flashDuration;

            // Scale pulse: expand then fade
            float size = Mathf.Lerp(minSize, maxSize, t);
            sr.size = new Vector2(size, size);

            // Fade out
            Color c = _flashColor;
            c.a = Mathf.Lerp(0.8f, 0f, t);
            sr.color = c;

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(flashObj);
    }

    private static Sprite CreateCircleSprite()
    {
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
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
