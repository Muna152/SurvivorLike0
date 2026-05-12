using UnityEngine;

/// <summary>
/// Passive database — holds all PassiveData references, accessible via Resources.Load singleton.
/// Path: Resources/Data/PassiveDatabase
/// </summary>
[CreateAssetMenu(fileName = "PassiveDatabase", menuName = "Data/PassiveDatabase")]
public class PassiveDatabase : ScriptableObject
{
    private static PassiveDatabase _instance;
    public static PassiveDatabase Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<PassiveDatabase>("Data/PassiveDatabase");
            return _instance;
        }
    }

    public PassiveData[] passives;

    /// <summary>Find passive by id. Returns null if not found.</summary>
    public PassiveData GetById(string id)
    {
        if (passives == null || string.IsNullOrEmpty(id)) return null;
        for (int i = 0; i < passives.Length; i++)
        {
            if (passives[i] != null && passives[i].id == id)
                return passives[i];
        }
        return null;
    }
}
