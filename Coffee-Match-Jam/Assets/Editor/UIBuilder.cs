using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// Coffee Match > 4. Build UI
/// Builds the in-game Canvas: top bar (level / coin / pause / timer)
/// plus Win, Lose and Pause panels. Wires a UIController on the Canvas.
public static class UIBuilder
{
    const string UIAtlasPath = "Assets/sprites/ui-elements.png";
    const string CoinPath    = "Assets/sprites/coin.png";

    const float ReferenceWidth  = 1080f;
    const float ReferenceHeight = 1920f;

    [MenuItem("Coffee Match/4. Build UI")]
    public static void BuildUI()
    {
        var existing = Object.FindFirstObjectByType<UIController>();
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var canvasGO = CreateCanvas();
        var canvasRT = canvasGO.GetComponent<RectTransform>();

        var ui = canvasGO.AddComponent<UIController>();

        // ── Top bar ────────────────────────────────────────────────────────
        var topBar = CreatePanel("TopBar", canvasRT, new Vector2(0f, 1f), new Vector2(1f, 1f),
                                  new Vector2(0f, -210f), new Vector2(0f, -30f));

        ui.levelLabel  = AddBarSlot(topBar, "Level",  Sprite("pannel"), null,           "Level 1", new Vector2(0.02f, 0f), new Vector2(0.36f, 1f));
        ui.coinLabel   = AddBarSlot(topBar, "Coin",   Sprite("pannel"), CoinIcon(),     "0",       new Vector2(0.36f, 0f), new Vector2(0.70f, 1f));
        ui.pauseButton = AddIconButton(topBar, "PauseButton", Sprite("pause"),          new Vector2(0.78f, 0f), new Vector2(0.98f, 1f));

        // ── Timer (just below the top bar) ─────────────────────────────────
        var timerHolder = CreatePanel("Timer", canvasRT, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                       new Vector2(-160f, -340f), new Vector2(160f, -240f));
        ui.timerLabel = AddTextWithBg(timerHolder, "TimerLabel", Sprite("pannel"), "01:00", 56);

        // ── Panels ─────────────────────────────────────────────────────────
        ui.winPanel   = BuildWinPanel(canvasRT, ui);
        ui.losePanel  = BuildLosePanel(canvasRT, ui);
        ui.pausePanel = BuildPausePanel(canvasRT, ui);

        ui.winPanel.SetActive(false);
        ui.losePanel.SetActive(false);
        ui.pausePanel.SetActive(false);

        BuildCoinRewardAnimation(canvasRT, ui);

        var messageLabel = BuildMessageLabel(canvasRT);
        var powerUp      = BuildPowerUpBar(canvasRT, messageLabel);

        // ── Wire button OnClick handlers (visible in each Button's Inspector) ──
        WireButton(ui.pauseButton,        ui, nameof(UIController.Pause));
        WireButton(ui.winNextButton,      ui, nameof(UIController.Next));
        WireButton(ui.loseRetryButton,    ui, nameof(UIController.Restart));
        WireButton(ui.pauseResumeButton,  ui, nameof(UIController.Resume));
        WireButton(ui.pauseRestartButton, ui, nameof(UIController.Restart));
        WireButton(ui.pauseHomeButton,    ui, nameof(UIController.Home));
        WirePowerUp(powerUp.hintButton,    powerUp, nameof(PowerUpController.UseHint));
        WirePowerUp(powerUp.addTimeButton, powerUp, nameof(PowerUpController.UseAddTime));
        WirePowerUp(powerUp.undoButton,    powerUp, nameof(PowerUpController.UseUndo));

        EnsureEventSystem();

        EditorUtility.SetDirty(canvasGO);
        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("UI built. Top bar + timer + win/lose/pause panels wired to UIController.");
    }

    // ── Panel builders ─────────────────────────────────────────────────────

    static GameObject BuildWinPanel(RectTransform parent, UIController ui)
    {
        var root  = CreatePanelOverlay("WinPanel", parent);
        var frame = AddCenteredImage(root.transform, "Frame", Sprite("won-pannel"), new Vector2(820, 1100));
        ui.winNextButton = AddButton(frame, "NextButton", Sprite("next"), new Vector2(0.5f, 0.10f), new Vector2(0.5f, 0.10f), new Vector2(280, 120));
        return root;
    }

