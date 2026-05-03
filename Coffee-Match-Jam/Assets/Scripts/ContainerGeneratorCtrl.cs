using UnityEngine;

// Marker on each lane's "Generator" GameObject. The visual yellow machine.
// All actual spawning is now driven by LaneController; this exists only so
// existing scene/prefab references don't break.
public class ContainerGeneratorCtrl : MonoBehaviour
{
    public LaneController targetLane;
}
