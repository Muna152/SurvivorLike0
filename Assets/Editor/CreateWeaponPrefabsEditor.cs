using UnityEngine;
using UnityEditor;
using System.IO;

public class CreateWeaponPrefabsEditor
{
    private static string _weaponDataPath = "Assets/Data/Weapons/";
    private static string _weaponsPrefabPath = "Assets/Prefabs/Weapons/";
    private static string _projectilesPrefabPath = "Assets/Prefabs/Projectiles/";

    [MenuItem("Tools/Create Phase 2 Weapon Prefabs")]
    public static void CreateAllWeaponPrefabs()
    {
        EnsureDirectoryExists(_weaponsPrefabPath);
        EnsureDirectoryExists(_projectilesPrefabPath);

        CreateSpinningShieldPrefab();
        CreateKnifeProjectilePrefab();
        CreateEnergyBallProjectilePrefab();
        CreateHolyLightZonePrefab();
        CreateHolyWaterZonePrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Phase 2 weapon prefabs created successfully!");
    }

    private static void CreateSpinningShieldPrefab()
    {
        // Create orbital object prefab
        string orbitalPrefabPath = _weaponsPrefabPath + "SpinningShieldOrbital.prefab";
        GameObject orbitalObj = new GameObject("SpinningShieldOrbital");
        orbitalObj.AddComponent<OrbitalObject>();

        // Add a sprite renderer with placeholder
        SpriteRenderer sr = orbitalObj.GetComponent<SpriteRenderer>();
        if (sr == null) sr = orbitalObj.AddComponent<SpriteRenderer>();
        sr.sprite = CreatePlaceholderSprite("SpinningShieldOrbital", Color.blue);

        // Add collider
        CircleCollider2D collider = orbitalObj.GetComponent<CircleCollider2D>();
        if (collider == null) collider = orbitalObj.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.5f;

        // Add rigidbody
        Rigidbody2D rb = orbitalObj.GetComponent<Rigidbody2D>();
        if (rb == null) rb = orbitalObj.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        PrefabUtility.SaveAsPrefabAsset(orbitalObj, orbitalPrefabPath);
        Object.DestroyImmediate(orbitalObj);

        // Load weapon data and set prefab reference
        string dataPath = _weaponDataPath + "SpinningShield.asset";
        WeaponData data = AssetDatabase.LoadAssetAtPath<WeaponData>(dataPath);
        if (data != null)
        {
            GameObject orbitalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(orbitalPrefabPath);
            data.projectilePrefab = orbitalPrefab;
            EditorUtility.SetDirty(data);
        }

        // Create weapon container prefab (for PlayerWeaponManager)
        string weaponPrefabPath = _weaponsPrefabPath + "SpinningShield.prefab";
        GameObject weaponObj = new GameObject("SpinningShield");
        weaponObj.AddComponent<OrbitalWeapon>();

        PrefabUtility.SaveAsPrefabAsset(weaponObj, weaponPrefabPath);
        Object.DestroyImmediate(weaponObj);
    }

    private static void CreateKnifeProjectilePrefab()
    {
        // Load weapon data
        string dataPath = _weaponDataPath + "Knife.asset";
        WeaponData data = AssetDatabase.LoadAssetAtPath<WeaponData>(dataPath);

        // Create projectile prefab
        string projectilePath = _projectilesPrefabPath + "KnifeProj.prefab";
        GameObject projObj = new GameObject("KnifeProj");
        Projectile proj = projObj.AddComponent<Projectile>();

        // Add sprite renderer
        SpriteRenderer sr = projObj.GetComponent<SpriteRenderer>();
        if (sr == null) sr = projObj.AddComponent<SpriteRenderer>();
        sr.sprite = CreatePlaceholderSprite("Knife", Color.green);

        // Add collider
        CircleCollider2D collider = projObj.GetComponent<CircleCollider2D>();
        if (collider == null) collider = projObj.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.3f;

        // Add rigidbody
        Rigidbody2D rb = projObj.GetComponent<Rigidbody2D>();
        if (rb == null) rb = projObj.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        PrefabUtility.SaveAsPrefabAsset(projObj, projectilePath);
        Object.DestroyImmediate(projObj);

        // Set prefab reference in weapon data
        if (data != null)
        {
            GameObject projPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(projectilePath);
            data.projectilePrefab = projPrefab;
            EditorUtility.SetDirty(data);
        }

        // Create weapon container prefab
        string weaponPrefabPath = _weaponsPrefabPath + "Knife.prefab";
        GameObject weaponObj = new GameObject("Knife");
        weaponObj.AddComponent<ProjectileWeapon>();

        PrefabUtility.SaveAsPrefabAsset(weaponObj, weaponPrefabPath);
        Object.DestroyImmediate(weaponObj);
    }

