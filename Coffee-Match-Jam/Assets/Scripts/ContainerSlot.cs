using UnityEngine;

// One holding slot in the slot row.
public class ContainerSlot : MonoBehaviour
{
    public Container heldContainer { get; private set; }
    public bool IsReserved { get; private set; }

    public bool IsEmpty     => heldContainer == null;
    public bool IsAvailable => heldContainer == null && !IsReserved;

    public void Reserve()   => IsReserved = true;
    public void Unreserve() => IsReserved = false;

    public void Place(Container c)
    {
        heldContainer = c;
        IsReserved    = false;
        c.transform.SetParent(transform);
        c.transform.localPosition = new Vector3(0f, 0.15f, 0f);
        c.transform.localRotation = Quaternion.identity;

        var handler = c.GetComponent<ContainerClickHandler>();
        if (handler != null) handler.lane = null;
        var col = c.GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    public void Clear()
    {
        heldContainer = null;
        IsReserved    = false;
    }
}
