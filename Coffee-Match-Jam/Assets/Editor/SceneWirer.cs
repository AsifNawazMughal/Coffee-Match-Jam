using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Coffee Match > 3. Wire Scene
/// Adds scripts to prefabs and wires all scene references automatically.
/// Run after both "1. Build All Assets" and "2. Build Scene".
/// </summary>
public static class SceneWirer
{
    const string PfbPath = "Assets/Prefabs";
    const string SOPath  = "Assets/ScriptableObjects";

    [MenuItem("Coffee Match/3. Wire Scene")]
    public static void WireScene()
    {
        WirePrefabs();
        WireSceneGameObjects();
        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("Wiring complete! Press Play to test.");
    }

    // ── Step 1: Add scripts to prefabs ────────────────────────────────────

    static void WirePrefabs()
    {
        AddToPrefab<Container>("Container_Small");
        AddToPrefab<Container>("Container_Large");
        AddToPrefab<PlayerAgent>("Player");
    }

    static void AddToPrefab<T>(string prefabName) where T : Component
    {
        string path = $"{PfbPath}/{prefabName}.prefab";
        using var scope = new PrefabUtility.EditPrefabContentsScope(path);
        var root = scope.prefabContentsRoot;
        if (root == null) { Debug.LogWarning($"Prefab not found: {path}"); return; }
        if (root.GetComponent<T>() == null) root.AddComponent<T>();
    }

    // ── Step 2: Wire scene GameObjects ────────────────────────────────────

    static void WireSceneGameObjects()
    {
        var lanesParent  = GameObject.Find("--- Lanes ---");
        var stripParent  = GameObject.Find("--- PlayerStrip ---");
        var gmParent     = GameObject.Find("--- GameManager ---");

        if (lanesParent == null || stripParent == null || gmParent == null)
        {
            Debug.LogError("Scene objects missing. Run 'Coffee Match > 2. Build Scene' first.");
            return;
        }

        // Wire lanes and generators
        int laneCount = lanesParent.transform.childCount;
        var laneQueues    = new LaneQueue[laneCount];
        var genCtrls      = new ContainerGeneratorCtrl[laneCount];

        for (int i = 0; i < laneCount; i++)
        {
            var laneGO = lanesParent.transform.GetChild(i).gameObject;
            laneQueues[i] = WireLane(laneGO);
            genCtrls[i]   = WireGenerator(laneGO, laneQueues[i]);
        }

        // Wire player strip
        var stripCtrl = WireStrip(stripParent);

        // Create player exit point if missing
        var exitPoint = EnsureExitPoint();

        // Wire GameManager
        WireGameManager(gmParent, laneQueues, genCtrls, stripCtrl, exitPoint);
    }

    // ── Lane wiring ────────────────────────────────────────────────────────

    static LaneQueue WireLane(GameObject laneGO)
    {
        var laneQueue = laneGO.GetComponent<LaneQueue>();
        if (laneQueue == null) laneQueue = laneGO.AddComponent<LaneQueue>();

        // Find the LaneTrack child
        var track = laneGO.transform.Find("Track");
        if (track == null)
        {
            // Track might be named "LaneTrack" if prefab was not renamed
            foreach (Transform child in laneGO.transform)
                if (child.name.StartsWith("Lane") || child.name == "Track") { track = child; break; }
        }

        if (track == null) { Debug.LogWarning($"No Track child on {laneGO.name}"); return laneQueue; }

        // Slot positions
        laneQueue.slotPositions = new Transform[5];
        for (int s = 0; s < 5; s++)
        {
            var slot = track.Find($"ContainerSlot_{s}");
            if (slot != null) laneQueue.slotPositions[s] = slot;
            else Debug.LogWarning($"ContainerSlot_{s} not found in {track.name}");
        }

        // Spawn point
        laneQueue.spawnPoint = track.Find("SpawnPoint");
        laneQueue.moveSpeed  = 3f;

        return laneQueue;
    }

    // ── Generator wiring ───────────────────────────────────────────────────

    static ContainerGeneratorCtrl WireGenerator(GameObject laneGO, LaneQueue lane)
    {
        var genGO = laneGO.transform.Find("Generator")?.gameObject;
        if (genGO == null)
        {
            Debug.LogWarning($"No Generator child on {laneGO.name}");
            return null;
        }

        var ctrl = genGO.GetComponent<ContainerGeneratorCtrl>();
        if (ctrl == null) ctrl = genGO.AddComponent<ContainerGeneratorCtrl>();
        ctrl.targetLane    = lane;
        ctrl.checkInterval = 2f;
        return ctrl;
    }

    // ── Player strip wiring ────────────────────────────────────────────────

    static PlayerStripCtrl WireStrip(GameObject stripParent)
    {
        var ctrl = stripParent.GetComponent<PlayerStripCtrl>();
        if (ctrl == null) ctrl = stripParent.AddComponent<PlayerStripCtrl>();

        ctrl.playerPrefab    = AssetDatabase.LoadAssetAtPath<GameObject>($"{PfbPath}/Player.prefab");
        ctrl.stripSpawnPoint = stripParent.transform.Find("PlayerSpawnPoint");

        return ctrl;
    }

    // ── Exit point ─────────────────────────────────────────────────────────

    static Transform EnsureExitPoint()
    {
        var existing = GameObject.Find("PlayerExitPoint");
        if (existing != null) return existing.transform;

        var go = new GameObject("PlayerExitPoint");
        go.transform.position = new Vector3(-8f, 0f, 9.5f);
        return go.transform;
    }

    // ── GameManager wiring ─────────────────────────────────────────────────

    static void WireGameManager(
        GameObject        gmGO,
        LaneQueue[]       lanes,
        ContainerGeneratorCtrl[] generators,
        PlayerStripCtrl   strip,
        Transform         exitPoint)
    {
        var gm = gmGO.GetComponent<GameManager>();
        if (gm == null) gm = gmGO.AddComponent<GameManager>();

        gm.lanes               = lanes;
        gm.generators          = generators;
        gm.playerStrip         = strip;
        gm.containerSmallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PfbPath}/Container_Small.prefab");
        gm.containerLargePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PfbPath}/Container_Large.prefab");
        gm.containerTypeSmall  = AssetDatabase.LoadAssetAtPath<ContainerTypeSO>($"{SOPath}/ContainerType_Small.asset");
        gm.containerTypeLarge  = AssetDatabase.LoadAssetAtPath<ContainerTypeSO>($"{SOPath}/ContainerType_Large.asset");

        gm.colorDefinitions = new ColorDefinitionSO[]
        {
            AssetDatabase.LoadAssetAtPath<ColorDefinitionSO>($"{SOPath}/ColorDef_Red.asset"),
            AssetDatabase.LoadAssetAtPath<ColorDefinitionSO>($"{SOPath}/ColorDef_Blue.asset"),
            AssetDatabase.LoadAssetAtPath<ColorDefinitionSO>($"{SOPath}/ColorDef_Yellow.asset"),
        };

        // Also wire exit point on strip
        if (strip != null) strip.playerExitPoint = exitPoint;

        EditorUtility.SetDirty(gm);
        EditorUtility.SetDirty(strip);
    }
}
