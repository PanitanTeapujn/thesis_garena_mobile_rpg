using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum RewardType
{
    Item,
    Character,
    Currency
}

[System.Serializable]
public class GachaReward
{
    [Header("Reward Type")]
    public RewardType rewardType;

    [Header("Item Reward")]
    public ItemData itemData;

    [Header("Character Reward")]
    public CharacterData characterData;

    [Header("Currency Reward")]
    public string currencyType = "Gold"; // Gold, Gems, etc.

    [Header("Common Properties")]
    public int quantity;
    public bool isGuaranteed;
    public bool isNewItem; // item ใหม่ที่ยังไม่เคยได้
    public bool isDuplicate; // สำหรับตัวละครที่ได้ซ้ำ

    // Constructor สำหรับ Item
    public GachaReward(ItemData item, int qty, bool guaranteed = false)
    {
        rewardType = RewardType.Item;
        itemData = item;
        quantity = qty;
        isGuaranteed = guaranteed;
        isNewItem = false;
        isDuplicate = false;
    }

    // Constructor สำหรับ Character
    public GachaReward(CharacterData character, bool guaranteed = false, bool duplicate = false)
    {
        rewardType = RewardType.Character;
        characterData = character;
        quantity = 1; // ตัวละครได้ทีละ 1
        isGuaranteed = guaranteed;
        isNewItem = !duplicate;
        isDuplicate = duplicate;
    }

    // Constructor สำหรับ Currency
    public GachaReward(string currency, int amount)
    {
        rewardType = RewardType.Currency;
        currencyType = currency;
        quantity = amount;
        isGuaranteed = false;
        isNewItem = false;
        isDuplicate = false;
    }

    // Default constructor
    public GachaReward()
    {
        rewardType = RewardType.Item;
        quantity = 1;
        isGuaranteed = false;
        isNewItem = false;
        isDuplicate = false;
    }

    public bool IsValid()
    {
        switch (rewardType)
        {
            case RewardType.Item:
                return itemData != null && quantity > 0;
            case RewardType.Character:
                return characterData != null;
            case RewardType.Currency:
                return !string.IsNullOrEmpty(currencyType) && quantity > 0;
            default:
                return false;
        }
    }

    public string GetRewardText()
    {
        string text = "";

        switch (rewardType)
        {
            case RewardType.Item:
                text = itemData != null ? $"{itemData.ItemName} x{quantity}" : "Unknown Item";
                break;
            case RewardType.Character:
                text = characterData != null ? characterData.characterName : "Unknown Character";
                if (isDuplicate) text += " (Duplicate)";
                break;
            case RewardType.Currency:
                text = $"{quantity} {currencyType}";
                break;
        }

        if (isGuaranteed) text += " [GUARANTEED]";
        if (isNewItem) text += " [NEW]";

        return text;
    }

    public Sprite GetRewardIcon()
    {
        switch (rewardType)
        {
            case RewardType.Item:
                return itemData?.ItemIcon;
            case RewardType.Character:
                return characterData?.characterIcon;
            case RewardType.Currency:
                // TODO: Return currency icon based on currencyType
                return null;
            default:
                return null;
        }
    }

    public Color GetRewardColor()
    {
        switch (rewardType)
        {
            case RewardType.Item:
                return itemData?.GetTierColor() ?? Color.white;
            case RewardType.Character:
                return characterData?.GetRarityColor() ?? Color.white;
            case RewardType.Currency:
                return Color.yellow; // Default currency color
            default:
                return Color.white;
        }
    }

    public string GetRewardTierText()
    {
        switch (rewardType)
        {
            case RewardType.Item:
                return itemData?.GetTierText() ?? "";
            case RewardType.Character:
                return characterData?.GetRarityText() ?? "";
            case RewardType.Currency:
                return "Currency";
            default:
                return "";
        }
    }

    public int GetRewardRarity()
    {
        switch (rewardType)
        {
            case RewardType.Item:
                return itemData != null ? (int)itemData.Tier : 0;
            case RewardType.Character:
                return characterData != null ? (int)characterData.rarity : 0;
            case RewardType.Currency:
                return 0; // Currency has no rarity
            default:
                return 0;
        }
    }

    public bool IsRareReward()
    {
        switch (rewardType)
        {
            case RewardType.Item:
                return itemData != null && itemData.Tier >= ItemTier.Rare;
            case RewardType.Character:
                return characterData != null && characterData.rarity >= CharacterRarity.Rare;
            case RewardType.Currency:
                return quantity >= 1000; // ถือว่า currency จำนวนมากเป็น rare
            default:
                return false;
        }
    }

    // Helper methods สำหรับ UI
    public string GetDisplayName()
    {
        switch (rewardType)
        {
            case RewardType.Item:
                return itemData?.ItemName ?? "Unknown Item";
            case RewardType.Character:
                return characterData?.characterName ?? "Unknown Character";
            case RewardType.Currency:
                return currencyType;
            default:
                return "Unknown Reward";
        }
    }

    public string GetDisplayQuantity()
    {
        if (rewardType == RewardType.Character)
        {
            return isDuplicate ? "Duplicate" : "New";
        }
        return quantity > 1 ? $"x{quantity}" : "";
    }

    public string GetFullDisplayText()
    {
        string name = GetDisplayName();
        string qty = GetDisplayQuantity();
        string tier = GetRewardTierText();

        string result = name;
        if (!string.IsNullOrEmpty(qty)) result += $" {qty}";
        if (!string.IsNullOrEmpty(tier)) result += $" ({tier})";

        return result;
    }
}