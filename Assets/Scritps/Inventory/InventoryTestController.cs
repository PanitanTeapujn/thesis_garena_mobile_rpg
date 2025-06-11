using Fusion;
using System.Collections;
using UnityEngine;

public class InventoryTestController : MonoBehaviour
{
    [Header("Test Settings")]
    public KeyCode addItemKey = KeyCode.T;
    public KeyCode equipItemKey = KeyCode.E;
    public KeyCode useItemKey = KeyCode.U;
    public KeyCode clearKey = KeyCode.C;

    [Header("Auto Test")]
    public bool autoFindInventoryManager = true;
    public float retryDelay = 1f;

    private InventoryManager inventoryManager;
    private InventoryUIManager uiManager;
    private bool isInventoryReady = false;
    private int retryCount = 0;
    private const int maxRetries = 10;

    void Start()
    {
        StartCoroutine(FindAndWaitForInventoryManager());
    }

    IEnumerator FindAndWaitForInventoryManager()
    {
        Debug.Log("Starting InventoryManager detection...");

        while (!isInventoryReady && retryCount < maxRetries)
        {
            yield return new WaitForSeconds(retryDelay);
            retryCount++;

            Debug.Log($"Attempt {retryCount}/{maxRetries} - Searching for InventoryManager...");

            // ลองหา InventoryManager
            if (inventoryManager == null)
            {
                FindInventoryManager();
            }

            // ถ้าเจอแล้ว ตรวจสอบว่า ready หรือยัง
            if (inventoryManager != null)
            {
                Debug.Log($"Found InventoryManager: {inventoryManager.name}");
                Debug.Log($"HasInputAuthority: {inventoryManager.HasInputAuthority}");
                Debug.Log($"NetworkObject exists: {inventoryManager.Object != null}");

                if (inventoryManager.Object != null)
                {
                    Debug.Log($"NetworkObject IsValid: {inventoryManager.Object.IsValid}");
                }

                if (IsInventoryManagerReady())
                {
                    isInventoryReady = true;
                    Debug.Log("✅ InventoryManager is ready for testing!");

                    // หา UIManager
                    if (uiManager == null)
                    {
                        uiManager = FindObjectOfType<InventoryUIManager>();
                        if (uiManager != null)
                        {
                            Debug.Log("Found InventoryUIManager");
                        }
                    }

                    break;
                }
                else
                {
                    Debug.Log("InventoryManager found but not ready yet...");
                }
            }
            else
            {
                Debug.Log("InventoryManager not found, continuing search...");
            }
        }

        if (!isInventoryReady)
        {
            Debug.LogError("❌ Failed to initialize InventoryManager after maximum retries!");
            Debug.LogError("📋 Troubleshooting checklist:");
            Debug.LogError("1. Is there a Character with InventoryManager component?");
            Debug.LogError("2. Is the NetworkRunner running?");
            Debug.LogError("3. Has the Character been spawned on the network?");
            Debug.LogError("4. Does the Character have InputAuthority?");

            // แสดงข้อมูล debug
            LogDebugInfo();
        }
    }

    void FindInventoryManager()
    {
        Debug.Log("🔍 Searching for InventoryManager...");

        // Method 1: หาจาก NetworkPlayerManager ที่มี InputAuthority
        Debug.Log("Method 1: Searching via NetworkPlayerManager...");
        var networkPlayers = FindObjectsOfType<NetworkPlayerManager>();
        Debug.Log($"Found {networkPlayers.Length} NetworkPlayerManager(s)");

        foreach (var networkPlayer in networkPlayers)
        {
            Debug.Log($"Checking NetworkPlayerManager: {networkPlayer.name}, HasInputAuthority: {networkPlayer.HasInputAuthority}");

            if (networkPlayer.HasInputAuthority)
            {
                var manager = networkPlayer.GetComponent<InventoryManager>();
                if (manager != null)
                {
                    inventoryManager = manager;
                    Debug.Log("✅ Found InventoryManager via NetworkPlayerManager!");
                    return;
                }
                else
                {
                    Debug.LogWarning("NetworkPlayerManager with InputAuthority found but no InventoryManager component!");
                }
            }
        }

        // Method 2: หาจาก InventoryManager ทั้งหมดที่มี InputAuthority
        Debug.Log("Method 2: Searching all InventoryManagers...");
        var allManagers = FindObjectsOfType<InventoryManager>();
        Debug.Log($"Found {allManagers.Length} InventoryManager(s)");

        foreach (var manager in allManagers)
        {
            Debug.Log($"Checking InventoryManager: {manager.name}");
            Debug.Log($"  - HasInputAuthority: {manager.HasInputAuthority}");
            Debug.Log($"  - NetworkObject: {manager.Object != null}");

            if (manager.HasInputAuthority)
            {
                inventoryManager = manager;
                Debug.Log("✅ Found InventoryManager with InputAuthority!");
                return;
            }
        }

        // Method 3: Fallback - หาตัวแรกที่เจอ (สำหรับ single player)
        Debug.Log("Method 3: Fallback search...");
        if (allManagers.Length > 0)
        {
            inventoryManager = allManagers[0];
            Debug.Log($"⚠️ Using fallback InventoryManager: {inventoryManager.name}");
            Debug.Log("Note: This may not have InputAuthority");
            return;
        }

        Debug.LogWarning("❌ No InventoryManager found in any method!");
    }

