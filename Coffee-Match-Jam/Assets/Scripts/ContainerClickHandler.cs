using UnityEngine;

// Marker component on the container root.
// ClickManager on the camera reads this when a raycast hits.
public class ContainerClickHandler : MonoBehaviour
{
    [HideInInspector] public LaneController lane;
}
