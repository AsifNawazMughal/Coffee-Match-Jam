using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

// Manages a stack of boxes on one lane.
// Slot 0 = front (closest to slots row); last slot = near the generator.
// Only the front (top) box is interactable.
public class LaneController : MonoBehaviour
{
    [Header("Slot transforms: 0=front, last=near generator")]
    public Transform[] slotPositions = new Transform[5];
    public Transform spawnPoint;

    [Header("Animation tuning")]
    public float arrivalDuration = 0.45f;
    public float arrivalGap      = 0.18f;
    public float shiftDuration   = 0.30f;

    private readonly List<Container> stack = new();
    private bool busy;
    private GameManager gm;

    public int  BoxCount => stack.Count;
    public bool HasBoxes => stack.Count > 0;

    public void Initialize(GameManager manager, int boxCount)
    {
        gm = manager;
        StartCoroutine(SpawnAndArrange(boxCount));
    }

    IEnumerator SpawnAndArrange(int boxCount)
    {
        for (int i = 0; i < boxCount; i++)
        {
            var box = SpawnBox();
            if (box != null) stack.Add(box);
        }

        int slotCount = slotPositions.Length;
        int visible   = Mathf.Min(stack.Count, slotCount);

        // Animate visible boxes one-by-one from spawn point to their slot.
        for (int i = 0; i < visible; i++)
        {
            var b = stack[i];
            if (b == null) continue;
            b.transform.position = spawnPoint != null ? spawnPoint.position : slotPositions[slotCount - 1].position;
            b.transform.DOMove(slotPositions[i].position, arrivalDuration).SetEase(Ease.OutQuad);
            yield return new WaitForSeconds(arrivalGap);
        }

        yield return new WaitForSeconds(arrivalDuration);
        OpenTopAndEnableClick();
    }

    Container SpawnBox()
    {
        if (gm == null) return null;
        var (prefab, typeSO) = gm.GetRandomContainerSpec();
        if (prefab == null) return null;

        Vector3 pos = spawnPoint != null
            ? spawnPoint.position
            : slotPositions[slotPositions.Length - 1].position;

        var go  = Instantiate(prefab, pos, Quaternion.identity, transform);
        var box = go.GetComponent<Container>();
        if (box == null) { Destroy(go); return null; }

        PackageColor color = gm.GetRandomColor();
        var def = gm.GetColorDef(color);
        box.Init(color, typeSO, def != null ? def.material : null);

        var col = box.GetComponent<Collider>();
        if (col != null) col.enabled = false;
        return box;
    }

    void OpenTopAndEnableClick()
    {
        if (stack.Count == 0) return;
        var top = stack[0];
        if (top == null) return;

        top.Open();

        var handler = top.GetComponent<ContainerClickHandler>();
        if (handler != null) handler.lane = this;
        var col = top.GetComponent<Collider>();
        if (col != null) col.enabled = true;
    }

    // Called by ClickManager via ContainerClickHandler.
    public void OnTopBoxClicked()
    {
        if (busy || stack.Count == 0) return;

        var slotRow = gm != null ? gm.slotRow : null;
        if (slotRow == null || !slotRow.HasEmptySlot) return;

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
        int slotCount = slotPositions.Length;
        if (stack.Count == 0)
        {
            busy = false;
            return;
        }

        var seq = DOTween.Sequence();
        for (int i = 0; i < stack.Count; i++)
        {
            var b = stack[i];
            if (b == null) continue;

            Vector3 target = i < slotCount
                ? slotPositions[i].position
                : (spawnPoint != null ? spawnPoint.position : slotPositions[slotCount - 1].position);
            seq.Join(b.transform.DOMove(target, shiftDuration).SetEase(Ease.OutQuad));
        }
        seq.OnComplete(() =>
        {
            busy = false;
            OpenTopAndEnableClick();
        });
    }
}
