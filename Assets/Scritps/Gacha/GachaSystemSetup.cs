using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Helper script สำหรับ setup Gacha System ให้ทำงานร่วมกับระบบอื่นๆ
/// ใส่ script นี้ใน Scene ที่มี Lobby เพื่อ auto-setup
/// </summary>
public class GachaSystemSetup : MonoBehaviour
{
    [Header(" Gacha System Components")]
    public GachaSystem gachaSystem;
    public GachaUIManager gachaUIManager;
    public LobbyManager lobbyManager;

    [Header(" Gacha Machines in Scene")]
    public List<GachaMachine> gachaMachines = new List<GachaMachine>();

    [Header(" Currency Integration")]
    public bool ensureCurrencyManager = true;
    public long defaultGold = 5000;
    public int defaultGems = 500;

    [Header(" Inventory Integration")]
    public bool ensureInventorySystem = true;
    public bool autoAddGachaRewardsToInventory = true;

    [Header(" Auto Setup Options")]
    public bool autoSetupOnStart = true;
    public bool validateSetupOnStart = true;
    public bool createMissingComponents = true;

    void Start()
    {
        if (autoSetupOnStart)
        {
            SetupGachaSystem();
        }

        if (validateSetupOnStart)
        {
            ValidateSetup();
        }
    }

    [ContextMenu("Setup Gacha System")]
    public void SetupGachaSystem()
    {
        Debug.Log(" [GachaSystemSetup] Setting up Gacha System...");

        // 1. Setup GachaSystem
        SetupGachaSystemComponent();

        // 2. Setup GachaUIManager
        SetupGachaUIManagerComponent();

        // 3. Setup LobbyManager integration
        SetupLobbyManagerIntegration();

        // 4. Setup Currency integration
        SetupCurrencyIntegration();

        // 5. Setup Inventory integration
        SetupInventoryIntegration();

        // 6. Setup Gacha Machines
        SetupGachaMachines();

        // 7. Final validation
        ValidateSetup();

        Debug.Log(" [GachaSystemSetup] Gacha System setup completed!");
    }

    private void SetupGachaSystemComponent()
    {
        // หา หรือสร้าง GachaSystem
        if (gachaSystem == null)
        {
            gachaSystem = FindObjectOfType<GachaSystem>();
        }

        if (gachaSystem == null && createMissingComponents)
        {
            GameObject gachaSystemObj = new GameObject("GachaSystem");
            gachaSystem = gachaSystemObj.AddComponent<GachaSystem>();
            Debug.Log(" Created GachaSystem component");
        }

        if (gachaSystem != null)
        {
            // Configure GachaSystem
            gachaSystem.enableDebugLog = true;
            gachaSystem.autoAddRewardsToInventory = autoAddGachaRewardsToInventory;

            // เชื่อมต่อกับ UI Manager
            if (gachaUIManager != null)
            {
                gachaSystem.uiManager = gachaUIManager;
            }
        }
    }

    private void SetupGachaUIManagerComponent()
    {
        // หา GachaUIManager
        if (gachaUIManager == null)
        {
            gachaUIManager = FindObjectOfType<GachaUIManager>();
        }

        if (gachaUIManager != null)
        {
            // เชื่อมต่อกับ LobbyManager
            if (lobbyManager != null)
            {
                gachaUIManager.lobbyManager = lobbyManager;
            }

            Debug.Log(" Configured GachaUIManager");
        }
        else
        {
            Debug.LogWarning(" GachaUIManager not found! Please add it manually.");
        }
    }

    private void SetupLobbyManagerIntegration()
    {
        // หา LobbyManager
        if (lobbyManager == null)
        {
            lobbyManager = FindObjectOfType<LobbyManager>();
        }

        if (lobbyManager != null)
        {
            // ตรวจสอบว่า LobbyManager มี gacha references หรือไม่
            // (ต้องเพิ่ม manually ใน Inspector)
            Debug.Log(" Found LobbyManager - Please ensure gacha button is connected manually");
        }
        else
        {
            Debug.LogWarning(" LobbyManager not found!");
        }
    }

    private void SetupCurrencyIntegration()
    {
        if (!ensureCurrencyManager) return;

        // หา CurrencyManager
        CurrencyManager currencyManager = CurrencyManager.FindCurrencyManager();

        if (currencyManager == null)
        {
            Debug.LogWarning(" CurrencyManager not found! Gacha system needs currency for transactions.");
        }
        else
        {
            Debug.Log(" Found CurrencyManager - Currency integration ready");

            // ตรวจสอบว่ามี currency เพียงพอสำหรับทดสอบ gacha หรือไม่
            if (currencyManager.GetCurrentGold() < 1000 || currencyManager.GetCurrentGems() < 100)
            {
                Debug.Log(" Adding default currency for gacha testing...");
                currencyManager.AddGold(defaultGold);
                currencyManager.AddGems(defaultGems);
            }
        }
    }

    private void SetupInventoryIntegration()
    {
        if (!ensureInventorySystem) return;

        // หา Inventory system
        Inventory[] inventories = FindObjectsOfType<Inventory>();

        if (inventories.Length == 0)
        {
            Debug.LogWarning(" No Inventory found! Gacha rewards cannot be added to inventory.");
        }
        else
        {
            Debug.Log($" Found {inventories.Length} Inventory components - Inventory integration ready");
        }
    }

