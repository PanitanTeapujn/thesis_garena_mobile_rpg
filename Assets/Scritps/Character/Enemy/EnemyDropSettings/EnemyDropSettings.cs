using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ตั้งค่าการ drop ของ enemy แต่ละประเภท
/// Game Designer สามารถสร้าง ScriptableObject นี้ได้เพื่อกำหนดของที่ drop
/// </summary>
[CreateAssetMenu(fileName = "New Enemy Drop Settings", menuName = "Game/Enemy Drop Settings")]
public class EnemyDropSettings : ScriptableObject
{
    [Header("💰 Currency Drops")]
    [Tooltip("จำนวนเงินขั้นต่ำที่จะ drop")]
    public long minGoldDrop = 10;

    [Tooltip("จำนวนเงินสูงสุดที่จะ drop")]
    public long maxGoldDrop = 50;

    [Tooltip("โอกาสที่จะ drop เงิน (0-100%)")]
    [Range(0f, 100f)]
    public float goldDropChance = 80f;

    [Space]
    [Tooltip("จำนวนเพชรขั้นต่ำที่จะ drop")]
    public int minGemsDrop = 0;

    [Tooltip("จำนวนเพชรสูงสุดที่จะ drop")]
    public int maxGemsDrop = 3;

    [Tooltip("โอกาสที่จะ drop เพชร (0-100%)")]
    [Range(0f, 100f)]
    public float gemsDropChance = 5f;

    [Header("🎒 Item Drops")]
    [Tooltip("รายการไอเทมที่สามารถ drop ได้")]
    public List<DropItem> itemDrops = new List<DropItem>();

    [Tooltip("จำนวนไอเทมสูงสุดที่จะ drop ในครั้งเดียว")]
    [Range(0, 10)]
    public int maxItemsPerDrop = 3;

    [Header("🎯 Level Scaling")]
    [Tooltip("เพิ่มเงินตาม level ของ enemy (% per level)")]
    [Range(0f, 100f)]
    public float goldLevelBonus = 10f;

    [Tooltip("เพิ่มโอกาส drop ตาม level ของ enemy (% per level)")]
    [Range(0f, 10f)]
    public float dropChanceLevelBonus = 2f;

    [Header("🎊 Special Drops")]
    [Tooltip("รายการไอเทมหายากที่มีโอกาส drop น้อย")]
    public List<RareDropItem> rareDrops = new List<RareDropItem>();

    [Header("🔧 Debug")]
    [Tooltip("แสดง log เมื่อมีการ drop")]
    public bool showDropLogs = true;

    [Tooltip("บังคับ drop ทุกอย่างเพื่อทดสอบ")]
    public bool guaranteedDropsForTesting = false;

    /// <summary>
    /// คำนวณจำนวนเงินที่จะ drop โดยพิจารณาจาก level
    /// </summary>
    public long CalculateGoldDrop(int enemyLevel)
    {
        if (Random.Range(0f, 100f) > GetEffectiveGoldDropChance(enemyLevel) && !guaranteedDropsForTesting)
            return 0;

        long baseGold = Random.Range((int)minGoldDrop, (int)maxGoldDrop + 1);

        // เพิ่มเงินตาม level
        float levelMultiplier = 1f + (goldLevelBonus / 100f) * (enemyLevel - 1);
        long finalGold = Mathf.RoundToInt(baseGold * levelMultiplier);

        return finalGold;
    }

    /// <summary>
    /// คำนวณจำนวนเพชรที่จะ drop โดยพิจารณาจาก level
    /// </summary>
    public int CalculateGemsDrop(int enemyLevel)
    {
        if (Random.Range(0f, 100f) > GetEffectiveGemsDropChance(enemyLevel) && !guaranteedDropsForTesting)
            return 0;

        return Random.Range(minGemsDrop, maxGemsDrop + 1);
    }

