using UnityEngine;

/// <summary>
/// Enemy data definition (ScriptableObject).
/// Defines an enemy type's base stats and spawn parameters.
/// </summary>
[CreateAssetMenu(fileName = "EnemyData", menuName = "Data/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("Basic Info")]
    public string enemyName;
    public GameObject prefab;
    public Sprite icon;

    [Header("Stats")]
    public float baseHP = 10f;
    public float moveSpeed = 2f;
    public float damage = 1f;

    [Header("Rewards")]
    public int expValue = 1;
    public int goldValue = 1;

    [Header("Spawn Settings")]
    public float spawnWeight = 1f;
    public float minSpawnTime;  // Earliest time in seconds this enemy can appear
}