    // Coin animation lives on the Canvas root so it can run while the WinPanel is hidden.
    static void BuildCoinRewardAnimation(RectTransform canvasRT, UIController ui)
    {
        var canvasGO = canvasRT.gameObject;

        var pileGO = new GameObject("CoinPileAnchor", typeof(RectTransform));
        pileGO.transform.SetParent(canvasRT, false);
        var pile = pileGO.GetComponent<RectTransform>();
        pile.anchorMin = pile.anchorMax = pile.pivot = new Vector2(0.5f, 0.5f);
        pile.anchoredPosition = new Vector2(0f, 60f);
        pile.sizeDelta = Vector2.zero;

        var anim = canvasGO.GetComponent<CoinRewardAnimation>();
        if (anim == null) anim = canvasGO.AddComponent<CoinRewardAnimation>();
        anim.pileAnchor   = pile;
        anim.targetAnchor = ui.coinLabel != null ? ui.coinLabel.rectTransform : null;
        anim.coinSprite   = CoinIcon();
        ui.coinReward     = anim;
    }

    static GameObject BuildLosePanel(RectTransform parent, UIController ui)
    {
        var root  = CreatePanelOverlay("LosePanel", parent);
        var frame = AddCenteredImage(root.transform, "Frame", Sprite("lose-pannel"), new Vector2(820, 1100));
        ui.loseRetryButton = AddButton(frame, "RetryButton", Sprite("retry"), new Vector2(0.5f, 0.10f), new Vector2(0.5f, 0.10f), new Vector2(280, 120));
        return root;
    }

