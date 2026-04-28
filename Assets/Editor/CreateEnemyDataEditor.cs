using UnityEngine;
using UnityEditor;
using System.IO;

public class CreateEnemyDataEditor
{
    [MenuItem("Tools/Create Phase 2 Enemy Data")]
    public static void CreateAllEnemyData()
    {
        CreateBatData();
        CreateZombieData();
        CreateMageData();
        CreateGhostData();
        CreateGargoyleData();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Phase 2 enemy data created successfully!");
    }

    private static void CreateBatData()
    {
        string path = "Assets/Data/Enemies/Bat.asset";
        EnsureDirectoryExists(path);

        EnemyData data = ScriptableObject.CreateInstance<EnemyData>();
        data.enemyName = "Bat";
        data.baseHP = 5f;
        data.moveSpeed = 3.0f;
        data.damage = 3f;
        data.expValue = 2;
        data.goldValue = 1;
        data.spawnWeight = 2.0f;
        data.minSpawnTime = 0f;

        AssetDatabase.CreateAsset(data, path);
        EditorUtility.SetDirty(data);
    }

    private static void CreateZombieData()
    {
        string path = "Assets/Data/Enemies/Zombie.asset";
        EnsureDirectoryExists(path);

        EnemyData data = ScriptableObject.CreateInstance<EnemyData>();
        data.enemyName = "Zombie";
        data.baseHP = 25f;
        data.moveSpeed = 1.0f;
        data.damage = 8f;
        data.expValue = 5;
        data.goldValue = 2;
        data.spawnWeight = 1.5f;
        data.minSpawnTime = 30f;  // Appears after 30 seconds

        AssetDatabase.CreateAsset(data, path);
        EditorUtility.SetDirty(data);
    }

    private static void CreateMageData()
    {
        string path = "Assets/Data/Enemies/Mage.asset";
        EnsureDirectoryExists(path);

        EnemyData data = ScriptableObject.CreateInstance<EnemyData>();
        data.enemyName = "Mage";
        data.baseHP = 15f;
        data.moveSpeed = 1.2f;
        data.damage = 12f;
        data.expValue = 8;
        data.goldValue = 3;
        data.spawnWeight = 1.0f;
        data.minSpawnTime = 60f;  // Appears after 1 minute

        AssetDatabase.CreateAsset(data, path);
        EditorUtility.SetDirty(data);
    }

    private static void CreateGhostData()
    {
        string path = "Assets/Data/Enemies/Ghost.asset";
        EnsureDirectoryExists(path);

        EnemyData data = ScriptableObject.CreateInstance<EnemyData>();
        data.enemyName = "Ghost";
        data.baseHP = 8f;
        data.moveSpeed = 2.5f;
        data.damage = 6f;
        data.expValue = 4;
        data.goldValue = 2;
        data.spawnWeight = 1.2f;
        data.minSpawnTime = 90f;  // Appears after 1.5 minutes

        AssetDatabase.CreateAsset(data, path);
        EditorUtility.SetDirty(data);
    }

    private static void CreateGargoyleData()
    {
        string path = "Assets/Data/Enemies/Gargoyle.asset";
        EnsureDirectoryExists(path);

        EnemyData data = ScriptableObject.CreateInstance<EnemyData>();
        data.enemyName = "Gargoyle";
        data.baseHP = 50f;
        data.moveSpeed = 1.8f;
        data.damage = 15f;
        data.expValue = 15;
        data.goldValue = 5;
        data.spawnWeight = 0.5f;
        data.minSpawnTime = 120f;  // Appears after 2 minutes

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
