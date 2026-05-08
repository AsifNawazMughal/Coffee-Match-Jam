using System.Collections;
using UnityEngine;
using DG.Tweening;

// One customer in the queue. Wants one can of a specific colour. When matched
// the can DOJumps from the box to the customer, and the customer walks off.
public class Customer : MonoBehaviour
{
    [HideInInspector] public PackageColor color;

    public bool IsServing { get; private set; }

    // Set true once the customer has finished walking to the front of the queue.
    [HideInInspector] public bool IsReady;

    [Header("Can flight")]
    public float canJumpPower   = 1.0f;
    public float canFlyDuration = 0.45f;

    public void Init(PackageColor col, Material mat)
    {
        color = col;
        if (mat == null) return;
        foreach (Transform child in transform)
        {
            var mr = child.GetComponent<MeshRenderer>();
            if (mr != null) mr.material = mat;
        }
    }

    // Take exactly one can from the matching box and have it fly here on a DOJump arc.
    public IEnumerator ReceiveOneCan(Container box)
    {
        IsServing = true;

        var can = box != null ? box.TakeOneCan() : null;
        if (can != null)
        {
            Vector3 target = transform.position + Vector3.up * 0.85f;
            can.DOJump(target, canJumpPower, 1, canFlyDuration);
            yield return new WaitForSeconds(canFlyDuration);
            if (can != null) Destroy(can.gameObject);
        }

        IsServing = false;
    }
}
