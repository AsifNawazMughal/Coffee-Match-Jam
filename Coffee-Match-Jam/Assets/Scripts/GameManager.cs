using System;
using System.Collections.Generic;
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

    struct Move
    {
        public LaneController lane;
        public Container box;
        public ContainerSlot slot;
    }
    readonly Stack<Move> moveHistory = new();

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
        Coins = Mathf.Max(0, Coins + amount);
        OnCoinsChanged?.Invoke(Coins);
    }

    public bool SpendCoins(int amount)
    {
        if (amount <= 0) return true;
        if (Coins < amount) return false;
        Coins -= amount;
        OnCoinsChanged?.Invoke(Coins);
        return true;
    }

    public void AddTime(float seconds)
    {
        if (!LevelActive || seconds == 0f) return;
        TimeRemaining += seconds;
        OnTimerTick?.Invoke(TimeRemaining);
    }

    public void RecordMove(LaneController lane, Container box, ContainerSlot slot)
    {
        if (lane == null || box == null || slot == null) return;
        moveHistory.Push(new Move { lane = lane, box = box, slot = slot });
    }

    // True if at least one move on the history can still be undone (the box
    // hasn't been taken by a customer). Used by PowerUpController to decide
    // whether to consume a charge before calling UndoLastMove.
    public bool UndoLastMoveDryRun()
    {
        foreach (var m in moveHistory)
        {
            if (m.box == null || m.lane == null || m.slot == null) continue;
            if (m.slot.heldContainer != m.box) continue;
            return true;
        }
        return false;
    }

    // Pops history until a valid move is found and reverses it: the box leaves
    // the slot and re-enters the lane at the front. Returns false if no
    // recoverable move exists (e.g. the box was already taken by a customer).
    public bool UndoLastMove()
    {
        while (moveHistory.Count > 0)
        {
            var m = moveHistory.Pop();
            if (m.box == null || m.lane == null || m.slot == null) continue;
            if (m.slot.heldContainer != m.box) continue;

            m.slot.Clear();
            m.lane.InsertAtFront(m.box);
            if (customerQueue != null) customerQueue.OnSlotsChanged();
            return true;
        }
        return false;
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
