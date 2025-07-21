using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

#region Set Bonus Data Classes
[System.Serializable]
public enum SetBonusType
{
    AttackDamage,
    MagicDamage,
    MaxHp,
    MaxMana,
    CriticalChance,
    CriticalDamage,
    AttackSpeed,
    MoveSpeed,
    Armor,
    LifeSteal,
    PhysicalResistance,
    MagicalResistance,
    AllStats
}

[System.Serializable]
public class SetBonusEffect
{
    [Header("Bonus Effect")]
    public SetBonusType bonusType;
    public float bonusValue;           // ค่าโบนัส (เช่น 30 = +30% หรือ +30 points)
    public bool isPercentage = true;   // true = เปอร์เซ็นต์, false = ค่าคงที่

    [Header("Visual")]
    public string bonusDescription;    // คำอธิบายโบนัส เช่น "ATK +30%"
    public Color bonusColor = Color.yellow;

    public string GetBonusText()
    {
        if (!string.IsNullOrEmpty(bonusDescription))
            return bonusDescription;

        string prefix = isPercentage ? "+" : "+";
        string suffix = isPercentage ? "%" : "";
        return $"{bonusType} {prefix}{bonusValue}{suffix}";
    }
}

[System.Serializable]
public class SetBonusRequirement
{
    [Header("Set Requirement")]
    public int requiredPieces = 2;           // จำนวนชิ้นที่ต้องใส่
    public List<SetBonusEffect> bonusEffects = new List<SetBonusEffect>();

    [Header("Visual")]
    public string requirementDescription;    // คำอธิบาย เช่น "Set 2 pieces"
    public Color requirementColor = Color.green;

    public string GetRequirementText()
    {
        if (!string.IsNullOrEmpty(requirementDescription))
            return requirementDescription;

        return $"Set {requiredPieces} pieces";
    }
}

[CreateAssetMenu(fileName = "New Equipment Set", menuName = "Equipment System/Equipment Set")]
public class EquipmentSet : ScriptableObject
{
    [Header("Set Information")]
    public string setName;
    public string setDescription;
    public Sprite setIcon;
    public Color setColor = Color.yellow;

    [Header("Set Items")]
    public List<ItemData> setItems = new List<ItemData>();

    [Header("Set Bonuses")]
    public List<SetBonusRequirement> setBonuses = new List<SetBonusRequirement>();

    [Header("Set Rarity")]
    public ItemTier setTier = ItemTier.Common;

    public bool IsValidSet()
    {
        return !string.IsNullOrEmpty(setName) &&
               setItems.Count >= 2 &&
               setBonuses.Count > 0;
    }

    public bool IsItemInSet(ItemData item)
    {
        if (item == null) return false;

        // ✅ ตรวจสอบทั้งการอ้างอิง object และ ItemId
        foreach (var setItem in setItems)
        {
            if (setItem == item ||
                (setItem != null && item != null && setItem.ItemId == item.ItemId))
            {
                return true;
            }
        }
        return false;
    }

    public int GetEquippedPiecesCount(List<ItemData> equippedItems)
    {
        int count = 0;
        foreach (var item in equippedItems)
        {
            if (item != null && IsItemInSet(item))
                count++;
        }
        return count;
    }

    public List<SetBonusRequirement> GetActiveSetBonuses(int equippedPieces)
    {
        List<SetBonusRequirement> activeBonuses = new List<SetBonusRequirement>();

        foreach (var requirement in setBonuses)
        {
            if (equippedPieces >= requirement.requiredPieces)
            {
                activeBonuses.Add(requirement);
            }
        }

        return activeBonuses;
    }

    public string GetSetBonusInfo(int currentPieces)
    {
        List<string> bonusInfo = new List<string>();

        bonusInfo.Add($"<color=#{ColorUtility.ToHtmlStringRGB(setColor)}>{setName}</color>");
        bonusInfo.Add($"<color=grey>{setDescription}</color>");
        bonusInfo.Add($"<color=white>Current: {currentPieces}/{setItems.Count} pieces</color>");
        bonusInfo.Add("");

        foreach (var requirement in setBonuses)
        {
            bool isActive = currentPieces >= requirement.requiredPieces;
            Color textColor = isActive ? Color.green : Color.grey;
            string colorHex = ColorUtility.ToHtmlStringRGB(textColor);

            bonusInfo.Add($"<color=#{colorHex}>{requirement.GetRequirementText()}:</color>");

            foreach (var effect in requirement.bonusEffects)
            {
                bonusInfo.Add($"<color=#{colorHex}>• {effect.GetBonusText()}</color>");
            }
            bonusInfo.Add("");
        }

        return string.Join("\n", bonusInfo);
    }
}