    /// <summary>
    /// สุ่มไอเทมที่จะ drop
    /// </summary>
    public List<ItemDropResult> RollItemDrops(int enemyLevel)
    {
        List<ItemDropResult> droppedItems = new List<ItemDropResult>();

        if (itemDrops.Count == 0) return droppedItems;

        int itemsDropped = 0;

        // สุ่มไอเทมปกติ
        foreach (var dropItem in itemDrops)
        {
            if (itemsDropped >= maxItemsPerDrop) break;

            float effectiveDropChance = GetEffectiveItemDropChance(dropItem.dropChance, enemyLevel);

            if (Random.Range(0f, 100f) <= effectiveDropChance || guaranteedDropsForTesting)
            {
                int dropCount = Random.Range(dropItem.minQuantity, dropItem.maxQuantity + 1);
                if (dropCount > 0)
                {
                    droppedItems.Add(new ItemDropResult
                    {
                        itemData = dropItem.itemData,
                        quantity = dropCount,
                        isRareDrop = false
                    });
                    itemsDropped++;
                }
            }
        }

        // สุ่มไอเทมหายาก
        foreach (var rareDrop in rareDrops)
        {
            if (itemsDropped >= maxItemsPerDrop) break;

            float effectiveDropChance = GetEffectiveRareDropChance(rareDrop.dropChance, enemyLevel);

            if (Random.Range(0f, 100f) <= effectiveDropChance || guaranteedDropsForTesting)
            {
                int dropCount = Random.Range(rareDrop.minQuantity, rareDrop.maxQuantity + 1);
                if (dropCount > 0)
                {
                    droppedItems.Add(new ItemDropResult
                    {
                        itemData = rareDrop.itemData,
                        quantity = dropCount,
                        isRareDrop = true
                    });
                    itemsDropped++;
                }
            }
        }

        return droppedItems;
    }

    /// <summary>
    /// คำนวณโอกาส drop เงินที่มีผลจาก level
    /// </summary>
    private float GetEffectiveGoldDropChance(int enemyLevel)
    {
        return Mathf.Min(100f, goldDropChance + (dropChanceLevelBonus * (enemyLevel - 1)));
    }

    /// <summary>
    /// คำนวณโอกาส drop เพชรที่มีผลจาก level
    /// </summary>
    private float GetEffectiveGemsDropChance(int enemyLevel)
    {
        return Mathf.Min(100f, gemsDropChance + (dropChanceLevelBonus * (enemyLevel - 1)));
    }

    /// <summary>
    /// คำนวณโอกาส drop ไอเทมที่มีผลจาก level
    /// </summary>
    private float GetEffectiveItemDropChance(float baseChance, int enemyLevel)
    {
        return Mathf.Min(100f, baseChance + (dropChanceLevelBonus * (enemyLevel - 1)));
    }

    /// <summary>
    /// คำนวณโอกาส drop ไอเทมหายากที่มีผลจาก level
    /// </summary>
    private float GetEffectiveRareDropChance(float baseChance, int enemyLevel)
    {
        // ไอเทมหายากได้รับ bonus น้อยกว่า
        float reducedBonus = dropChanceLevelBonus * 0.5f;
        return Mathf.Min(100f, baseChance + (reducedBonus * (enemyLevel - 1)));
    }

