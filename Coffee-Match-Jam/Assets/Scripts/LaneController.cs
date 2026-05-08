using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

// Manages a stack of boxes on one lane.
// Slot 0 = front (closest to slots row); last slot = near the generator.
// Only the front (top) box is interactable.
//
// Capacity model: at most `VisibleCap` boxes sit on the lane at the same time.
// Anything past that waits at the spawn point (invisible, scale 0). When the
// front box is taken, the queued box at the new tail slides in from spawn.
public class LaneController : MonoBehaviour
{
    [Header("Slot transforms: 0=front, last=near generator")]
    public Transform[] slotPositions = new Transform[5];
    public Transform spawnPoint;

    [Header("Layout")]
    [Tooltip("Constant world-space gap between adjacent boxes, regardless of box length.")]
    public float boxSpacing = 0.08f;

    [Tooltip("Max boxes visible on the lane at once. <= 0 means use slotPositions.Length.")]
    public int visibleSlots = 0;

    [Header("Initial pop-in animation")]
    public float popDuration = 0.35f;
    public float popGap      = 0.12f;
    public Ease  popEase     = Ease.OutBack;

    [Header("Mid-game animation")]
    [Tooltip("Used when a queued box slides in from the conveyor onto the lane.")]
    public float arrivalDuration = 0.45f;
    public float shiftDuration   = 0.30f;

    private readonly List<Container> stack = new();
    private bool busy;
    private GameManager gm;

    Vector3 frontAnchor;
    Vector3 laneDir = Vector3.forward;
    float   trackLength;

    public int  BoxCount => stack.Count;
    public bool HasBoxes => stack.Count > 0;

    int VisibleCap
    {
        get
        {
            if (visibleSlots > 0) return visibleSlots;
            return slotPositions != null && slotPositions.Length > 0
                ? slotPositions.Length
                : int.MaxValue;
        }
    }

    Vector3 SpawnPos => spawnPoint != null
        ? spawnPoint.position
        : frontAnchor + laneDir * trackLength;

    public void Initialize(GameManager manager, int boxCount)
    {
        gm = manager;
        CacheLaneGeometry();
        StartCoroutine(InitialSpawnSequence(boxCount));
    }

    void CacheLaneGeometry()
    {
        if (slotPositions == null || slotPositions.Length == 0 || slotPositions[0] == null)
        {
            frontAnchor = transform.position;
            trackLength = float.MaxValue;
            return;
        }

        frontAnchor = slotPositions[0].position;

        Transform back = slotPositions[slotPositions.Length - 1];
        if (slotPositions.Length > 1 && back != null)
        {
            laneDir     = (back.position - frontAnchor).normalized;
            trackLength = Vector3.Distance(frontAnchor, back.position);
        }
        else if (spawnPoint != null)
        {
            laneDir     = (spawnPoint.position - frontAnchor).normalized;
            trackLength = Vector3.Distance(frontAnchor, spawnPoint.position);
        }
        else
        {
            laneDir     = Vector3.forward;
            trackLength = float.MaxValue;
        }
    }

    // Walk the stack and accumulate (prevHalf + spacing + currHalf) per gap so
    // adjacent on-lane boxes always share the same visible spacing — small (4
    // cans) and large (6 cans) boxes mix freely.
    Vector3 GetOnLanePosition(int index)
    {
        float cum = 0f;
        for (int i = 1; i <= index && i < stack.Count; i++)
        {
            float prev = stack[i - 1] != null ? stack[i - 1].Length : 0f;
            float curr = stack[i]     != null ? stack[i].Length     : 0f;
            cum += prev * 0.5f + boxSpacing + curr * 0.5f;
        }
        return frontAnchor + laneDir * cum;
    }

    Vector3 TargetFor(int index) => index < VisibleCap ? GetOnLanePosition(index) : SpawnPos;

