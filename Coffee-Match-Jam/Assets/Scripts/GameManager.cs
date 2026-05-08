using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Lanes")]
    public LaneController[] lanes;

    [Header("Slot Row")]
    public SlotRowManager slotRow;

    [Header("Customers")]
    public CustomerQueue customerQueue;

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
    [Tooltip("Total number of customers spawned across the level")]
    [SerializeField] private int customerCount = 8;

    [Header("Level / Meta")]
    [SerializeField] private int   levelNumber   = 1;
    [SerializeField] private float levelDuration = 60f;
    [SerializeField] private int   coinsPerCustomer = 5;

    public int   BoxesPerLane     => boxesPerLane;
    public int   CustomerCount    => customerCount;
    public int   LevelNumber      => levelNumber;
    public float LevelDuration    => levelDuration;
    public int   CoinsPerCustomer => coinsPerCustomer;
    public int   LevelReward      => customerCount * coinsPerCustomer;
    public int   Coins            { get; private set; }
    public float TimeRemaining    { get; private set; }
    public bool  LevelActive      { get; private set; }

    public event Action<int>   OnCoinsChanged;
    public event Action<float> OnTimerTick;
    public event Action        OnWin;
    public event Action        OnLose;
    public event Action        OnPaused;
    public event Action        OnResumed;

    void Awake() => Instance = this;

    void Start()
    {
        Time.timeScale = 1f;

        if (lanes != null)
            foreach (var lane in lanes)
                if (lane != null) lane.Initialize(this, boxesPerLane);

        if (customerQueue != null)
        {
            customerQueue.OnAllServed += HandleAllServed;
            customerQueue.OnDeadlock  += HandleDeadlock;
            customerQueue.Initialize(customerCount);
        }

        TimeRemaining = levelDuration;
        LevelActive   = true;
    }

    void Update()
    {
        if (!LevelActive) return;

        TimeRemaining -= Time.deltaTime;
        if (TimeRemaining <= 0f)
        {
            TimeRemaining = 0f;
            OnTimerTick?.Invoke(TimeRemaining);
            TriggerLose();
            return;
        }

        OnTimerTick?.Invoke(TimeRemaining);
    }

    public void AddCoins(int amount)
    {
        if (amount == 0) return;
        Coins += amount;
        OnCoinsChanged?.Invoke(Coins);
    }

    public void Pause()
    {
        if (!LevelActive) return;
        Time.timeScale = 0f;
        OnPaused?.Invoke();
    }

    public void Resume()
    {
        if (!LevelActive) return;
        Time.timeScale = 1f;
        OnResumed?.Invoke();
    }

    void HandleAllServed() => TriggerWin();
    void HandleDeadlock()  => TriggerLose();

    void TriggerWin()
    {
        if (!LevelActive) return;
        LevelActive = false;
        Time.timeScale = 0f;
        OnWin?.Invoke();
    }

    void TriggerLose()
    {
        if (!LevelActive) return;
        LevelActive = false;
        Time.timeScale = 0f;
        OnLose?.Invoke();
    }

    public ColorDefinitionSO GetColorDef(PackageColor color)
    {
        foreach (var def in colorDefinitions)
            if (def != null && def.colorType == color) return def;
        return null;
    }

    public PackageColor GetRandomColor()
    {
        return colorDefinitions[UnityEngine.Random.Range(0, colorDefinitions.Length)].colorType;
    }

    public (GameObject prefab, ContainerTypeSO type) GetRandomContainerSpec()
    {
        bool small = UnityEngine.Random.value < 0.5f;
        return small
            ? (containerSmallPrefab, containerTypeSmall)
            : (containerLargePrefab, containerTypeLarge);
    }
}
