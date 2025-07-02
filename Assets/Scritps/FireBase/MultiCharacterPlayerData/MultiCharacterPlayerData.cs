using UnityEngine;
using System.Collections.Generic;
using System;

#region Saved Inventory Data Classes
/// <summary>
/// ข้อมูลไอเทมที่บันทึกใน shared inventory
/// </summary>
[System.Serializable]
public class SavedInventoryItem
{
    [Header("Basic Item Info")]
    public string itemId = "";           // ID ของไอเทม (เช่น "weapon_iron_sword_1234")
    public int slotIndex = -1;           // ตำแหน่งใน inventory grid (0-47)
    public int stackCount = 1;           // จำนวนที่ stack กัน

    [Header("Debug Info")]
    public string itemName = "";         // ชื่อไอเทม (สำหรับ debug เท่านั้น)
    public string itemType = "";         // ประเภทไอเทม (สำหรับ debug เท่านั้น)

    // Constructor
    public SavedInventoryItem()
    {
        itemId = "";
        slotIndex = -1;
        stackCount = 1;
        itemName = "";
        itemType = "";
    }

    public SavedInventoryItem(string id, int slot, int count, string name = "", string type = "")
    {
        itemId = id;
        slotIndex = slot;
        stackCount = count;
        itemName = name;       // สำหรับ debug
        itemType = type;       // สำหรับ debug
    }

    // ตรวจสอบว่าข้อมูลถูกต้อง
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(itemId) &&
               slotIndex >= 0 &&
               stackCount > 0;
    }
}

/// <summary>
/// ข้อมูล inventory ที่ใช้ร่วมกันทุกตัวละคร
/// </summary>
[System.Serializable]
public class SharedInventoryData
{
    [Header("Inventory Items")]
    public List<SavedInventoryItem> items = new List<SavedInventoryItem>();

    [Header("Grid Settings")]
    public int currentSlots = 80;        // จำนวนช่องที่ใช้ได้ปัจจุบัน (เริ่มต้น 24)
    public int maxSlots = 500;            // จำนวนช่องสูงสุด (48)

    [Header("Grid Layout")]
    public int gridWidth = 6;            // ความกว้าง grid
    public int gridHeight = 4;           // ความสูง grid

    [Header("Debug Info")]
    public string lastSaveTime = "";     // เวลาที่ save ล่าสุด
    public int totalItemCount = 0;       // จำนวนไอเทมทั้งหมด (สำหรับ debug)

    // Constructor
    public SharedInventoryData()
    {
        items = new List<SavedInventoryItem>();
        currentSlots = 80;
        maxSlots = 500;
        gridWidth = 6;
        gridHeight = 4;
        lastSaveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        totalItemCount = 0;
    }

    // ตรวจสอบว่าข้อมูลถูกต้อง
    public bool IsValid()
    {
        return currentSlots > 0 &&
               maxSlots >= currentSlots &&
               gridWidth > 0 &&
               gridHeight > 0 &&
               items != null;
    }

    // อัปเดตข้อมูล debug
    public void UpdateDebugInfo()
    {
        lastSaveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        totalItemCount = items.Count;
    }

    // หา item ใน slot ที่ระบุ
    public SavedInventoryItem GetItemInSlot(int slotIndex)
    {
        foreach (var item in items)
        {
            if (item.slotIndex == slotIndex)
                return item;
        }
        return null;
    }

    // ลบ item จาก slot ที่ระบุ
    public bool RemoveItemFromSlot(int slotIndex)
    {
        for (int i = items.Count - 1; i >= 0; i--)
        {
            if (items[i].slotIndex == slotIndex)
            {
                items.RemoveAt(i);
                UpdateDebugInfo();
                return true;
            }
        }
        return false;
    }

    // เพิ่ม item ลงใน slot
    public void AddOrUpdateItem(SavedInventoryItem newItem)
    {
        // ลบ item เก่าใน slot นี้ก่อน (ถ้ามี)
        RemoveItemFromSlot(newItem.slotIndex);

        // เพิ่ม item ใหม่
        items.Add(newItem);
        UpdateDebugInfo();
    }
}
#endregion

#region Saved Equipment Data Classes
/// <summary>
/// อุปกรณ์ที่สวมใส่ (6 ช่อง: Head, Armor, Weapon, Pants, Shoes, Rune)
/// </summary>
[System.Serializable]
public class SavedEquipmentSlot
{
    [Header("Equipment Slots (6 slots)")]
    public string headItemId = "";      // ช่อง 0: หมวก/หน้ากาก
    public string armorItemId = "";     // ช่อง 1: เสื้อเกราะ
    public string weaponItemId = "";    // ช่อง 2: อาวุธ
    public string pantsItemId = "";     // ช่อง 3: กางเกง
    public string shoesItemId = "";     // ช่อง 4: รองเท้า
    public string runeItemId = "";      // ช่อง 5: รูน

