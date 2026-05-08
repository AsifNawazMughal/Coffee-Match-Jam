using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

// Three power-ups, each with a stock (free uses) and a fallback coin cost.
//   Hint    — pulses the front box on a lane that matches the front customer.
//   AddTime — adds N seconds to the level timer.
//   Undo    — reverts the most recent box→slot move.
//
// Click order:
//   1. If stock > 0, use a charge for free (decrement count).
//   2. Else, if Coins >= cost, spend coins.
//   3. Else, show "Need X coins" via Message-Popup.
public class PowerUpController : MonoBehaviour
{
    [Header("Power-up costs (when stock is 0)")]
    [SerializeField] int hintCost    = 10;
    [SerializeField] int addTimeCost = 10;
    [SerializeField] int undoCost    = 10;

    [Header("Power-up stock (free uses available)")]
    [SerializeField] int hintCount    = 0;
    [SerializeField] int addTimeCount = 0;
    [SerializeField] int undoCount    = 0;

    [Header("Effect amounts")]
    [SerializeField] float addTimeAmount = 30f;

    [Header("Buttons")]
    public Button hintButton;
    public Button addTimeButton;
    public Button undoButton;

    [Header("Cost labels (shown next to each button)")]
    public TMP_Text hintCostLabel;
    public TMP_Text addTimeCostLabel;
    public TMP_Text undoCostLabel;

    [Header("Count labels (badge on each icon)")]
    public TMP_Text hintCountLabel;
    public TMP_Text addTimeCountLabel;
    public TMP_Text undoCountLabel;

    [Header("Message popup")]
    [Tooltip("The popup GameObject toggled on/off when showing a message.")]
    public GameObject messagePopup;
    [Tooltip("TMP_Text inside the popup that displays the message.")]
    public TMP_Text   messageLabel;
    public float      messageDuration = 1.4f;

    public int HintCost    => hintCost;
    public int AddTimeCost => addTimeCost;
    public int UndoCost    => undoCost;

    void Start()
    {
        SyncLabels();
        if (messagePopup != null) messagePopup.SetActive(false);
    }

    void SyncLabels()
    {
        if (hintCostLabel    != null) hintCostLabel.text    = hintCost.ToString();
        if (addTimeCostLabel != null) addTimeCostLabel.text = addTimeCost.ToString();
        if (undoCostLabel    != null) undoCostLabel.text    = undoCost.ToString();

        if (hintCountLabel    != null) hintCountLabel.text    = hintCount.ToString();
        if (addTimeCountLabel != null) addTimeCountLabel.text = addTimeCount.ToString();
        if (undoCountLabel    != null) undoCountLabel.text    = undoCount.ToString();
    }

    // ── Public handlers (wire to button OnClick) ──────────────────────────

    public void UseHint()
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.LevelActive) return;

        // Resolve the target first so we don't burn a charge for nothing.
        var queue = gm.customerQueue;
        var customer = queue != null ? queue.FrontCustomer : null;
        if (customer == null || !customer.IsReady) { ShowMessage("No customer waiting"); return; }

        Container target = null;
        if (gm.lanes != null)
        {
            foreach (var lane in gm.lanes)
            {
                if (lane == null) continue;
                var front = lane.FrontBox;
                if (front != null && front.color == customer.color) { target = front; break; }
            }
        }
        if (target == null) { ShowMessage("No matching box on lanes"); return; }

        if (!ConsumeCharge(ref hintCount, hintCost, hintCountLabel)) return;

        target.transform.DOPunchScale(Vector3.one * 0.25f, 0.6f, 6, 0.6f);
    }

    public void UseAddTime()
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.LevelActive) return;

        if (!ConsumeCharge(ref addTimeCount, addTimeCost, addTimeCountLabel)) return;
        gm.AddTime(addTimeAmount);
    }

    public void UseUndo()
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.LevelActive) return;

        if (GameManager.Instance.UndoLastMoveDryRun() == false)
        {
            ShowMessage("Nothing to undo");
            return;
        }

        if (!ConsumeCharge(ref undoCount, undoCost, undoCountLabel)) return;
        gm.UndoLastMove();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    // Spends a stock charge if any are available; otherwise spends coins.
    // Returns false (and shows a message) if neither is possible.
    bool ConsumeCharge(ref int stock, int cost, TMP_Text countLabel)
    {
        if (stock > 0)
        {
            stock--;
            if (countLabel != null) countLabel.text = stock.ToString();
            return true;
        }

        var gm = GameManager.Instance;
        if (gm == null) return false;
        if (gm.Coins < cost) { ShowMessage($"Need {cost} coins"); return false; }

        gm.SpendCoins(cost);
        return true;
    }

    public void AddCharges(int hint = 0, int addTime = 0, int undo = 0)
    {
        hintCount    = Mathf.Max(0, hintCount + hint);
        addTimeCount = Mathf.Max(0, addTimeCount + addTime);
        undoCount    = Mathf.Max(0, undoCount + undo);
        SyncLabels();
    }

    void ShowMessage(string text)
    {
        if (messageLabel == null && messagePopup == null) { Debug.Log($"[PowerUp] {text}"); return; }

        if (messageLabel != null) messageLabel.text = text;

        var popup = messagePopup != null ? messagePopup : (messageLabel != null ? messageLabel.gameObject : null);
        if (popup == null) return;

        popup.SetActive(true);

        var cg = popup.GetComponent<CanvasGroup>();
        if (cg == null) cg = popup.AddComponent<CanvasGroup>();

        cg.DOKill();
        cg.alpha = 0f;
        var seq = DOTween.Sequence().SetUpdate(true).SetTarget(cg);
        seq.Append(cg.DOFade(1f, 0.15f));
        seq.AppendInterval(messageDuration);
        seq.Append(cg.DOFade(0f, 0.4f));
        seq.OnComplete(() => { if (popup != null) popup.SetActive(false); });
    }
}
