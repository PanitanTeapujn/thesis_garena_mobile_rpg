using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections;

public class GachaSystem : MonoBehaviour
{
    [Header("System Settings")]
    public bool enableDebugLog = true;
    public bool autoAddRewardsToInventory = true;

    [Header("Gacha Machines")]
    [SerializeField] private List<GachaMachine> gachaMachines = new List<GachaMachine>();

    [Header("UI References")]
    public GachaUIManager uiManager;

    #region Singleton
    private static GachaSystem _instance;
    public static GachaSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GachaSystem>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GachaSystem");
                    _instance = go.AddComponent<GachaSystem>();
                }
            }
            return _instance;
        }
    }
    #endregion

    #region Events
    public static event Action<List<GachaReward>> OnRewardsAddedToInventory;
    public static event Action<GachaReward> OnInventoryFull;
    public static event Action<GachaMachine, List<GachaReward>> OnGachaCompleted;
    #endregion

    #region Properties
    public List<GachaMachine> AllMachines => gachaMachines;
    public int MachineCount => gachaMachines.Count;
    #endregion

    #region Initialization
    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        InitializeSystem();
    }

    private void InitializeSystem()
    {
        // ค้นหา gacha machines ใน scene
        RefreshMachineList();

        // Subscribe to events
        GachaMachine.OnGachaRolled += HandleGachaRolled;
        GachaMachine.OnRareItemObtained += HandleRareItemObtained;
        GachaMachine.OnGachaError += HandleGachaError;

        Debug.Log($" GachaSystem initialized with {gachaMachines.Count} machines");
    }

    void OnDestroy()
    {
        GachaMachine.OnGachaRolled -= HandleGachaRolled;
        GachaMachine.OnRareItemObtained -= HandleRareItemObtained;
        GachaMachine.OnGachaError -= HandleGachaError;
    }
    #endregion

    #region Machine Management
    public void RefreshMachineList()
    {
        gachaMachines.Clear();
        gachaMachines.AddRange(FindObjectsOfType<GachaMachine>());

        if (enableDebugLog)
        {
            Debug.Log($" Refreshed gacha machines: {gachaMachines.Count} found");
        }
    }

    public GachaMachine GetMachine(string machineId)
    {
        return gachaMachines.FirstOrDefault(m => m.machineId == machineId);
    }

    public GachaMachine GetMachineByName(string machineName)
    {
        return gachaMachines.FirstOrDefault(m => m.machineName == machineName);
    }

    public List<GachaMachine> GetMachinesByPool(GachaPoolData pool)
    {
        return gachaMachines.Where(m => m.Pool == pool).ToList();
    }
    #endregion

    #region Gacha Operations
    public List<GachaReward> RollGacha(string machineId, int rollCount = 1)
    {
        GachaMachine machine = GetMachine(machineId);
        if (machine == null)
        {
            Debug.LogError($" Machine '{machineId}' not found!");
            return new List<GachaReward>();
        }

        return machine.Roll(rollCount);
    }

    public List<GachaReward> RollSingle(string machineId)
    {
        return RollGacha(machineId, 1);
    }

    public List<GachaReward> RollTen(string machineId)
    {
        return RollGacha(machineId, 10);
    }
    #endregion

    #region Event Handlers
    private void HandleGachaRolled(GachaMachine machine, List<GachaReward> rewards)
    {
        if (enableDebugLog)
        {
            Debug.Log($" Gacha rolled on '{machine.machineName}': {rewards.Count} rewards");
        }

        // เพิ่ม rewards เข้า inventory
        if (autoAddRewardsToInventory)
        {
            StartCoroutine(AddRewardsToInventory(rewards));
        }

        // แจ้ง UI
        if (uiManager != null)
        {
            uiManager.ShowGachaResults(machine, rewards);
        }

        OnGachaCompleted?.Invoke(machine, rewards);
    }

    private void HandleRareItemObtained(GachaMachine machine, GachaReward reward)
    {
        if (enableDebugLog)
        {
            Debug.Log($" Rare item obtained: {reward.GetRewardText()} from '{machine.machineName}'");
        }

        // แจ้ง UI แสดง rare item effect
        if (uiManager != null)
        {
            uiManager.ShowRareItemEffect(reward);
        }
    }

    private void HandleGachaError(GachaMachine machine, string error)
    {
        Debug.LogError($" Gacha error on '{machine.machineName}': {error}");

        if (uiManager != null)
        {
            uiManager.ShowErrorMessage(error);
        }
    }
    #endregion

    #region Inventory Integration
    private IEnumerator AddRewardsToInventory(List<GachaReward> rewards)
    {
        // หา player character และ inventory
        Character playerCharacter = FindPlayerCharacter();
        if (playerCharacter == null)
        {
            Debug.LogError(" Cannot find player character for inventory");
            yield break;
        }

        Inventory inventory = playerCharacter.GetComponent<Inventory>();
        if (inventory == null)
        {
            Debug.LogError(" Player character has no inventory component");
            yield break;
        }

        List<GachaReward> addedRewards = new List<GachaReward>();
        List<GachaReward> failedRewards = new List<GachaReward>();

        foreach (GachaReward reward in rewards)
        {
            if (reward == null || !reward.IsValid()) continue;

            // ลองเพิ่มเข้า inventory
            bool success = inventory.AddItem(reward.itemData, reward.quantity);

            if (success)
            {
                addedRewards.Add(reward);
                if (enableDebugLog)
                {
                    Debug.Log($" Added to inventory: {reward.GetRewardText()}");
                }
            }
            else
            {
                failedRewards.Add(reward);
                Debug.LogWarning($" Failed to add to inventory: {reward.GetRewardText()} (Inventory may be full)");
                OnInventoryFull?.Invoke(reward);
            }

            yield return null; // รอ 1 frame ระหว่างการเพิ่ม item
        }

        if (addedRewards.Count > 0)
        {
            OnRewardsAddedToInventory?.Invoke(addedRewards);
        }

        if (failedRewards.Count > 0)
        {
            // TODO: จัดการ items ที่เพิ่มไม่ได้ (เช่น เก็บไว้ใน mailbox)
            Debug.LogWarning($" {failedRewards.Count} items could not be added to inventory");
        }
    }

    private Character FindPlayerCharacter()
    {
        // วิธีหาตัว player character - อาจจะต้องปรับตาม project
        Character[] characters = FindObjectsOfType<Character>();

        // ลองหา character ที่เป็น local player หรือมี authority
        foreach (Character character in characters)
        {
            if (character.HasStateAuthority) // Fusion Network
            {
                return character;
            }
        }

        // fallback: เอาตัวแรกที่เจอ
        return characters.Length > 0 ? characters[0] : null;
    }
    #endregion

    #region Statistics & Analytics
    public Dictionary<string, int> GetMachineUsageStats()
    {
        // TODO: implement usage tracking
        return new Dictionary<string, int>();
    }

    public List<GachaReward> GetRecentRewards(int count = 10)
    {
        // TODO: implement recent rewards tracking
        return new List<GachaReward>();
    }
    #endregion

    #region Debug & Testing
    [ContextMenu("Test All Machines")]
    public void TestAllMachines()
    {
        foreach (var machine in gachaMachines)
        {
            Debug.Log($" Testing machine: {machine.machineName}");
            machine.TestSingleRoll();
        }
    }

    [ContextMenu("Debug System Info")]
    public void DebugSystemInfo()
    {
        Debug.Log(" === GACHA SYSTEM INFO === ");
        Debug.Log($"Machines: {gachaMachines.Count}");
        Debug.Log($"Auto-add to inventory: {autoAddRewardsToInventory}");
        Debug.Log($"Debug logging: {enableDebugLog}");

        foreach (var machine in gachaMachines)
        {
            Debug.Log($"  Machine: {machine.machineName} - Pool: {machine.Pool?.poolName ?? "None"}");
        }
    }
    #endregion
}