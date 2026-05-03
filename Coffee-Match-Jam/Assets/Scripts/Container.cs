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

    public void Init(PackageColor col, ContainerTypeSO containerType, Material colorMat)
    {
        color = col;
        type  = containerType;

        CaptureLid();

        // Tint walls + lid; cans (capsules) stay white.
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Can") || child.name.StartsWith("Cup")) continue;
            if (colorMat == null) continue;
            var mr = child.GetComponent<MeshRenderer>();
            if (mr != null) mr.material = colorMat;
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
