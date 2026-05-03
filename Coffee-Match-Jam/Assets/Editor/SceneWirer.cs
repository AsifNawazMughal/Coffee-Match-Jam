using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Coffee Match > 3. Wire Scene
/// Wires all scene references.
public static class SceneWirer
{
    const string PfbPath = "Assets/Prefabs";
    const string SOPath  = "Assets/ScriptableObjects";

    [MenuItem("Coffee Match/3. Wire Scene")]
    public static void WireScene()
    {
        var lanesParent = GameObject.Find("--- Lanes ---");
        var slotsParent = GameObject.Find("--- SlotsRow ---");
        var gmParent    = GameObject.Find("--- GameManager ---");

        if (lanesParent == null || slotsParent == null || gmParent == null)
        {
            Debug.LogError("Scene objects missing. Run 'Coffee Match > 2. Build Scene' first.");
            return;
        }

        var gm                = WireGameManagerBase(gmParent);
        var laneControllers   = WireLanes(lanesParent);
        var slotRow           = WireSlotRow(slotsParent);

        gm.lanes   = laneControllers;
        gm.slotRow = slotRow;
        EditorUtility.SetDirty(gmParent);

        WireClickManager();

        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log($"Wiring done! Boxes per lane: {gm.BoxesPerLane}  |  Press Play to test.");
    }

    static GameManager WireGameManagerBase(GameObject gmGO)
    {
        var gm = gmGO.GetComponent<GameManager>();
        if (gm == null) gm = gmGO.AddComponent<GameManager>();

        gm.containerSmallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PfbPath}/Container_Small.prefab");
        gm.containerLargePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PfbPath}/Container_Large.prefab");
        gm.containerTypeSmall   = AssetDatabase.LoadAssetAtPath<ContainerTypeSO>($"{SOPath}/ContainerType_Small.asset");
        gm.containerTypeLarge   = AssetDatabase.LoadAssetAtPath<ContainerTypeSO>($"{SOPath}/ContainerType_Large.asset");
        gm.colorDefinitions     = new[]
        {
            AssetDatabase.LoadAssetAtPath<ColorDefinitionSO>($"{SOPath}/ColorDef_Red.asset"),
            AssetDatabase.LoadAssetAtPath<ColorDefinitionSO>($"{SOPath}/ColorDef_Blue.asset"),
            AssetDatabase.LoadAssetAtPath<ColorDefinitionSO>($"{SOPath}/ColorDef_Yellow.asset"),
        };
        return gm;
    }

    static LaneController[] WireLanes(GameObject lanesParent)
    {
        int count = lanesParent.transform.childCount;
        var lanes = new LaneController[count];

        for (int i = 0; i < count; i++)
        {
            var laneGO = lanesParent.transform.GetChild(i).gameObject;

            var ctrl = laneGO.GetComponent<LaneController>();
            if (ctrl == null) ctrl = laneGO.AddComponent<LaneController>();

            Transform track = laneGO.transform.Find("Track");
            if (track == null)
                foreach (Transform c in laneGO.transform)
                    if (c.name.StartsWith("Track") || c.name == "LaneTrack") { track = c; break; }

            if (track != null)
            {
                ctrl.slotPositions = new Transform[5];
                for (int s = 0; s < 5; s++)
                    ctrl.slotPositions[s] = track.Find($"ContainerSlot_{s}");
                ctrl.spawnPoint = track.Find("SpawnPoint");
            }

            lanes[i] = ctrl;
            EditorUtility.SetDirty(laneGO);
        }

        return lanes;
    }

    static SlotRowManager WireSlotRow(GameObject slotsParent)
    {
        var mgr = slotsParent.GetComponent<SlotRowManager>();
        if (mgr == null) mgr = slotsParent.AddComponent<SlotRowManager>();

        int count = slotsParent.transform.childCount;
        mgr.slots = new ContainerSlot[count];
        for (int i = 0; i < count; i++)
        {
            var slotGO = slotsParent.transform.GetChild(i).gameObject;
            var slot   = slotGO.GetComponent<ContainerSlot>();
            if (slot == null) slot = slotGO.AddComponent<ContainerSlot>();
            mgr.slots[i] = slot;
        }

        EditorUtility.SetDirty(slotsParent);
        return mgr;
    }

    static void WireClickManager()
    {
        var cam = GameObject.Find("Main Camera");
        if (cam == null) { Debug.LogWarning("Main Camera not found — ClickManager not added."); return; }
        if (cam.GetComponent<ClickManager>() == null)
            cam.AddComponent<ClickManager>();
        EditorUtility.SetDirty(cam);
    }
}
