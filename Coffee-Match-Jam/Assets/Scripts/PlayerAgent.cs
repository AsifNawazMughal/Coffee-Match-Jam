using System.Collections;
using UnityEngine;

// One colored player that walks to a container, takes a cup, and exits.
public class PlayerAgent : MonoBehaviour
{
    public float moveSpeed = 3.5f;

    private Container targetContainer;
    private Transform exitPoint;

    public void Init(ColorDefinitionSO colorDef, Container container, Transform exit)
    {
        targetContainer = container;
        exitPoint       = exit;
        TintSelf(colorDef?.material);
        StartCoroutine(Routine());
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

    IEnumerator Routine()
    {
        // Walk to delivery container
        if (targetContainer != null)
        {
            Vector3 dest = targetContainer.transform.position + Vector3.back * 0.7f;
            yield return WalkTo(dest);

            // Take one cup and carry it
            Transform cup = targetContainer.TakeCup();
            if (cup != null)
            {
                cup.SetParent(transform);
                cup.localPosition = Vector3.up * 1.0f;
                cup.localRotation = Quaternion.identity;
                cup.localScale    = Vector3.one * 0.5f;
            }

            GameManager.Instance?.OnPlayerServed();
        }

        // Walk to exit and clean up
        if (exitPoint != null)
            yield return WalkTo(exitPoint.position);

        Destroy(gameObject);
    }

    IEnumerator WalkTo(Vector3 target)
    {
        target.y = transform.position.y;
        while (Vector3.Distance(transform.position, target) > 0.12f)
        {
            Vector3 dir = (target - transform.position).normalized;
            transform.position += dir * moveSpeed * Time.deltaTime;
            if (dir != Vector3.zero)
                transform.forward = Vector3.Lerp(transform.forward, dir, 15f * Time.deltaTime);
            yield return null;
        }
    }
}