    bool IsInventoryManagerReady()
    {
        if (inventoryManager == null)
        {
            Debug.Log("InventoryManager is null");
            return false;
        }

        // ตรวจสอบ NetworkObject
        if (inventoryManager.Object == null)
        {
            Debug.Log("NetworkObject is null");
            return false;
        }

        if (!inventoryManager.Object.IsValid)
        {
            Debug.Log("NetworkObject is not valid");
            return false;
        }

        // ลองเข้าถึง NetworkArray
        try
        {
            var testAccess = inventoryManager.NetworkInventory;
            Debug.Log("✅ NetworkInventory accessible");
            return true;
        }
        catch (System.InvalidOperationException ex)
        {
            Debug.Log($"NetworkInventory not accessible: {ex.Message}");
            return false;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Unexpected error accessing NetworkInventory: {ex.Message}");
            return false;
        }
    }

    void LogDebugInfo()
    {
        Debug.Log("=== DEBUG INFO ===");

        // NetworkRunner info
        var networkRunner = FindObjectOfType<NetworkRunner>();
        if (networkRunner != null)
        {
            Debug.Log($"NetworkRunner found: {networkRunner.name}");
            Debug.Log($"  - IsRunning: {networkRunner.IsRunning}");
            Debug.Log($"  - IsServer: {networkRunner.IsServer}");
            Debug.Log($"  - IsClient: {networkRunner.IsClient}");
            Debug.Log($"  - LocalPlayer: {networkRunner.LocalPlayer}");
        }
        else
        {
            Debug.LogError("❌ No NetworkRunner found!");
        }

        // Character info
        var characters = FindObjectsOfType<Character>();
        Debug.Log($"Found {characters.Length} Character(s)");

        foreach (var character in characters)
        {
            Debug.Log($"Character: {character.name}");
            var networkBehaviour = character.GetComponent<NetworkBehaviour>();
            if (networkBehaviour != null)
            {
                Debug.Log($"  - HasInputAuthority: {networkBehaviour.HasInputAuthority}");
                Debug.Log($"  - NetworkObject: {networkBehaviour.Object != null}");
            }

            var invManager = character.GetComponent<InventoryManager>();
            Debug.Log($"  - Has InventoryManager: {invManager != null}");
        }

        // InventoryManager info
        var allInventoryManagers = FindObjectsOfType<InventoryManager>();
        Debug.Log($"Total InventoryManagers found: {allInventoryManagers.Length}");

        for (int i = 0; i < allInventoryManagers.Length; i++)
        {
            var manager = allInventoryManagers[i];
            Debug.Log($"InventoryManager {i}: {manager.name}");
            Debug.Log($"  - HasInputAuthority: {manager.HasInputAuthority}");
            Debug.Log($"  - NetworkObject: {manager.Object != null}");
            if (manager.Object != null)
            {
                Debug.Log($"  - IsValid: {manager.Object.IsValid}");
            }
        }

        Debug.Log("=== END DEBUG INFO ===");
    }

