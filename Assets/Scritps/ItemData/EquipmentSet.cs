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
    MagicArmor,        // Magic Armor
    HealthRegen,       // Health Regeneration per second
    ManaRegen,
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
                // ATK สามารถเป็นทั้ง % และ flat ได้
                stats.attackDamageBonus += Mathf.RoundToInt(value);
                break;

            case SetBonusType.MagicDamage:
                stats.magicDamageBonus += Mathf.RoundToInt(value);
                break;

            case SetBonusType.MaxHp:
                stats.maxHpBonus += Mathf.RoundToInt(value);
                break;

            case SetBonusType.MaxMana:
                stats.maxManaBonus += Mathf.RoundToInt(value);
                break;

            case SetBonusType.Armor:
                // ✅ Armor - ระบุใน SetBonusEffect ว่าเป็น % หรือ flat
                if (effect.isPercentage)
                {
                    // เก็บเป็นค่าเล็กเพื่อให้ CalculateArmorBonus รู้ว่าเป็น %
                    stats.armorBonus += Mathf.RoundToInt(value);
                    Debug.Log($"[SetBonus] Armor percentage bonus: {value}% stored as {stats.armorBonus}");
                }
                else
                {
                    // เก็บเป็นค่าใหญ่เพื่อให้ CalculateArmorBonus รู้ว่าเป็น flat
                    stats.armorBonus += Mathf.RoundToInt(value);
                    Debug.Log($"[SetBonus] Armor flat bonus: {value} points");
                }
                break;

            case SetBonusType.MagicArmor:
                // ✅ Magic Armor - เหมือน Armor
                if (effect.isPercentage)
                {
                    stats.magicArmorBonus += Mathf.RoundToInt(value);
                    Debug.Log($"[SetBonus] Magic Armor percentage bonus: {value}% stored as {stats.magicArmorBonus}");
                }
                else
                {
                    stats.magicArmorBonus += Mathf.RoundToInt(value);
                    Debug.Log($"[SetBonus] Magic Armor flat bonus: {value} points");
                }
                break;

            case SetBonusType.CriticalChance:
                stats.criticalChanceBonus += value;
                break;

            case SetBonusType.CriticalDamage:
                stats.criticalMultiplierBonus += value;
                break;

            case SetBonusType.AttackSpeed:
                stats.attackSpeedBonus += value;
                break;

            case SetBonusType.MoveSpeed:
                stats.moveSpeedBonus += value;
                break;

            case SetBonusType.LifeSteal:
                stats.lifeStealBonus += value;
                break;

            case SetBonusType.PhysicalResistance:
                stats.physicalResistanceBonus += value;
                break;

            case SetBonusType.MagicalResistance:
                stats.magicalResistanceBonus += value;
                break;

            case SetBonusType.HealthRegen:
                // ✅ Health Regen มักจะเป็น flat bonus เสมอ
                stats.healthRegenBonus += value;
                Debug.Log($"[SetBonus] Health Regen flat bonus: +{value}/s");
                break;

            case SetBonusType.ManaRegen:
                // ✅ Mana Regen มักจะเป็น flat bonus เสมอ
                stats.manaRegenBonus += value;
                Debug.Log($"[SetBonus] Mana Regen flat bonus: +{value}/s");
                break;

            case SetBonusType.AllStats:
                // All Stats bonus
                if (effect.isPercentage)
                {
                    // สำหรับ AllStats ถ้าเป็น % ให้ใช้ค่าเล็ก
                    stats.attackDamageBonus += Mathf.RoundToInt(value);
                    stats.magicDamageBonus += Mathf.RoundToInt(value);
                    stats.maxHpBonus += Mathf.RoundToInt(value);
                    stats.maxManaBonus += Mathf.RoundToInt(value);
                    stats.armorBonus += Mathf.RoundToInt(value); // เก็บเป็น % 
                    stats.magicArmorBonus += Mathf.RoundToInt(value); // เก็บเป็น %
                }
                else
                {
                    // สำหรับ AllStats ถ้าเป็น flat ให้ใช้ค่าใหญ่
                    stats.attackDamageBonus += Mathf.RoundToInt(value + 100f); // เพิ่ม 100 เพื่อบอกว่าเป็น flat
                    stats.magicDamageBonus += Mathf.RoundToInt(value + 100f);
                    stats.maxHpBonus += Mathf.RoundToInt(value + 100f);
                    stats.maxManaBonus += Mathf.RoundToInt(value + 100f);
                    stats.armorBonus += Mathf.RoundToInt(value + 100f); // เก็บเป็น flat
                    stats.magicArmorBonus += Mathf.RoundToInt(value + 100f); // เก็บเป็น flat
                }
                stats.criticalChanceBonus += value;
                stats.criticalMultiplierBonus += value;
                stats.attackSpeedBonus += value;
                stats.moveSpeedBonus += value;
                stats.lifeStealBonus += value;
                stats.healthRegenBonus += value; // Regen เป็น flat เสมอ
                stats.manaRegenBonus += value; // Regen เป็น flat เสมอ
                break;
        }
    }
}
#endregion