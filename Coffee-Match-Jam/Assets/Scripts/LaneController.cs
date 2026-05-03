using System.Collections.Generic;
using UnityEngine;

// Manages the queue of containers on one lane.
// Slot 0 = delivery (bottom/front, closest to camera & slots row).
// Slot 4 = near generator (top/back).
public class LaneController : MonoBehaviour
{
    [Header("Slot transforms: 0=delivery, 4=near generator")]
    public Transform[] slotPositions = new Transform[5];
    public Transform spawnPoint;
    public float moveSpeed = 3f;

    private readonly List<Container> queue = new();
    private bool deliveryClickable;

    public bool CanAcceptNew     => queue.Count < slotPositions.Length;
    public bool HasDelivery      => queue.Count > 0;

    void Update()
    {
        // Remove any containers destroyed externally
        queue.RemoveAll(c => c == null);

        // Slide each container toward its target slot
        for (int i = 0; i < queue.Count && i < slotPositions.Length; i++)
            queue[i].transform.position = Vector3.MoveTowards(
                queue[i].transform.position,
                slotPositions[i].position,
                moveSpeed * Time.deltaTime);

        // Enable click once front container arrives at delivery slot
        if (queue.Count > 0 && !deliveryClickable)
        {
            float dist = Vector3.Distance(queue[0].transform.position, slotPositions[0].position);
            if (dist < 0.08f)
            {
                deliveryClickable = true;
                SetClickable(queue[0], true);
            }
        }
    }

    public void Enqueue(Container c)
    {
        if (!CanAcceptNew) return;
        queue.Add(c);
        c.transform.SetParent(transform);
        c.transform.position = spawnPoint != null
            ? spawnPoint.position
            : slotPositions[slotPositions.Length - 1].position;
    }

    // Called when user clicks the front container
    public void OnFrontContainerClicked()
    {
        if (queue.Count == 0 || !deliveryClickable) return;

        var slotRow = GameManager.Instance?.slotRow;
        if (slotRow == null || !slotRow.HasEmptySlot) return;

        Container front = queue[0];
        SetClickable(front, false);
        front.transform.SetParent(null);
        queue.RemoveAt(0);
        deliveryClickable = false;

        slotRow.AcceptContainer(front);
    }

    void SetClickable(Container c, bool on)
    {
        var handler = c.GetComponent<ContainerClickHandler>();
        if (handler != null) handler.lane = on ? this : null;

        var col = c.GetComponent<Collider>();
        if (col != null) col.enabled = on;
    }
}