    private static void CreateEnergyBallProjectilePrefab()
    {
        // Load weapon data
        string dataPath = _weaponDataPath + "EnergyBall.asset";
        WeaponData data = AssetDatabase.LoadAssetAtPath<WeaponData>(dataPath);

        // Create projectile prefab
        string projectilePath = _projectilesPrefabPath + "EnergyBallProj.prefab";
        GameObject projObj = new GameObject("EnergyBallProj");
        Projectile proj = projObj.AddComponent<Projectile>();

        // Add sprite renderer
        SpriteRenderer sr = projObj.GetComponent<SpriteRenderer>();
        if (sr == null) sr = projObj.AddComponent<SpriteRenderer>();
        sr.sprite = CreatePlaceholderSprite("EnergyBall", Color.magenta);

        // Add collider
        CircleCollider2D collider = projObj.GetComponent<CircleCollider2D>();
        if (collider == null) collider = projObj.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.4f;

        // Add rigidbody
        Rigidbody2D rb = projObj.GetComponent<Rigidbody2D>();
        if (rb == null) rb = projObj.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        PrefabUtility.SaveAsPrefabAsset(projObj, projectilePath);
        Object.DestroyImmediate(projObj);

        // Set prefab reference in weapon data
        if (data != null)
        {
            GameObject projPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(projectilePath);
            data.projectilePrefab = projPrefab;
            EditorUtility.SetDirty(data);
        }

        // Create weapon container prefab
        string weaponPrefabPath = _weaponsPrefabPath + "EnergyBall.prefab";
        GameObject weaponObj = new GameObject("EnergyBall");
        weaponObj.AddComponent<ProjectileWeapon>();

        PrefabUtility.SaveAsPrefabAsset(weaponObj, weaponPrefabPath);
        Object.DestroyImmediate(weaponObj);
    }

    private static void CreateHolyLightZonePrefab()
    {
        // Load weapon data
        string dataPath = _weaponDataPath + "HolyLight.asset";
        WeaponData data = AssetDatabase.LoadAssetAtPath<WeaponData>(dataPath);

        // Create zone prefab
        string zonePath = _weaponsPrefabPath + "HolyLightZone.prefab";
        GameObject zoneObj = new GameObject("HolyLightZone");

        // Add sprite renderer (large circle)
        SpriteRenderer sr = zoneObj.GetComponent<SpriteRenderer>();
        if (sr == null) sr = zoneObj.AddComponent<SpriteRenderer>();
        sr.sprite = CreatePlaceholderSprite("HolyLightZone", Color.yellow);
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(6f, 6f);

        PrefabUtility.SaveAsPrefabAsset(zoneObj, zonePath);
        Object.DestroyImmediate(zoneObj);

        // Set prefab reference in weapon data
        if (data != null)
        {
            GameObject zonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(zonePath);
            data.projectilePrefab = zonePrefab;
            EditorUtility.SetDirty(data);
        }

        // Create weapon container prefab
        string weaponPrefabPath = _weaponsPrefabPath + "HolyLight.prefab";
        GameObject weaponObj = new GameObject("HolyLight");
        weaponObj.AddComponent<HolyLight>();

        PrefabUtility.SaveAsPrefabAsset(weaponObj, weaponPrefabPath);
        Object.DestroyImmediate(weaponObj);
    }

    private static void CreateHolyWaterZonePrefab()
    {
        // Load weapon data
        string dataPath = _weaponDataPath + "HolyWater.asset";
        WeaponData data = AssetDatabase.LoadAssetAtPath<WeaponData>(dataPath);

        // Create zone prefab
        string zonePath = _weaponsPrefabPath + "HolyWaterZone.prefab";
        GameObject zoneObj = new GameObject("HolyWaterZone");

        // Add sprite renderer (large circle)
        SpriteRenderer sr = zoneObj.GetComponent<SpriteRenderer>();
        if (sr == null) sr = zoneObj.AddComponent<SpriteRenderer>();
        sr.sprite = CreatePlaceholderSprite("HolyWaterZone", Color.cyan);
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(5f, 5f);

        PrefabUtility.SaveAsPrefabAsset(zoneObj, zonePath);
        Object.DestroyImmediate(zoneObj);

        // Set prefab reference in weapon data
        if (data != null)
        {
            GameObject zonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(zonePath);
            data.projectilePrefab = zonePrefab;
            EditorUtility.SetDirty(data);
        }

        // Create weapon container prefab
        string weaponPrefabPath = _weaponsPrefabPath + "HolyWater.prefab";
        GameObject weaponObj = new GameObject("HolyWater");
        weaponObj.AddComponent<HolyWater>();

        PrefabUtility.SaveAsPrefabAsset(weaponObj, weaponPrefabPath);
        Object.DestroyImmediate(weaponObj);
    }

    private static Sprite CreatePlaceholderSprite(string name, Color color)
    {
        // Create a simple colored texture
        int size = 64;
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];

        // Create a circle
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - size / 2f;
                float dy = y - size / 2f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist < size / 2f)
                {
                    pixels[y * size + x] = color;
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        // Save texture as asset
        string texPath = "Assets/Art/Temp/" + name + ".png";
        EnsureDirectoryExists(texPath);
        File.WriteAllBytes(texPath, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(texPath);
        Texture2D importedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        return Sprite.Create(importedTex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private static void EnsureDirectoryExists(string assetPath)
    {
        string directory = Path.GetDirectoryName(assetPath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