    static GameObject BuildPausePanel(RectTransform parent, UIController ui)
    {
        var root  = CreatePanelOverlay("PausePanel", parent);
        var frame = AddCenteredImage(root.transform, "Frame", Sprite("pause-pannel"), new Vector2(820, 1100));
        ui.pauseResumeButton  = AddButton(frame, "ResumeButton",  Sprite("resume"),  new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.45f), new Vector2(260, 120));
        ui.pauseRestartButton = AddButton(frame, "RestartButton", Sprite("restart"), new Vector2(0.5f, 0.30f), new Vector2(0.5f, 0.30f), new Vector2(260, 120));
        ui.pauseHomeButton    = AddButton(frame, "HomeButton",    Sprite("home"),    new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.15f), new Vector2(260, 120));
        return root;
    }

    // ── Building blocks ────────────────────────────────────────────────────

    static GameObject CreateCanvas()
    {
        var go = new GameObject("--- UI Canvas ---", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return go;
    }

    static RectTransform CreatePanel(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return rt;
    }

    static GameObject CreatePanelOverlay(string name, RectTransform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var dim = go.GetComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.55f);
        dim.raycastTarget = true;
        return go;
    }

    static RectTransform AddCenteredImage(Transform parent, string name, Sprite sprite, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        if (sprite == null) img.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        return rt;
    }

    static TMP_Text AddBarSlot(RectTransform parent, string name, Sprite bg, Sprite icon, string text,
                                Vector2 anchorMin, Vector2 anchorMax)
    {
        var slot = CreatePanel(name, parent, anchorMin, anchorMax, new Vector2(8, 8), new Vector2(-8, -8));
        var bgGO = new GameObject("BG", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(slot, false);
        Stretch(bgGO);
        var bgImg = bgGO.GetComponent<Image>();
        bgImg.sprite = bg;
        bgImg.type   = Image.Type.Sliced;
        bgImg.preserveAspect = false;

        if (icon != null)
        {
            var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(slot, false);
            var ir = iconGO.GetComponent<RectTransform>();
            ir.anchorMin = new Vector2(0f, 0.5f);
            ir.anchorMax = new Vector2(0f, 0.5f);
            ir.pivot     = new Vector2(0f, 0.5f);
            ir.anchoredPosition = new Vector2(20f, 0f);
            ir.sizeDelta = new Vector2(80f, 80f);
            iconGO.GetComponent<Image>().sprite = icon;
        }

        var labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(slot, false);
        var lr = labelGO.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = new Vector2(icon != null ? 110f : 30f, 0f);
        lr.offsetMax = new Vector2(-30f, 0f);
        var t = labelGO.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = 56;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        t.enableWordWrapping = false;
        return t;
    }

    static Button AddIconButton(RectTransform parent, string name, Sprite icon, Vector2 anchorMin, Vector2 anchorMax)
    {
        var slot = CreatePanel(name, parent, anchorMin, anchorMax, new Vector2(8, 8), new Vector2(-8, -8));
        var img  = slot.gameObject.AddComponent<Image>();
        img.sprite = icon;
        img.preserveAspect = true;
        var btn = slot.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        return btn;
    }

    static TMP_Text AddTextWithBg(RectTransform parent, string name, Sprite bg, string text, int fontSize)
    {
        var bgGO = new GameObject("BG", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(parent, false);
        Stretch(bgGO);
        var bgImg = bgGO.GetComponent<Image>();
        bgImg.sprite = bg;
        bgImg.type   = Image.Type.Sliced;

        var labelGO = new GameObject(name, typeof(RectTransform));
        labelGO.transform.SetParent(parent, false);
        Stretch(labelGO);
        var t = labelGO.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = fontSize;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        return t;
    }

    // ── Power-up bar + message label ───────────────────────────────────────

    static TMP_Text BuildMessageLabel(RectTransform canvasRT)
    {
        var go = new GameObject("MessageLabel", typeof(RectTransform));
        go.transform.SetParent(canvasRT, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.offsetMin = new Vector2(40f, -50f);
        rt.offsetMax = new Vector2(-40f, 50f);

        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = "";
        t.fontSize = 60;
        t.alignment = TextAlignmentOptions.Center;
        t.color = new Color(1f, 1f, 1f, 0f);
        t.raycastTarget = false;
        return t;
    }

    static PowerUpController BuildPowerUpBar(RectTransform canvasRT, TMP_Text messageLabel)
    {
        var bar = CreatePanel("PowerUpBar", canvasRT,
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 30f), new Vector2(0f, 260f));

        var hint = BuildPowerUpSlot(bar, "HintButton",  Sprite("hint"),    new Vector2(0.08f, 0f), new Vector2(0.32f, 1f));
        var time = BuildPowerUpSlot(bar, "TimerButton", Sprite("forward"), new Vector2(0.38f, 0f), new Vector2(0.62f, 1f));
        var undo = BuildPowerUpSlot(bar, "UndoButton",  Sprite("reverse"), new Vector2(0.68f, 0f), new Vector2(0.92f, 1f));

        var pu = canvasRT.gameObject.AddComponent<PowerUpController>();
        pu.hintButton       = hint.button;
        pu.addTimeButton    = time.button;
        pu.undoButton       = undo.button;
        pu.hintCostLabel    = hint.cost;
        pu.addTimeCostLabel = time.cost;
        pu.undoCostLabel    = undo.cost;
        pu.messageLabel     = messageLabel;
        return pu;
    }

    struct PowerUpSlot { public Button button; public TMP_Text cost; }

    static PowerUpSlot BuildPowerUpSlot(RectTransform parent, string name, Sprite icon, Vector2 anchorMin, Vector2 anchorMax)
    {
        var slot = CreatePanel(name, parent, anchorMin, anchorMax, new Vector2(8, 8), new Vector2(-8, -8));

        // Button (top portion)
        var btnGO = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(slot, false);
        var brt = (RectTransform)btnGO.transform;
        brt.anchorMin = new Vector2(0f, 0.30f);
        brt.anchorMax = new Vector2(1f, 1f);
        brt.offsetMin = Vector2.zero;
        brt.offsetMax = Vector2.zero;
        var img = btnGO.GetComponent<Image>();
        img.sprite = icon;
        img.preserveAspect = true;
        var btn = btnGO.GetComponent<Button>();
        btn.targetGraphic = img;

        // Cost label (coin icon + number) along the bottom
        var costGO = new GameObject("Cost", typeof(RectTransform), typeof(Image));
        costGO.transform.SetParent(slot, false);
        var crt = (RectTransform)costGO.transform;
        crt.anchorMin = new Vector2(0.10f, 0f);
        crt.anchorMax = new Vector2(0.90f, 0.28f);
        crt.offsetMin = Vector2.zero;
        crt.offsetMax = Vector2.zero;
        var costBg = costGO.GetComponent<Image>();
        costBg.sprite = Sprite("pannel");
        costBg.type   = Image.Type.Sliced;

        var coinIconGO = new GameObject("Coin", typeof(RectTransform), typeof(Image));
        coinIconGO.transform.SetParent(costGO.transform, false);
        var crIcon = (RectTransform)coinIconGO.transform;
        crIcon.anchorMin = new Vector2(0f, 0.5f);
        crIcon.anchorMax = new Vector2(0f, 0.5f);
        crIcon.pivot     = new Vector2(0f, 0.5f);
        crIcon.anchoredPosition = new Vector2(8f, 0f);
        crIcon.sizeDelta = new Vector2(48f, 48f);
        coinIconGO.GetComponent<Image>().sprite = CoinIcon();

        var costLabelGO = new GameObject("Amount", typeof(RectTransform));
        costLabelGO.transform.SetParent(costGO.transform, false);
        var clrt = (RectTransform)costLabelGO.transform;
        clrt.anchorMin = Vector2.zero;
        clrt.anchorMax = Vector2.one;
        clrt.offsetMin = new Vector2(60f, 0f);
        clrt.offsetMax = new Vector2(-8f, 0f);
        var costText = costLabelGO.AddComponent<TextMeshProUGUI>();
        costText.text = "10";
        costText.fontSize = 40;
        costText.alignment = TextAlignmentOptions.Center;
        costText.color = Color.white;

        return new PowerUpSlot { button = btn, cost = costText };
    }

    public static void WirePowerUp(Button button, PowerUpController target, string methodName)
    {
        if (button == null || target == null || string.IsNullOrEmpty(methodName)) return;

        int n = button.onClick.GetPersistentEventCount();
        for (int i = 0; i < n; i++)
        {
            if (button.onClick.GetPersistentTarget(i) == target &&
                button.onClick.GetPersistentMethodName(i) == methodName)
                return;
        }

        var method = typeof(PowerUpController).GetMethod(methodName);
        if (method == null) { Debug.LogWarning($"UIBuilder: PowerUpController.{methodName} not found"); return; }

        UnityEngine.Events.UnityAction call =
            (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction), target, method);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, call);
        EditorUtility.SetDirty(button);
    }

    static Button AddButton(RectTransform parent, string name, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        return btn;
    }

    static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ── Sprite lookup ──────────────────────────────────────────────────────

    static Sprite Sprite(string name)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(UIAtlasPath);
        foreach (var a in assets)
            if (a is Sprite s && s.name == name) return s;
        Debug.LogWarning($"UIBuilder: sprite '{name}' not found in {UIAtlasPath}");
        return null;
    }

    static Sprite CoinIcon() => AssetDatabase.LoadAssetAtPath<Sprite>(CoinPath);

    // Adds a *persistent* OnClick listener targeting target.methodName so the
    // wiring shows up in the Button's Inspector under the OnClick section
    // (rather than being added invisibly at runtime). Idempotent: skips if the
    // same target+method is already registered.
    public static void WireButton(Button button, UIController target, string methodName)
    {
        if (button == null || target == null || string.IsNullOrEmpty(methodName)) return;

        int n = button.onClick.GetPersistentEventCount();
        for (int i = 0; i < n; i++)
        {
            if (button.onClick.GetPersistentTarget(i) == target &&
                button.onClick.GetPersistentMethodName(i) == methodName)
                return;
        }

        var method = typeof(UIController).GetMethod(methodName);
        if (method == null) { Debug.LogWarning($"UIBuilder: UIController.{methodName} not found"); return; }

        UnityAction call = (UnityAction)System.Delegate.CreateDelegate(typeof(UnityAction), target, method);
        UnityEventTools.AddPersistentListener(button.onClick, call);
        EditorUtility.SetDirty(button);
    }

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
        var go = new GameObject("EventSystem",
            typeof(UnityEngine.EventSystems.EventSystem),
            typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
    }
}
