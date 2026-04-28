using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

public static class SceneBuilder
{
    [MenuItem("Tools/Build GameLevel Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        new GameObject("GameManager").AddComponent<GameManager>();
        new GameObject("PoolManager");
        new GameObject("EnemyManager");
        new GameObject("DropManager");

        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab");
        if (playerPrefab != null)
        {
            var player = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
            player.name = "Player";
            player.transform.position = Vector3.zero;
            var wm = player.GetComponent<PlayerWeaponManager>();
            var swordData = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/Data/Weapons/FlyingSword.asset");
            if (wm != null && swordData != null) wm.EquipWeapon(swordData);
        }

        var spawnerObj = new GameObject("EnemySpawner");
        var spawner = spawnerObj.AddComponent<EnemySpawner>();
        var skeletonData = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/Data/Enemies/Skeleton.asset");
        if (skeletonData != null)
        {
            var so = new SerializedObject(spawner);
            var p = so.FindProperty("_enemyPool");
            p.arraySize = 1;
            p.GetArrayElementAtIndex(0).objectReferenceValue = skeletonData;
            so.ApplyModifiedProperties();
        }

        var camObj = new GameObject("Main Camera");
        camObj.AddComponent<Camera>().orthographic = true;
        camObj.GetComponent<Camera>().orthographicSize = 8f;
        camObj.tag = "MainCamera";
        camObj.transform.position = new Vector3(0f, 0f, -10f);
        camObj.AddComponent<CameraFollow>();

        var walls = new GameObject("BoundaryWalls");
        float half = 100f;
        var wt = new GameObject("Wall_Top"); wt.transform.parent = walls.transform;
        wt.AddComponent<EdgeCollider2D>().points = new[] { new Vector2(-half, half), new Vector2(half, half) };
        var wb = new GameObject("Wall_Bottom"); wb.transform.parent = walls.transform;
        wb.AddComponent<EdgeCollider2D>().points = new[] { new Vector2(-half, -half), new Vector2(half, -half) };
        var wl = new GameObject("Wall_Left"); wl.transform.parent = walls.transform;
        wl.AddComponent<EdgeCollider2D>().points = new[] { new Vector2(-half, -half), new Vector2(-half, half) };
        var wr = new GameObject("Wall_Right"); wr.transform.parent = walls.transform;
        wr.AddComponent<EdgeCollider2D>().points = new[] { new Vector2(half, -half), new Vector2(half, half) };

        var groundObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        groundObj.name = "Ground";
        groundObj.transform.position = new Vector3(0f, 0f, 1f);
        groundObj.transform.localScale = new Vector3(200f, 200f, 1f);
        var groundMat = AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Art/Materials/GroundGrass.mat");
        if (groundMat != null) groundObj.GetComponent<Renderer>().material = groundMat;
        else groundObj.GetComponent<Renderer>().material.color = new Color(0.2f, 0.3f, 0.15f);

        var canvasObj = new GameObject("HUD Canvas");
        var cv = canvasObj.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        canvasObj.AddComponent<HUDController>();

        var hpBar = new GameObject("HP Bar");
        hpBar.transform.SetParent(canvasObj.transform, false);
        var hpr = hpBar.AddComponent<RectTransform>();
        hpr.anchorMin = new Vector2(0f, 1f); hpr.anchorMax = new Vector2(0f, 1f);
        hpr.pivot = new Vector2(0f, 1f); hpr.anchoredPosition = new Vector2(20f, -20f); hpr.sizeDelta = new Vector2(200f, 20f);
        var hs = hpBar.AddComponent<Slider>();
        hs.direction = Slider.Direction.LeftToRight; hs.maxValue = 100f; hs.value = 100f;

        var hbg = new GameObject("BG"); hbg.transform.SetParent(hpBar.transform, false);
        hbg.AddComponent<UnityEngine.UI.Image>().color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        var hfl = new GameObject("Fill"); hfl.transform.SetParent(hpBar.transform, false);
        var hfi = hfl.AddComponent<UnityEngine.UI.Image>(); hfi.color = Color.red;
        hs.fillRect = hfl.GetComponent<RectTransform>(); hs.targetGraphic = hfi;

        var hto = new GameObject("HPText"); hto.transform.SetParent(hpBar.transform, false);
        var htt = hto.AddComponent<Text>(); htt.font = Font.CreateDynamicFontFromOSFont("Arial", 14);
        htt.fontSize = 14; htt.color = Color.white; htt.alignment = TextAnchor.MiddleCenter; htt.text = "HP: 100/100";

        var tmo = new GameObject("Timer"); tmo.transform.SetParent(canvasObj.transform, false);
        var tmr = tmo.AddComponent<RectTransform>();
        tmr.anchorMin = new Vector2(1f, 1f); tmr.anchorMax = new Vector2(1f, 1f);
        tmr.pivot = new Vector2(1f, 1f); tmr.anchoredPosition = new Vector2(-20f, -20f); tmr.sizeDelta = new Vector2(150f, 40f);
        var tmt = tmo.AddComponent<Text>(); tmt.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
        tmt.fontSize = 24; tmt.color = Color.white; tmt.alignment = TextAnchor.MiddleRight; tmt.text = "00:00";

        var hudSO = new SerializedObject(canvasObj.GetComponent<HUDController>());
        hudSO.FindProperty("_hpSlider").objectReferenceValue = hs;
        hudSO.FindProperty("_hpText").objectReferenceValue = htt;
        hudSO.FindProperty("_timerText").objectReferenceValue = tmt;
        hudSO.ApplyModifiedProperties();

        var ugo = new GameObject("UpgradeManager");
        var umg = ugo.AddComponent<UpgradeManager>();
        var uso = new SerializedObject(umg);
        uso.FindProperty("_availableWeapons").arraySize = 1;
        uso.FindProperty("_availableWeapons").GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/Data/Weapons/FlyingSword.asset");
        uso.FindProperty("_availablePassives").arraySize = 2;
        uso.FindProperty("_availablePassives").GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<PassiveData>("Assets/Data/Passives/SpeedBoost.asset");
        uso.FindProperty("_availablePassives").GetArrayElementAtIndex(1).objectReferenceValue = AssetDatabase.LoadAssetAtPath<PassiveData>("Assets/Data/Passives/PowerUp.asset");
        uso.ApplyModifiedProperties();

        var upo = new GameObject("UpgradePanel"); upo.transform.SetParent(canvasObj.transform, false);
        var upr = upo.AddComponent<RectTransform>(); upr.anchorMin = Vector2.zero; upr.anchorMax = Vector2.one;
        upr.offsetMin = Vector2.zero; upr.offsetMax = Vector2.zero;
        upo.AddComponent<UnityEngine.UI.Image>().color = new Color(0f, 0f, 0f, 0.7f);
        upo.SetActive(false);
        var uui = upo.AddComponent<UpgradeUI>();
        var uuso = new SerializedObject(uui);
        uuso.FindProperty("_panel").objectReferenceValue = upo;
        uuso.ApplyModifiedProperties();

        var rso = new GameObject("ResultScreen"); rso.transform.SetParent(canvasObj.transform, false);
        var rsr = rso.AddComponent<RectTransform>(); rsr.anchorMin = Vector2.zero; rsr.anchorMax = Vector2.one;
        rsr.offsetMin = Vector2.zero; rsr.offsetMax = Vector2.zero;
        rso.AddComponent<UnityEngine.UI.Image>().color = new Color(0f, 0f, 0f, 0.8f);
        rso.SetActive(false);
        var rss = rso.AddComponent<ResultScreen>();
        var rssSO = new SerializedObject(rss);
        rssSO.FindProperty("_panel").objectReferenceValue = rso;
        rssSO.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/GameLevel.unity");
        Debug.Log("[SceneBuilder] GameLevel scene created and saved!");
    }
}