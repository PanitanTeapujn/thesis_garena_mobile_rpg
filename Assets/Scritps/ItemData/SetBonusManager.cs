using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SetBonusManager : MonoBehaviour
{
    [Header("Set Bonus Database")]
    public List<EquipmentSet> allEquipmentSets = new List<EquipmentSet>();

    [Header("Current Set Bonuses")]
    [SerializeField] private List<ActiveSetBonus> currentSetBonuses = new List<ActiveSetBonus>();

    [Header("Debug")]
    public bool showDebugInfo = false;

    [Header("🔧 Auto-Load Settings")]
    [SerializeField] private bool autoLoadSetsFromResources = true;
    [SerializeField] private string[] resourcePaths = { "EquipmentSets", "Sets", "Items/Sets" };

    // Events
    public static event Action<Character, List<ActiveSetBonus>> OnSetBonusChanged;
    public static event Action<Character, EquipmentSet, int> OnSetPiecesChanged;

    // Component References
    private Character character;
    private EquipmentManager equipmentManager;

    private void Awake()
    {
        character = GetComponent<Character>();
        equipmentManager = GetComponent<EquipmentManager>();
    }

    private void Start()
    {
        // ✅ Auto-load equipment sets หาก list ว่าง
        if (autoLoadSetsFromResources && allEquipmentSets.Count == 0)
        {
            LoadEquipmentSetsFromResources();
        }

        // ✅ Auto-link items กับ sets ที่เจอ
        AutoLinkItemsToSets();
    }

    /// <summary>
    /// ✅ โหลด EquipmentSets จาก Resources อัตโนมัติ
    /// </summary>
    private void LoadEquipmentSetsFromResources()
    {
        List<EquipmentSet> foundSets = new List<EquipmentSet>();

        foreach (string path in resourcePaths)
        {
            try
            {
                EquipmentSet[] sets = Resources.LoadAll<EquipmentSet>(path);
                if (sets != null && sets.Length > 0)
                {
                    foundSets.AddRange(sets);
                    if (showDebugInfo)
                    {
                        Debug.Log($"[SetBonusManager] Found {sets.Length} sets in '{path}'");
                    }
                }
            }
            catch (System.Exception e)
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"[SetBonusManager] Failed to load from '{path}': {e.Message}");
                }
            }
        }

        // เพิ่มเฉพาะ sets ที่ยังไม่มี (หลีกเลี่ยง duplicates)
        foreach (var set in foundSets)
        {
            if (!allEquipmentSets.Contains(set))
            {
                allEquipmentSets.Add(set);
            }
        }

        if (showDebugInfo)
        {
            Debug.Log($"[SetBonusManager] Total loaded {allEquipmentSets.Count} equipment sets");
        }
    }

    /// <summary>
    /// ✅ เชื่อมต่อ items กับ sets อัตโนมัติโดยใช้ setName และ ItemId
    /// </summary>
    private void AutoLinkItemsToSets()
    {
        if (allEquipmentSets.Count == 0) return;

        int linkedCount = 0;

        foreach (var set in allEquipmentSets)
        {
            if (set == null || string.IsNullOrEmpty(set.setName)) continue;

            // ✅ หา items ที่มี setName ตรงกัน จาก ItemDatabase
            var itemDatabase = ItemDatabase.Instance;
            if (itemDatabase != null)
            {
                var allItems = itemDatabase.GetAllItems();

                foreach (var item in allItems)
                {
                    if (item != null && item.IsSetItem &&
                        !string.IsNullOrEmpty(item.GetSetName()) &&
                        item.GetSetName().Equals(set.setName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        // ✅ เพิ่ม item ลง set ถ้ายังไม่มี
                        if (!set.setItems.Contains(item))
                        {
                            set.setItems.Add(item);
                            linkedCount++;

                            if (showDebugInfo)
                            {
                                Debug.Log($"[SetBonusManager] Auto-linked '{item.ItemName}' to set '{set.setName}'");
                            }
                        }
                    }
                }
            }
        }

        if (showDebugInfo && linkedCount > 0)
        {
            Debug.Log($"[SetBonusManager] Auto-linked {linkedCount} items to their sets");
        }
    }

    /// <summary>
    /// เรียกใช้เมื่อมีการเปลี่ยนแปลงอุปกรณ์
    /// </summary>
    public void UpdateSetBonuses()
    {
        List<ItemData> equippedItems = character.GetAllEquippedItems();

        // เฉพาะอุปกรณ์ที่ไม่ใช่ potion
        List<ItemData> equipmentOnly = equippedItems.Where(item =>
            item != null && item.ItemType != ItemType.Potion).ToList();

        List<ActiveSetBonus> newSetBonuses = new List<ActiveSetBonus>();

        // ตรวจสอบทุก set ที่มี
        foreach (var equipmentSet in allEquipmentSets)
        {
            if (!equipmentSet.IsValidSet()) continue;

            int equippedPieces = equipmentSet.GetEquippedPiecesCount(equipmentOnly);

            if (equippedPieces >= 2) // ต้องมีอย่างน้อย 2 ชิ้น
            {
                ActiveSetBonus activeSet = new ActiveSetBonus(equipmentSet, equippedPieces);
                newSetBonuses.Add(activeSet);

                if (showDebugInfo)
                {
                    Debug.Log($"[SetBonus] {equipmentSet.setName}: {equippedPieces}/{equipmentSet.setItems.Count} pieces");
                }
            }
        }

        // ตรวจสอบการเปลี่ยนแปลง
        bool hasChanged = !AreSetBonusesEqual(currentSetBonuses, newSetBonuses);

        if (hasChanged)
        {
            currentSetBonuses = newSetBonuses;

            // Apply set bonuses to equipment manager
            ApplySetBonusesToEquipment();

            // Fire events
            OnSetBonusChanged?.Invoke(character, currentSetBonuses);

            if (showDebugInfo)
            {
                Debug.Log($"[SetBonusManager] {character.CharacterName} set bonuses updated: {currentSetBonuses.Count} active sets");
            }
        }
    }

    private bool AreSetBonusesEqual(List<ActiveSetBonus> oldBonuses, List<ActiveSetBonus> newBonuses)
    {
        if (oldBonuses.Count != newBonuses.Count) return false;

        for (int i = 0; i < oldBonuses.Count; i++)
        {
            if (oldBonuses[i].equipmentSet != newBonuses[i].equipmentSet ||
                oldBonuses[i].equippedPieces != newBonuses[i].equippedPieces)
            {
                return false;
            }
        }

        return true;
    }

    private void ApplySetBonusesToEquipment()
    {
        if (equipmentManager == null) return;

        // คำนวณ total set stats
        EquipmentStats totalSetStats = CalculateTotalSetStats();

        // 🔧 คำนวณ percentage bonuses จาก base stats แล้วแปลงเป็นค่าคงที่
        EquipmentStats finalBonuses = CalculateFinalBonusesFromBaseStats(totalSetStats);

        // สร้าง EquipmentData จาก set bonuses (ทั้งหมดเป็นค่าคงที่)
        EquipmentData setBonusEquipment = new EquipmentData
        {
            itemName = "Set Bonuses",
            stats = finalBonuses,
            itemIcon = null
        };

        // Apply set bonuses ผ่าน equipment manager (จะมีการ clear อัตโนมัติ)
        equipmentManager.ApplySetBonuses(setBonusEquipment);

        if (showDebugInfo)
        {
            Debug.Log($"[SetBonusManager] Applied set bonuses: ATK+{finalBonuses.attackDamageBonus}, HP+{finalBonuses.maxHpBonus}");
        }
    }

    private EquipmentStats CalculateTotalSetStats()
    {
        EquipmentStats totalStats = new EquipmentStats();

        foreach (var setBonus in currentSetBonuses)
        {
            // รวม stats จากแต่ละ set
            totalStats.attackDamageBonus += setBonus.totalSetStats.attackDamageBonus;
            totalStats.magicDamageBonus += setBonus.totalSetStats.magicDamageBonus;
            totalStats.armorBonus += setBonus.totalSetStats.armorBonus;
            totalStats.criticalChanceBonus += setBonus.totalSetStats.criticalChanceBonus;
            totalStats.criticalMultiplierBonus += setBonus.totalSetStats.criticalMultiplierBonus;
            totalStats.maxHpBonus += setBonus.totalSetStats.maxHpBonus;
            totalStats.maxManaBonus += setBonus.totalSetStats.maxManaBonus;
            totalStats.moveSpeedBonus += setBonus.totalSetStats.moveSpeedBonus;
            totalStats.attackSpeedBonus += setBonus.totalSetStats.attackSpeedBonus;
            totalStats.hitRateBonus += setBonus.totalSetStats.hitRateBonus;
            totalStats.evasionRateBonus += setBonus.totalSetStats.evasionRateBonus;
            totalStats.lifeStealBonus += setBonus.totalSetStats.lifeStealBonus;
            totalStats.reductionCoolDownBonus += setBonus.totalSetStats.reductionCoolDownBonus;
            totalStats.physicalResistanceBonus += setBonus.totalSetStats.physicalResistanceBonus;
            totalStats.magicalResistanceBonus += setBonus.totalSetStats.magicalResistanceBonus;
        }

        return totalStats;
    }

    /// <summary>
    /// 🔧 คำนวณ final bonuses จาก base stats (แปลง % เป็นค่าคงที่)
    /// </summary>
    private EquipmentStats CalculateFinalBonusesFromBaseStats(EquipmentStats setBonuses)
    {
        EquipmentStats finalBonuses = new EquipmentStats();

        // ✅ ดึง current base stats จาก PersistentPlayerData แทน ScriptableObject
        var characterData = PersistentPlayerData.Instance?.GetCurrentCharacterData();

        if (characterData == null || !characterData.HasValidBaseStats())
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("[SetBonusManager] No valid base stats found, using current character stats as fallback");
            }

            // ✅ Fallback: ใช้ current stats ของ character (ไม่ลบ equipment bonus)
            return CalculateBonusFromCurrentStats(setBonuses);
        }

        if (showDebugInfo)
        {
            Debug.Log($"[SetBonusManager] Using base stats: ATK={characterData.baseAttackDamage}, HP={characterData.baseMaxHp}");
        }

        // ✅ ใช้ current base stats (รวม level bonuses + stat upgrades แล้ว)
        finalBonuses.attackDamageBonus = CalculateBonusValue(characterData.baseAttackDamage, setBonuses.attackDamageBonus);
        finalBonuses.magicDamageBonus = CalculateBonusValue(characterData.baseMagicDamage, setBonuses.magicDamageBonus);
        finalBonuses.maxHpBonus = CalculateBonusValue(characterData.baseMaxHp, setBonuses.maxHpBonus);
        finalBonuses.maxManaBonus = CalculateBonusValue(characterData.baseMaxMana, setBonuses.maxManaBonus);
        finalBonuses.armorBonus = CalculateBonusValue(characterData.baseArmor, setBonuses.armorBonus);

        // Stats ที่เป็น % อยู่แล้ว - ไม่ต้องคำนวณ
        finalBonuses.criticalChanceBonus = setBonuses.criticalChanceBonus;
        finalBonuses.criticalMultiplierBonus = setBonuses.criticalMultiplierBonus;
        finalBonuses.attackSpeedBonus = setBonuses.attackSpeedBonus;
        finalBonuses.moveSpeedBonus = setBonuses.moveSpeedBonus;
        finalBonuses.hitRateBonus = setBonuses.hitRateBonus;
        finalBonuses.evasionRateBonus = setBonuses.evasionRateBonus;
        finalBonuses.lifeStealBonus = setBonuses.lifeStealBonus;
        finalBonuses.reductionCoolDownBonus = setBonuses.reductionCoolDownBonus;
        finalBonuses.physicalResistanceBonus = setBonuses.physicalResistanceBonus;
        finalBonuses.magicalResistanceBonus = setBonuses.magicalResistanceBonus;

        if (showDebugInfo)
        {
            Debug.Log($"[SetBonusManager] Set bonus calculated:");
            Debug.Log($"  Base ATK: {characterData.baseAttackDamage} × {setBonuses.attackDamageBonus}% = +{finalBonuses.attackDamageBonus}");
            Debug.Log($"  Base HP: {characterData.baseMaxHp} × {setBonuses.maxHpBonus}% = +{finalBonuses.maxHpBonus}");
        }

        return finalBonuses;
    }
    private EquipmentStats CalculateBonusFromCurrentStats(EquipmentStats setBonuses)
    {
        EquipmentStats finalBonuses = new EquipmentStats();

        if (character == null)
        {
            return setBonuses; // ใช้ค่าคงที่
        }

        // ✅ ใช้ current stats ของ character (แต่ต้องระวัง equipment bonus ที่อาจรวมอยู่)
        // หัก equipment bonus ออกเพื่อได้ base stats ประมาณ
        var equipmentStats = equipmentManager?.GetTotalStats() ?? new EquipmentStats();

        int baseAttack = character.AttackDamage - equipmentStats.attackDamageBonus;
        int baseMagic = character.MagicDamage - equipmentStats.magicDamageBonus;
        int baseHp = character.MaxHp - equipmentStats.maxHpBonus;
        int baseMana = character.MaxMana - equipmentStats.maxManaBonus;
        int baseArmor = character.Armor - equipmentStats.armorBonus;

        finalBonuses.attackDamageBonus = CalculateBonusValue(baseAttack, setBonuses.attackDamageBonus);
        finalBonuses.magicDamageBonus = CalculateBonusValue(baseMagic, setBonuses.magicDamageBonus);
        finalBonuses.maxHpBonus = CalculateBonusValue(baseHp, setBonuses.maxHpBonus);
        finalBonuses.maxManaBonus = CalculateBonusValue(baseMana, setBonuses.maxManaBonus);
        finalBonuses.armorBonus = CalculateBonusValue(baseArmor, setBonuses.armorBonus);

        // Stats ที่เป็น % อยู่แล้ว
        finalBonuses.criticalChanceBonus = setBonuses.criticalChanceBonus;
        finalBonuses.criticalMultiplierBonus = setBonuses.criticalMultiplierBonus;
        finalBonuses.attackSpeedBonus = setBonuses.attackSpeedBonus;
        finalBonuses.moveSpeedBonus = setBonuses.moveSpeedBonus;
        finalBonuses.hitRateBonus = setBonuses.hitRateBonus;
        finalBonuses.evasionRateBonus = setBonuses.evasionRateBonus;
        finalBonuses.lifeStealBonus = setBonuses.lifeStealBonus;
        finalBonuses.reductionCoolDownBonus = setBonuses.reductionCoolDownBonus;
        finalBonuses.physicalResistanceBonus = setBonuses.physicalResistanceBonus;
        finalBonuses.magicalResistanceBonus = setBonuses.magicalResistanceBonus;

        if (showDebugInfo)
        {
            Debug.Log($"[SetBonusManager] Fallback calculation:");
            Debug.Log($"  Estimated Base ATK: {baseAttack} × {setBonuses.attackDamageBonus}% = +{finalBonuses.attackDamageBonus}");
        }

        return finalBonuses;
    }

    /// <summary>
    /// 🔧 คำนวณค่า bonus จาก base stat และ percentage
    /// </summary>
    private int CalculateBonusValue(int baseStat, float bonusPercentage)
    {
        if (bonusPercentage == 0f) return 0;

        // สมมุติว่าค่าที่ได้มาเป็น percentage (เช่น 100 = 100%)
        return Mathf.RoundToInt(baseStat * bonusPercentage / 100f);
    }

    /// <summary>
    /// ดู set bonus ที่ใช้งานอยู่
    /// </summary>
    public List<ActiveSetBonus> GetCurrentSetBonuses()
    {
        return new List<ActiveSetBonus>(currentSetBonuses);
    }

    /// <summary>
    /// ดู set bonus ทั้งหมดที่เป็นไปได้
    /// </summary>
    public List<EquipmentSet> GetAllPossibleSets(List<ItemData> equippedItems)
    {
        List<EquipmentSet> possibleSets = new List<EquipmentSet>();

        foreach (var equipmentSet in allEquipmentSets)
        {
            if (equipmentSet.GetEquippedPiecesCount(equippedItems) >= 1)
            {
                possibleSets.Add(equipmentSet);
            }
        }

        return possibleSets;
    }

    /// <summary>
    /// ดู set bonus info สำหรับแสดงใน UI
    /// </summary>
    public string GetSetBonusInfoText()
    {
        if (currentSetBonuses.Count == 0)
        {
            return "No set bonuses active";
        }

        List<string> setBonusTexts = new List<string>();

        foreach (var setBonus in currentSetBonuses)
        {
            setBonusTexts.Add(setBonus.equipmentSet.GetSetBonusInfo(setBonus.equippedPieces));
        }

        return string.Join("\n\n", setBonusTexts);
    }

    /// <summary>
    /// Force update set bonuses
    /// </summary>
    public void ForceUpdateSetBonuses()
    {
        UpdateSetBonuses();
    }

    /// <summary>
    /// Clear all set bonuses
    /// </summary>
    public void ClearAllSetBonuses()
    {
        currentSetBonuses.Clear();

        if (equipmentManager != null)
        {
            equipmentManager.ClearSetBonuses();
        }

        OnSetBonusChanged?.Invoke(character, currentSetBonuses);
    }

    /// <summary>
    /// ✅ Manual method สำหรับเพิ่ม EquipmentSet
    /// </summary>
    public void AddEquipmentSet(EquipmentSet equipmentSet)
    {
        if (equipmentSet != null && !allEquipmentSets.Contains(equipmentSet))
        {
            allEquipmentSets.Add(equipmentSet);

            if (showDebugInfo)
            {
                Debug.Log($"[SetBonusManager] Manually added equipment set: {equipmentSet.setName}");
            }
        }
    }

    /// <summary>
    /// ✅ Manual method สำหรับ refresh การเชื่อมต่อ
    /// </summary>
   
}