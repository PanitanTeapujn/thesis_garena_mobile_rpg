using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StageRewardData
{
    [Header("💰 Currency Rewards")]
    public long totalGoldEarned = 0;
    public int totalGemsEarned = 0;

    [Header("🎁 Item Rewards")]
    public List<ItemRewardInfo> itemsEarned = new List<ItemRewardInfo>();

    [Header("📊 Statistics")]
    public int totalEnemiesKilled = 0;
    public float stageCompletionTime = 0f;
    public string stageName = "";

    public void AddGold(long amount)
    {
        totalGoldEarned += amount;
    }

    public void AddGems(int amount)
    {
        totalGemsEarned += amount;
    }

    public void AddItem(ItemData itemData, int quantity = 1)
    {
        if (itemData == null) return;

        // ตรวจสอบว่ามี item นี้อยู่แล้วหรือไม่
        var existingItem = itemsEarned.Find(item => item.itemId == itemData.ItemId);
        if (existingItem != null)
        {
            existingItem.quantity += quantity;
        }
        else
        {
            var newItem = new ItemRewardInfo
            {
                itemId = itemData.ItemId,
                itemName = itemData.ItemName,
                itemType = itemData.ItemType,
                itemTier = itemData.Tier,
                quantity = quantity,
                itemIcon = itemData.ItemIcon
            };
            itemsEarned.Add(newItem);
        }
    }

    public void AddEnemyKill()
    {
        totalEnemiesKilled++;
    }

    public void SetCompletionTime(float time)
    {
        stageCompletionTime = time;
    }

    public void SetStageName(string name)
    {
        stageName = name;
    }

    public void Reset()
    {
        totalGoldEarned = 0;
        totalGemsEarned = 0;
        itemsEarned.Clear();
        totalEnemiesKilled = 0;
        stageCompletionTime = 0f;
        stageName = "";
    }

    public bool HasAnyRewards()
    {
        return totalGoldEarned > 0 || totalGemsEarned > 0 || itemsEarned.Count > 0;
    }
}

[System.Serializable]
public class ItemRewardInfo
{
    public string itemId;
    public string itemName;
    public ItemType itemType;
    public ItemTier itemTier;
    public int quantity;
    public Sprite itemIcon;

    public Color GetTierColor()
    {
        switch (itemTier)
        {
            case ItemTier.Common: return Color.white;
            case ItemTier.Uncommon: return Color.green;
            case ItemTier.Rare: return Color.blue;
            case ItemTier.Epic: return Color.magenta;
            case ItemTier.Legendary: return Color.yellow;
            default: return Color.white;
        }
    }

    public string GetTierText()
    {
        return itemTier.ToString();
    }
}