    void Update()
    {
        if (!isInventoryReady || inventoryManager == null) return;

        // ตรวจสอบอีกครั้งว่ายัง ready อยู่ไหม
        if (!IsInventoryManagerReady()) return;

        // Test hotkeys
        if (Input.GetKeyDown(addItemKey))
        {
            AddRandomTestItem();
        }

        if (Input.GetKeyDown(equipItemKey))
        {
            EquipTestItems();
        }

        if (Input.GetKeyDown(useItemKey))
        {
            UseHealthPotion();
        }

        if (Input.GetKeyDown(clearKey))
        {
            ClearInventory();
        }
    }

    void AddRandomTestItem()
    {
        if (!CanPerformInventoryOperation()) return;

        string[] testItems = {
            "health_potion_small",
            "mana_potion_small",
            "iron_sword",
            "leather_armor",
            "leather_helmet",
            "leather_pants",
            "leather_boots"
        };

        string randomItem = testItems[Random.Range(0, testItems.Length)];
        int quantity = randomItem.Contains("potion") ? Random.Range(1, 5) : 1;

        try
        {
            bool success = inventoryManager.AddItem(randomItem, quantity);
            Debug.Log($"Added {quantity}x {randomItem}: {(success ? "Success" : "Failed")}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error adding item: {e.Message}");
        }
    }

    void EquipTestItems()
    {
        if (!CanPerformInventoryOperation()) return;

        try
        {
            inventoryManager.EquipItem("iron_sword");
            inventoryManager.EquipItem("leather_armor");
            inventoryManager.EquipItem("leather_helmet");
            inventoryManager.EquipItem("leather_pants");
            inventoryManager.EquipItem("leather_boots");
            Debug.Log("Test equipment equipped!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error equipping items: {e.Message}");
        }
    }

    void UseHealthPotion()
    {
        if (!CanPerformInventoryOperation()) return;

        try
        {
            bool success = inventoryManager.UseItem("health_potion_small");
            Debug.Log($"Used health potion: {(success ? "Success" : "Failed")}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error using item: {e.Message}");
        }
    }

    void ClearInventory()
    {
        if (!CanPerformInventoryOperation()) return;

        try
        {
            // ใช้ Dictionary แทนการเข้าถึง NetworkArray โดยตรง
            var items = inventoryManager.GetInventoryItems();
            foreach (var kvp in items)
            {
                var item = kvp.Value;
                inventoryManager.RemoveItem(item.itemId, item.quantity);
            }
            Debug.Log("Inventory cleared!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error clearing inventory: {e.Message}");
        }
    }

    bool CanPerformInventoryOperation()
    {
        if (!isInventoryReady)
        {
            Debug.LogWarning("InventoryManager not ready yet!");
            return false;
        }

        if (inventoryManager == null)
        {
            Debug.LogWarning("InventoryManager is null!");
            return false;
        }

        if (!inventoryManager.HasInputAuthority)
        {
            Debug.LogWarning("No input authority for inventory operations!");
            return false;
        }

        if (!IsInventoryManagerReady())
        {
            Debug.LogWarning("InventoryManager NetworkObject not ready!");
            return false;
        }

        return true;
    }

    [ContextMenu("Run Full Test")]
    void RunFullTest()
    {
        if (CanPerformInventoryOperation())
        {
            StartCoroutine(FullTestSequence());
        }
        else
        {
            Debug.LogError("Cannot run full test - InventoryManager not ready!");
        }
    }

    System.Collections.IEnumerator FullTestSequence()
    {
        Debug.Log("=== Starting Full Inventory Test ===");

        // Test 1: Add items
        Debug.Log("Test 1: Adding items...");
        for (int i = 0; i < 3; i++)
        {
            AddRandomTestItem();
            yield return new WaitForSeconds(0.5f);
        }

        // Test 2: Equip items
        Debug.Log("Test 2: Equipping items...");
        EquipTestItems();
        yield return new WaitForSeconds(1f);

        // Test 3: Use consumable
        Debug.Log("Test 3: Using consumable...");
        UseHealthPotion();
        yield return new WaitForSeconds(1f);

        // Test 4: Unequip
        Debug.Log("Test 4: Unequipping items...");
        try
        {
            inventoryManager.UnequipItem(EquipmentType.Weapon);
            inventoryManager.UnequipItem(EquipmentType.Armor);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error unequipping: {e.Message}");
        }
        yield return new WaitForSeconds(1f);

        Debug.Log("=== Full Test Completed ===");
    }

