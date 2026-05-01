using System.Collections.Generic;
using UnityEngine;

// Sits on Container_Small and Container_Large prefabs.
// Tracks cups inside and tints walls at runtime.
public class Container : MonoBehaviour
{
    [HideInInspector] public PackageColor color;
    [HideInInspector] public ContainerTypeSO type;

    private readonly List<Transform> cups = new();
    private int nextCup;

    public int  TotalCups => cups.Count;
    public int  Remaining => cups.Count - nextCup;
    public bool IsEmpty   => nextCup >= cups.Count;

    public System.Action OnEmpty;

    public void Init(PackageColor col, ContainerTypeSO containerType, Material colorMat)
    {
        color = col;
        type  = containerType;
        cups.Clear();
        nextCup = 0;

        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Cup"))
            {
                cups.Add(child);
            }
            else if (colorMat != null)
            {
                var mr = child.GetComponent<MeshRenderer>();
                if (mr != null) mr.material = colorMat;
            }
        }
    }

    // Detaches and returns one cup so the player can carry it
    public Transform TakeCup()
    {
        if (IsEmpty) return null;
        Transform cup = cups[nextCup++];
        cup.SetParent(null);
        if (IsEmpty) OnEmpty?.Invoke();
        return cup;
    }
}