    [Header("Debug Info")]
    public int equippedCount = 0;       // จำนวนอุปกรณ์ที่สวมใส่

    // Constructor
    public SavedEquipmentSlot()
    {
        headItemId = "";
        armorItemId = "";
        weaponItemId = "";
        pantsItemId = "";
        shoesItemId = "";
        runeItemId = "";
        equippedCount = 0;
    }

    // ดึง itemId ตาม ItemType
    public string GetItemId(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Head: return headItemId;
            case ItemType.Armor: return armorItemId;
            case ItemType.Weapon: return weaponItemId;
            case ItemType.Pants: return pantsItemId;
            case ItemType.Shoes: return shoesItemId;
            case ItemType.Rune: return runeItemId;
            default: return "";
        }
    }

    // ตั้งค่า itemId ตาม ItemType
    public void SetItemId(ItemType itemType, string itemId)
    {
        switch (itemType)
        {
            case ItemType.Head: headItemId = itemId; break;
            case ItemType.Armor: armorItemId = itemId; break;
            case ItemType.Weapon: weaponItemId = itemId; break;
            case ItemType.Pants: pantsItemId = itemId; break;
            case ItemType.Shoes: shoesItemId = itemId; break;
            case ItemType.Rune: runeItemId = itemId; break;
        }
        UpdateEquippedCount();
    }

    // อัปเดตจำนวนอุปกรณ์ที่สวมใส่
    private void UpdateEquippedCount()
    {
        equippedCount = 0;
        if (!string.IsNullOrEmpty(headItemId)) equippedCount++;
        if (!string.IsNullOrEmpty(armorItemId)) equippedCount++;
        if (!string.IsNullOrEmpty(weaponItemId)) equippedCount++;
        if (!string.IsNullOrEmpty(pantsItemId)) equippedCount++;
        if (!string.IsNullOrEmpty(shoesItemId)) equippedCount++;
        if (!string.IsNullOrEmpty(runeItemId)) equippedCount++;
    }

    // ลบอุปกรณ์ตาม ItemType
    public void RemoveItem(ItemType itemType)
    {
        SetItemId(itemType, "");
    }

    // ตรวจสอบว่ามีอุปกรณ์ใน slot หรือไม่
    public bool HasItem(ItemType itemType)
    {
        return !string.IsNullOrEmpty(GetItemId(itemType));
    }
}

/// <summary>
/// ยาที่อยู่ใน quick slots (5 ช่อง)
/// </summary>
[System.Serializable]
public class SavedPotionSlot
{
    [Header("Potion Info")]
    public string itemId = "";           // ID ของยา
    public int stackCount = 0;           // จำนวนยาที่มี

    [Header("Debug Info")]
    public string itemName = "";         // ชื่อยา (สำหรับ debug)

    // Constructor
    public SavedPotionSlot()
    {
        itemId = "";
        stackCount = 0;
        itemName = "";
    }

    public SavedPotionSlot(string id, int count, string name = "")
    {
        itemId = id;
        stackCount = count;
        itemName = name;
    }

    // ตรวจสอบว่าช่องนี้ว่างหรือไม่
    public bool IsEmpty()
    {
        return string.IsNullOrEmpty(itemId) || stackCount <= 0;
    }

    // ตรวจสอบว่าข้อมูลถูกต้อง
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(itemId) && stackCount > 0;
    }

    // เคลียร์ช่อง
    public void Clear()
    {
        itemId = "";
        stackCount = 0;
        itemName = "";
    }
}

/// <summary>
/// ข้อมูลอุปกรณ์และยาของตัวละครแต่ละตัว
/// </summary>
[System.Serializable]
public class CharacterEquipmentData
{
    [Header("Character Info")]
    public string characterType = "";    // ประเภทตัวละคร (Assassin, Archer, etc.)

    [Header("Equipment (6 slots)")]
    public SavedEquipmentSlot equipment = new SavedEquipmentSlot();

    [Header("Potion Quick Slots (5 slots)")]
    public List<SavedPotionSlot> potionSlots = new List<SavedPotionSlot>();

    [Header("Debug Info")]
    public string lastEquipTime = "";    // เวลาที่ equip ล่าสุด
    public int totalPotionCount = 0;     // จำนวนยาทั้งหมด

    // Constructor
    public CharacterEquipmentData()
    {
        characterType = "";
        equipment = new SavedEquipmentSlot();
        potionSlots = new List<SavedPotionSlot>();

        // สร้าง 5 potion slots
        for (int i = 0; i < 5; i++)
        {
            potionSlots.Add(new SavedPotionSlot());
        }

        lastEquipTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        totalPotionCount = 0;
    }