    int GetTotalItemCount()
    {
        if (!CanPerformInventoryOperation()) return 0;

        try
        {
            // ใช้ Dictionary แทนการเข้าถึง NetworkArray โดยตรง
            var items = inventoryManager.GetInventoryItems();
            int count = 0;
            foreach (var kvp in items)
            {
                count += kvp.Value.quantity;
            }
            return count;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error getting item count: {e.Message}");
            return 0;
        }
    }

    ItemStats GetEquipmentStats()
    {
        if (!CanPerformInventoryOperation()) return new ItemStats();

        try
        {
            return inventoryManager.GetTotalEquipmentStats();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error getting equipment stats: {e.Message}");
            return new ItemStats();
        }
    }

    void OnGUI()
    {
        // Status panel
        GUI.Box(new Rect(10, 10, 300, 200), "Inventory Test Controls");

        // แสดงสถานะ
        GUI.Label(new Rect(20, 35, 280, 20), $"Status: {(isInventoryReady ? "Ready" : "Not Ready")}");
        GUI.Label(new Rect(20, 55, 280, 20), $"Manager: {(inventoryManager != null ? "Found" : "Not Found")}");
        GUI.Label(new Rect(20, 75, 280, 20), $"Authority: {(inventoryManager?.HasInputAuthority ?? false)}");

        // ปุ่มทดสอบ (ใช้งานได้เฉพาะเมื่อ ready)
        GUI.enabled = isInventoryReady && CanPerformInventoryOperation();

        if (GUI.Button(new Rect(20, 100, 120, 30), $"Add Item ({addItemKey})"))
        {
            AddRandomTestItem();
        }

        if (GUI.Button(new Rect(150, 100, 120, 30), $"Equip ({equipItemKey})"))
        {
            EquipTestItems();
        }

        if (GUI.Button(new Rect(20, 140, 120, 30), $"Use Potion ({useItemKey})"))
        {
            UseHealthPotion();
        }

        if (GUI.Button(new Rect(150, 140, 120, 30), $"Clear ({clearKey})"))
        {
            ClearInventory();
        }

        if (GUI.Button(new Rect(20, 180, 250, 25), "Run Full Test Sequence"))
        {
            RunFullTest();
        }

        GUI.enabled = true; // Reset GUI enabled state

        // แสดงข้อมูลสถิติ
        if (isInventoryReady)
        {
            int totalItems = GetTotalItemCount();
            var stats = GetEquipmentStats();

            GUI.Box(new Rect(320, 10, 220, 120), "Current Status");
            GUI.Label(new Rect(330, 35, 200, 20), $"Total Items: {totalItems}");
            GUI.Label(new Rect(330, 55, 200, 80),
                $"Equipment Stats:\n" +
                $"Attack: +{stats.attackDamage}\n" +
                $"Armor: +{stats.armor}\n" +
                $"HP: +{stats.maxHp}\n" +
                $"Mana: +{stats.maxMana}");
        }

        // Retry button ถ้าไม่ ready
        if (!isInventoryReady)
        {
            if (GUI.Button(new Rect(320, 10, 150, 30), "Retry Initialize"))
            {
                retryCount = 0;
                StartCoroutine(FindAndWaitForInventoryManager());
            }
        }
    }

    // Context menu methods
    [ContextMenu("Force Find InventoryManager")]
    void ForceFindInventoryManager()
    {
        FindInventoryManager();
        Debug.Log($"Force find result: {(inventoryManager != null ? "Found" : "Not Found")}");
    }

    [ContextMenu("Check Ready Status")]
    void CheckReadyStatus()
    {
        Debug.Log($"InventoryManager: {inventoryManager != null}");
        if (inventoryManager != null)
        {
            Debug.Log($"HasInputAuthority: {inventoryManager.HasInputAuthority}");
            Debug.Log($"NetworkObject Valid: {inventoryManager.Object?.IsValid}");
            Debug.Log($"Is Ready: {IsInventoryManagerReady()}");
        }
    }

    [ContextMenu("Reset Test Controller")]
    void ResetTestController()
    {
        isInventoryReady = false;
        inventoryManager = null;
        uiManager = null;
        retryCount = 0;
        StartCoroutine(FindAndWaitForInventoryManager());
    }
}