    // Initial level start: only the on-lane boxes pop in via DOScale at their
    // final lane positions. Excess boxes wait at the spawn point with scale 0
    // and remain invisible until something on the lane leaves.
    IEnumerator InitialSpawnSequence(int boxCount)
    {
        Vector3 spawnPos = SpawnPos;
        int cap = VisibleCap;

        for (int i = 0; i < boxCount; i++)
        {
            var box = SpawnBox();
            if (box == null) continue;

            stack.Add(box);
            int idx = stack.Count - 1;
            box.transform.position   = idx < cap ? GetOnLanePosition(idx) : spawnPos;
            box.transform.localScale = Vector3.zero;
        }

        int popCount = Mathf.Min(stack.Count, cap);
        for (int i = 0; i < popCount; i++)
        {
            var b = stack[i];
            if (b == null) continue;
            b.transform.DOScale(Vector3.one, popDuration).SetEase(popEase);
            yield return new WaitForSeconds(popGap);
        }

        yield return new WaitForSeconds(popDuration);
        OpenTop();
    }

    // Adds a box at runtime: it appears at spawn (scale 0) and slides+pops on
    // when there's a free spot on the lane.
    public void AddBoxFromBelt()
    {
        var box = SpawnBox();
        if (box == null) return;

        stack.Add(box);
        int idx = stack.Count - 1;

        box.transform.position   = SpawnPos;
        box.transform.localScale = Vector3.zero;

        if (idx < VisibleCap)
            SlideQueuedBoxOn(box, idx);
    }

    void SlideQueuedBoxOn(Container box, int targetIndex)
    {
        box.transform.DOScale(Vector3.one, arrivalDuration).SetEase(popEase);
        box.transform.DOMove(GetOnLanePosition(targetIndex), arrivalDuration).SetEase(Ease.OutQuad);
    }

    Container SpawnBox()
    {
        if (gm == null) return null;
        var (prefab, typeSO) = gm.GetRandomContainerSpec();
        if (prefab == null) return null;

        var go  = Instantiate(prefab, transform.position, Quaternion.identity, transform);
        var box = go.GetComponent<Container>();
        if (box == null) { Destroy(go); return null; }

        PackageColor color = gm.GetRandomColor();
        var def = gm.GetColorDef(color);
        box.Init(color, typeSO, def != null ? def.material : null);

        // Every box is clickable — non-top boxes will just shake.
        var handler = box.GetComponent<ContainerClickHandler>();
        if (handler != null) handler.lane = this;
        var col = box.GetComponent<Collider>();
        if (col != null) col.enabled = true;
        return box;
    }

    void OpenTop()
    {
        if (stack.Count == 0) return;
        var top = stack[0];
        if (top != null) top.Open();
    }

    // Called by ClickManager via ContainerClickHandler.
    // Top box → DOJump to a slot. Any other box → DOShakePosition.
    public void OnBoxClicked(Container clicked)
    {
        if (clicked == null || stack.Count == 0) return;

        if (clicked != stack[0])
        {
            clicked.Shake();
            return;
        }

        if (busy) return;

        var slotRow = gm != null ? gm.slotRow : null;
        if (slotRow == null || !slotRow.HasEmptySlot)
        {
            clicked.Shake();
            return;
        }

        busy = true;
        var top = stack[0];
        stack.RemoveAt(0);

        var handler = top.GetComponent<ContainerClickHandler>();
        if (handler != null) handler.lane = null;
        var col = top.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        top.transform.SetParent(null);
        slotRow.AcceptContainer(top);

        ShiftBoxesForward();
    }

    void ShiftBoxesForward()
    {
        if (stack.Count == 0)
        {
            busy = false;
            return;
        }

        int cap = VisibleCap;
        var seq = DOTween.Sequence();

        for (int i = 0; i < stack.Count; i++)
        {
            var b = stack[i];
            if (b == null) continue;

            if (i < cap)
            {
                bool wasQueued = b.transform.localScale.x < 0.5f;
                Vector3 target = GetOnLanePosition(i);
                if (wasQueued)
                {
                    // Queued box sliding onto the lane from spawn.
                    seq.Join(b.transform.DOScale(Vector3.one, shiftDuration).SetEase(popEase));
                    seq.Join(b.transform.DOMove(target, shiftDuration).SetEase(Ease.OutQuad));
                }
                else
                {
                    seq.Join(b.transform.DOMove(target, shiftDuration).SetEase(Ease.OutQuad));
                }
            }
            // i >= cap → still queued at spawn, no animation needed.
        }

        seq.OnComplete(() =>
        {
            busy = false;
            OpenTop();
        });
    }
}
