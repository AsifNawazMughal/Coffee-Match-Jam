using UnityEngine;

// Central coordinator. Wired automatically by SceneWirer.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Lanes & Generators")]
    public LaneQueue[]              lanes;
    public ContainerGeneratorCtrl[] generators;

    [Header("Player Strip")]
    public PlayerStripCtrl playerStrip;

    [Header("ScriptableObjects")]
    public ColorDefinitionSO[]  colorDefinitions; // Red, Blue, Yellow
    public ContainerTypeSO      containerTypeSmall;
    public ContainerTypeSO      containerTypeLarge;

    [Header("Container Prefabs")]
    public GameObject containerSmallPrefab;
    public GameObject containerLargePrefab;

    private int score;

    void Awake() => Instance = this;

    void Start()
    {
        foreach (var lane in lanes)
            lane.OnDeliveryReady += OnDeliveryReady;

        foreach (var gen in generators)
            gen.Initialize(this);
    }

    // ── Events ─────────────────────────────────────────────────────────────

    void OnDeliveryReady(Container container)
    {
        if (playerStrip == null) return;
        var colorDef = GetColorDef(container.color);
        if (colorDef == null) return;
        // Number of players spawned = number of cups in this container
        playerStrip.SpawnPlayersFor(container, colorDef);
    }

    public void OnPlayerServed()
    {
        score++;
        Debug.Log($"Score: {score}");
    }

    // ── Queries ─────────────────────────────────────────────────────────────

    public ColorDefinitionSO GetColorDef(PackageColor color)
    {
        foreach (var def in colorDefinitions)
            if (def.colorType == color) return def;
        return null;
    }

    public PackageColor GetRandomColor()
    {
        int i = Random.Range(0, colorDefinitions.Length);
        return colorDefinitions[i].colorType;
    }

    // Returns a random container prefab + matching type SO
    public (GameObject prefab, ContainerTypeSO type) GetRandomContainerSpec()
    {
        bool small = Random.value < 0.5f;
        return small
            ? (containerSmallPrefab, containerTypeSmall)
            : (containerLargePrefab, containerTypeLarge);
    }
}
