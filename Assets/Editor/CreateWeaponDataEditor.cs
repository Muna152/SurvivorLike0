using UnityEngine;
using UnityEditor;
using System.IO;

public class CreateWeaponDataEditor
{
    [MenuItem("Tools/Create Phase 2 Weapon Data")]
    public static void CreateAllWeaponData()
    {
        CreateSpinningShieldData();
        CreateKnifeData();
        CreateEnergyBallData();
        CreateHolyLightData();
        CreateHolyWaterData();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Phase 2 weapon data created successfully!");
    }

    private static void CreateSpinningShieldData()
    {
        string path = "Assets/Data/Weapons/SpinningShield.asset";
        EnsureDirectoryExists(path);

        WeaponData data = ScriptableObject.CreateInstance<WeaponData>();
        data.weaponName = "Spinning Shield";
        data.description = "Orbiting shields that damage enemies on contact.";
        data.weaponType = WeaponData.WeaponType.Orbital;
        data.maxLevel = 8;
        data.canEvolve = true;
        data.requiredPassiveId = "shell";
        data.levelData = new LevelData[8];

        // Level 1-8 data for Spinning Shield
        for (int i = 0; i < 8; i++)
        {
            int level = i + 1;
            data.levelData[i] = new LevelData
            {
                damage = 5 + (i * 2),           // 5, 7, 9, 11, 13, 15, 17, 19
                cooldown = 1.0f - (i * 0.05f),  // 1.0, 0.95, 0.9, 0.85, 0.8, 0.75, 0.7, 0.65
                area = 2.5f + (i * 0.2f),       // 2.5, 2.7, 2.9, 3.1, 3.3, 3.5, 3.7, 3.9 (radius)
                projectileCount = 1 + (i / 2),  // 1, 1, 2, 2, 3, 3, 4, 4 (orbital count)
                pierce = 1,                      // Always 1 hit per cooldown
                speed = 120f + (i * 10f),       // 120, 130, 140, 150, 160, 170, 180, 190 (degrees/sec)
                duration = 0                     // Not used for orbital weapons
            };
        }

        AssetDatabase.CreateAsset(data, path);
        EditorUtility.SetDirty(data);
    }

    private static void CreateKnifeData()
    {
        string path = "Assets/Data/Weapons/Knife.asset";
        EnsureDirectoryExists(path);

        WeaponData data = ScriptableObject.CreateInstance<WeaponData>();
        data.weaponName = "Knife";
        data.description = "Throws multiple knives in rapid succession.";
        data.weaponType = WeaponData.WeaponType.Projectile;
        data.maxLevel = 8;
        data.canEvolve = true;
        data.requiredPassiveId = "feather";
        data.levelData = new LevelData[8];

        for (int i = 0; i < 8; i++)
        {
            int level = i + 1;
            data.levelData[i] = new LevelData
            {
                damage = 8 + (i * 3),           // 8, 11, 14, 17, 20, 23, 26, 29
                cooldown = 0.8f - (i * 0.05f),  // 0.8, 0.75, 0.7, 0.65, 0.6, 0.55, 0.5, 0.45
                area = 0.5f,                     // Fixed small area
                projectileCount = 1 + i,         // 1, 2, 3, 4, 5, 6, 7, 8
                pierce = 1,                      // No pierce
                speed = 10f + (i * 0.5f),       // 10, 10.5, 11, 11.5, 12, 12.5, 13, 13.5
                duration = 0                     // Not used
            };
        }

        AssetDatabase.CreateAsset(data, path);
        EditorUtility.SetDirty(data);
    }

    private static void CreateEnergyBallData()
    {
        string path = "Assets/Data/Weapons/EnergyBall.asset";
        EnsureDirectoryExists(path);

        WeaponData data = ScriptableObject.CreateInstance<WeaponData>();
        data.weaponName = "Energy Ball";
        data.description = "Fires explosive energy balls that deal area damage on impact.";
        data.weaponType = WeaponData.WeaponType.Projectile;
        data.maxLevel = 8;
        data.canEvolve = true;
        data.requiredPassiveId = "codex";
        data.levelData = new LevelData[8];

        for (int i = 0; i < 8; i++)
        {
            int level = i + 1;
            data.levelData[i] = new LevelData
            {
                damage = 15 + (i * 5),          // 15, 20, 25, 30, 35, 40, 45, 50
                cooldown = 1.5f - (i * 0.05f),  // 1.5, 1.45, 1.4, 1.35, 1.3, 1.25, 1.2, 1.15
                area = 2.0f + (i * 0.3f),       // 2.0, 2.3, 2.6, 2.9, 3.2, 3.5, 3.8, 4.1 (explosion radius)
                projectileCount = 1,             // Single projectile
                pierce = 0,                      // Explodes on impact
                speed = 8f,                      // Fixed speed
                duration = 0                     // Not used
            };
        }

        AssetDatabase.CreateAsset(data, path);
        EditorUtility.SetDirty(data);
    }

    private static void CreateHolyLightData()
    {
        string path = "Assets/Data/Weapons/HolyLight.asset";
        EnsureDirectoryExists(path);

        WeaponData data = ScriptableObject.CreateInstance<WeaponData>();
        data.weaponName = "Holy Light";
        data.description = "Creates a healing aura that restores player HP.";
        data.weaponType = WeaponData.WeaponType.Area;
        data.maxLevel = 8;
        data.canEvolve = true;
        data.requiredPassiveId = "heart";
        data.levelData = new LevelData[8];

        for (int i = 0; i < 8; i++)
        {
            int level = i + 1;
            data.levelData[i] = new LevelData
            {
                damage = 5 + (i * 2),           // Healing amount per tick: 5, 7, 9, 11, 13, 15, 17, 19
                cooldown = 0.5f,                 // Tick interval: fixed at 0.5s
                area = 3.0f + (i * 0.5f),       // 3.0, 3.5, 4.0, 4.5, 5.0, 5.5, 6.0, 6.5 (radius)
                projectileCount = 0,             // Not used
                pierce = 0,                      // Not used
                speed = 0,                       // Not used
                duration = 5f + (i * 1f)         // 5, 6, 7, 8, 9, 10, 11, 12 seconds
            };
        }

        AssetDatabase.CreateAsset(data, path);
        EditorUtility.SetDirty(data);
    }

    private static void CreateHolyWaterData()
    {
        string path = "Assets/Data/Weapons/HolyWater.asset";
        EnsureDirectoryExists(path);

        WeaponData data = ScriptableObject.CreateInstance<WeaponData>();
        data.weaponName = "Holy Water";
        data.description = "Creates a damaging pool on the ground.";
        data.weaponType = WeaponData.WeaponType.Area;
        data.maxLevel = 8;
        data.canEvolve = true;
        data.requiredPassiveId = "bone";
        data.levelData = new LevelData[8];

        for (int i = 0; i < 8; i++)
        {
            int level = i + 1;
            data.levelData[i] = new LevelData
            {
                damage = 3 + (i * 1),           // Damage per tick: 3, 4, 5, 6, 7, 8, 9, 10
                cooldown = 0.5f,                 // Tick interval: fixed at 0.5s
                area = 2.5f + (i * 0.4f),       // 2.5, 2.9, 3.3, 3.7, 4.1, 4.5, 4.9, 5.3 (radius)
                projectileCount = 0,             // Not used
                pierce = 0,                      // Not used
                speed = 0,                       // Not used
                duration = 4f + (i * 1f)         // 4, 5, 6, 7, 8, 9, 10, 11 seconds
            };
        }

        AssetDatabase.CreateAsset(data, path);
        EditorUtility.SetDirty(data);
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
