using UnityEngine;

/// <summary>
/// Safe destruction helpers for dynamically-created Unity objects (Sprite, Texture2D, Mesh).
/// Sprite.Create does NOT take ownership of the source Texture2D — both must be destroyed explicitly.
/// </summary>
public static class DestroyHelper
{
    /// <summary>Destroy a dynamically-created Sprite and its underlying texture.</summary>
    public static void Destroy(Sprite sprite)
    {
        if (sprite == null) return;
        if (sprite.texture != null)
            Destroy(sprite.texture);
        if (Application.isPlaying)
            Object.Destroy(sprite);
        else
            Object.DestroyImmediate(sprite);
    }

    /// <summary>Destroy a dynamically-created Texture2D to free GPU/native memory.</summary>
    public static void Destroy(Texture2D tex)
    {
        if (tex == null) return;
        if (Application.isPlaying)
            Object.Destroy(tex);
        else
            Object.DestroyImmediate(tex);
    }

    /// <summary>Destroy a dynamically-created Mesh to free native memory.</summary>
    public static void Destroy(Mesh mesh)
    {
        if (mesh == null) return;
        if (Application.isPlaying)
            Object.Destroy(mesh);
        else
            Object.DestroyImmediate(mesh);
    }
}
