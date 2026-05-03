using System.Collections;
using UnityEngine;

// Manages the player queue strip.
// Index 0 = front of queue (served first — true FIFO).
public class PlayerQueue : MonoBehaviour
{
    [Header("Stop Positions (set by Wire Scene — reposition freely)")]
    public Transform[] stopPositions;

    [Header("Spawning")]
    public GameObject playerPrefab;
    public float spawnInterval = 1.5f;

    [Header("Exit — players leave here after pickup")]
    public Transform exitPoint;

    private PlayerAgent[] queue;
    private float spawnTimer;

    void Awake()
    {
        queue = new PlayerAgent[stopPositions.Length];
    }

    void Start()
    {
        StartCoroutine(PreFill());
    }

    void Update()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            TrySpawnAtBack();
        }
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public void CheckFront()
    {
        for (int i = 0; i < stopPositions.Length; i++)
        {
            if (queue[i] == null) continue;

            var slot = GameManager.Instance?.slotRow?.FindMatchingSlot(queue[i].color);
            if (slot != null)
            {
                slot.Reserve();
                var player = queue[i];
                queue[i] = null;

                player.PickupFromSlot(slot, exitPoint, () =>
                {
                    slot.Unreserve();
                    // Clear the slot only once the container is fully empty
                    if (slot.heldContainer == null || slot.heldContainer.IsEmpty)
                        GameManager.Instance?.slotRow?.ClearSlot(slot);

                    ShiftForward();
                });
            }
            return; // Only front non-null player acts per call
        }
    }

    // ── Internal ───────────────────────────────────────────────────────────

    IEnumerator PreFill()
    {
        int preFill = Mathf.Min(8, stopPositions.Length);
        for (int i = 0; i < preFill; i++)
        {
            SpawnIntoSlot(i);
            yield return new WaitForSeconds(0.1f);
        }
    }

    void TrySpawnAtBack()
    {
        // Find the last occupied slot, then spawn one position behind it.
        // Never fill a gap left by a departing front player — that fixes LIFO behaviour.
        int lastOccupied = -1;
        for (int i = 0; i < stopPositions.Length; i++)
            if (queue[i] != null) lastOccupied = i;

        int spawnIndex = lastOccupied + 1;
        if (spawnIndex < stopPositions.Length)
            SpawnIntoSlot(spawnIndex);
    }

    void SpawnIntoSlot(int index)
    {
        if (playerPrefab == null || GameManager.Instance == null) return;

        PackageColor      color = GameManager.Instance.GetRandomColor();
        ColorDefinitionSO def   = GameManager.Instance.GetColorDef(color);

        Vector3 spawnPos = stopPositions[stopPositions.Length - 1].position + Vector3.right * 2.5f;
        var go    = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        var agent = go.GetComponent<PlayerAgent>();
        if (agent == null) { Destroy(go); return; }

        queue[index] = agent;
        agent.WalkToStop(color, def?.material, stopPositions[index]);
    }

    void ShiftForward()
    {
        int write = 0;
        for (int read = 0; read < stopPositions.Length; read++)
        {
            if (queue[read] == null) continue;
            if (read != write)
            {
                queue[write] = queue[read];
                queue[read]  = null;
                queue[write].MoveToStop(stopPositions[write]);
            }
            write++;
        }

        CheckFront();
    }
}
