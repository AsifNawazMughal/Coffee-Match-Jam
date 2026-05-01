using System.Collections;
using UnityEngine;

// Manages the player queue strip.
// When a container becomes ready, spawns N players of that color (N = cup count).
public class PlayerStripCtrl : MonoBehaviour
{
    public GameObject playerPrefab;
    public Transform  stripSpawnPoint; // right edge, players enter from here
    public Transform  playerExitPoint;

    // Called by GameManager when a delivery container is ready
    public void SpawnPlayersFor(Container container, ColorDefinitionSO colorDef)
    {
        int count = container.TotalCups;
        for (int i = 0; i < count; i++)
            StartCoroutine(SpawnOne(i * 0.4f, container, colorDef));
    }

    IEnumerator SpawnOne(float delay, Container container, ColorDefinitionSO colorDef)
    {
        yield return new WaitForSeconds(delay);
        if (playerPrefab == null || container == null) yield break;

        // Spread players slightly so they don't overlap
        Vector3 spawnPos = stripSpawnPoint != null
            ? stripSpawnPoint.position + Vector3.right * Random.Range(-0.5f, 0.5f)
            : container.transform.position + new Vector3(5f, 0f, 3f);

        var go    = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        var agent = go.GetComponent<PlayerAgent>();
        if (agent == null) { Destroy(go); yield break; }

        agent.Init(colorDef, container, playerExitPoint);
    }
}