    public CharacterEquipmentData(string charType) : this()
    {
        characterType = charType;
    }

    // ตรวจสอบว่าข้อมูลถูกต้อง
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(characterType) &&
               equipment != null &&
               potionSlots != null &&
               potionSlots.Count == 5;
    }

    // ดึงยาจาก slot ที่ระบุ
    public SavedPotionSlot GetPotionSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < potionSlots.Count)
            return potionSlots[slotIndex];
        return null;
    }

    // ตั้งค่ายาใน slot ที่ระบุ
    public void SetPotionSlot(int slotIndex, string itemId, int stackCount, string itemName = "")
    {
        if (slotIndex >= 0 && slotIndex < potionSlots.Count)
        {
            potionSlots[slotIndex].itemId = itemId;
            potionSlots[slotIndex].stackCount = stackCount;
            potionSlots[slotIndex].itemName = itemName;
            UpdateDebugInfo();
        }
    }

    // เคลียร์ potion slot
    public void ClearPotionSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < potionSlots.Count)
        {
            potionSlots[slotIndex].Clear();
            UpdateDebugInfo();
        }
    }

    // อัปเดตข้อมูล debug
    public void UpdateDebugInfo()
    {
        lastEquipTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        totalPotionCount = 0;
        foreach (var potionSlot in potionSlots)
        {
            if (!potionSlot.IsEmpty())
                totalPotionCount += potionSlot.stackCount;
        }
    }

    // ตรวจสอบว่ามี potion ใน slot หรือไม่
    public bool HasPotionInSlot(int slotIndex)
    {
        var potionSlot = GetPotionSlot(slotIndex);
        return potionSlot != null && !potionSlot.IsEmpty();
    }

    // หา potion slot ว่างแรก
    public int FindEmptyPotionSlot()
    {
        for (int i = 0; i < potionSlots.Count; i++)
        {
            if (potionSlots[i].IsEmpty())
                return i;
        }
        return -1; // ไม่มีช่องว่าง
    }
}
#endregion

#region Utility Classes
/// <summary>
/// คลาสช่วยสำหรับการแปลงข้อมูลระหว่าง ItemData และ SavedInventoryItem
/// </summary>
public static class InventoryDataConverter
{
    /// <summary>
    /// แปลง InventoryItem เป็น SavedInventoryItem
    /// </summary>
    public static SavedInventoryItem ToSavedItem(InventoryItem inventoryItem)
    {
        if (inventoryItem == null || inventoryItem.IsEmpty)
            return null;

        return new SavedInventoryItem(
            inventoryItem.itemData.ItemId,
            inventoryItem.slotIndex,
            inventoryItem.stackCount,
            inventoryItem.itemData.ItemName,     // debug
            inventoryItem.itemData.ItemType.ToString()  // debug
        );
    }

    /// <summary>
    /// แปลง ItemData เป็น SavedInventoryItem
    /// </summary>
    public static SavedInventoryItem ToSavedItem(ItemData itemData, int slotIndex, int stackCount)
    {
        if (itemData == null)
            return null;

        return new SavedInventoryItem(
            itemData.ItemId,
            slotIndex,
            stackCount,
            itemData.ItemName,     // debug
            itemData.ItemType.ToString()  // debug
        );
    }