    private void SetupGachaMachines()
    {
        // หา Gacha Machines ใน scene
        GachaMachine[] foundMachines = FindObjectsOfType<GachaMachine>();

        if (foundMachines.Length == 0)
        {
            Debug.LogWarning(" No GachaMachine found in scene!");

            if (createMissingComponents)
            {
                CreateDefaultGachaMachine();
            }
        }
        else
        {
            // เพิ่ม machines ที่เจอลงใน list
            gachaMachines.Clear();
            gachaMachines.AddRange(foundMachines);

            Debug.Log($" Found {foundMachines.Length} GachaMachine(s) in scene");

            // Validate แต่ละ machine
            foreach (GachaMachine machine in gachaMachines)
            {
                ValidateGachaMachine(machine);
            }
        }

        // Refresh machine list ใน GachaSystem
        if (gachaSystem != null)
        {
            gachaSystem.RefreshMachineList();
        }
    }

    private void CreateDefaultGachaMachine()
    {
        Debug.Log(" Creating default GachaMachine for testing...");

        GameObject machineObj = new GameObject("DefaultGachaMachine");
        GachaMachine machine = machineObj.AddComponent<GachaMachine>();

        // ต้องสร้าง GachaPoolData ด้วย (หรือใช้ existing pool)
        // TODO: สร้าง default pool หรือ load จาก Resources

        gachaMachines.Add(machine);

        Debug.Log(" Created default GachaMachine");
    }

    private void ValidateGachaMachine(GachaMachine machine)
    {
        if (machine.Pool == null)
        {
            Debug.LogWarning($" GachaMachine '{machine.name}' has no GachaPoolData assigned!");
        }
        else if (machine.Pool.GachaItems.Count == 0)
        {
            Debug.LogWarning($" GachaMachine '{machine.name}' pool has no items!");
        }
        else
        {
            Debug.Log($" GachaMachine '{machine.name}' validation passed");
        }
    }

    [ContextMenu("Validate Setup")]
    public void ValidateSetup()
    {
        Debug.Log(" [GachaSystemSetup] Validating Gacha System setup...");

        bool allValid = true;

        // ตรวจสอบ GachaSystem
        if (gachaSystem == null)
        {
            Debug.LogError(" GachaSystem not found!");
            allValid = false;
        }

        // ตรวจสอบ GachaUIManager
        if (gachaUIManager == null)
        {
            Debug.LogError(" GachaUIManager not found!");
            allValid = false;
        }

        // ตรวจสอบ CurrencyManager
        if (ensureCurrencyManager && CurrencyManager.FindCurrencyManager() == null)
        {
            Debug.LogError(" CurrencyManager not found!");
            allValid = false;
        }

        // ตรวจสอบ Inventory
        if (ensureInventorySystem && FindObjectsOfType<Inventory>().Length == 0)
        {
            Debug.LogError(" No Inventory found!");
            allValid = false;
        }

        // ตรวจสอบ GachaMachine
        if (gachaMachines.Count == 0)
        {
            Debug.LogError(" No GachaMachine configured!");
            allValid = false;
        }

        // ตรวจสอบ LobbyManager integration
        if (lobbyManager == null)
        {
            Debug.LogWarning(" LobbyManager not connected - Gacha UI may not be accessible from lobby");
        }

        if (allValid)
        {
            Debug.Log(" [GachaSystemSetup] All validations passed!");
        }
        else
        {
            Debug.LogError(" [GachaSystemSetup] Some validations failed!");
        }
    }

    [ContextMenu("Test Gacha System")]
    public void TestGachaSystem()
    {
        if (gachaSystem == null)
        {
            Debug.LogError(" Cannot test: GachaSystem not found!");
            return;
        }

        Debug.Log("[GachaSystemSetup] Testing Gacha System...");

        // ทดสอบ gacha ทุกเครื่อง
        foreach (GachaMachine machine in gachaMachines)
        {
            if (machine != null && machine.Pool != null)
            {
                Debug.Log($" Testing machine: {machine.machineName}");
                machine.TestSingleRoll();
            }
        }

        Debug.Log(" [GachaSystemSetup] Gacha System test completed!");
    }

    [ContextMenu("Add Test Currency")]
    public void AddTestCurrency()
    {
        CurrencyManager currencyManager = CurrencyManager.FindCurrencyManager();

        if (currencyManager != null)
        {
            currencyManager.AddGold(10000);
            currencyManager.AddGems(1000);
            Debug.Log(" Added test currency: 10,000 Gold + 1,000 Gems");
        }
        else
        {
            Debug.LogError(" CurrencyManager not found!");
        }
    }

    #region Debug Info
    [ContextMenu("Show System Info")]
    public void ShowSystemInfo()
    {
        Debug.Log(" [GachaSystemSetup] === GACHA SYSTEM INFO ===");
        Debug.Log($"GachaSystem: {(gachaSystem != null ? "C" : "X")}");
        Debug.Log($"GachaUIManager: {(gachaUIManager != null ? "C" : "X")}");
        Debug.Log($"LobbyManager: {(lobbyManager != null ? "C" : "X")}");
        Debug.Log($"CurrencyManager: {(CurrencyManager.FindCurrencyManager() != null ? "C" : "X")}");
        Debug.Log($"Inventory Count: {FindObjectsOfType<Inventory>().Length}");
        Debug.Log($"GachaMachine Count: {gachaMachines.Count}");

        if (gachaSystem != null)
        {
            Debug.Log($"Auto-add to inventory: {gachaSystem.autoAddRewardsToInventory}");
            Debug.Log($"Debug logging: {gachaSystem.enableDebugLog}");
        }
    }
    #endregion
}