[System.Serializable]
public class ActiveSetBonus
{
    public EquipmentSet equipmentSet;
    public int equippedPieces;
    public List<SetBonusRequirement> activeBonuses;
    public EquipmentStats totalSetStats;

    public ActiveSetBonus(EquipmentSet set, int pieces)
    {
        equipmentSet = set;
        equippedPieces = pieces;
        activeBonuses = set.GetActiveSetBonuses(pieces);
        totalSetStats = CalculateSetStats();
    }

    private EquipmentStats CalculateSetStats()
    {
        EquipmentStats stats = new EquipmentStats();

        foreach (var requirement in activeBonuses)
        {
            foreach (var effect in requirement.bonusEffects)
            {
                ApplyBonusToStats(stats, effect);
            }
        }

        return stats;
    }

    private void ApplyBonusToStats(EquipmentStats stats, SetBonusEffect effect)
    {
        float value = effect.bonusValue;

        switch (effect.bonusType)
        {
            case SetBonusType.AttackDamage:
                if (effect.isPercentage)
                    stats.attackDamageBonus += Mathf.RoundToInt(value); // ✅ เก็บเป็น % โดยตรง
                else
                    stats.attackDamageBonus += Mathf.RoundToInt(value);
                break;

            case SetBonusType.MagicDamage:
                if (effect.isPercentage)
                    stats.magicDamageBonus += Mathf.RoundToInt(value); // ✅ เก็บเป็น % โดยตรง
                else
                    stats.magicDamageBonus += Mathf.RoundToInt(value);
                break;

            case SetBonusType.MaxHp:
                if (effect.isPercentage)
                    stats.maxHpBonus += Mathf.RoundToInt(value); // ✅ เก็บเป็น % โดยตรง
                else
                    stats.maxHpBonus += Mathf.RoundToInt(value);
                break;

            case SetBonusType.MaxMana:
                if (effect.isPercentage)
                    stats.maxManaBonus += Mathf.RoundToInt(value); // ✅ เก็บเป็น % โดยตรง
                else
                    stats.maxManaBonus += Mathf.RoundToInt(value);
                break;

            case SetBonusType.CriticalChance:
                stats.criticalChanceBonus += value; // ✅ รับ % โดยตรง
                break;

            case SetBonusType.CriticalDamage:
                stats.criticalMultiplierBonus += value; // ✅ รับ % โดยตรง
                break;

            case SetBonusType.AttackSpeed:
                stats.attackSpeedBonus += value; // ✅ รับ % โดยตรง
                break;

            case SetBonusType.MoveSpeed:
                stats.moveSpeedBonus += value; // ✅ รับค่าโดยตรง
                break;

            case SetBonusType.Armor:
                if (effect.isPercentage)
                    stats.armorBonus += Mathf.RoundToInt(value); // ✅ เก็บเป็น % โดยตรง
                else
                    stats.armorBonus += Mathf.RoundToInt(value);
                break;

            case SetBonusType.LifeSteal:
                stats.lifeStealBonus += value; // ✅ รับ % โดยตรง
                break;

            case SetBonusType.PhysicalResistance:
                stats.physicalResistanceBonus += value; // ✅ รับ % โดยตรง
                break;

            case SetBonusType.MagicalResistance:
                stats.magicalResistanceBonus += value; // ✅ รับ % โดยตรง
                break;

            case SetBonusType.AllStats:
                // All Stats bonus - เพิ่มทุก stat ด้วยค่าเดียวกัน
                if (effect.isPercentage)
                {
                    stats.attackDamageBonus += Mathf.RoundToInt(value); // ✅ เก็บเป็น % โดยตรง
                    stats.magicDamageBonus += Mathf.RoundToInt(value);
                    stats.maxHpBonus += Mathf.RoundToInt(value);
                    stats.maxManaBonus += Mathf.RoundToInt(value);
                    stats.armorBonus += Mathf.RoundToInt(value);
                }
                else
                {
                    stats.attackDamageBonus += Mathf.RoundToInt(value);
                    stats.magicDamageBonus += Mathf.RoundToInt(value);
                    stats.maxHpBonus += Mathf.RoundToInt(value);
                    stats.maxManaBonus += Mathf.RoundToInt(value);
                    stats.armorBonus += Mathf.RoundToInt(value);
                }
                stats.criticalChanceBonus += value;
                stats.criticalMultiplierBonus += value;
                stats.attackSpeedBonus += value;
                stats.moveSpeedBonus += value;
                stats.lifeStealBonus += value;
                break;
        }
    }
}
#endregion