    /// <summary>
    /// แปลง Character equipment เป็น CharacterEquipmentData
    /// </summary>
    public static CharacterEquipmentData ToCharacterEquipmentData(Character character)
    {
        if (character == null)
            return null;

        // 🔧 แก้ไข: ใช้ currentActiveCharacter จาก PersistentPlayerData
        string characterType = PersistentPlayerData.Instance?.GetCurrentActiveCharacter() ?? "Assassin";

        var equipmentData = new CharacterEquipmentData(characterType);

        Debug.Log($"[ToCharacterEquipmentData] Converting equipment for {characterType} (Character: {character.CharacterName})");

        // บันทึก equipment slots (6 ช่อง)
        var headItem = character.GetEquippedItem(ItemType.Head);
        if (headItem != null)
        {
            equipmentData.equipment.SetItemId(ItemType.Head, headItem.ItemId);
            Debug.Log($"[ToCharacterEquipmentData] Head: {headItem.ItemName} ({headItem.ItemId})");
        }

        var armorItem = character.GetEquippedItem(ItemType.Armor);
        if (armorItem != null)
        {
            equipmentData.equipment.SetItemId(ItemType.Armor, armorItem.ItemId);
            Debug.Log($"[ToCharacterEquipmentData] Armor: {armorItem.ItemName} ({armorItem.ItemId})");
        }

        var weaponItem = character.GetEquippedItem(ItemType.Weapon);
        if (weaponItem != null)
        {
            equipmentData.equipment.SetItemId(ItemType.Weapon, weaponItem.ItemId);
            Debug.Log($"[ToCharacterEquipmentData] Weapon: {weaponItem.ItemName} ({weaponItem.ItemId})");
        }

        var pantsItem = character.GetEquippedItem(ItemType.Pants);
        if (pantsItem != null)
        {
            equipmentData.equipment.SetItemId(ItemType.Pants, pantsItem.ItemId);
            Debug.Log($"[ToCharacterEquipmentData] Pants: {pantsItem.ItemName} ({pantsItem.ItemId})");
        }

        var shoesItem = character.GetEquippedItem(ItemType.Shoes);
        if (shoesItem != null)
        {
            equipmentData.equipment.SetItemId(ItemType.Shoes, shoesItem.ItemId);
            Debug.Log($"[ToCharacterEquipmentData] Shoes: {shoesItem.ItemName} ({shoesItem.ItemId})");
        }

        var runeItem = character.GetEquippedItem(ItemType.Rune);
        if (runeItem != null)
        {
            equipmentData.equipment.SetItemId(ItemType.Rune, runeItem.ItemId);
            Debug.Log($"[ToCharacterEquipmentData] Rune: {runeItem.ItemName} ({runeItem.ItemId})");
        }

        // บันทึก potion slots (5 ช่อง)
        for (int i = 0; i < 5; i++)
        {
            var potionItem = character.GetPotionInSlot(i);
            if (potionItem != null)
            {
                int stackCount = character.GetPotionStackCount(i);
                equipmentData.SetPotionSlot(i, potionItem.ItemId, stackCount, potionItem.ItemName);
                Debug.Log($"[ToCharacterEquipmentData] Potion {i}: {potionItem.ItemName} x{stackCount} ({potionItem.ItemId})");
            }
        }

        equipmentData.UpdateDebugInfo();
        Debug.Log($"[ToCharacterEquipmentData] ✅ Converted equipment data for {characterType}");

        return equipmentData;
    }
    /// <summary>
    /// แปลง Inventory เป็น SharedInventoryData
    /// </summary>
    public static SharedInventoryData ToSharedInventoryData(Inventory inventory)
    {
        if (inventory == null)
            return null;

        var sharedData = new SharedInventoryData();
        sharedData.currentSlots = inventory.CurrentSlots;
        sharedData.maxSlots = inventory.MaxSlots;
        sharedData.gridWidth = inventory.GridWidth;
        sharedData.gridHeight = inventory.GridHeight;

        // แปลง items ทั้งหมด
        for (int i = 0; i < inventory.CurrentSlots; i++)
        {
            var inventoryItem = inventory.GetItem(i);
            if (inventoryItem != null && !inventoryItem.IsEmpty)
            {
                var savedItem = ToSavedItem(inventoryItem);
                if (savedItem != null)
                {
                    sharedData.items.Add(savedItem);
                }
            }
        }

        sharedData.UpdateDebugInfo();
        return sharedData;
    }
}

// เพิ่มใน Saved Inventory Data Classes section
#region Currency Data Classes
/// <summary>
/// ข้อมูลเงินและเพชรที่ใช้ร่วมกันทุกตัวละคร
/// </summary>
[System.Serializable]
public class SharedCurrencyData
{
    [Header("Currency Amounts")]
    public long gold = 0;                // เงิน (ใช้ long เพื่อรองรับจำนวนมาก)
    public int gems = 0;                 // เพชร

    [Header("Currency Limits")]
    public long maxGold = 999999999;     // จำนวนเงินสูงสุด
    public int maxGems = 999999;         // จำนวนเพชรสูงสุด

    [Header("Debug Info")]
    public string lastUpdateTime = "";   // เวลาที่อัปเดตล่าสุด
    public int totalTransactions = 0;    // จำนวน transaction ทั้งหมด

    // Constructor
    public SharedCurrencyData()
    {
        gold = 1000;  // เงินเริ่มต้น
        gems = 50;    // เพชรเริ่มต้น
        maxGold = 999999999;
        maxGems = 999999;
        lastUpdateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        totalTransactions = 0;
    }

    // ตรวจสอบว่าข้อมูลถูกต้อง
    public bool IsValid()
    {
        return gold >= 0 &&
               gems >= 0 &&
               gold <= maxGold &&
               gems <= maxGems;
    }

    // อัปเดตข้อมูล debug
    public void UpdateDebugInfo()
    {
        lastUpdateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        totalTransactions++;
    }

    // เพิ่มเงิน
    public bool AddGold(long amount)
    {
        if (amount <= 0) return false;

        long newAmount = gold + amount;
        if (newAmount > maxGold) newAmount = maxGold;

        gold = newAmount;
        UpdateDebugInfo();
        return true;
    }

