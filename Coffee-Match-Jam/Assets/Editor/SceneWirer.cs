using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

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
        var custParent  = GameObject.Find("--- Customer ---");
        var gmParent    = GameObject.Find("--- GameManager ---");

        if (lanesParent == null || slotsParent == null || gmParent == null)
        {
            Debug.LogError("Scene objects missing. Run 'Coffee Match > 2. Build Scene' first.");
            return;
        }

        var gm              = WireGameManagerBase(gmParent);
        var laneControllers = WireLanes(lanesParent);
        var slotRow         = WireSlotRow(slotsParent);
        var customerQueue   = custParent != null ? WireCustomerQueue(custParent) : null;

        gm.lanes         = laneControllers;
        gm.slotRow       = slotRow;
        gm.customerQueue = customerQueue;
        EditorUtility.SetDirty(gmParent);

        WireClickManager();
        PatchCoinRewardAnimation();
        PatchButtonHandlers();

        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log($"Wiring done! Boxes per lane: {gm.BoxesPerLane}  |  Press Play to test.");
    }

    // Adds CoinRewardAnimation to an existing WinPanel and wires it to the
    // UIController so users don't have to do this by hand after the feature
    // was introduced.
    // Wires each UI button's OnClick to the matching public UIController
    // method as a persistent listener — so the binding is visible (and editable)
    // in each Button's Inspector under the OnClick section.
    static void PatchButtonHandlers()
    {
        var ui = Object.FindFirstObjectByType<UIController>();
        if (ui == null) return;

        UIBuilder.WireButton(ui.pauseButton,        ui, nameof(UIController.Pause));
        UIBuilder.WireButton(ui.winNextButton,      ui, nameof(UIController.Next));
        UIBuilder.WireButton(ui.loseRetryButton,    ui, nameof(UIController.Restart));
        UIBuilder.WireButton(ui.pauseResumeButton,  ui, nameof(UIController.Resume));
        UIBuilder.WireButton(ui.pauseRestartButton, ui, nameof(UIController.Restart));
        UIBuilder.WireButton(ui.pauseHomeButton,    ui, nameof(UIController.Home));
    }

    static void PatchCoinRewardAnimation()
    {
        var ui = Object.FindFirstObjectByType<UIController>();
        if (ui == null || ui.winPanel == null) return;

        var anim = ui.winPanel.GetComponent<CoinRewardAnimation>();
        if (anim == null) anim = ui.winPanel.AddComponent<CoinRewardAnimation>();

        if (anim.coinSprite == null)
            anim.coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprites/coin.png");

        if (anim.targetAnchor == null && ui.coinLabel != null)
            anim.targetAnchor = ui.coinLabel.rectTransform;

        if (anim.pileAnchor == null)
        {
            var existing = ui.winPanel.transform.Find("CoinPileAnchor");
            RectTransform pile;
            if (existing == null)
            {
                var go = new GameObject("CoinPileAnchor", typeof(RectTransform));
                go.transform.SetParent(ui.winPanel.transform, false);
                pile = go.GetComponent<RectTransform>();
                pile.anchorMin = pile.anchorMax = pile.pivot = new Vector2(0.5f, 0.5f);
                pile.anchoredPosition = new Vector2(0f, 60f);
                pile.sizeDelta = Vector2.zero;
            }
            else
            {
                pile = (RectTransform)existing;
            }
            anim.pileAnchor = pile;
        }

        ui.coinReward = anim;

        EditorUtility.SetDirty(ui);
        EditorUtility.SetDirty(anim);
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

    static CustomerQueue WireCustomerQueue(GameObject parent)
    {
        var q = parent.GetComponent<CustomerQueue>();
        if (q == null) q = parent.AddComponent<CustomerQueue>();

        q.startPoint     = parent.transform.Find("StartPoint");
        q.waitPoint      = parent.transform.Find("WaitPoint");
        q.exitPoint      = parent.transform.Find("ExitPoint");
        q.customerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PfbPath}/Customer.prefab");

        EditorUtility.SetDirty(parent);
        return q;
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
