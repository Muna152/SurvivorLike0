using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to populate UpgradeManager with all available weapons.
/// </summary>
public class SetupUpgradeManagerEditor
{
    [MenuItem("Tools/Setup Upgrade Manager Weapons")]
    public static void SetupUpgradeManagerWeapons()
    {
        // Find UpgradeManager in the scene
        UpgradeManager upgradeManager = GameObject.FindObjectOfType<UpgradeManager>();
        if (upgradeManager == null)
        {
            Debug.LogError("UpgradeManager not found in the scene!");
            return;
        }

        // Load all weapon data
        System.Collections.Generic.List<WeaponData> weaponList = new System.Collections.Generic.List<WeaponData>();
        string[] guids = AssetDatabase.FindAssets("t:WeaponData", new[] { "Assets/Data/Weapons" });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var weaponData = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            if (weaponData != null)
            {
                weaponList.Add(weaponData);
            }
        }

        // Set the weapons array using serialized object
        SerializedObject so = new SerializedObject(upgradeManager);
        SerializedProperty weaponsProp = so.FindProperty("_availableWeapons");
        weaponsProp.ClearArray();

        foreach (var weapon in weaponList)
        {
            weaponsProp.arraySize++;
            SerializedProperty element = weaponsProp.GetArrayElementAtIndex(weaponsProp.arraySize - 1);
            element.objectReferenceValue = weapon;
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(upgradeManager);

        Debug.Log($"Setup UpgradeManager with {weaponList.Count} weapons.");
    }
}
