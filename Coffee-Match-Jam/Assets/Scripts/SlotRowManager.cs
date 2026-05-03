using UnityEngine;
using DG.Tweening;

// Manages the holding slots row between the lanes and the camera.
public class SlotRowManager : MonoBehaviour
{
    public ContainerSlot[] slots;

    [Header("Animation tuning")]
    public float flyDuration = 0.4f;
    public float flyJumpPower = 0.6f;

    public bool HasEmptySlot
    {
        get
        {
            if (slots == null) return false;
            foreach (var s in slots) if (s != null && s.IsEmpty) return true;
            return false;
        }
    }

    public void AcceptContainer(Container c)
    {
        foreach (var slot in slots)
        {
            if (slot == null || !slot.IsEmpty) continue;
            FlyToSlot(c, slot);
            return;
        }
        Debug.LogWarning("No empty slot — container dropped.");
        Object.Destroy(c.gameObject);
    }

    void FlyToSlot(Container c, ContainerSlot slot)
    {
        var col = c.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Vector3 target = slot.transform.position + Vector3.up * 0.15f;
        c.transform
            .DOJump(target, flyJumpPower, 1, flyDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => slot.Place(c));
    }

    public void ClearSlot(ContainerSlot slot) => slot.Clear();
}
