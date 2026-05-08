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
        PatchPowerUpController();
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

    // Make sure CoinRewardAnimation lives on the Canvas (not inside WinPanel)
    // so it can run while WinPanel is hidden. Migrates any prior placement
    // on the WinPanel to the Canvas, preserving tuning values.
    // Adds a PowerUpController to the Canvas if missing, and auto-finds any
    // existing power-up buttons by name. If none are present (e.g. on an
    // older scene), nothing is added — the user has to drag references in.
    static void PatchPowerUpController()
    {
        var ui = Object.FindFirstObjectByType<UIController>();
        if (ui == null) return;

        var canvas = ui.GetComponent<Canvas>();
        if (canvas == null) return;
        var canvasGO = canvas.gameObject;

        var pu = canvasGO.GetComponent<PowerUpController>();
        if (pu == null) pu = canvasGO.AddComponent<PowerUpController>();

        if (pu.hintButton    == null) pu.hintButton    = FindButton(canvasGO.transform, "HintButton",  "Hint");
        if (pu.addTimeButton == null) pu.addTimeButton = FindButton(canvasGO.transform, "TimerButton", "AddTimeButton", "AddTime", "Add_Time", "AddTime_Button");
        if (pu.undoButton    == null) pu.undoButton    = FindButton(canvasGO.transform, "UndoButton",  "Undo");

        // Make sure each button is actually clickable: a Button with no Image
        // and no targetGraphic ignores clicks. Pick any child Graphic to use.
        EnsureButtonClickable(pu.hintButton);
        EnsureButtonClickable(pu.addTimeButton);
        EnsureButtonClickable(pu.undoButton);

        // ── Count badges (free-use stock displayed on each icon) ────────────
        if (pu.hintCountLabel    == null) pu.hintCountLabel    = FindCountLabel(pu.hintButton);
        if (pu.addTimeCountLabel == null) pu.addTimeCountLabel = FindCountLabel(pu.addTimeButton);
        if (pu.undoCountLabel    == null) pu.undoCountLabel    = FindCountLabel(pu.undoButton);

        // ── Message popup (auto-detect a Message-Popup GO with a TMP child) ─
        if (pu.messagePopup == null)
        {
            var popupT =
                FindRecursive(canvasGO.transform, "Message-Popup") ??
                FindRecursive(canvasGO.transform, "MessagePopup")  ??
                FindRecursive(canvasGO.transform, "Message_Popup");
            if (popupT != null) pu.messagePopup = popupT.gameObject;
        }

        if (pu.messageLabel == null)
        {
            // Try inside the popup first.
            if (pu.messagePopup != null)
                pu.messageLabel = pu.messagePopup.GetComponentInChildren<TMPro.TMP_Text>(true);

            // Otherwise scan the canvas for a TMP labelled MessageLabel.
            if (pu.messageLabel == null)
            {
                var msg = FindRecursive(canvasGO.transform, "MessageLabel");
                if (msg != null) pu.messageLabel = msg.GetComponent<TMPro.TMP_Text>();
            }
        }

        // Last-resort fallback: create a Canvas-level MessageLabel so "Need X
        // coins" feedback is visible even on scenes without a Message-Popup.
        if (pu.messageLabel == null)
        {
            var go = new GameObject("MessageLabel", typeof(RectTransform));
            go.transform.SetParent(canvasGO.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.offsetMin = new Vector2(40f, -50f);
            rt.offsetMax = new Vector2(-40f, 50f);
            var t = go.AddComponent<TMPro.TextMeshProUGUI>();
            t.text = "";
            t.fontSize = 60;
            t.alignment = TMPro.TextAlignmentOptions.Center;
            t.color = new Color(1f, 1f, 1f, 0f);
            t.raycastTarget = false;
            pu.messageLabel = t;
        }

        // Wire button OnClick handlers as persistent listeners (visible in Inspector).
        UIBuilder.WirePowerUp(pu.hintButton,    pu, nameof(PowerUpController.UseHint));
        UIBuilder.WirePowerUp(pu.addTimeButton, pu, nameof(PowerUpController.UseAddTime));
        UIBuilder.WirePowerUp(pu.undoButton,    pu, nameof(PowerUpController.UseUndo));

        if (pu.hintButton    == null) Debug.LogWarning("[SceneWirer] Hint button not found (named 'Hint' or 'HintButton').");
        if (pu.addTimeButton == null) Debug.LogWarning("[SceneWirer] AddTime button not found (named 'Add_Time', 'AddTime', 'AddTimeButton', or 'TimerButton').");
        if (pu.undoButton    == null) Debug.LogWarning("[SceneWirer] Undo button not found (named 'Undo' or 'UndoButton').");

        EditorUtility.SetDirty(pu);
    }

    // A Unity Button needs a Graphic (Image, Text, etc.) on itself or via
    // targetGraphic to receive raycasts and visual press feedback. If the
    // button has neither, point targetGraphic at the first child Graphic.
    static void EnsureButtonClickable(UnityEngine.UI.Button btn)
    {
        if (btn == null) return;
        if (btn.targetGraphic != null) return;

        var ownGraphic = btn.GetComponent<UnityEngine.UI.Graphic>();
        if (ownGraphic != null) { btn.targetGraphic = ownGraphic; EditorUtility.SetDirty(btn); return; }

        var childGraphic = btn.GetComponentInChildren<UnityEngine.UI.Graphic>(true);
        if (childGraphic != null)
        {
            btn.targetGraphic = childGraphic;
            // Ensure the child receives raycasts so clicks reach the button.
            childGraphic.raycastTarget = true;
            EditorUtility.SetDirty(btn);
            EditorUtility.SetDirty(childGraphic);
        }
    }

    // Walks the button's hierarchy (including parent's siblings) looking for
    // a TMP labelled COUNT — that's the free-use stock badge on each icon.
    static TMPro.TMP_Text FindCountLabel(UnityEngine.UI.Button button)
    {
        if (button == null) return null;
        var t = FindRecursiveCaseInsensitive(button.transform, "COUNT");
        if (t == null && button.transform.parent != null)
            t = FindRecursiveCaseInsensitive(button.transform.parent, "COUNT");
        return t != null ? t.GetComponent<TMPro.TMP_Text>() : null;
    }

    static Transform FindRecursiveCaseInsensitive(Transform root, string name)
    {
        if (root == null) return null;
        if (string.Equals(root.name, name, System.StringComparison.OrdinalIgnoreCase)) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var t = FindRecursiveCaseInsensitive(root.GetChild(i), name);
            if (t != null) return t;
        }
        return null;
    }

    static UnityEngine.UI.Button FindButton(Transform root, params string[] names)
    {
        foreach (var name in names)
        {
            var t = FindRecursive(root, name);
            if (t != null)
            {
                var b = t.GetComponent<UnityEngine.UI.Button>();
                if (b != null) return b;
            }
        }
        return null;
    }

    static Transform FindRecursive(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var t = FindRecursive(root.GetChild(i), name);
            if (t != null) return t;
        }
        return null;
    }

    static void PatchCoinRewardAnimation()
    {
        var ui = Object.FindFirstObjectByType<UIController>();
        if (ui == null) return;

        var canvas = ui.GetComponent<Canvas>();
        if (canvas == null) return;
        var canvasGO = canvas.gameObject;

        // If the component is currently on WinPanel (older builds), move it to the Canvas.
        var animOnPanel = ui.winPanel != null ? ui.winPanel.GetComponent<CoinRewardAnimation>() : null;
        var animOnCanvas = canvasGO.GetComponent<CoinRewardAnimation>();

        CoinRewardAnimation anim;
        if (animOnCanvas != null)
        {
            anim = animOnCanvas;
        }
        else if (animOnPanel != null)
        {
            anim = canvasGO.AddComponent<CoinRewardAnimation>();
            anim.pileAnchor      = animOnPanel.pileAnchor;
            anim.targetAnchor    = animOnPanel.targetAnchor;
            anim.coinSprite      = animOnPanel.coinSprite;
            anim.visualCoinCount = animOnPanel.visualCoinCount;
            anim.pileSpawnGap    = animOnPanel.pileSpawnGap;
            anim.pileSpawnDur    = animOnPanel.pileSpawnDur;
            anim.pilePause       = animOnPanel.pilePause;
            anim.pileRadius      = animOnPanel.pileRadius;
            anim.flyStaggerGap   = animOnPanel.flyStaggerGap;
            anim.flyDuration     = animOnPanel.flyDuration;
            anim.flyJumpPower    = animOnPanel.flyJumpPower;
            anim.coinSize        = animOnPanel.coinSize;
            Object.DestroyImmediate(animOnPanel);
        }
        else
        {
            anim = canvasGO.AddComponent<CoinRewardAnimation>();
        }

        if (anim.coinSprite == null)
            anim.coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprites/coin.png");

        if (anim.targetAnchor == null && ui.coinLabel != null)
            anim.targetAnchor = ui.coinLabel.rectTransform;

        // Pile anchor lives on the Canvas root too.
        if (anim.pileAnchor == null || anim.pileAnchor.parent != canvasGO.transform)
        {
            var existing = canvasGO.transform.Find("CoinPileAnchor");
            RectTransform pile;
            if (existing == null)
            {
                var go = new GameObject("CoinPileAnchor", typeof(RectTransform));
                go.transform.SetParent(canvasGO.transform, false);
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
