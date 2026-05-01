using System.Collections.Generic;
using UnityEngine;

// Manages the queue of containers sliding down one lane.
// Slot 0 = delivery end (bottom/close to camera).
// Slot 4 = spawn end (top/near generator).
public class LaneQueue : MonoBehaviour
{
    [Header("Slot transforms — 0=delivery, 4=spawn top")]
    public Transform[] slotPositions = new Transform[5];

    [Header("Where new containers appear from")]
    public Transform spawnPoint;

    public float moveSpeed = 3f;

    // Fires once when the delivery container has physically arrived at slot 0
    public System.Action<Container> OnDeliveryReady;

    private readonly List<Container> queue = new();
    private bool deliveryFired;

    public bool CanAcceptNew => queue.Count < slotPositions.Length;
    public Container Delivery => queue.Count > 0 ? queue[0] : null;

    void Update()
    {
        // Slide each container toward its target slot
        for (int i = 0; i < queue.Count && i < slotPositions.Length; i++)
        {
            if (queue[i] == null) continue;
            queue[i].transform.position = Vector3.MoveTowards(
                queue[i].transform.position,
                slotPositions[i].position,
                moveSpeed * Time.deltaTime);
        }

        // Fire delivery event once when delivery container arrives at slot 0
        if (queue.Count > 0 && !deliveryFired && queue[0] != null)
        {
            float dist = Vector3.Distance(queue[0].transform.position, slotPositions[0].position);
            if (dist < 0.08f)
            {
                deliveryFired = true;
                // Auto-remove when all cups are served
                queue[0].OnEmpty += RemoveDelivery;
                OnDeliveryReady?.Invoke(queue[0]);
            }
        }
    }

    // Called by the generator to push a new container onto the back of the queue
    public void Enqueue(Container c)
    {
        if (!CanAcceptNew) return;
        queue.Add(c);
        c.transform.SetParent(transform);
        c.transform.position = spawnPoint != null
            ? spawnPoint.position
            : slotPositions[slotPositions.Length - 1].position;
    }

    // Removes the delivery container and slides everyone forward
    public void RemoveDelivery()
    {
        if (queue.Count == 0) return;
        if (queue[0] != null) Destroy(queue[0].gameObject);
        queue.RemoveAt(0);
        deliveryFired = false;
    }
}