    // ใช้เงิน
    public bool SpendGold(long amount)
    {
        if (amount <= 0 || gold < amount) return false;

        gold -= amount;
        UpdateDebugInfo();
        return true;
    }

    // เพิ่มเพชร
    public bool AddGems(int amount)
    {
        if (amount <= 0) return false;

        int newAmount = gems + amount;
        if (newAmount > maxGems) newAmount = maxGems;

        gems = newAmount;
        UpdateDebugInfo();
        return true;
    }

    // ใช้เพชร
    public bool SpendGems(int amount)
    {
        if (amount <= 0 || gems < amount) return false;

        gems -= amount;
        UpdateDebugInfo();
        return true;
    }

    // ตรวจสอบว่ามีเงินเพียงพอ
    public bool HasEnoughGold(long amount)
    {
        return gold >= amount;
    }

    // ตรวจสอบว่ามีเพชรเพียงพอ
    public bool HasEnoughGems(int amount)
    {
        return gems >= amount;
    }
}

/// <summary>
/// ประเภทของสกุลเงิน
/// </summary>
public enum CurrencyType
{
    Gold,
    Gems
}

/// <summary>
/// ประเภทของ transaction
/// </summary>
public enum TransactionType
{
    Earn,     // ได้รับ
    Spend,    // ใช้จ่าย
    Admin     // Admin adjustment
}
#endregion
#endregion
[System.Serializable]
public class MultiCharacterPlayerData
{
    #region Variables and Properties  ตัวแปรทั้งหมดรวมถึงข้อมูลผู้เล่น, ตัวละคร, และระบบเพื่อน
    [Header("Player Info")]
    public string playerName;
    public string password;
    public string registrationDate;
    public string lastLoginDate;
    public string currentActiveCharacter = "Assassin";

    [Header("Character Data")]
    public List<CharacterProgressData> characters = new List<CharacterProgressData>();

    [Header("Stage Progress")]
    public StageProgressData stageProgress = new StageProgressData();

    [Header("Friends System")]
    public List<string> friends = new List<string>();
    public List<string> pendingFriendRequests = new List<string>();

    #region 🆕 Inventory System
    [Header("🎒 Shared Inventory System")]
    public SharedInventoryData sharedInventory = new SharedInventoryData();

    [Header("🔍 Inventory Debug Info")]
    public bool hasInventoryData = false;        // มีข้อมูล inventory หรือไม่
    public string inventoryLastSaveTime = "";    // เวลาที่ save inventory ล่าสุด
    public int totalSharedItems = 0;             // จำนวนไอเทมใน shared inventory
    #endregion

    // เพิ่มใน MultiCharacterPlayerData class หลัง Inventory System
    #region 🆕 Currency System
    [Header("💰 Shared Currency System")]
    public SharedCurrencyData sharedCurrency = new SharedCurrencyData();

    [Header("🔍 Currency Debug Info")]
    public bool hasCurrencyData = false;        // มีข้อมูลเงินหรือไม่
    public string currencyLastSaveTime = "";    // เวลาที่ save เงินล่าสุด
    #endregion

    // เพิ่ม methods ใน MultiCharacterPlayerData
    public void UpdateCurrencyDebugInfo()
    {
        if (sharedCurrency != null)
        {
            currencyLastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            hasCurrencyData = true;
            sharedCurrency.UpdateDebugInfo();
        }
    }

    public bool HasCurrencyData()
    {
        return hasCurrencyData &&
               sharedCurrency != null &&
               sharedCurrency.IsValid();
    }

    // เพิ่มใน InitializeInventorySystem method
    private void InitializeCurrencySystem()
    {
        // สร้าง shared currency ใหม่
        sharedCurrency = new SharedCurrencyData();

        // ตั้งค่าเริ่มต้น
        hasCurrencyData = false;
        currencyLastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        Debug.Log("✅ Currency system initialized for new player data");
    }

    // เพิ่มใน HasAnyInventoryOrEquipmentData method
    public bool HasAnyData()
    {
        return HasInventoryData() || HasCurrencyData() || HasAnyInventoryOrEquipmentData();
    }
    #endregion

    #region Constructor and Initialization Constructor และฟังก์ชันสร้างตัวละครเริ่มต้น

    public MultiCharacterPlayerData()
    {
        playerName = "";
        password = "";
        registrationDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        lastLoginDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        currentActiveCharacter = "Assassin";
        stageProgress = new StageProgressData();
        InitializeInventorySystem();
        InitializeCurrencySystem();
        InitializeDefaultCharacter();
    }
    private void InitializeInventorySystem()
    {
        // สร้าง shared inventory ใหม่
        sharedInventory = new SharedInventoryData();

        // ตั้งค่าเริ่มต้น
        hasInventoryData = false;
        inventoryLastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        totalSharedItems = 0;

        Debug.Log("✅ Inventory system initialized for new player data");
    }
    public void UpdateInventoryDebugInfo()
    {
        if (sharedInventory != null)
        {
            inventoryLastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            totalSharedItems = sharedInventory.items.Count;
            hasInventoryData = totalSharedItems > 0;

            sharedInventory.UpdateDebugInfo();
        }
    }

