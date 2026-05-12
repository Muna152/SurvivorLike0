using UnityEngine;

/// <summary>
/// Character database — holds all CharacterData references, accessible via Resources.Load singleton.
/// Path: Resources/Data/CharacterDatabase
/// </summary>
[CreateAssetMenu(fileName = "CharacterDatabase", menuName = "Data/CharacterDatabase")]
public class CharacterDatabase : ScriptableObject
{
    private static CharacterDatabase _instance;
    public static CharacterDatabase Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<CharacterDatabase>("Data/CharacterDatabase");
            return _instance;
        }
    }

    public CharacterData[] characters;

    /// <summary>Find character by id. Returns null if not found.</summary>
    public CharacterData GetById(string id)
    {
        if (characters == null || string.IsNullOrEmpty(id)) return null;
        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] != null && characters[i].id == id)
                return characters[i];
        }
        return null;
    }
}
