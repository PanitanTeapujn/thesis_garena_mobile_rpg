using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class GachaItemEntry
{
    [Header("Item & Drop Rate")]
    public ItemData itemData;
    [Range(0.1f, 100f)]
    public float dropRate = 1f; // เปอร์เซ็นต์ของการสุ่มได้
    [Range(1, 99)]
    public int minQuantity = 1;
    [Range(1, 99)]
    public int maxQuantity = 1;

    [Header("Visual Settings")]
    public bool isRareItem = false;
    public bool isFeaturedItem = false;

    [TextArea(2, 3)]
    public string description = "";

    public bool IsValid()
    {
        return itemData != null && dropRate > 0 && minQuantity > 0 && maxQuantity >= minQuantity;
    }

    public int GetRandomQuantity()
    {
        return Random.Range(minQuantity, maxQuantity + 1);
    }
}

[CreateAssetMenu(fileName = "New Gacha Pool", menuName = "Gacha System/Gacha Pool")]
public class GachaPoolData : ScriptableObject
{
    [Header("Pool Information")]
    public string poolId = "";
    public string poolName = "";
    public Sprite poolIcon;

    [TextArea(3, 5)]
    public string description = "";

    [Header("Cost Settings")]
    public int costPerRoll = 100; // ราคาต่อการสุ่ม 1 ครั้ง
    public string costCurrency = "Gems"; // สกุลเงิน
    public int costPerTenRolls = 900; // ราคาต่อการสุ่ม 10 ครั้ง (ส่วนลด)

    [Header("Gacha Items")]
    [SerializeField] private List<GachaItemEntry> gachaItems = new List<GachaItemEntry>();

    [Header("Guarantee System")]
    public bool hasGuarantee = false;
    public int guaranteeCount = 10; // สุ่ม 10 ครั้งแล้วได้ rare แน่นอน
    public ItemTier guaranteeTier = ItemTier.Rare;

    #region Properties
    public List<GachaItemEntry> GachaItems => gachaItems;
    public float TotalDropRate
    {
        get
        {
            return gachaItems.Where(item => item.IsValid()).Sum(item => item.dropRate);
        }
    }
    #endregion

    #region Validation
    void OnValidate()
    {
        // Auto-generate pool ID
        if (string.IsNullOrEmpty(poolId))
        {
            poolId = $"pool_{name.Replace(" ", "_").ToLower()}_{Random.Range(1000, 9999)}";
        }

        // Auto-generate pool name
        if (string.IsNullOrEmpty(poolName))
        {
            poolName = name;
        }

        // Validate items
        for (int i = gachaItems.Count - 1; i >= 0; i--)
        {
            if (gachaItems[i] == null || !gachaItems[i].IsValid())
            {
                Debug.LogWarning($"Invalid gacha item at index {i} in pool {poolName}");
            }
        }
    }
    #endregion

    #region Gacha Logic
    public GachaItemEntry GetRandomItem()
    {
        if (gachaItems.Count == 0) return null;

        var validItems = gachaItems.Where(item => item.IsValid()).ToList();
        if (validItems.Count == 0) return null;

        float totalRate = validItems.Sum(item => item.dropRate);
        float randomValue = Random.Range(0f, totalRate);
        float currentRate = 0f;

        foreach (var item in validItems)
        {
            currentRate += item.dropRate;
            if (randomValue <= currentRate)
            {
                return item;
            }
        }

        return validItems.Last(); // fallback
    }

    public GachaItemEntry GetGuaranteedItem(ItemTier minTier)
    {
        var validItems = gachaItems.Where(item =>
            item.IsValid() &&
            item.itemData.Tier >= minTier
        ).ToList();

        if (validItems.Count == 0)
        {
            // fallback ให้ item tier สูงสุดที่มี
            validItems = gachaItems.Where(item => item.IsValid())
                .OrderByDescending(item => (int)item.itemData.Tier)
                .Take(1)
                .ToList();
        }

        if (validItems.Count == 0) return null;

        return validItems[Random.Range(0, validItems.Count)];
    }

    public List<GachaItemEntry> GetItemsByTier(ItemTier tier)
    {
        return gachaItems.Where(item =>
            item.IsValid() &&
            item.itemData.Tier == tier
        ).ToList();
    }
    #endregion

    #region Debug
    [ContextMenu("Debug Pool Info")]
    public void DebugPoolInfo()
    {
        Debug.Log($"🎰 Gacha Pool: {poolName} ({poolId})");
        Debug.Log($"💰 Cost: {costPerRoll} {costCurrency} (10x: {costPerTenRolls})");
        Debug.Log($"📝 Description: {description}");
        Debug.Log($"🎯 Items: {gachaItems.Count}, Total Rate: {TotalDropRate:F2}%");

        foreach (var tier in System.Enum.GetValues(typeof(ItemTier)).Cast<ItemTier>())
        {
            var itemsOfTier = GetItemsByTier(tier);
            if (itemsOfTier.Count > 0)
            {
                float tierRate = itemsOfTier.Sum(item => item.dropRate);
                Debug.Log($"  {tier}: {itemsOfTier.Count} items, {tierRate:F2}% rate");
            }
        }
    }
    #endregion
}
