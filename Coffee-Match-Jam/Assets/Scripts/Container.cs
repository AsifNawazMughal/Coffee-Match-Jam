using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

// Sits on the Container_Small / Container_Large prefabs.
// Holds a stack of "cans" (capsules) and a Lid that opens with DOTween.
public class Container : MonoBehaviour
{
    [HideInInspector] public PackageColor color;
    [HideInInspector] public ContainerTypeSO type;

    Transform lid;
    Vector3 lidStartPos;
    Vector3 lidStartScale;
    bool lidCaptured;

    public bool IsOpen { get; private set; }

    // World-space length of the box along its local +Z axis. Cached once.
    public float Length { get; private set; } = 1f;

    void Awake()
    {
        // Compute the visible Z extent from all child renderers. The BoxCollider
        // only spans the interior cavity, so reading from it leaves the wall
        // thickness out and adjacent boxes end up overlapping.
        var renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);
            Length = b.size.z;
        }
        else if (TryGetComponent<BoxCollider>(out var col))
        {
            Length = col.size.z * transform.localScale.z;
        }
    }

    public void Init(PackageColor col, ContainerTypeSO containerType, Material colorMat)
    {
        color = col;
        type  = containerType;

        CaptureLid();

        // Tint walls, lid, and cans with the box color so the cans visually
        // match the container they came from.
        if (colorMat != null)
        {
            foreach (Transform child in transform)
            {
                var mr = child.GetComponent<MeshRenderer>();
                if (mr != null) mr.material = colorMat;
            }
        }

        SetClosedImmediate();
    }

    void CaptureLid()
    {
        if (lidCaptured) return;
        lid = transform.Find("Lid");
        if (lid != null)
        {
            lidStartPos   = lid.localPosition;
            lidStartScale = lid.localScale;
        }
        lidCaptured = true;
    }

    public void SetClosedImmediate()
    {
        IsOpen = false;
        if (lid == null) return;
        lid.DOKill();
        lid.gameObject.SetActive(true);
        lid.localPosition = lidStartPos;
        lid.localScale    = lidStartScale;
        lid.localRotation = Quaternion.identity;
    }

    // Wiggle in place — used to reject clicks on non-top boxes.
    public Tween Shake()
    {
        return transform
            .DOShakePosition(0.35f, new Vector3(0.12f, 0f, 0.12f), 14, 90f, false, true)
            .SetEase(Ease.OutQuad);
    }

    // Detach and return one can. Returns null when no cans remain.
    public Transform TakeOneCan()
    {
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Can"))
            {
                child.SetParent(null);
                return child;
            }
        }
        return null;
    }

    public bool IsEmpty
    {
        get
        {
            foreach (Transform child in transform)
                if (child.name.StartsWith("Can")) return false;
            return true;
        }
    }

    // Animate the lid jumping up and shrinking out, revealing the cans inside.
    public Tween Open()
    {
        if (IsOpen) return null;
        IsOpen = true;
        CaptureLid();
        if (lid == null) return null;

        lid.DOKill();
        var seq = DOTween.Sequence();
        seq.Append(lid.DOLocalMoveY(lidStartPos.y + 0.7f, 0.25f).SetEase(Ease.OutQuad));
        seq.Join(lid.DOLocalRotate(new Vector3(0f, 0f, 35f), 0.25f, RotateMode.LocalAxisAdd));
        seq.Append(lid.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InQuad));
        seq.OnComplete(() => { if (lid != null) lid.gameObject.SetActive(false); });
        return seq;
    }
}
