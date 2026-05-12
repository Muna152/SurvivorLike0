using UnityEngine;

/// <summary>
/// Enemy database — holds all EnemyData references (enemies + bosses), accessible via Resources.Load singleton.
/// Path: Resources/Data/EnemyDatabase
/// </summary>
[CreateAssetMenu(fileName = "EnemyDatabase", menuName = "Data/EnemyDatabase")]
public class EnemyDatabase : ScriptableObject
{
    private static EnemyDatabase _instance;
    public static EnemyDatabase Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<EnemyDatabase>("Data/EnemyDatabase");
            return _instance;
        }
    }

    public EnemyData[] enemies;
    public EnemyData[] bosses;

    /// <summary>Find enemy or boss by id. Searches enemies first, then bosses. Returns null if not found.</summary>
    public EnemyData GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        if (enemies != null)
        {
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] != null && enemies[i].id == id)
                    return enemies[i];
            }
        }

        if (bosses != null)
        {
            for (int i = 0; i < bosses.Length; i++)
            {
                if (bosses[i] != null && bosses[i].id == id)
                    return bosses[i];
            }
        }

        return null;
    }

    /// <summary>Find boss by id. Only searches the bosses array. Returns null if not found.</summary>
    public EnemyData GetBossById(string id)
    {
        if (bosses == null || string.IsNullOrEmpty(id)) return null;
        for (int i = 0; i < bosses.Length; i++)
        {
            if (bosses[i] != null && bosses[i].id == id)
                return bosses[i];
        }
        return null;
    }
}