    /// <summary>
    /// ตรวจสอบว่า settings ถูกต้องหรือไม่
    /// </summary>
    public bool ValidateSettings()
    {
        if (minGoldDrop < 0 || maxGoldDrop < minGoldDrop)
        {
            Debug.LogError("[EnemyDropSettings] Invalid gold drop range!");
            return false;
        }

        if (minGemsDrop < 0 || maxGemsDrop < minGemsDrop)
        {
            Debug.LogError("[EnemyDropSettings] Invalid gems drop range!");
            return false;
        }

        foreach (var dropItem in itemDrops)
        {
            if (dropItem.itemData == null)
            {
                Debug.LogError("[EnemyDropSettings] Found null item in drop list!");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// สร้าง preset สำหรับ enemy ประเภทต่างๆ
    /// </summary>
    [ContextMenu("Create Weak Enemy Preset")]
    public void CreateWeakEnemyPreset()
    {
        minGoldDrop = 5;
        maxGoldDrop = 15;
        goldDropChance = 70f;
        minGemsDrop = 0;
        maxGemsDrop = 1;
        gemsDropChance = 3f;
        maxItemsPerDrop = 1;
        goldLevelBonus = 5f;
        dropChanceLevelBonus = 1f;
        Debug.Log("Applied Weak Enemy preset");
    }

    [ContextMenu("Create Normal Enemy Preset")]
    public void CreateNormalEnemyPreset()
    {
        minGoldDrop = 15;
        maxGoldDrop = 40;
        goldDropChance = 80f;
        minGemsDrop = 0;
        maxGemsDrop = 2;
        gemsDropChance = 5f;
        maxItemsPerDrop = 2;
        goldLevelBonus = 10f;
        dropChanceLevelBonus = 2f;
        Debug.Log("Applied Normal Enemy preset");
    }

    [ContextMenu("Create Boss Enemy Preset")]
    public void CreateBossEnemyPreset()
    {
        minGoldDrop = 100;
        maxGoldDrop = 300;
        goldDropChance = 100f;
        minGemsDrop = 3;
        maxGemsDrop = 10;
        gemsDropChance = 80f;
        maxItemsPerDrop = 5;
        goldLevelBonus = 25f;
        dropChanceLevelBonus = 5f;
        Debug.Log("Applied Boss Enemy preset");
    }
}

/// <summary>
/// ข้อมูลไอเทมที่สามารถ drop ได้
/// </summary>
[System.Serializable]
public class DropItem
{
    [Tooltip("ไอเทมที่จะ drop")]
    public ItemData itemData;

    [Tooltip("โอกาสที่จะ drop (0-100%)")]
    [Range(0f, 100f)]
    public float dropChance = 20f;

    [Tooltip("จำนวนขั้นต่ำที่จะ drop")]
    [Range(1, 10)]
    public int minQuantity = 1;

    [Tooltip("จำนวนสูงสุดที่จะ drop")]
    [Range(1, 10)]
    public int maxQuantity = 1;

    public DropItem()
    {
        dropChance = 20f;
        minQuantity = 1;
        maxQuantity = 1;
    }

    public DropItem(ItemData item, float chance, int min = 1, int max = 1)
    {
        itemData = item;
        dropChance = chance;
        minQuantity = min;
        maxQuantity = max;
    }
}

/// <summary>
/// ไอเทมหายากที่มีโอกาส drop น้อย
/// </summary>
[System.Serializable]
public class RareDropItem : DropItem
{
    [Tooltip("ข้อความที่จะแสดงเมื่อได้ไอเทมหายาก")]
    public string rareDropMessage = "💎 Rare Item Dropped!";

    [Tooltip("สีของ effect เมื่อ drop")]
    public Color effectColor = Color.yellow;

    public RareDropItem() : base()
    {
        dropChance = 2f; // ค่าเริ่มต้นสำหรับไอเทมหายาก
        rareDropMessage = "💎 Rare Item Dropped!";
        effectColor = Color.yellow;
    }
}

/// <summary>
/// ผลลัพธ์การ drop ไอเทม
/// </summary>
[System.Serializable]
public class ItemDropResult
{
    public ItemData itemData;
    public int quantity;
    public bool isRareDrop;
}

/// <summary>
/// ผลลัพธ์การ drop ทั้งหมด
/// </summary>
[System.Serializable]
public class EnemyDropResult
{
    public long goldDropped;
    public int gemsDropped;
    public List<ItemDropResult> itemsDropped;
    public bool hasRareItems;

    public EnemyDropResult()
    {
        itemsDropped = new List<ItemDropResult>();
        hasRareItems = false;
    }
}