    public bool HasInventoryData()
    {
        return hasInventoryData &&
               sharedInventory != null &&
               sharedInventory.IsValid();
    }
    private void InitializeDefaultCharacter()
    {
        CharacterProgressData defaultAssassin = new CharacterProgressData("Assassin"); // 🆕 ใช้ constructor ใหม่
        defaultAssassin.currentLevel = 1;
        defaultAssassin.currentExp = 0;
        defaultAssassin.expToNextLevel = 100;

        CharacterStats assassinStats = Resources.Load<CharacterStats>("Characters/AssassinStats");
        if (assassinStats != null)
        {
            defaultAssassin.totalMaxHp = assassinStats.maxHp;
            defaultAssassin.totalMaxMana = assassinStats.maxMana;
            defaultAssassin.totalAttackDamage = assassinStats.attackDamage;
            defaultAssassin.totalMagicDamage = assassinStats.magicDamage;
            defaultAssassin.totalArmor = assassinStats.arrmor;
            defaultAssassin.totalCriticalChance = assassinStats.criticalChance;

            defaultAssassin.totalCriticalDamageBonus = assassinStats.criticalDamageBonus;

            defaultAssassin.totalMoveSpeed = assassinStats.moveSpeed;
            defaultAssassin.totalAttackRange = assassinStats.attackRange;
            defaultAssassin.totalAttackCooldown = assassinStats.attackCoolDown;
            defaultAssassin.totalHitRate = assassinStats.hitRate;
            defaultAssassin.totalEvasionRate = assassinStats.evasionRate;
            defaultAssassin.totalAttackSpeed = assassinStats.attackSpeed;
            defaultAssassin.totalReductionCoolDown = assassinStats.reductionCoolDown;

            Debug.Log($"✅ Default Assassin created with Critical Multiplier: {defaultAssassin.totalCriticalDamageBonus}");
        }

        characters.Add(defaultAssassin);
    }

    public CharacterProgressData CreateDefaultCharacterData(string characterType)
    {
        CharacterProgressData newCharacter = new CharacterProgressData(characterType); // 🆕 ใช้ constructor ใหม่
        newCharacter.currentLevel = 1;
        newCharacter.currentExp = 0;
        newCharacter.expToNextLevel = 100;

        CharacterStats characterStats = null;

        switch (characterType)
        {
            case "BloodKnight":
                characterStats = Resources.Load<CharacterStats>("Characters/BloodKnightStats");
                break;
            case "Archer":
                characterStats = Resources.Load<CharacterStats>("Characters/ArcherStats");
                break;
            case "Assassin":
                characterStats = Resources.Load<CharacterStats>("Characters/AssassinStats");
                break;
            case "IronJuggernaut":
                characterStats = Resources.Load<CharacterStats>("Characters/IronJuggernautStats");
                break;
        }

        if (characterStats != null)
        {
            newCharacter.totalMaxHp = characterStats.maxHp;
            newCharacter.totalMaxMana = characterStats.maxMana;
            newCharacter.totalAttackDamage = characterStats.attackDamage;
            newCharacter.totalMagicDamage = characterStats.magicDamage;
            newCharacter.totalArmor = characterStats.arrmor;
            newCharacter.totalCriticalChance = characterStats.criticalChance;

            newCharacter.totalCriticalDamageBonus = characterStats.criticalDamageBonus;

            newCharacter.totalMoveSpeed = characterStats.moveSpeed;
            newCharacter.totalAttackRange = characterStats.attackRange;
            newCharacter.totalAttackCooldown = characterStats.attackCoolDown;
            newCharacter.totalHitRate = characterStats.hitRate;
            newCharacter.totalEvasionRate = characterStats.evasionRate;
            newCharacter.totalAttackSpeed = characterStats.attackSpeed;
            newCharacter.totalReductionCoolDown = characterStats.reductionCoolDown;

            Debug.Log($"✅ Created {characterType} with Critical Multiplier: {newCharacter.totalCriticalDamageBonus}");
        }
        return newCharacter;
    }
    #endregion
    #region 🆕 Inventory System Helper Methods

    /// <summary>
    /// ดึงข้อมูลอุปกรณ์ของตัวละครปัจจุบัน
    /// </summary>
    public CharacterEquipmentData GetCurrentCharacterEquipment()
    {
        var activeCharacter = GetActiveCharacterData();
        return activeCharacter?.characterEquipment;
    }

