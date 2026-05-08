using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

// Drives the win-screen coin animation: spawn a pile of coins at the panel
// centre, then each one DOJumps to the top-bar coin counter, ticking it up.
// Runs while Time.timeScale = 0, so all tweens use SetUpdate(true) and waits
// use WaitForSecondsRealtime.
public class CoinRewardAnimation : MonoBehaviour
{
    [Header("References (set in Inspector or by UIBuilder)")]
    [Tooltip("Where coins spawn / pile up. Falls back to this transform.")]
    public RectTransform pileAnchor;
    [Tooltip("Where coins fly to. Usually the top-bar coin label/icon.")]
    public RectTransform targetAnchor;
    public Sprite coinSprite;

    [Header("Tuning")]
    public int   visualCoinCount = 8;
    public float pileSpawnGap   = 0.04f;
    public float pileSpawnDur   = 0.30f;
    public float pilePause      = 0.40f;
    public float pileRadius     = 70f;
    public float flyStaggerGap  = 0.08f;
    public float flyDuration    = 0.55f;
    public float flyJumpPower   = 120f;
    public Vector2 coinSize     = new Vector2(70f, 70f);

    public IEnumerator Play(int totalAmount, Action<int> onCoinLanded)
    {
        if (totalAmount <= 0) yield break;
        if (coinSprite == null) yield break;

        var anchor = pileAnchor != null ? pileAnchor : (RectTransform)transform;
        var canvasParent = (RectTransform)anchor.parent;
        if (canvasParent == null) canvasParent = anchor;

        int n = Mathf.Clamp(visualCoinCount, 1, Mathf.Max(1, totalAmount));
        int basePer   = totalAmount / n;
        int remainder = totalAmount - basePer * n;

        // ── Spawn the pile ────────────────────────────────────────────────
        var coins  = new List<RectTransform>();
        var amounts = new List<int>();
        Vector3 pilePos = anchor.position;

        for (int i = 0; i < n; i++)
        {
            var coin = SpawnCoin(canvasParent);
            coin.position   = pilePos;
            coin.localScale = Vector3.zero;

            Vector2 offset = UnityEngine.Random.insideUnitCircle * pileRadius;
            Vector3 pileTarget = pilePos + (Vector3)offset;

            coin.DOMove(pileTarget, pileSpawnDur).SetEase(Ease.OutQuad).SetUpdate(true);
            coin.DOScale(Vector3.one, pileSpawnDur).SetEase(Ease.OutBack).SetUpdate(true);

            coins.Add(coin);
            amounts.Add(basePer + (i < remainder ? 1 : 0));

            yield return new WaitForSecondsRealtime(pileSpawnGap);
        }

        yield return new WaitForSecondsRealtime(pilePause);

        // ── Fly each coin to the counter ──────────────────────────────────
        if (targetAnchor == null)
        {
            // No target — just fade the pile out so the panel can finish.
            foreach (var c in coins) if (c != null) Destroy(c.gameObject);
            yield break;
        }

        Vector3 target = targetAnchor.position;

        for (int i = 0; i < n; i++)
        {
            var coin = coins[i];
            int amt  = amounts[i];

            int captured = amt;
            RectTransform cRef = coin;

            coin.DOJump(target, flyJumpPower, 1, flyDuration)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    onCoinLanded?.Invoke(captured);
                    if (cRef != null) Destroy(cRef.gameObject);
                });
            coin.DORotate(new Vector3(0, 0, 360f), flyDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetUpdate(true);

            yield return new WaitForSecondsRealtime(flyStaggerGap);
        }

        yield return new WaitForSecondsRealtime(flyDuration + 0.1f);
    }

    RectTransform SpawnCoin(RectTransform parent)
    {
        var go = new GameObject("CoinFx", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = coinSize;
        var img = go.GetComponent<Image>();
        img.sprite          = coinSprite;
        img.preserveAspect  = true;
        img.raycastTarget   = false;
        return rt;
    }
}
