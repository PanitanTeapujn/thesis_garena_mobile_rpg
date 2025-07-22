using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Component สำหรับ Set Bonus Item UI (แนบกับ Prefab)
/// </summary>
public class SetBonusItemUI : MonoBehaviour
{
    [Header("🔥 Set Bonus Item Components")]
    public TextMeshProUGUI setNameText;
    public TextMeshProUGUI setPiecesText;
    public TextMeshProUGUI setBonusStatsText;
    public Image backgroundImage;
    public Image setIconImage; // ถ้ามี icon ของ set

    [Header("🎨 Visual Settings")]
    public Color defaultBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    public float backgroundAlpha = 0.1f;

    /// <summary>
    /// ✅ Setup UI สำหรับ Set Bonus จาก EquipmentSetWithCount
    /// </summary>
    public void SetupSetBonusItemFromSetInfo(SetBonusUI.EquipmentSetWithCount setInfo)
    {
        if (setInfo?.equipmentSet == null) return;

        // Set Name
        if (setNameText != null)
        {
            setNameText.text = setInfo.equipmentSet.setName;
            setNameText.color = setInfo.equipmentSet.setColor;
        }

        // Set Pieces Count
        if (setPiecesText != null)
        {
            setPiecesText.text = $"({setInfo.equippedPieces}/{setInfo.equipmentSet.setItems.Count} pieces)";

            // ✅ เปลี่ยนสีตามสถานะ
            if (setInfo.activeBonus != null)
            {
                setPiecesText.color = Color.green; // มี bonus
            }
            else
            {
                setPiecesText.color = Color.yellow; // ยังไม่มี bonus
            }
        }

        // Set Bonus Stats
        if (setBonusStatsText != null)
        {
            if (setInfo.activeBonus != null)
            {
                // ✅ มี bonus - แสดง active bonuses (เหมือนเดิม)
                setBonusStatsText.text = GetFormattedBonusStats(setInfo.activeBonus.totalSetStats);
                setBonusStatsText.color = Color.green;
            }
            else
            {
                // ✅ ยังไม่มี bonus - แสดงว่าต้องการอีกกี่ชิ้น
               
            }
        }

        // Background Color
        if (backgroundImage != null)
        {
            Color bgColor = setInfo.equipmentSet.setColor;
            bgColor.a = setInfo.activeBonus != null ? 0.15f : 0.05f; // เข้มกว่าถ้ามี bonus
            backgroundImage.color = bgColor;
        }

        // Set Icon (ถ้ามี)
        if (setIconImage != null && setInfo.equipmentSet.setIcon != null)
        {
            setIconImage.sprite = setInfo.equipmentSet.setIcon;
            setIconImage.color = setInfo.equipmentSet.setColor;
        }
    }

    /// <summary>
    /// Format Set Bonus Stats เป็นข้อความ
    /// </summary>
    private string GetFormattedBonusStats(EquipmentStats stats)
    {
        List<string> bonusLines = new List<string>();

        // Combat Stats
        if (stats.attackDamageBonus > 0)
            bonusLines.Add($"<color=#FF6B6B>+{stats.attackDamageBonus}% ATK</color>");
        if (stats.magicDamageBonus > 0)
            bonusLines.Add($"<color=#4ECDC4>+{stats.magicDamageBonus}% MAG</color>");

        // HP/Mana
        if (stats.maxHpBonus > 0)
            bonusLines.Add($"<color=#FF4757>+{stats.maxHpBonus}% HP</color>");
        if (stats.maxManaBonus > 0)
            bonusLines.Add($"<color=#3742FA>+{stats.maxManaBonus}% Mana</color>");

        // Critical
        if (stats.criticalChanceBonus > 0)
            bonusLines.Add($"<color=#FFA502>+{stats.criticalChanceBonus:F1}% Crit Chance</color>");
        if (stats.criticalMultiplierBonus > 0)
            bonusLines.Add($"<color=#FF6348>+{stats.criticalMultiplierBonus:F1}% Crit Damage</color>");

        // Speed
        if (stats.attackSpeedBonus > 0)
            bonusLines.Add($"<color=#2ED573>+{stats.attackSpeedBonus:F1}% Attack Speed</color>");
        if (stats.moveSpeedBonus > 0)
            bonusLines.Add($"<color=#1DD1A1>+{stats.moveSpeedBonus:F1} Move Speed</color>");

        // Defense
        if (stats.armorBonus > 0)
            bonusLines.Add($"<color=#A4B0BE>+{stats.armorBonus}% Armor</color>");
        if (stats.physicalResistanceBonus > 0)
            bonusLines.Add($"<color=#747D8C>+{stats.physicalResistanceBonus:F1}% Physical Res</color>");
        if (stats.magicalResistanceBonus > 0)
            bonusLines.Add($"<color=#5F27CD>+{stats.magicalResistanceBonus:F1}% Magical Res</color>");

        // Special
        if (stats.lifeStealBonus > 0)
            bonusLines.Add($"<color=#E74C3C>+{stats.lifeStealBonus:F1}% Life Steal</color>");
        if (stats.reductionCoolDownBonus > 0)
            bonusLines.Add($"<color=#00D2D3>-{stats.reductionCoolDownBonus:F1}% Cooldown</color>");
        if (stats.hitRateBonus > 0)
            bonusLines.Add($"<color=#F39C12>+{stats.hitRateBonus:F1}% Hit Rate</color>");
        if (stats.evasionRateBonus > 0)
            bonusLines.Add($"<color=#9B59B6>+{stats.evasionRateBonus:F1}% Evasion</color>");

        return bonusLines.Count > 0 ? string.Join("\n", bonusLines) : "";
    }

    /// <summary>
    /// Setup UI สำหรับข้อความ "No Set Bonus"
    /// </summary>
    public void SetupNoSetBonusMessage()
    {
        if (setNameText != null)
        {
            setNameText.text = "No Set Bonuses Active";
            setNameText.color = Color.gray;
        }

        if (setPiecesText != null)
        {
            setPiecesText.text = "";
        }

        if (setBonusStatsText != null)
        {
            setBonusStatsText.text = "";
            setBonusStatsText.color = Color.gray;
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = defaultBackgroundColor;
        }

        if (setIconImage != null)
        {
            setIconImage.sprite = null;
        }
    }
}