using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Lanes & Generators")]
    public LaneController[]         lanes;
    public ContainerGeneratorCtrl[] generators;

    [Header("Slot Row")]
    public SlotRowManager slotRow;

    [Header("Player Queue Strip")]
    public PlayerQueue playerQueue;

    [Header("ScriptableObjects")]
    public ColorDefinitionSO[]  colorDefinitions;
    public ContainerTypeSO      containerTypeSmall;
    public ContainerTypeSO      containerTypeLarge;

    [Header("Container Prefabs")]
    public GameObject containerSmallPrefab;
    public GameObject containerLargePrefab;

    [Header("Game Settings")]
    [Tooltip("How many stop positions exist in the player queue strip")]
    public int playerQueueSize = 15;
    [Tooltip("How many containers can sit in the holding slots row")]
    public int slotRowSize = 7;

    private int score;

    void Awake() => Instance = this;

    void Start()
    {
        foreach (var gen in generators)
            gen.Initialize(this);
    }

    public void OnPlayerServed()
    {
        score++;
        Debug.Log($"Score: {score}");
    }

    public ColorDefinitionSO GetColorDef(PackageColor color)
    {
        foreach (var def in colorDefinitions)
            if (def.colorType == color) return def;
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
