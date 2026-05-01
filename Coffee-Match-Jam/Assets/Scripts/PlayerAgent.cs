using System;
using System.Collections;
using UnityEngine;

public class PlayerAgent : MonoBehaviour
{
    [HideInInspector] public PackageColor color;
    public float moveSpeed = 3.5f;

    public void WalkToStop(PackageColor col, Material mat, Transform stop)
    {
        color = col;
        TintSelf(mat);
        StartCoroutine(WalkTo(stop.position));
    }

    public void MoveToStop(Transform newStop)
    {
        StartCoroutine(WalkTo(newStop.position));
    }

    public void PickupFromSlot(ContainerSlot slot, Transform exitPoint, Action onComplete)
    {
        StartCoroutine(PickupRoutine(slot, exitPoint, onComplete));
    }

    IEnumerator PickupRoutine(ContainerSlot slot, Transform exitPoint, Action onComplete)
    {
        var container = slot.heldContainer;

        if (container != null)
        {
            // Take exactly ONE cup — container stays in slot for the next player
            Transform cup = container.TakeCup();
            if (cup != null)
                yield return FlyToPlayer(cup);

            // Destroy empty container shell; slot cleared by onComplete below
            if (container.IsEmpty)
                Destroy(container.gameObject);
        }

        onComplete?.Invoke();
        GameManager.Instance?.OnPlayerServed();

        if (exitPoint != null)
            yield return WalkTo(exitPoint.position);

        Destroy(gameObject);
    }

    // Cup arcs through the air from the container slot to the player
    IEnumerator FlyToPlayer(Transform cup)
    {
        Vector3 start  = cup.position;
        Vector3 target = transform.position + Vector3.up * 0.6f;
        float t = 0f;
        const float dur = 0.35f;

        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float arc = Mathf.Sin(t * Mathf.PI) * 0.9f;
            Vector3 dest = transform.position + Vector3.up * 0.6f;
            cup.position = Vector3.Lerp(start, dest, t) + Vector3.up * arc;
            yield return null;
        }

        cup.SetParent(transform);
        cup.localPosition = Vector3.up * 0.6f;
        cup.localRotation = Quaternion.identity;
        cup.localScale    = Vector3.one * 0.75f;
    }

    void TintSelf(Material mat)
    {
        if (mat == null) return;
        foreach (Transform child in transform)
        {
            var mr = child.GetComponent<MeshRenderer>();
            if (mr != null) mr.material = mat;
        }
    }

    IEnumerator WalkTo(Vector3 target)
    {
        target.y = transform.position.y;
        while (Vector3.Distance(transform.position, target) > 0.1f)
        {
            Vector3 dir = (target - transform.position).normalized;
            transform.position += dir * moveSpeed * Time.deltaTime;
            if (dir != Vector3.zero)
                transform.forward = Vector3.Lerp(transform.forward, dir, 15f * Time.deltaTime);
            yield return null;
        }
    }
}
