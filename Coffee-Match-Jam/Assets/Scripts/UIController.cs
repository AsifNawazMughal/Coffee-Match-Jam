using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Wires the in-game UI (top bar + timer + win/lose/pause panels) to GameManager.
// Built and laid out by Editor/UIBuilder.cs.
public class UIController : MonoBehaviour
{
    [Header("Top bar")]
    public TMP_Text levelLabel;
    public TMP_Text coinLabel;
    public TMP_Text timerLabel;
    public Button   pauseButton;

    [Header("Panels (hidden by default)")]
    public GameObject winPanel;
    public GameObject losePanel;
    public GameObject pausePanel;

    [Header("Win panel buttons")]
    public Button winNextButton;

    [Header("Lose panel buttons")]
    public Button loseRetryButton;

    [Header("Pause panel buttons")]
    public Button pauseResumeButton;
    public Button pauseRestartButton;
    public Button pauseHomeButton;

    [Header("Win sequence")]
    public CoinRewardAnimation coinReward;
    [Tooltip("When true, the Win panel only appears AFTER the coin animation finishes. " +
             "When false, the panel appears immediately and the GET IT button reveals at the end.")]
    [SerializeField] bool showPanelAfterAnimation = true;

    void Start()
    {
        SetActiveSafe(winPanel,   false);
        SetActiveSafe(losePanel,  false);
        SetActiveSafe(pausePanel, false);

        BindGameEvents();
        RefreshStaticLabels();
    }

    void OnDestroy()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.OnCoinsChanged -= UpdateCoins;
        gm.OnTimerTick    -= UpdateTimer;
        gm.OnWin          -= ShowWin;
        gm.OnLose         -= ShowLose;
        gm.OnPaused       -= ShowPause;
        gm.OnResumed      -= HidePause;
    }

    void BindGameEvents()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.OnCoinsChanged += UpdateCoins;
        gm.OnTimerTick    += UpdateTimer;
        gm.OnWin          += ShowWin;
        gm.OnLose         += ShowLose;
        gm.OnPaused       += ShowPause;
        gm.OnResumed      += HidePause;
    }

    void RefreshStaticLabels()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (levelLabel != null) levelLabel.text = $"Level {gm.LevelNumber}";
        UpdateCoins(gm.Coins);
        UpdateTimer(gm.TimeRemaining);
    }

    void UpdateCoins(int n)
    {
        if (coinLabel != null) coinLabel.text = n.ToString();
    }

    void UpdateTimer(float t)
    {
        if (timerLabel == null) return;
        int total = Mathf.CeilToInt(t);
        int m = total / 60;
        int s = total % 60;
        timerLabel.text = $"{m:00}:{s:00}";
    }

    // ── Public button handlers ─────────────────────────────────────────────
    // Wire these to each Button's OnClick in the Inspector. UIBuilder /
    // SceneWirer hooks them up automatically as persistent listeners.

    public void Pause()
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.LevelActive) return;
        gm.Pause();
    }

    public void Resume()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.Resume();
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // For now Home and Next reload the level too — wire them to a real menu
    // scene / next-level loader once those exist.
    public void Home() => Restart();
    public void Next() => Restart();

    // ── Internal panel/event handlers ──────────────────────────────────────

    void ShowWin() => StartCoroutine(WinSequence());

    IEnumerator WinSequence()
    {
        var gm = GameManager.Instance;

        if (showPanelAfterAnimation)
        {
            // Panel hidden during the coin rain; appears once it finishes.
            SetActiveSafe(winPanel, false);

            if (coinReward != null && gm != null)
                yield return coinReward.Play(gm.LevelReward, dy => gm.AddCoins(dy));

            SetActiveSafe(winPanel, true);
            if (winNextButton != null) winNextButton.gameObject.SetActive(true);
        }
        else
        {
            // Panel visible during the coin rain; only the GET IT button hides until done.
            SetActiveSafe(winPanel, true);
            if (winNextButton != null) winNextButton.gameObject.SetActive(false);

            if (coinReward != null && gm != null)
                yield return coinReward.Play(gm.LevelReward, dy => gm.AddCoins(dy));

            if (winNextButton != null) winNextButton.gameObject.SetActive(true);
        }
    }

    void ShowLose()  => SetActiveSafe(losePanel,  true);
    void ShowPause() => SetActiveSafe(pausePanel, true);
    void HidePause() => SetActiveSafe(pausePanel, false);

    static void SetActiveSafe(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }
}
