using UnityEngine;

/// <summary>
/// Data-driven drop table: all gameplay-tunable values for drop rates,
/// item values, and EXP gem tier thresholds. Single SO referenced by DropManager.
/// Designers can tweak without recompile.
/// </summary>
[CreateAssetMenu(fileName = "DropTableData", menuName = "Data/DropTableData")]
public class DropTableData : ScriptableObject
{
    [Header("Drop Rates")]
    [Range(0f, 1f)] public float expGemChance = 0.8f;
    [Range(0f, 1f)] public float healthChance = 0.05f;
    [Range(0f, 1f)] public float chestChance = 0.02f;
    [Range(0f, 1f)] public float magnetChance = 0.03f;

    [Header("EXP Gem Tiers")]
    public int expGemSmallValue = 1;
    public int expGemMediumValue = 5;
    public int expGemLargeValue = 20;
    [Tooltip("Game minutes to start dropping medium gems")] public float expGemMediumThreshold = 8f;
    [Tooltip("Game minutes to start dropping large gems")] public float expGemLargeThreshold = 18f;

    [Header("Health Drop")]
    public int healthValue = 30;

    [Header("Magnet Drop")]
    public float magnetDuration = 10f;
    public float magnetPickupBoost = 5f;

    [Header("Performance")]
    public int maxSpawnsPerFrame = 6;
    public float mergeRadius = 2.5f;
}