    /// <summary>
    /// ดึงข้อมูลอุปกรณ์ของตัวละครที่ระบุ
    /// </summary>
    public CharacterEquipmentData GetCharacterEquipment(string characterType)
    {
        var character = GetCharacterData(characterType);
        return character?.characterEquipment;
    }

    /// <summary>
    /// อัปเดตข้อมูล debug ทั้งหมด
    /// </summary>
    public void UpdateAllInventoryDebugInfo()
    {
        // อัปเดต shared inventory
        UpdateInventoryDebugInfo();

        // อัปเดต equipment ของทุกตัวละคร
        foreach (var character in characters)
        {
            character?.UpdateEquipmentDebugInfo();
        }
    }

    /// <summary>
    /// ตรวจสอบว่าผู้เล่นมีข้อมูล inventory/equipment หรือไม่
    /// </summary>
    public bool HasAnyInventoryOrEquipmentData()
    {
        // ตรวจสอบ shared inventory
        if (HasInventoryData())
            return true;

        // ตรวจสอบ equipment ของทุกตัวละคร
        foreach (var character in characters)
        {
            if (character?.HasEquipmentData() == true)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Debug: แสดงสถิติ inventory ทั้งหมด
    /// </summary>
    public void LogInventoryStats()
    {
        Debug.Log("=== INVENTORY SYSTEM STATS ===");
        Debug.Log($"Player: {playerName}");
        Debug.Log($"Shared Items: {totalSharedItems}");
        Debug.Log($"Has Inventory Data: {HasInventoryData()}");

        foreach (var character in characters)
        {
            if (character != null)
            {
                Debug.Log($"{character.characterType}: Equipment={character.totalEquippedItems}, Potions={character.totalPotions}");
            }
        }
        Debug.Log("=============================");
    }

    #endregion
    #region Character Management การจัดการตัวละคร (ดึงข้อมูล, สร้าง, เปลี่ยน, อัปเดต)
    public CharacterProgressData GetCharacterData(string characterType)
    {
        return characters.Find(c => c.characterType == characterType);
    }

    public CharacterProgressData GetOrCreateCharacterData(string characterType)
    {
        CharacterProgressData existing = GetCharacterData(characterType);
        if (existing != null)
            return existing;

        CharacterProgressData newCharacter = CreateDefaultCharacterData(characterType);
        characters.Add(newCharacter);
        return newCharacter;
    }

    public CharacterProgressData GetActiveCharacterData()
    {
        return GetOrCreateCharacterData(currentActiveCharacter);
    }

    public void SwitchActiveCharacter(string characterType)
    {
        currentActiveCharacter = characterType;
        GetOrCreateCharacterData(characterType);
    }

    public void UpdateCharacterStats(string characterType, int level, int exp, int expToNext,
        int maxHp, int maxMana, int attackDamage, int magicDamage, int armor, float critChance,
        float critDamageBonus, float moveSpeed, float hitRate, float evasion, float attackSpeed,
        float reductionCoolDown)
    {
        CharacterProgressData character = GetOrCreateCharacterData(characterType);
        character.currentLevel = level;
        character.currentExp = exp;
        character.expToNextLevel = expToNext;
        character.totalMaxHp = maxHp;
        character.totalMaxMana = maxMana;
        character.totalAttackDamage = attackDamage;
        character.totalMagicDamage = magicDamage;
        character.totalArmor = armor;
        character.totalCriticalChance = critChance;
        character.totalCriticalDamageBonus = critDamageBonus;
        character.totalMoveSpeed = moveSpeed;
        character.totalHitRate = hitRate;
        character.totalEvasionRate = evasion;
        character.totalAttackSpeed = attackSpeed;
        character.totalReductionCoolDown = reductionCoolDown;
    }
    #endregion

    #region Data Validation and Utility  ฟังก์ชันตรวจสอบความถูกต้องของข้อมูล
    public bool IsValid()
    {
        bool basicValid = !string.IsNullOrEmpty(playerName) &&
                         !string.IsNullOrEmpty(currentActiveCharacter) &&
                         characters.Count > 0;

        // 🆕 ตรวจสอบ inventory system
        bool inventoryValid = sharedInventory != null && sharedInventory.IsValid();

        // 🆕 ตรวจสอบ character equipment
        bool equipmentValid = true;
        foreach (var character in characters)
        {
            if (character.characterEquipment == null || !character.characterEquipment.IsValid())
            {
                equipmentValid = false;
                break;
            }
        }

        Debug.Log($"[IsValid] Basic: {basicValid}, Inventory: {inventoryValid}, Equipment: {equipmentValid}");

        return basicValid && inventoryValid && equipmentValid;
    }
    #endregion

    #region Debug Methods ฟังก์ชันสำหรับ debug และแสดงข้อมูล
    public void LogAllCharacters()
    {
        Debug.Log($"=== {playerName}'s Characters ===");
        Debug.Log($"🎯 Active: {currentActiveCharacter}");

        foreach (var character in characters)
        {
            Debug.Log($"🎭 {character.characterType} - Level {character.currentLevel} " +
                     $"(HP: {character.totalMaxHp}, ATK: {character.totalAttackDamage})");
        }
    }
    #endregion
}

[System.Serializable]
public class CharacterProgressData
{
    #region Character Identity
    public string characterType;
    #endregion ชื่อประเภทตัวละคร

    #region Level and Experience เลเวลและประสบการณ์
    [Header("Level Progress")]
    public int currentLevel = 1;
    public int currentExp = 0;
    public int expToNextLevel = 100;
    #endregion

    #region Basic Combat Stats สถานะพื้นฐาน (HP, Mana, Attack, Magic, Armor)
    [Header("Basic Stats")]
    public int totalMaxHp;
    public int totalMaxMana;
    public int totalAttackDamage;
    public int totalMagicDamage;
    public int totalArmor;
    #endregion

    #region Critical Strike Stats สถานะ Critical Strike
    [Header("Critical Strike")]
    public float totalCriticalChance;
    public float totalCriticalDamageBonus;
    #endregion

    #region Movement and Attack Stats ความเร็ว, ระยะโจมตี, คูลดาวน์
    [Header("Movement & Attack")]
    public float totalMoveSpeed;
    public float totalAttackRange;
    public float totalAttackCooldown;
    public float totalAttackSpeed;
    #endregion

    #region Accuracy and Defense Stats  Hit Rate และ Evasion Rate
    [Header("Accuracy & Defense")]
    public float totalHitRate;
    public float totalEvasionRate;
    #endregion

    #region Special Stats สถานะพิเศษอื่นๆ
    [Header("Special Stats")]
    public float totalReductionCoolDown;
    #endregion


    #region 🆕 Character Equipment System
    [Header("🎯 Character Equipment & Potions")]
    public CharacterEquipmentData characterEquipment = new CharacterEquipmentData();

    [Header("🔍 Equipment Debug Info")]
    public bool hasEquipmentData = false;        // มีข้อมูลอุปกรณ์หรือไม่
    public string equipmentLastSaveTime = "";    // เวลาที่ save อุปกรณ์ล่าสุด
    public int totalEquippedItems = 0;           // จำนวนอุปกรณ์ที่สวมใส่
    public int totalPotions = 0;                 // จำนวนยาทั้งหมด
    #endregion

    // 🆕 เพิ่ม Constructor หรือ method สำหรับ initialize equipment (ถ้ายังไม่มี constructor ให้เพิ่มทั้งหมด)

    /// <summary>
    /// 🆕 Constructor สำหรับ CharacterProgressData
    /// </summary>
    public CharacterProgressData()
    {
        // Initialize equipment system
        InitializeEquipmentSystem();
    }

    /// <summary>
    /// 🆕 Constructor สำหรับ CharacterProgressData พร้อม character type
    /// </summary>
    public CharacterProgressData(string charType) : this()
    {
        characterType = charType;
        characterEquipment.characterType = charType;
    }

    /// <summary>
    /// 🆕 เริ่มต้นระบบอุปกรณ์
    /// </summary>
    private void InitializeEquipmentSystem()
    {
        // สร้างข้อมูลอุปกรณ์ใหม่
        characterEquipment = new CharacterEquipmentData(characterType);

        // ตั้งค่าเริ่มต้น
        hasEquipmentData = false;
        equipmentLastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        totalEquippedItems = 0;
        totalPotions = 0;

        Debug.Log($"✅ Equipment system initialized for {characterType}");
    }

    /// <summary>
    /// 🆕 อัปเดตข้อมูล debug ของอุปกรณ์
    /// </summary>
    public void UpdateEquipmentDebugInfo()
    {
        if (characterEquipment != null)
        {
            equipmentLastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            totalEquippedItems = characterEquipment.equipment.equippedCount;
            totalPotions = characterEquipment.totalPotionCount;
            hasEquipmentData = totalEquippedItems > 0 || totalPotions > 0;

            characterEquipment.UpdateDebugInfo();
        }
    }

    /// <summary>
    /// 🆕 ตรวจสอบว่ามีข้อมูลอุปกรณ์หรือไม่
    /// </summary>
    public bool HasEquipmentData()
    {
        return hasEquipmentData &&
               characterEquipment != null &&
               characterEquipment.IsValid();
    }

    /// <summary>
    /// 🆕 ตั้งค่า character type และอัปเดต equipment data
    /// </summary>
    public void SetCharacterType(string charType)
    {
        characterType = charType;
        if (characterEquipment != null)
        {
            characterEquipment.characterType = charType;
        }
    }
}