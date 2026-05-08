using UnityEngine;
using DG.Tweening;

// Manages the holding slots row between the lanes and the camera.
public class SlotRowManager : MonoBehaviour
{
    public ContainerSlot[] slots;

    [Header("Animation tuning")]
    public float flyDuration  = 0.5f;
    [Tooltip("World-space arc height. Tune up if the curve isn't visible at the camera angle.")]
    public float flyJumpPower = 2f;

    public bool HasEmptySlot
    {
        get
        {
            if (slots == null) return false;
            foreach (var s in slots) if (s != null && s.IsAvailable) return true;
            return false;
        }
    }

    public bool IsFull
    {
        get
        {
            if (slots == null || slots.Length == 0) return false;
            foreach (var s in slots) if (s != null && s.IsAvailable) return false;
            return true;
        }
    }

    public ContainerSlot AcceptContainer(Container c)
    {
        foreach (var slot in slots)
        {
            if (slot == null || !slot.IsAvailable) continue;
            slot.Reserve();
            FlyToSlot(c, slot);
            return slot;
        }
        Debug.LogWarning("No empty slot — container dropped.");
        Object.Destroy(c.gameObject);
        return null;
    }

    void FlyToSlot(Container c, ContainerSlot slot)
    {
        var col = c.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Vector3 target = slot.transform.position + Vector3.up * 0.15f;
        // No SetEase on the returned Sequence — DOJump already bakes a Y arc;
        // overriding the ease remaps time and visibly flattens the curve.
        c.transform
            .DOJump(target, flyJumpPower, 1, flyDuration)
            .OnComplete(() =>
            {
                if (c == null) { slot.Unreserve(); return; }
                slot.Place(c); // also clears the reservation
                if (GameManager.Instance != null && GameManager.Instance.customerQueue != null)
                    GameManager.Instance.customerQueue.OnSlotsChanged();
            });
    }

    public void ClearSlot(ContainerSlot slot) => slot.Clear();

    // Returns the first slot whose held container matches the requested colour.
    public ContainerSlot FindMatchingSlot(PackageColor color)
    {
        if (slots == null) return null;
        foreach (var slot in slots)
            if (slot != null && !slot.IsEmpty && slot.heldContainer.color == color)
                return slot;
        return null;
    }
}
