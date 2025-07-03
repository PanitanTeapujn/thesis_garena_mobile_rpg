using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ตั้งค่าการ drop item ของ enemy แต่ละประเภท
/// </summary>
[CreateAssetMenu(fileName = "New Item Drop Settings", menuName = "Game/Item Drop Settings")]
public class ItemDropSettings : ScriptableObject
{
    [Header("🎁 Item Drop Settings")]
    [Tooltip("โอกาสที่จะ drop item โดยรวม (0-100%)")]
    [Range(0f, 100f)]
    public float overallDropChance = 20f;

    [Tooltip("จำนวนไอเท็มสูงสุดที่จะ drop")]
    [Range(1, 5)]
    public int maxItemDrops = 2;

    [Tooltip("เพิ่มโอกาส drop item ตาม level (% per level)")]
    [Range(0f, 5f)]
    public float dropChanceLevelBonus = 1f;

    [Header("📦 Item List")]
    [Tooltip("รายการไอเท็มที่จะ drop")]
    public List<ItemDropEntry> itemDrops = new List<ItemDropEntry>();

    [Header("🔧 Debug")]
    [Tooltip("แสดง log เมื่อมีการ drop")]
    public bool showDropLogs = true;

    [Tooltip("บังคับ drop ทุกอย่างเพื่อทดสอบ")]
    public bool guaranteedDropsForTesting = false;

    /// <summary>
    /// คำนวณโอกาส drop โดยรวมที่มีผลจาก level
    /// </summary>
    public float GetEffectiveDropChance(int enemyLevel)
    {
        float effective = overallDropChance + (dropChanceLevelBonus * (enemyLevel - 1));
        return Mathf.Min(100f, effective);
    }

    /// <summary>
    /// หา items ที่สามารถ drop ได้ในระดับนี้
    /// </summary>
    public List<ItemDropEntry> GetAvailableDropsForLevel(int enemyLevel)
    {
        List<ItemDropEntry> available = new List<ItemDropEntry>();

        foreach (var drop in itemDrops)
        {
            if (drop.IsValid() && drop.CanDropAtLevel(enemyLevel))
            {
                available.Add(drop);
            }
        }

        return available;
    }

    /// <summary>
    /// ตรวจสอบว่า settings ถูกต้องหรือไม่
    /// </summary>
    public bool ValidateSettings()
    {
        if (itemDrops.Count == 0)
        {
            Debug.LogWarning("[ItemDropSettings] No item drops configured!");
            return false;
        }

        foreach (var drop in itemDrops)
        {
            if (!drop.IsValid())
            {
                Debug.LogError($"[ItemDropSettings] Invalid drop entry found!");
                return false;
            }
        }

        return true;
    }

    #region Preset Methods
    [ContextMenu("Create Weak Enemy Preset")]
    public void CreateWeakEnemyPreset()
    {
        overallDropChance = 15f;
        maxItemDrops = 1;
        dropChanceLevelBonus = 1f;
        Debug.Log("Applied Weak Enemy Item Drop preset");
    }

    [ContextMenu("Create Normal Enemy Preset")]
    public void CreateNormalEnemyPreset()
    {
        overallDropChance = 25f;
        maxItemDrops = 2;
        dropChanceLevelBonus = 2f;
        Debug.Log("Applied Normal Enemy Item Drop preset");
    }

    [ContextMenu("Create Boss Enemy Preset")]
    public void CreateBossEnemyPreset()
    {
        overallDropChance = 80f;
        maxItemDrops = 4;
        dropChanceLevelBonus = 5f;
        Debug.Log("Applied Boss Enemy Item Drop preset");
    }

    [ContextMenu("Add Sample Potion Drop")]
    public void AddSamplePotionDrop()
    {
        ItemDropEntry potionDrop = new ItemDropEntry();
        potionDrop.dropChance = 30f;
        potionDrop.minQuantity = 1;
        potionDrop.maxQuantity = 3;
        potionDrop.minEnemyLevel = 1;
        potionDrop.maxEnemyLevel = 10;

        itemDrops.Add(potionDrop);
        Debug.Log("Added sample potion drop. Please assign ItemData in inspector.");
    }

    [ContextMenu("Add Sample Weapon Drop")]
    public void AddSampleWeaponDrop()
    {
        ItemDropEntry weaponDrop = new ItemDropEntry();
        weaponDrop.dropChance = 15f;
        weaponDrop.minQuantity = 1;
        weaponDrop.maxQuantity = 1;
        weaponDrop.minEnemyLevel = 3;
        weaponDrop.maxEnemyLevel = 0;

        itemDrops.Add(weaponDrop);
        Debug.Log("Added sample weapon drop. Please assign ItemData in inspector.");
    }

    [ContextMenu("Add Sample Rare Drop")]
    public void AddSampleRareDrop()
    {
        ItemDropEntry rareDrop = new ItemDropEntry();
        rareDrop.dropChance = 5f;
        rareDrop.minQuantity = 1;
        rareDrop.maxQuantity = 1;
        rareDrop.minEnemyLevel = 5;
        rareDrop.maxEnemyLevel = 0;

        itemDrops.Add(rareDrop);
        Debug.Log("Added sample rare drop. Please assign ItemData in inspector.");
    }
    #endregion
}

/// <summary>
/// ข้อมูลการ drop item แต่ละชิ้น
/// </summary>
[System.Serializable]
public class ItemDropEntry
{
    [Header("📦 Item Settings")]
    public ItemData itemData;

    [Header("🎯 Drop Settings")]
    [Tooltip("โอกาสที่จะ drop item นี้ (0-100%)")]
    [Range(0f, 100f)]
    public float dropChance = 15f;

    [Tooltip("จำนวนขั้นต่ำที่จะ drop")]
    [Range(1, 10)]
    public int minQuantity = 1;

    [Tooltip("จำนวนสูงสุดที่จะ drop")]
    [Range(1, 10)]
    public int maxQuantity = 1;

    [Header("🎯 Level Requirements")]
    [Tooltip("Level ขั้นต่ำของ enemy ที่จะ drop item นี้")]
    [Range(1, 50)]
    public int minEnemyLevel = 1;

    [Tooltip("Level สูงสุดของ enemy ที่จะ drop item นี้ (0 = ไม่จำกัด)")]
    [Range(0, 50)]
    public int maxEnemyLevel = 0;

    public ItemDropEntry()
    {
        dropChance = 15f;
        minQuantity = 1;
        maxQuantity = 1;
        minEnemyLevel = 1;
        maxEnemyLevel = 0;
    }

    public ItemDropEntry(ItemData item, float chance, int minQty = 1, int maxQty = 1)
    {
        itemData = item;
        dropChance = chance;
        minQuantity = minQty;
        maxQuantity = maxQty;
        minEnemyLevel = 1;
        maxEnemyLevel = 0;
    }

    public bool CanDropAtLevel(int enemyLevel)
    {
        if (enemyLevel < minEnemyLevel) return false;
        if (maxEnemyLevel > 0 && enemyLevel > maxEnemyLevel) return false;
        return true;
    }

    public bool IsValid()
    {
        return itemData != null && dropChance > 0f && minQuantity > 0 && maxQuantity >= minQuantity;
    }

    public int RollQuantity()
    {
        return Random.Range(minQuantity, maxQuantity + 1);
    }
}