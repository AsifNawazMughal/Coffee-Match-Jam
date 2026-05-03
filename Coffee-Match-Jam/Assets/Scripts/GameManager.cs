using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Lanes")]
    public LaneController[] lanes;

    [Header("Slot Row")]
    public SlotRowManager slotRow;

    [Header("ScriptableObjects")]
    public ColorDefinitionSO[] colorDefinitions;
    public ContainerTypeSO     containerTypeSmall;
    public ContainerTypeSO     containerTypeLarge;

    [Header("Container Prefabs")]
    public GameObject containerSmallPrefab;
    public GameObject containerLargePrefab;

    [Header("Game Settings")]
    [Tooltip("How many boxes spawn on each lane at level start")]
    [SerializeField] private int boxesPerLane = 4;
    [Tooltip("How many containers can sit in the holding slots row")]
    public int slotRowSize = 7;

    public int BoxesPerLane => boxesPerLane;

    void Awake() => Instance = this;

    void Start()
    {
        if (lanes == null) return;
        foreach (var lane in lanes)
            if (lane != null) lane.Initialize(this, boxesPerLane);
    }

    public ColorDefinitionSO GetColorDef(PackageColor color)
    {
        foreach (var def in colorDefinitions)
            if (def != null && def.colorType == color) return def;
        return null;
    }

    public PackageColor GetRandomColor()
    {
        return colorDefinitions[Random.Range(0, colorDefinitions.Length)].colorType;
    }

    public (GameObject prefab, ContainerTypeSO type) GetRandomContainerSpec()
    {
        bool small = Random.value < 0.5f;
        return small
            ? (containerSmallPrefab, containerTypeSmall)
            : (containerLargePrefab, containerTypeLarge);
    }
}
