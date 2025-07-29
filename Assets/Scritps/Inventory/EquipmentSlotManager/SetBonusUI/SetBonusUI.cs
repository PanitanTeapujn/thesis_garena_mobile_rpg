using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class SetBonusUI : MonoBehaviour
{
    [Header("🔥 Set Bonus Panel")]
    public GameObject setBonusPanel;
    public Button setBonusToggleButton;
    public Button setBonusCloseButton;
    public ScrollRect setBonusScrollRect;
    public Transform setBonusContentParent;

    [Header("🔥 Set Bonus Prefab")]
    public GameObject setBonusItemPrefab; // Prefab สำหรับแสดง 1 set

    [Header("🔥 Header")]
    public TextMeshProUGUI setBonusHeaderText;

    // Internal
    private List<GameObject> currentSetBonusItems = new List<GameObject>();
    private Character ownerCharacter;
    private SetBonusManager setBonusManager;
    private bool isPanelOpen = false;

    private void Start()
    {
        // ซ่อน panel ตอนเริ่มต้น
        if (setBonusPanel != null)
        {
            setBonusPanel.SetActive(false);
        }

        // Setup button events
        SetupButtonEvents();
    }

    private void SetupButtonEvents()
    {
        // ปุ่มเปิด/ปิด set bonus panel
        if (setBonusToggleButton != null)
        {
            setBonusToggleButton.onClick.RemoveAllListeners();
            setBonusToggleButton.onClick.AddListener(() => {
                ToggleSetBonusPanel();
            });
        }

        // ปุ่มปิด panel
        if (setBonusCloseButton != null)
        {
            setBonusCloseButton.onClick.RemoveAllListeners();
            setBonusCloseButton.onClick.AddListener(() => {
                CloseSetBonusPanel();
            });
        }
    }

    /// <summary>
    /// เชื่อมต่อกับ Character
    /// </summary>
    public void SetOwnerCharacter(Character character)
    {
        ownerCharacter = character;

        if (character != null)
        {
            setBonusManager = character.GetComponent<SetBonusManager>();

            if (setBonusManager != null)
            {
                // Subscribe to set bonus changes
                SetBonusManager.OnSetBonusChanged += OnSetBonusChanged;
                Debug.Log($"[SetBonusUI] Connected to {character.CharacterName}");

                // อัปเดต UI ทันที
                RefreshSetBonusDisplay();
            }
            else
            {
                Debug.LogWarning($"[SetBonusUI] No SetBonusManager found on {character.CharacterName}");
            }
        }
    }

    /// <summary>
    /// เปิด/ปิด Set Bonus Panel
    /// </summary>
    public void ToggleSetBonusPanel()
    {
        if (isPanelOpen)
        {
            CloseSetBonusPanel();
        }
        else
        {
            OpenSetBonusPanel();
        }
    }

    /// <summary>
    /// เปิด Set Bonus Panel
    /// </summary>
    public void OpenSetBonusPanel()
    {
        if (setBonusPanel != null)
        {
            setBonusPanel.SetActive(true);
            isPanelOpen = true;

            // Refresh ข้อมูลทันที
            RefreshSetBonusDisplay();

            Debug.Log("[SetBonusUI] Set Bonus Panel opened");
        }
    }

    /// <summary>
    /// ปิด Set Bonus Panel
    /// </summary>
    public void CloseSetBonusPanel()
    {
        if (setBonusPanel != null)
        {
            setBonusPanel.SetActive(false);
            isPanelOpen = false;

            Debug.Log("[SetBonusUI] Set Bonus Panel closed");
        }
    }

    /// <summary>
    /// Event handler เมื่อ set bonus เปลี่ยน
    /// </summary>
    private void OnSetBonusChanged(Character character, List<ActiveSetBonus> activeSetBonuses)
    {
        // ตรวจสอบว่าเป็น character ของเราหรือไม่
        if (character == ownerCharacter)
        {
            RefreshSetBonusDisplay();
        }
    }

    /// <summary>
    /// รีเฟรช UI แสดง Set Bonus
    /// </summary>
    public void RefreshSetBonusDisplay()
    {
        if (setBonusManager == null) return;

        // ล้าง items เดิม
        ClearCurrentSetBonusItems();

        // ✅ ดึงข้อมูล sets ที่มีอุปกรณ์อย่างน้อย 1 ชิ้น
        List<EquipmentSetWithCount> setsWithItems = GetSetsWithEquippedItems();

        // อัปเดต header
        UpdateHeaderText(setsWithItems.Count);

        // สร้าง UI สำหรับแต่ละ set ที่มีอุปกรณ์
        foreach (var setInfo in setsWithItems)
        {
            CreateSetBonusItem(setInfo);
        }

        // ✅ ไม่แสดงข้อความ "No Set Bonus" ถ้าไม่มีของเลย
    }

    /// <summary>
    /// ล้าง Set Bonus Items เดิม
    /// </summary>
    private void ClearCurrentSetBonusItems()
    {
        foreach (GameObject item in currentSetBonusItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        currentSetBonusItems.Clear();
    }

    /// <summary>
    /// อัปเดต Header Text
    /// </summary>
    private void UpdateHeaderText(int setsWithItemsCount)
    {
        if (setBonusHeaderText != null)
        {
            if (setsWithItemsCount > 0)
            {
                // ✅ นับ active set bonuses แยกต่างหาก
                int activeBonusCount = setBonusManager?.GetCurrentSetBonuses()?.Count ?? 0;

                if (activeBonusCount > 0)
                {
                    setBonusHeaderText.text = $" Set Equipment {setsWithItemsCount} sets \n{activeBonusCount} active bonuses";
                    setBonusHeaderText.color = Color.white;
                }
                else
                {
                    setBonusHeaderText.text = $" Set Equipment ({setsWithItemsCount} sets)";
                    setBonusHeaderText.color = Color.white;
                }
            }
            else
            {
                setBonusHeaderText.text = "🔥 No Set Equipment";
                setBonusHeaderText.color = Color.gray;
            }
        }
    }

    /// <summary>
    /// ข้อมูล Set พร้อมจำนวนชิ้นและ Active Bonus
    /// </summary>
    [System.Serializable]
    public class EquipmentSetWithCount
    {
        public EquipmentSet equipmentSet;
        public int equippedPieces;
        public ActiveSetBonus activeBonus; // null ถ้าไม่มี bonus

        public EquipmentSetWithCount(EquipmentSet set, int pieces, ActiveSetBonus bonus = null)
        {
            equipmentSet = set;
            equippedPieces = pieces;
            activeBonus = bonus;
        }
    }

    /// <summary>
    /// ✅ ดึง Sets ที่มีอุปกรณ์ใส่อยู่อย่างน้อย 1 ชิ้น
    /// </summary>
    private List<EquipmentSetWithCount> GetSetsWithEquippedItems()
    {
        List<EquipmentSetWithCount> setsWithItems = new List<EquipmentSetWithCount>();

        if (ownerCharacter == null) return setsWithItems;

        // ดึงรายการอุปกรณ์ที่ใส่อยู่
        List<ItemData> equippedItems = ownerCharacter.GetAllEquippedItems();

        // เฉพาะอุปกรณ์ที่ไม่ใช่ potion
        List<ItemData> equipmentOnly = equippedItems.Where(item =>
            item != null && item.ItemType != ItemType.Potion).ToList();

        // ดึง active set bonuses
        List<ActiveSetBonus> currentSetBonuses = setBonusManager.GetCurrentSetBonuses();

        // ตรวจสอบทุก set ที่มี
        foreach (var equipmentSet in setBonusManager.allEquipmentSets)
        {
            if (!equipmentSet.IsValidSet()) continue;

            int equippedPieces = equipmentSet.GetEquippedPiecesCount(equipmentOnly);

            // ✅ แสดงเฉพาะ sets ที่มีอุปกรณ์อย่างน้อย 1 ชิ้น
            if (equippedPieces >= 1)
            {
                // หา active bonus (ถ้ามี)
                ActiveSetBonus activeBonus = currentSetBonuses.FirstOrDefault(ab => ab.equipmentSet == equipmentSet);

                setsWithItems.Add(new EquipmentSetWithCount(equipmentSet, equippedPieces, activeBonus));
            }
        }

        return setsWithItems;
    }

    /// <summary>
    /// ✅ สร้าง UI Item สำหรับ Set (กลับไปใช้ ActiveSetBonus เหมือนเดิม)
    /// </summary>
    private void CreateSetBonusItem(EquipmentSetWithCount setInfo)
    {
        if (setBonusItemPrefab == null || setBonusContentParent == null)
        {
            Debug.LogError("[SetBonusUI] Missing prefab or content parent!");
            return;
        }

        // สร้าง GameObject จาก prefab
        GameObject setBonusItem = Instantiate(setBonusItemPrefab, setBonusContentParent);
        currentSetBonusItems.Add(setBonusItem);

        // หา components ใน prefab
        SetBonusItemUI itemUI = setBonusItem.GetComponent<SetBonusItemUI>();
        if (itemUI != null)
        {
            // ✅ ใช้ method ใหม่ที่รับ setInfo
            itemUI.SetupSetBonusItemFromSetInfo(setInfo);
        }
        else
        {
            // ถ้าไม่มี component ให้ setup แบบง่าย
            SetupSimpleSetBonusItemFromSetInfo(setBonusItem, setInfo);
        }
    }

    /// <summary>
    /// ✅ Setup Set Bonus Item แบบง่าย จาก EquipmentSetWithCount
    /// </summary>
    private void SetupSimpleSetBonusItemFromSetInfo(GameObject setBonusItem, EquipmentSetWithCount setInfo)
    {
        // หา Text components
        TextMeshProUGUI setNameText = setBonusItem.transform.Find("SetNameText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI setPiecesText = setBonusItem.transform.Find("SetPiecesText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI setBonusStatsText = setBonusItem.transform.Find("SetBonusStatsText")?.GetComponent<TextMeshProUGUI>();

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
                setBonusStatsText.text = GetSetBonusStatsText(setInfo.activeBonus);
                setBonusStatsText.color = Color.green;
            }
            else
            {
                // ✅ ยังไม่มี bonus - แสดงว่าต้องการอีกกี่ชิ้น
                int needed = 2 - setInfo.equippedPieces;
                setBonusStatsText.text = $"No bonus";
                setBonusStatsText.color = Color.gray;
            }
        }

        // Set Background Color (ถ้ามี Image component)
        Image backgroundImage = setBonusItem.GetComponent<Image>();
        if (backgroundImage != null)
        {
            Color bgColor = setInfo.equipmentSet.setColor;
            bgColor.a = setInfo.activeBonus != null ? 0.15f : 0.05f; // เข้มกว่าถ้ามี bonus
            backgroundImage.color = bgColor;
        }
    }

    /// <summary>
    /// สร้างข้อความ Set Bonus Stats
    /// </summary>
    private string GetSetBonusStatsText(ActiveSetBonus setBonus)
    {
        List<string> bonusTexts = new List<string>();
        EquipmentStats stats = setBonus.totalSetStats;

        // Attack/Magic Damage
        if (stats.attackDamageBonus > 0)
            bonusTexts.Add(FormatStatBonusForUI("ATK", stats.attackDamageBonus, true));
        if (stats.magicDamageBonus > 0)
            bonusTexts.Add(FormatStatBonusForUI("MAG", stats.magicDamageBonus, true));

        // HP/Mana
        if (stats.maxHpBonus > 0)
            bonusTexts.Add(FormatStatBonusForUI("HP", stats.maxHpBonus, true));
        if (stats.maxManaBonus > 0)
            bonusTexts.Add(FormatStatBonusForUI("Mana", stats.maxManaBonus, true));

        // ✅ Regeneration Stats (เป็น flat เสมอ)
        if (stats.healthRegenBonus > 0)
            bonusTexts.Add($"+{stats.healthRegenBonus:F1}/s HP Regen");
        if (stats.manaRegenBonus > 0)
            bonusTexts.Add($"+{stats.manaRegenBonus:F1}/s MP Regen");

        // Critical
        if (stats.criticalChanceBonus > 0)
            bonusTexts.Add($"+{stats.criticalChanceBonus:F1}% Crit Chance");
        if (stats.criticalMultiplierBonus > 0)
            bonusTexts.Add($"+{stats.criticalMultiplierBonus:F1}% Crit Damage");

        // Speed
        if (stats.attackSpeedBonus > 0)
            bonusTexts.Add($"+{stats.attackSpeedBonus:F1}% Attack Speed");
        if (stats.moveSpeedBonus > 0)
            bonusTexts.Add($"+{stats.moveSpeedBonus:F1} Move Speed");

        // ✅ Defense - ใช้ smart formatting
        if (stats.armorBonus > 0)
            bonusTexts.Add(FormatArmorBonusForUI("Armor", stats.armorBonus));
        if (stats.magicArmorBonus > 0)
            bonusTexts.Add(FormatArmorBonusForUI("Magic Armor", stats.magicArmorBonus));

        if (stats.physicalResistanceBonus > 0)
            bonusTexts.Add($"+{stats.physicalResistanceBonus:F1}% Physical Res");
        if (stats.magicalResistanceBonus > 0)
            bonusTexts.Add($"+{stats.magicalResistanceBonus:F1}% Magical Res");

        // Special
        if (stats.lifeStealBonus > 0)
            bonusTexts.Add($"+{stats.lifeStealBonus:F1}% Life Steal"); 
        if (stats.ampDamageBonus > 0)
            bonusTexts.Add($"+{stats.ampDamageBonus:F1}% Amp Damage");
        if (stats.reductionCoolDownBonus > 0)
            bonusTexts.Add($"-{stats.reductionCoolDownBonus:F1}% Cooldown");
        if (stats.hitRateBonus > 0)
            bonusTexts.Add($"+{stats.hitRateBonus:F1}% Hit Rate");
        if (stats.evasionRateBonus > 0)
            bonusTexts.Add($"+{stats.evasionRateBonus:F1}% Evasion");

        return bonusTexts.Count > 0 ? string.Join("\n", bonusTexts) : "No active bonuses";
    }
    private string FormatArmorBonusForUI(string armorType, int bonusValue)
    {
        // ถ้าค่า >= 50 = flat bonus (points)
        if (bonusValue >= 50)
        {
            return $"+{bonusValue} {armorType}";
        }
        // ถ้าค่า < 50 = percentage bonus
        else
        {
            return $"+{bonusValue}% {armorType}";
        }
    }
    private string GetCompactBonusStats(EquipmentStats stats)
    {
        List<string> compactStats = new List<string>();

        // เลือกเฉพาะ stats ที่สำคัญ
        if (stats.attackDamageBonus > 0)
        {
            string atkText = stats.attackDamageBonus >= 100 ? $"ATK+{stats.attackDamageBonus}" : $"ATK+{stats.attackDamageBonus}%";
            compactStats.Add($"<color=#FF6B6B>{atkText}</color>");
        }
        if (stats.magicDamageBonus > 0)
        {
            string magText = stats.magicDamageBonus >= 100 ? $"MAG+{stats.magicDamageBonus}" : $"MAG+{stats.magicDamageBonus}%";
            compactStats.Add($"<color=#4ECDC4>{magText}</color>");
        }
        if (stats.maxHpBonus > 0)
        {
            string hpText = stats.maxHpBonus >= 100 ? $"HP+{stats.maxHpBonus}" : $"HP+{stats.maxHpBonus}%";
            compactStats.Add($"<color=#FF4757>{hpText}</color>");
        }
        if (stats.healthRegenBonus > 0)
            compactStats.Add($"<color=#FF8A80>HPReg+{stats.healthRegenBonus:F1}</color>");
        if (stats.manaRegenBonus > 0)
            compactStats.Add($"<color=#82B1FF>MPReg+{stats.manaRegenBonus:F1}</color>");
        if (stats.magicArmorBonus > 0)
        {
            string maText = stats.magicArmorBonus >= 50 ? $"MArmor+{stats.magicArmorBonus}" : $"MArmor+{stats.magicArmorBonus}%";
            compactStats.Add($"<color=#B19CD9>{maText}</color>");
        }
        if (stats.armorBonus > 0)
        {
            string armorText = stats.armorBonus >= 50 ? $"Armor+{stats.armorBonus}" : $"Armor+{stats.armorBonus}%";
            compactStats.Add($"<color=#A4B0BE>{armorText}</color>");
        }
        if (stats.lifeStealBonus > 0)
            compactStats.Add($"<color=#E74C3C>LS+{stats.lifeStealBonus:F1}%</color>");
        if (stats.ampDamageBonus > 0)
            compactStats.Add($"<color=#E74C3C>LS+{stats.ampDamageBonus:F1}%</color>");
        if (stats.criticalChanceBonus > 0)
            compactStats.Add($"<color=#FFA502>Crit+{stats.criticalChanceBonus:F1}%</color>");

        // แสดงแค่ 4-6 stats แรก เพื่อไม่ให้ยาวเกิน
        if (compactStats.Count > 6)
        {
            var displayStats = compactStats.Take(6).ToList();
            displayStats.Add($"<color=#888>+{compactStats.Count - 6} more...</color>");
            return string.Join(" ", displayStats);
        }

        return string.Join(" ", compactStats);
    }
    // ✅ เพิ่ม helper methods สำหรับ SetBonusUI
    private string FormatStatBonusForUI(string statName, int bonusValue, bool canBePercentage)
    {
        if (!canBePercentage)
        {
            return $"+{bonusValue} {statName}";
        }

        // ถ้าค่ามากกว่า 100 = flat bonus
        if (bonusValue >= 100)
        {
            return $"+{bonusValue} {statName}";
        }
        // ถ้าค่าน้อยกว่า 100 = percentage bonus
        else
        {
            return $"+{bonusValue}% {statName}";
        }
    }
    /// <summary>
    /// ตรวจสอบว่า Panel เปิดอยู่หรือไม่
    /// </summary>
    public bool IsPanelOpen()
    {
        return isPanelOpen;
    }

    private void OnDestroy()
    {
        // Unsubscribe events
        SetBonusManager.OnSetBonusChanged -= OnSetBonusChanged;
    }
}