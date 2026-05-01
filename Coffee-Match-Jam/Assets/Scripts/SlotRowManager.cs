using System.Collections;
using UnityEngine;

// Manages all 7 holding slots between the player strip and the lanes.
public class SlotRowManager : MonoBehaviour
{
    public ContainerSlot[] slots;

    public bool HasEmptySlot
    {
        get { foreach (var s in slots) if (s.IsEmpty) return true; return false; }
    }

    // Called when user clicks a lane's front container
    public void AcceptContainer(Container c)
    {
        foreach (var slot in slots)
        {
            if (!slot.IsEmpty) continue;
            StartCoroutine(FlyToSlot(c, slot));
            return;
        }
        Debug.LogWarning("No empty slot — container dropped.");
        Object.Destroy(c.gameObject);
    }

    IEnumerator FlyToSlot(Container c, ContainerSlot slot)
    {
        // Disable click immediately so it can't be double-tapped mid-flight
        var col = c.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Vector3 start  = c.transform.position;
        Vector3 target = slot.transform.position + Vector3.up * 0.15f;
        float   t      = 0f;
        const float duration = 0.35f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            // Arc: lift in first half, drop in second half
            float arc = Mathf.Sin(t * Mathf.PI) * 0.6f;
            c.transform.position = Vector3.Lerp(start, target, t) + Vector3.up * arc;
            yield return null;
        }

        slot.Place(c);
        GameManager.Instance?.playerQueue?.CheckFront();
    }

    public void ClearSlot(ContainerSlot slot)
    {
        slot.Clear();
    }

    // Returns first slot whose container color matches
    public ContainerSlot FindMatchingSlot(PackageColor color)
    {
        foreach (var slot in slots)
            if (!slot.IsEmpty && slot.heldContainer.color == color)
                return slot;
        return null;
    }
}
