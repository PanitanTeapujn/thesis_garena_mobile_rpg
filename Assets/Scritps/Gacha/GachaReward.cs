using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GachaReward
{
    public ItemData itemData;
    public int quantity;
    public bool isGuaranteed;
    public bool isNewItem; // item ใหม่ที่ยังไม่เคยได้

    public GachaReward(ItemData item, int qty, bool guaranteed = false)
    {
        itemData = item;
        quantity = qty;
        isGuaranteed = guaranteed;
        isNewItem = false;
    }

    public bool IsValid()
    {
        return itemData != null && quantity > 0;
    }

    public string GetRewardText()
    {
        string text = $"{itemData.ItemName} x{quantity}";
        if (isGuaranteed) text += " GT";
        if (isNewItem) text += " New";
        return text;
    }
}
