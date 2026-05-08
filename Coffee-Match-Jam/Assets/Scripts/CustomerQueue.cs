using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

// Multi-customer queue.
//
// startPoint  — off-stage spawn point at the back of the queue.
// waitPoint   — index 0 of the queue. The customer here is the one being served.
// exitPoint   — where served customers walk off to.
//
// Queue line runs from waitPoint toward startPoint with `queueSpacing` between
// adjacent customers. Up to `queueLength` customers are visible at once; new
// ones spawn at startPoint and walk to the back of the line.
public class CustomerQueue : MonoBehaviour
{
    [Header("Queue points")]
    public Transform startPoint;
    public Transform waitPoint;
    public Transform exitPoint;

    [Header("Queue layout")]
    [Tooltip("Distance between adjacent customers in the queue.")]
    public float queueSpacing = 1.0f;
    [Tooltip("Max customers visible in the queue at any time.")]
    public int queueLength = 4;

    [Header("Customer prefab")]
    public GameObject customerPrefab;

    [Header("Animation")]
    public float walkDuration  = 0.5f;
    public float spawnDuration = 0.3f;
    public float exitDuration  = 0.55f;

    [Header("Lose detection")]
    [Tooltip("If the queue is full, slot row is full, and the front customer cannot be served for this many seconds, fire OnDeadlock.")]
    public float deadlockGrace = 1.5f;

    readonly List<Customer> queue = new();
    int spawnedSoFar;
    int servedSoFar;
    int totalToSpawn;
    float deadlockTimer;

    public int  ServedCount    => servedSoFar;
    public int  SpawnedCount   => spawnedSoFar;
    public int  QueueOccupancy => queue.Count;
    public bool IsQueueFull    => queue.Count >= queueLength;

    public event Action OnAllServed;
    public event Action OnDeadlock;

    public void Initialize(int count)
    {
        totalToSpawn = count;
        spawnedSoFar = 0;
        servedSoFar  = 0;
        queue.Clear();
        deadlockTimer = 0f;
        FillQueue();
    }

    public void OnSlotsChanged()
    {
        deadlockTimer = 0f;
        TryServe();
    }

    void Update()
    {
        if (totalToSpawn <= 0 || servedSoFar >= totalToSpawn) return;
        if (!IsDeadlockState()) { deadlockTimer = 0f; return; }

        deadlockTimer += Time.deltaTime;
        if (deadlockTimer >= deadlockGrace)
        {
            deadlockTimer = 0f;
            OnDeadlock?.Invoke();
        }
    }

    // Stuck = slot row has no free space AND the front customer's colour
    // isn't represented in any slot. The player can't move boxes (slots full)
    // and can't drain the queue (no match), so the level is unwinnable.
    bool IsDeadlockState()
    {
        if (queue.Count == 0) return false;

        var slotRow = GameManager.Instance != null ? GameManager.Instance.slotRow : null;
        if (slotRow == null) return false;
        if (!slotRow.IsFull) return false;

        var front = queue[0];
        if (front == null || !front.IsReady) return false;

        return slotRow.FindMatchingSlot(front.color) == null;
    }

    void FillQueue()
    {
        while (queue.Count < queueLength && spawnedSoFar < totalToSpawn)
            SpawnAtBack();
    }

    void SpawnAtBack()
    {
        if (waitPoint == null || customerPrefab == null) return;
        if (GameManager.Instance == null) return;

        var color = GameManager.Instance.GetRandomColor();
        var def   = GameManager.Instance.GetColorDef(color);

        Vector3 spawnPos = startPoint != null ? startPoint.position : QueuePos(queue.Count);

        var go = Instantiate(customerPrefab, spawnPos, Quaternion.identity, transform);
        var c  = go.GetComponent<Customer>();
        if (c == null) c = go.AddComponent<Customer>();
        c.Init(color, def != null ? def.material : null);
        c.IsReady = false;

        go.transform.localScale = Vector3.zero;
        go.transform.DOScale(Vector3.one, spawnDuration).SetEase(Ease.OutBack);

        queue.Add(c);
        spawnedSoFar++;

        WalkTo(c, queue.Count - 1);
    }

    Vector3 QueuePos(int idx)
    {
        if (waitPoint == null) return Vector3.zero;
        Vector3 dir = startPoint != null
            ? (startPoint.position - waitPoint.position).normalized
            : Vector3.back;
        return waitPoint.position + dir * (queueSpacing * idx);
    }

    void WalkTo(Customer c, int idx)
    {
        if (c == null) return;
        c.IsReady = false;
        c.transform
            .DOMove(QueuePos(idx), walkDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                if (c == null) return;
                if (queue.Count > 0 && queue[0] == c)
                {
                    c.IsReady = true;
                    TryServe();
                }
            });
    }

    void TryServe()
    {
        if (queue.Count == 0) return;
        var front = queue[0];
        if (front == null || front.IsServing || !front.IsReady) return;

        var slotRow = GameManager.Instance != null ? GameManager.Instance.slotRow : null;
        if (slotRow == null) return;

        var slot = slotRow.FindMatchingSlot(front.color);
        if (slot == null) return;

        var box = slot.heldContainer;
        if (box == null) return;

        StartCoroutine(ServeFlow(front, slot, box));
    }

    IEnumerator ServeFlow(Customer customer, ContainerSlot slot, Container box)
    {
        yield return customer.ReceiveOneCan(box);

        if (box != null && box.IsEmpty)
        {
            Destroy(box.gameObject);
            slot.Clear();
        }

        servedSoFar++;

        queue.RemoveAt(0);
        Vector3 exitPos = exitPoint != null
            ? exitPoint.position
            : customer.transform.position + Vector3.left * 8f;

        customer.transform
            .DOMove(exitPos, exitDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() => { if (customer != null) Destroy(customer.gameObject); });

        for (int i = 0; i < queue.Count; i++)
            if (queue[i] != null) WalkTo(queue[i], i);

        FillQueue();

        if (servedSoFar >= totalToSpawn)
            OnAllServed?.Invoke();
    }
}
