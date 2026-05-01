using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Coffee Match > 3. Wire Scene
/// Wires all scene references. Stop positions are created as real scene GameObjects — drag them freely.
public static class SceneWirer
{
    const string PfbPath = "Assets/Prefabs";
    const string SOPath  = "Assets/ScriptableObjects";

    [MenuItem("Coffee Match/3. Wire Scene")]
    public static void WireScene()
    {
        var lanesParent = GameObject.Find("--- Lanes ---");
        var slotsParent = GameObject.Find("--- SlotsRow ---");
        var stripParent = GameObject.Find("--- PlayerStrip ---");
        var gmParent    = GameObject.Find("--- GameManager ---");

        if (lanesParent == null || slotsParent == null || stripParent == null || gmParent == null)
        {
            Debug.LogError("Scene objects missing. Run 'Coffee Match > 2. Build Scene' first.");
            return;
        }

        // Wire GameManager first so we can read its settings (playerQueueSize, slotRowSize)
        var gm = WireGameManagerBase(gmParent);

        var (laneControllers, genCtrls) = WireLanes(lanesParent);
        var slotRow                     = WireSlotRow(slotsParent);
        var playerQueue                 = WirePlayerQueue(stripParent, gm.playerQueueSize);

        // Now fill in the remaining GameManager references
        gm.lanes                = laneControllers;
        gm.generators           = genCtrls;
        gm.slotRow              = slotRow;
        gm.playerQueue          = playerQueue;
        EditorUtility.SetDirty(gmParent);

        WireClickManager();

        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log($"Wiring done! Queue size: {gm.playerQueueSize}  |  Press Play to test.");
    }

    // ── GameManager (base pass — loads assets, reads settings) ─────────────

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

    // ── Lanes ──────────────────────────────────────────────────────────────

    static (LaneController[], ContainerGeneratorCtrl[]) WireLanes(GameObject lanesParent)
    {
        int count = lanesParent.transform.childCount;
        var lanes = new LaneController[count];
        var gens  = new ContainerGeneratorCtrl[count];

        for (int i = 0; i < count; i++)
        {
            var laneGO = lanesParent.transform.GetChild(i).gameObject;

            var ctrl = laneGO.GetComponent<LaneController>();
            if (ctrl == null) ctrl = laneGO.AddComponent<LaneController>();
            ctrl.moveSpeed = 3f;

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

            var genGO = laneGO.transform.Find("Generator")?.gameObject;
            if (genGO != null)
            {
                var gen = genGO.GetComponent<ContainerGeneratorCtrl>();
                if (gen == null) gen = genGO.AddComponent<ContainerGeneratorCtrl>();
                gen.targetLane    = ctrl;
                gen.checkInterval = 2.5f;
                gens[i] = gen;
            }

            EditorUtility.SetDirty(laneGO);
        }

        return (lanes, gens);
    }

    // ── Slot Row ───────────────────────────────────────────────────────────

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

    // ── Player Queue ───────────────────────────────────────────────────────
    // Creates stop positions as real scene GameObjects so you can drag them.

    static PlayerQueue WirePlayerQueue(GameObject stripParent, int queueSize)
    {
        // Remove previously generated stop positions
        var old = new List<GameObject>();
        foreach (Transform c in stripParent.transform)
            if (c.name.StartsWith("StopPos_")) old.Add(c.gameObject);
        foreach (var go in old) Object.DestroyImmediate(go);

        // Spread stop positions evenly across the strip
        const float startX  = -5f;
        const float endX    =  5f;
        const float stripZ  =  9.5f;
        float spacing = queueSize > 1 ? (endX - startX) / (queueSize - 1) : 0f;

        var stops = new Transform[queueSize];
        for (int i = 0; i < queueSize; i++)
        {
            var go = new GameObject($"StopPos_{i}");
            go.transform.SetParent(stripParent.transform);
            go.transform.position = new Vector3(startX + i * spacing, 0f, stripZ);
            stops[i] = go.transform;
        }

        var pq = stripParent.GetComponent<PlayerQueue>();
        if (pq == null) pq = stripParent.AddComponent<PlayerQueue>();

        pq.playerPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>($"{PfbPath}/Player.prefab");
        pq.spawnInterval = 1.5f;
        pq.stopPositions = stops;

        // Exit point — players walk here and vanish
        var exit = GameObject.Find("PlayerExitPoint");
        if (exit == null)
        {
            exit = new GameObject("PlayerExitPoint");
            exit.transform.position = new Vector3(-9f, 0f, stripZ);
        }
        pq.exitPoint = exit.transform;

        EditorUtility.SetDirty(stripParent);
        return pq;
    }

    // ── Click Manager ──────────────────────────────────────────────────────

    static void WireClickManager()
    {
        var cam = GameObject.Find("Main Camera");
        if (cam == null) { Debug.LogWarning("Main Camera not found — ClickManager not added."); return; }
        if (cam.GetComponent<ClickManager>() == null)
            cam.AddComponent<ClickManager>();
        EditorUtility.SetDirty(cam);
    }
}
