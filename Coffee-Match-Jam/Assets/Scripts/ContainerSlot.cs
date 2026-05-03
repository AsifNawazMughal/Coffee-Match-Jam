using UnityEngine;

// One holding slot in the slot row.
public class ContainerSlot : MonoBehaviour
{
    public Container heldContainer { get; private set; }
    public bool isReserved { get; private set; }
    public bool IsEmpty => heldContainer == null;

    public void Reserve()   => isReserved = true;
    public void Unreserve() => isReserved = false;

    public void Place(Container c)
    {
        heldContainer = c;
        c.transform.SetParent(transform);
        c.transform.localPosition = new Vector3(0f, 0.15f, 0f);
        c.transform.localRotation = Quaternion.identity;

        // Disable click while in slot (not on a lane anymore)
        var handler = c.GetComponent<ContainerClickHandler>();
        if (handler != null) { handler.lane = null; }
        var col = c.GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    public void Clear()
    {
        heldContainer = null;
        isReserved = false;
    }
}
