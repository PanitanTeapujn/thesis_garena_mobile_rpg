using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System;

[System.Serializable]
public class LevelUpStats
{
    [Header("Stats Bonus Per Level")]
    public int hpBonusPerLevel = 10;
    public int manaBonusPerLevel = 5;
    public int attackDamageBonusPerLevel = 2;
    public int magicDamageBonusPerLevel = 2;
    public int armorBonusPerLevel = 1;
    public float criticalChanceBonusPerLevel = 0.5f;
    public float moveSpeedBonusPerLevel = 0f;
    public float lifeStealBonusPerLevel = 0f; // Optional: 0.1f = +0.1% ต่อ level
    public int magicArmorBonusPerLevel = 1; // Optional: 0.1f = +0.1% ต่อ level

}

[System.Serializable]
public class ExpSettings
{
    [Header("Experience Settings")]
    public int baseExpToNextLevel = 100;
    public float expGrowthRate = 1.2f;
    public int maxLevel = 100;

    [Header("Enemy Exp Rewards")]
    public int baseEnemyExp = 15;
    public float enemyExpLevelMultiplier = 1.1f;
}

public class LevelManager : NetworkBehaviour
{
    [Header("Level Settings")]
    public LevelUpStats levelUpStats = new LevelUpStats();
    public ExpSettings expSettings = new ExpSettings();
    [Header("🆕 Base Stats (Level + ScriptableObject)")]
    private int baseMaxHp;
    private int baseMaxMana;
    private int baseAttackDamage;
    private int baseMagicDamage;
    private int baseArmor;
    private float baseCriticalChance;
    private float baseCriticalDamageBonus;
    private float baseMoveSpeed;
    private float baseHitRate;
    private float baseEvasionRate;
    private float baseAttackSpeed;
    private float baseReductionCoolDown;
    private float baseLifeSteal;
    private int baseMagicArmor;
    private float baseHealthRegen;
    private float baseManaRegen;
    // ========== Network Properties ==========
    [Networked] public int CurrentLevel { get; set; } = 1;
    [Networked] public int CurrentExp { get; set; } = 0;
    [Networked] public int ExpToNextLevel { get; set; } = 100;
    [Networked] public bool IsInitialized { get; set; } = false;

    // ========== Events ==========
    public static event Action<Character, int> OnLevelUp;
    public static event Action<Character, int, int> OnExpGain;
    public static event Action<Character, LevelUpStats> OnStatsIncreased;

    // ========== Component References ==========
    private Character character;
    private bool hasTriedFirebaseLoad = false;

    protected virtual void Awake()
    {
        character = GetComponent<Character>();
    }

    protected virtual void Start()
    {
        CombatManager.OnCharacterDeath += HandleCharacterDeath;

        // Delayed initialization - ไม่ block การ spawn
        Invoke("TryInitialize", 1f);
    }

    protected virtual void OnDestroy()
    {
        CombatManager.OnCharacterDeath -= HandleCharacterDeath;
    }

    public override void Spawned()
    {
        base.Spawned();

        // Quick initialization for network
        if (HasStateAuthority && !IsInitialized)
        {
            // Check if this is the correct character type
            if (IsCorrectCharacter())
            {
                string activeCharacterType = PersistentPlayerData.Instance.GetCurrentActiveCharacter();
                InitializeForCharacter(activeCharacterType);
            }
            else
            {
            }
        }
    }

    public void RefreshCharacterData()
    {
        if (!HasInputAuthority) return;

        try
        {
            string activeCharacterType = PersistentPlayerData.Instance.GetCurrentActiveCharacter();
            CharacterProgressData characterData = PersistentPlayerData.Instance.GetCharacterData(activeCharacterType);

            if (characterData != null && IsCorrectCharacter())
            {
                Debug.Log($"[LevelManager] 🔄 Refreshing character data for {activeCharacterType}...");

                // Apply level และ exp
                CurrentLevel = characterData.currentLevel;
                CurrentExp = characterData.currentExp;
                ExpToNextLevel = characterData.expToNextLevel;

                // ✅ ตรวจสอบว่าเป็นการโหลดครั้งแรกหรือไม่
                bool isFirstLoad = (character.CurrentHp == 0 || character.MaxHp == 0);
                float hpPercentage = isFirstLoad ? 1f : (character.MaxHp > 0 ? (float)character.CurrentHp / character.MaxHp : 1f);
                float manaPercentage = isFirstLoad ? 1f : (character.MaxMana > 0 ? (float)character.CurrentMana / character.MaxMana : 1f);

                if (isFirstLoad)
                {
                    Debug.Log("[LevelManager] First login detected - setting full HP/Mana");
                }

                // ✅ ใช้ base stats แทน total stats เพื่อป้องกันการซ้ำ
                if (characterData.HasValidBaseStats())
                {
                    Debug.Log("[LevelManager] Using base stats from Firebase");

                    character.MaxHp = characterData.baseMaxHp;
                    character.MaxMana = characterData.baseMaxMana;
                    character.AttackDamage = characterData.baseAttackDamage;
                    character.MagicDamage = characterData.baseMagicDamage;
                    character.Armor = characterData.baseArmor;
                    character.CriticalChance = characterData.baseCriticalChance;
                    character.UpdateCriticalDamageBonus(characterData.baseCriticalDamageBonus, false);
                    character.MoveSpeed = characterData.baseMoveSpeed;
                    character.HitRate = characterData.baseHitRate;
                    character.EvasionRate = characterData.baseEvasionRate;
                    character.AttackSpeed = characterData.baseAttackSpeed;
                    character.ReductionCoolDown = characterData.baseReductionCoolDown;
                    character.LifeSteal = characterData.baseLifeSteal;

                    // ✅ เพิ่ม 3 stats ใหม่
                    character.MagicArmor = characterData.baseMagicArmor;
                    character.HealthRegen = characterData.baseHealthRegen;
                    character.ManaRegen = characterData.baseManaRegen;

                    // ✅ ปรับ HP/Mana ตามเปอร์เซ็นต์ที่คำนวณ
                    character.CurrentHp = Mathf.RoundToInt(character.MaxHp * hpPercentage);
                    character.CurrentMana = Mathf.RoundToInt(character.MaxMana * manaPercentage);
                    character.CurrentHp = Mathf.Clamp(character.CurrentHp, 1, character.MaxHp);
                    character.CurrentMana = Mathf.Clamp(character.CurrentMana, 0, character.MaxMana);

                    Debug.Log($"[LevelManager] ✅ Applied base stats from Firebase:");
                    Debug.Log($"  HP={character.CurrentHp}/{character.MaxHp}, ARM={character.Armor}");
                    Debug.Log($"  MAGIC_ARM={character.MagicArmor}, HEALTH_REGEN={character.HealthRegen:F1}, MANA_REGEN={character.ManaRegen:F1}");
                }
                else
                {
                    Debug.LogWarning("[LevelManager] No valid base stats, using total stats as fallback");

                    // ใช้ total stats เป็น fallback (กรณีข้อมูลเก่า)
                    character.MaxHp = characterData.totalMaxHp;
                    character.MaxMana = characterData.totalMaxMana;
                    character.AttackDamage = characterData.totalAttackDamage;
                    character.MagicDamage = characterData.totalMagicDamage;
                    character.Armor = characterData.totalArmor;
                    character.CriticalChance = characterData.totalCriticalChance;
                    character.UpdateCriticalDamageBonus(characterData.totalCriticalDamageBonus, false);
                    character.MoveSpeed = characterData.totalMoveSpeed;
                    character.HitRate = characterData.totalHitRate;
                    character.EvasionRate = characterData.totalEvasionRate;
                    character.AttackSpeed = characterData.totalAttackSpeed;
                    character.ReductionCoolDown = characterData.totalReductionCoolDown;
                    character.LifeSteal = characterData.totalLifeSteal;

                    // ✅ เพิ่ม 3 stats ใหม่
                    character.MagicArmor = characterData.totalMagicArmor;
                    character.HealthRegen = characterData.totalHealthRegen;
                    character.ManaRegen = characterData.totalManaRegen;

                    // ✅ ปรับ HP/Mana ตามเปอร์เซ็นต์ที่คำนวณ
                    character.CurrentHp = Mathf.RoundToInt(character.MaxHp * hpPercentage);
                    character.CurrentMana = Mathf.RoundToInt(character.MaxMana * manaPercentage);
                    character.CurrentHp = Mathf.Clamp(character.CurrentHp, 1, character.MaxHp);
                    character.CurrentMana = Mathf.Clamp(character.CurrentMana, 0, character.MaxMana);
                }

                character.ForceUpdateNetworkState();

                // แจ้ง stats changed
                Character.RaiseOnStatsChanged();
            }
            else
            {
                Debug.LogWarning($"[LevelManager] No character data found for {activeCharacterType}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LevelManager] ❌ Error in RefreshCharacterData: {e.Message}");
        }
    }

    public void OnEquipmentLoadedRecalculateStats()
    {
        try
        {
            Debug.Log($"[LevelManager] 🔄 Recalculating stats after equipment loaded...");

            if (character == null)
            {
                Debug.LogError("[LevelManager] Character is null!");
                return;
            }

            // อัปเดต stats จาก Firebase (base stats + level bonuses)
            RefreshCharacterData();

            // ให้ Character apply equipment stats ทับ
            character.ApplyLoadedEquipmentStatsWithReset();

            // บันทึก total stats ใหม่
            ForceSaveToFirebase();

            Debug.Log($"[LevelManager] ✅ Stats recalculated after equipment loaded");
            Debug.Log($"[LevelManager] Final stats: HP={character.MaxHp}, ATK={character.AttackDamage}, ARM={character.Armor}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LevelManager] ❌ Error recalculating stats: {e.Message}");
        }
    }


    // ========== Lightweight Initialization ==========
    private void TryInitialize()
    {
        if (HasInputAuthority && !hasTriedFirebaseLoad)
        {
            hasTriedFirebaseLoad = true;
            TryLoadFromFirebase();
        }
    }

    private void TryLoadFromFirebase()
    {
        // ไม่ใช้ coroutine - check แบบง่ายๆ
        if (PersistentPlayerData.Instance.HasValidData())
        {
            ApplyFirebaseData();
        }
        else
        {
            // ลองรอ 2 วินาที แล้วค่อยใช้ default
            Invoke("FallbackToDefault", 2f);
        }
    }
    private bool IsCorrectCharacter()
    {
        string activeCharacterType = PersistentPlayerData.Instance.GetCurrentActiveCharacter();

        // Get character type from the Character component
        string currentCharacterType = GetCharacterTypeFromComponent();

        return activeCharacterType == currentCharacterType;
    }

    private string GetCharacterTypeFromComponent()
    {
        if (character?.characterStats != null)
        {
            // Try to determine character type from CharacterStats name
            string statsName = character.characterStats.name;

            if (statsName.Contains("BloodKnight")) return "BloodKnight";
            if (statsName.Contains("Archer")) return "Archer";
            if (statsName.Contains("Assassin")) return "Assassin";
            if (statsName.Contains("IronJuggernaut")) return "IronJuggernaut";
        }

        // Fallback to checking component name or tag
        string objectName = character.gameObject.name;
        if (objectName.Contains("BloodKnight")) return "BloodKnight";
        if (objectName.Contains("Archer")) return "Archer";
        if (objectName.Contains("Assassin")) return "Assassin";
        if (objectName.Contains("IronJuggernaut")) return "IronJuggernaut";

        return "Assassin"; // Default fallback
    }

    private void ApplyFirebaseData()
    {
        string activeCharacterType = PersistentPlayerData.Instance.GetCurrentActiveCharacter();
        CharacterProgressData characterData = PersistentPlayerData.Instance.GetCharacterData(activeCharacterType);

        if (characterData != null && HasInputAuthority)
        {
            RPC_ApplyFirebaseStats(
                characterData.currentLevel,
                characterData.currentExp,
                characterData.expToNextLevel,
                characterData.totalMaxHp,
                characterData.totalMaxMana,
                characterData.totalAttackDamage,
                characterData.totalMagicDamage,
                characterData.totalArmor,
                characterData.totalCriticalChance,
                characterData.totalCriticalDamageBonus,
                characterData.totalMoveSpeed,
                characterData.totalHitRate,
                characterData.totalEvasionRate,
                characterData.totalAttackSpeed,
                characterData.totalReductionCoolDown,
                characterData.totalLifeSteal,
               characterData.totalMagicArmor,
            characterData.totalHealthRegen,
            characterData.totalManaRegen// ✅ เพิ่ม LifeSteal
            );

            Debug.Log($"✅ Applied Firebase data for {activeCharacterType}: Level {characterData.currentLevel}, LifeSteal {characterData.totalLifeSteal:F1}%");
        }
    }


   


    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_ApplyFirebaseStats(int level, int exp, int expToNext, int maxHp, int maxMana,
     int attackDamage, int magicDamage, int armor, float critChance, float critDamageBonus, float moveSpeed,
     float hitRate, float evasionRate, float attackSpeed, float reductionCoolDown, float lifeSteal,int magicArmor,float heathRegen, float manaRegen) // ✅ เพิ่ม LifeSteal parameter
    {
        // Apply base stats รวม LifeSteal
        ApplyBaseStatsOnly(level, exp, expToNext, maxHp, maxMana, attackDamage, magicDamage, armor,
                           critChance, critDamageBonus, moveSpeed, hitRate, evasionRate, attackSpeed, reductionCoolDown, lifeSteal,magicArmor, heathRegen, manaRegen);

        Debug.Log($"🔧 Applied Firebase base stats including LifeSteal: {lifeSteal:F1}%");

        // Broadcast to all clients
        RPC_BroadcastStats(level, exp, expToNext, maxHp, maxMana, attackDamage, magicDamage, armor, critChance, critDamageBonus, moveSpeed,
            hitRate, evasionRate, attackSpeed, reductionCoolDown, lifeSteal, magicArmor, heathRegen, manaRegen);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastStats(int level, int exp, int expToNext, int maxHp, int maxMana,
        int attackDamage, int magicDamage, int armor, float critChance, float critDamageBonus, float moveSpeed,
        float hitRate, float evasionRate, float attackSpeed, float reductionCoolDown, float lifeSteal,int magicArmor,float heathRegen, float manaRegen) // ✅ เพิ่ม LifeSteal parameter
    {
        // Apply base stats รวม LifeSteal
        ApplyBaseStatsOnly(level, exp, expToNext, maxHp, maxMana, attackDamage, magicDamage, armor,
                           critChance, critDamageBonus, moveSpeed, hitRate, evasionRate, attackSpeed, reductionCoolDown, lifeSteal,magicArmor, heathRegen, manaRegen);

        Debug.Log($"🔧 Broadcasted base stats including LifeSteal: {lifeSteal:F1}%");
    }



    /// <summary>
    /// 🆕 Apply เฉพาะ base stats (ไม่รวม equipment bonuses)
    /// </summary>
    // 🆕 แก้ไข ApplyBaseStatsOnly ให้คำนวณ base stats ก่อน
    // ในไฟล์ LevelManager.cs - แก้ไข ApplyBaseStatsOnly method
    private void ApplyBaseStatsOnly(int level, int exp, int expToNext, int maxHp, int maxMana,
     int attackDamage, int magicDamage, int armor, float critChance, float critDamageBonus,
     float moveSpeed, float hitRate, float evasionRate, float attackSpeed, float reductionCoolDown,
     float lifeSteal, int magicArmor, float heathRegen, float manaRegen) // ✅ เพิ่ม parameters
    {
        CurrentLevel = level;
        CurrentExp = exp;
        ExpToNextLevel = expToNext;

        // ✅ เก็บ HP/Mana percentage ก่อนเปลี่ยน stats - แต่ให้ default เป็น 100%
        float hpPercentage = character.MaxHp > 0 ? (float)character.CurrentHp / character.MaxHp : 1f;
        float manaPercentage = character.MaxMana > 0 ? (float)character.CurrentMana / character.MaxMana : 1f;

        // ✅ ตรวจสอบถ้าเป็นการโหลดครั้งแรกหลัง login
        bool isFirstLoad = (character.CurrentHp == 0 || character.MaxHp == 0);
        if (isFirstLoad)
        {
            hpPercentage = 1f;
            manaPercentage = 1f;
            Debug.Log("[LevelManager] First load detected, setting full HP/Mana");
        }

        // ✅ คำนวณ base stats ใหม่จาก ScriptableObject + Level bonuses + Stat upgrades
        CalculateBaseStatsWithUpgrades();

        // ✅ Apply base stats ไปยัง character
        character.MaxHp = baseMaxHp;
        character.MaxMana = baseMaxMana;
        character.AttackDamage = baseAttackDamage;
        character.MagicDamage = baseMagicDamage;
        character.Armor = baseArmor;
        character.CriticalChance = baseCriticalChance;
        character.UpdateCriticalDamageBonus(baseCriticalDamageBonus, false);
        character.MoveSpeed = baseMoveSpeed;
        character.HitRate = baseHitRate;
        character.EvasionRate = baseEvasionRate;
        character.AttackSpeed = baseAttackSpeed;
        character.ReductionCoolDown = baseReductionCoolDown;
        character.LifeSteal = baseLifeSteal;

        // ✅ เพิ่ม 3 stats ใหม่
        character.MagicArmor = baseMagicArmor;
        character.HealthRegen = baseHealthRegen;
        character.ManaRegen = baseManaRegen;

        // ✅ ปรับ currentHp และ currentMana ตามเปอร์เซ็นต์
        character.CurrentHp = Mathf.RoundToInt(character.MaxHp * hpPercentage);
        character.CurrentMana = Mathf.RoundToInt(character.MaxMana * manaPercentage);
        character.CurrentHp = Mathf.Clamp(character.CurrentHp, 1, character.MaxHp);
        character.CurrentMana = Mathf.Clamp(character.CurrentMana, 0, character.MaxMana);

        character.ForceUpdateNetworkState();
        IsInitialized = true;

        Debug.Log($"[LevelManager] ✅ Applied base stats:");
        Debug.Log($"  HP={baseMaxHp}, Current HP={character.CurrentHp}/{character.MaxHp} ({hpPercentage:P0})");
        Debug.Log($"  MAGIC_ARM={baseMagicArmor}, HEALTH_REGEN={baseHealthRegen:F1}, MANA_REGEN={baseManaRegen:F1}");
    }



    // ========== Experience System (Simplified) ==========
    public virtual void GainExp(int expAmount)
    {
        if (!HasStateAuthority && !HasInputAuthority) return;
        if (!IsInitialized) return;

        CurrentExp += expAmount;

        // Check level up (max 1 level per call to prevent loops)
        if (CurrentExp >= ExpToNextLevel && CurrentLevel < expSettings.maxLevel)
        {
            LevelUp();
        }

        OnExpGain?.Invoke(character, expAmount, CurrentExp);

        // Quick save
        QuickSave();
    }

    // แก้ไขใน LevelManager.cs - LevelUp method
    private void LevelUp()
    {
        CurrentExp -= ExpToNextLevel;
        CurrentLevel++;
        ExpToNextLevel = CalculateExpToNextLevel(CurrentLevel);

        // Apply stat bonuses (แบบเดิม)
        character.MaxHp += levelUpStats.hpBonusPerLevel;
        character.MaxMana += levelUpStats.manaBonusPerLevel;
        character.AttackDamage += levelUpStats.attackDamageBonusPerLevel;
        character.MagicDamage += levelUpStats.magicDamageBonusPerLevel;
        character.Armor += levelUpStats.armorBonusPerLevel;
        character.CriticalChance += levelUpStats.criticalChanceBonusPerLevel;
        character.MoveSpeed += levelUpStats.moveSpeedBonusPerLevel;
        character.LifeSteal += levelUpStats.lifeStealBonusPerLevel;
        character.MagicArmor += levelUpStats.magicArmorBonusPerLevel;

        // ✅ Full restore on level up
        character.CurrentHp = character.MaxHp;
        character.CurrentMana = character.MaxMana;

        // 🆕 เพิ่ม stat point เมื่อ level up
        AddStatPointOnLevelUp();

        // ✅ บันทึก current stats เป็น total stats (รวม equipment แล้ว)
        SaveCurrentStatsAfterLevelUp();

        // ✅ Force sync network state หลัง level up
        if (HasStateAuthority)
        {
            character.NetworkedMaxHp = character.MaxHp;
            character.NetworkedCurrentHp = character.CurrentHp;
            character.NetworkedMaxMana = character.MaxMana;
            character.NetworkedCurrentMana = character.CurrentMana;

            // Broadcast level up to all clients
            RPC_BroadcastLevelUp(CurrentLevel, character.MaxHp, character.MaxMana);
        }
        else if (HasInputAuthority)
        {
            // Client ส่ง request ไป server
            RPC_RequestLevelUp();
        }

        Debug.Log($"🎉 {character.CharacterName} reached Level {CurrentLevel}!");

        // Fire events
        OnLevelUp?.Invoke(character, CurrentLevel);
        OnStatsIncreased?.Invoke(character, levelUpStats);
    }
    private void SaveCurrentStatsAfterLevelUp()
    {
        if (!HasInputAuthority) return;

        try
        {
            string characterType = PersistentPlayerData.Instance.GetCurrentActiveCharacter();
            var characterData = PersistentPlayerData.Instance.GetOrCreateCharacterData(characterType);

            if (characterData != null)
            {
                // บันทึก level และ exp
                characterData.currentLevel = CurrentLevel;
                characterData.currentExp = CurrentExp;
                characterData.expToNextLevel = ExpToNextLevel;

                // ✅ บันทึก total stats ปัจจุบัน (รวม equipment แล้ว) - ไม่แตะ base stats
                characterData.UpdateTotalStats(
                    character.MaxHp, character.MaxMana, character.AttackDamage, character.MagicDamage, character.Armor,
                    character.CriticalChance, character.CriticalDamageBonus, character.MoveSpeed,
                    character.HitRate, character.EvasionRate, character.AttackSpeed, character.ReductionCoolDown, character.LifeSteal,character.MagicArmor,character.HealthRegen,character.ManaRegen
                );

                // บันทึกลง Firebase
                PersistentPlayerData.Instance.SavePlayerDataAsync();

                Debug.Log($"[LevelManager] 💾 Saved total stats after level up (preserving equipment bonuses)");
                Debug.Log($"  Total stats: HP={character.MaxHp}, ATK={character.AttackDamage}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LevelManager] ❌ Error saving stats after level up: {e.Message}");
        }
    }

    // ✅ เพิ่ม method ใหม่สำหรับ apply base stats
   
    private void AddStatPointOnLevelUp()
    {
        try
        {
            if (!HasInputAuthority) return;

            string activeCharacterType = PersistentPlayerData.Instance.GetCurrentActiveCharacter();
            var characterData = PersistentPlayerData.Instance.GetOrCreateCharacterData(activeCharacterType);

            if (characterData != null)
            {
                // ให้ stat point 1 point ต่อ level up
                characterData.AddStatPoints(1);

                Debug.Log($"🎯 {activeCharacterType} gained 1 stat point! Total available: {characterData.availableStatPoints}");

                // บันทึกลง Firebase
                PersistentPlayerData.Instance.SavePlayerDataAsync();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LevelManager] ❌ Error adding stat point: {e.Message}");
        }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastLevelUp(int newLevel, int newMaxHp, int newMaxMana)
    {
        CurrentLevel = newLevel;
        character.MaxHp = newMaxHp;
        character.MaxMana = newMaxMana;
        character.CurrentHp = newMaxHp; // ✅ รี HP/Mana เต็ม
        character.CurrentMana = newMaxMana;

        character.NetworkedMaxHp = newMaxHp;
        character.NetworkedCurrentHp = newMaxHp;
        character.NetworkedMaxMana = newMaxMana;
        character.NetworkedCurrentMana = newMaxMana;

        Debug.Log($"✅ Level up synced: Level {newLevel}, HP: {newMaxHp}, Mana: {newMaxMana}");
    }

    // ✅ เพิ่ม RPC methods สำหรับ Level Up sync
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestLevelUp()
    {
        // Server ตรวจสอบและ level up
        if (CurrentExp >= ExpToNextLevel && CurrentLevel < expSettings.maxLevel)
        {
            LevelUp();
        }
    }

    // ========== Quick Save (Non-blocking) ==========
    private void QuickSave()
    {
        if (!HasInputAuthority) return;

        string activeCharacterType = PersistentPlayerData.Instance.GetCurrentActiveCharacter();

        if (PersistentPlayerData.Instance.multiCharacterData != null)
        {
            // 🆕 Debug ก่อน save
            Debug.Log($"[LevelManager] 💾 Quick saving total stats (including equipment bonuses)...");
            Debug.Log($"  ATK: {character.AttackDamage}, ARM: {character.Armor}, CRIT: {character.CriticalChance:F1}%");

            // Update total stats (รวม equipment bonuses) ลง MultiCharacterPlayerData
            PersistentPlayerData.Instance.UpdateLevelAndStats(
                CurrentLevel,
                CurrentExp,
                ExpToNextLevel,
                character.MaxHp,
                character.MaxMana,
                character.AttackDamage,
                character.MagicDamage,
                character.Armor,
                character.CriticalChance,
                character.CriticalDamageBonus,
                character.MoveSpeed,
                character.HitRate,
                character.EvasionRate,
                character.AttackSpeed,
                character.ReductionCoolDown,
                character.LifeSteal,
                character.MagicArmor,
                character.HealthRegen,
                character.ManaRegen
            );

            Debug.Log($"💾 Quick saved {activeCharacterType} - Level {CurrentLevel} with total stats");
        }
    }

    private void InitializeForCharacter(string characterType)
    {
        if (IsInitialized) return;

        CharacterProgressData characterData = PersistentPlayerData.Instance.GetCharacterData(characterType);

        if (characterData != null)
        {
            // Apply character-specific data
            CurrentLevel = characterData.currentLevel;
            CurrentExp = characterData.currentExp;
            ExpToNextLevel = characterData.expToNextLevel;

            character.MaxHp = characterData.totalMaxHp;
            character.CurrentHp = characterData.totalMaxHp;
            character.MaxMana = characterData.totalMaxMana;
            character.CurrentMana = characterData.totalMaxMana;
            character.AttackDamage = characterData.totalAttackDamage;
            character.MagicDamage = characterData.totalMagicDamage;
            character.Armor = characterData.totalArmor;
            character.CriticalChance = characterData.totalCriticalChance;
            character.CriticalDamageBonus = characterData.totalCriticalDamageBonus;
            character.MoveSpeed = characterData.totalMoveSpeed;
            character.HitRate = characterData.totalHitRate;
            character.EvasionRate = characterData.totalEvasionRate;
            character.AttackSpeed = characterData.totalAttackSpeed;
            character.ReductionCoolDown = characterData.totalReductionCoolDown;
            character.LifeSteal = characterData.totalLifeSteal;
            character.MagicArmor = characterData.totalMagicArmor;
            character.HealthRegen = characterData.totalHealthRegen;
            character.ManaRegen = characterData.totalManaRegen;
            

            IsInitialized = true;
            Debug.Log($"✅ Initialized LevelManager for {characterType} - Level {CurrentLevel}");
        }
        else
        {
            Debug.LogWarning($"[LevelManager] No data found for {characterType}, using defaults");
        }
    }



    private void CalculateBaseStatsWithUpgrades()
    {
        if (character?.characterStats == null) return;

        // ✅ ใช้ ScriptableObject เป็นหลัก ไม่ใช้ stats ปัจจุบันของ character
        int levelBonus = CurrentLevel - 1;

        // ✅ คำนวณจาก ScriptableObject + Level bonuses เท่านั้น
        int baseHp = character.characterStats.maxHp + (levelBonus * levelUpStats.hpBonusPerLevel);
        int baseAttack = character.characterStats.attackDamage + (levelBonus * levelUpStats.attackDamageBonusPerLevel);
        int baseMagic = character.characterStats.magicDamage + (levelBonus * levelUpStats.magicDamageBonusPerLevel);
        int baseMana = character.characterStats.maxMana + (levelBonus * levelUpStats.manaBonusPerLevel);
        int baseArmors = character.characterStats.arrmor + (levelBonus * levelUpStats.armorBonusPerLevel);
        float baseCrit = character.characterStats.criticalChance + (levelBonus * levelUpStats.criticalChanceBonusPerLevel);
        float baseCritDmg = character.characterStats.criticalDamageBonus;
        float baseSpeed = character.characterStats.moveSpeed + (levelBonus * levelUpStats.moveSpeedBonusPerLevel);
        float baseAtkSpeed = character.characterStats.attackSpeed;
        float baseEvasion = character.characterStats.evasionRate;
        float baseCdr = character.characterStats.reductionCoolDown;
        float baseHit = character.characterStats.hitRate;
        float baseLifeSteals = character.characterStats.lifeSteal + (levelBonus * levelUpStats.lifeStealBonusPerLevel);

        // ✅ แก้ไข Magic Armor - ใช้ค่าจาก ScriptableObject โดยตรง
        int baseMagicArmors = character.characterStats.magicArmor + (levelBonus * levelUpStats.magicArmorBonusPerLevel);
        // ไม่ใช้ character.characterStats.arrmor สำหรับ Magic Armor

        float baseHealthRegens = character.characterStats.healthRegen;
        float baseManaRegens = character.characterStats.manaRegen;

        // ✅ เพิ่ม stat bonuses จาก upgrades
        try
        {
            string activeCharacterType = PersistentPlayerData.Instance.GetCurrentActiveCharacter();
            var characterData = PersistentPlayerData.Instance.GetOrCreateCharacterData(activeCharacterType);

            if (characterData != null)
            {
                characterData.GetStatBonuses(out int hpBonus, out int atkBonus, out float critDmgBonus,
                                           out float atkSpeedBonus, out float evaBonus, out float critChanceBonus,
                                           out int magicBonus, out int manaBonus, out float cdrBonus,
                                           out float hitBonus, out float lifeStealBonus, out float speedBonus);

                // รวม bonuses เข้ากับ base stats
                baseHp += hpBonus;
                baseAttack += atkBonus;
                baseCritDmg += critDmgBonus;
                baseAtkSpeed += atkSpeedBonus;
                baseEvasion += evaBonus;
                baseCrit += critChanceBonus;
                baseMagic += magicBonus;
                baseMana += manaBonus;
                baseCdr += cdrBonus;
                baseHit += hitBonus;
                baseLifeSteals += lifeStealBonus;
                baseSpeed += speedBonus;

                Debug.Log($"[LevelManager] 🎯 Applied stat upgrade bonuses:");
                Debug.Log($"  STR={characterData.upgradedSTR} → HP+{hpBonus}, ATK+{atkBonus}, CRIT_DMG+{critDmgBonus:F1}%");
                Debug.Log($"  DEX={characterData.upgradedDEX} → AS+{atkSpeedBonus:F1}%, EVA+{evaBonus:F1}%, CRIT+{critChanceBonus:F1}%");
                Debug.Log($"  INT={characterData.upgradedINT} → MAG+{magicBonus}, MANA+{manaBonus}, CDR+{cdrBonus:F1}%");
                Debug.Log($"  MAS={characterData.upgradedMAS} → HIT+{hitBonus:F1}%, LST+{lifeStealBonus:F1}%, SPD+{speedBonus:F1}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LevelManager] ❌ Error applying stat bonuses: {e.Message}");
        }

        // ✅ บันทึก base stats ที่คำนวณแล้ว
        baseMaxHp = baseHp;
        baseMaxMana = baseMana;
        baseAttackDamage = baseAttack;
        baseMagicDamage = baseMagic;
        baseArmor = baseArmors;
        baseCriticalChance = baseCrit;
        baseCriticalDamageBonus = baseCritDmg;
        baseMoveSpeed = baseSpeed;
        baseAttackSpeed = baseAtkSpeed;
        baseEvasionRate = baseEvasion;
        baseReductionCoolDown = baseCdr;
        baseHitRate = baseHit;
        baseLifeSteal = baseLifeSteals;
        baseMagicArmor = baseMagicArmors; // ✅ ใช้ค่าที่คำนวณถูกต้อง
        baseHealthRegen = baseHealthRegens;
        baseManaRegen = baseManaRegens;

        Debug.Log($"[LevelManager] 📊 Final base stats (ScriptableObject + Level + Upgrades):");
        Debug.Log($"  HP={baseHp}, ATK={baseAttack}, MAG={baseMagic}, ARM={baseArmors}");
        Debug.Log($"  MAGIC_ARM={baseMagicArmors}, HEALTH_REGEN={baseHealthRegens:F1}, MANA_REGEN={baseManaRegens:F1}");
        Debug.Log($"  CRIT={baseCrit:F1}%, CRIT_DMG={baseCritDmg:F1}%, LifeSteal={baseLifeSteals:F1}%");
    }
    public void ResetToBaseStats()
    {
       

        // ไม่ทำอะไร - เก็บ stats เดิมไว้
        return;
    }
    // ========== Enemy Death Handler (Simplified) ==========
    private void HandleCharacterDeath(Character deadCharacter)
    {
        if (deadCharacter.gameObject.layer != LayerMask.NameToLayer("Enemy")) return;

        Hero[] heroes = FindNearbyHeroes(deadCharacter.transform.position, 10f);
        if (heroes.Length == 0) return;

        int expReward = expSettings.baseEnemyExp;
        int expPerHero = Mathf.Max(1, expReward / heroes.Length);

        foreach (Hero hero in heroes)
        {
            if (hero?.IsSpawned == true)
            {
                LevelManager heroLevelManager = hero.GetComponent<LevelManager>();
                if (heroLevelManager?.IsInitialized == true)
                {
                    heroLevelManager.GainExp(expPerHero);
                }
            }
        }
    }

    private Hero[] FindNearbyHeroes(Vector3 position, float range)
    {
        List<Hero> nearbyHeroes = new List<Hero>();
        Hero[] allHeroes = FindObjectsOfType<Hero>();

        foreach (Hero hero in allHeroes)
        {
            if (hero?.IsSpawned == true && Vector3.Distance(position, hero.transform.position) <= range)
            {
                nearbyHeroes.Add(hero);
            }
        }

        return nearbyHeroes.ToArray();
    }

    // ========== Utility Methods ==========
    private int CalculateExpToNextLevel(int level)
    {
        if (level >= expSettings.maxLevel) return int.MaxValue;
        return Mathf.RoundToInt(expSettings.baseExpToNextLevel * Mathf.Pow(expSettings.expGrowthRate, level - 1));
    }

    // ========== Public Methods ==========
    public float GetExpProgress() => ExpToNextLevel == 0 ? 1f : (float)CurrentExp / ExpToNextLevel;
    public bool IsMaxLevel() => CurrentLevel >= expSettings.maxLevel;
    public void ForceSaveToFirebase()
    {
        try
        {
            Debug.Log($"[LevelManager] 💾 Force saving to Firebase...");

            if (character == null || PersistentPlayerData.Instance == null)
            {
                Debug.LogError("[LevelManager] Cannot save - missing components");
                return;
            }

            // บันทึก base stats และ total stats
            PersistentPlayerData.Instance.SaveBaseStats(character, this);

            Debug.Log($"[LevelManager] ✅ Force save completed");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LevelManager] ❌ Error force saving: {e.Message}");
        }
    }
    public void ForceLoadFromFirebase() => TryLoadFromFirebase();

    // ========== Debug Methods ==========
   

    [ContextMenu("Level Up Now")]
    public void TestLevelUp() => GainExp(ExpToNextLevel);

    // ===== เพิ่มใน LevelManager.cs =====

    [ContextMenu("🚀 Set Level 100")]
    public void SetLevel100()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[LevelManager] This only works in Play Mode!");
            return;
        }

        try
        {
            Debug.Log("[LevelManager] 🚀 Setting character to Level 100...");

            // ตั้งค่า level และ exp
            CurrentLevel = 90;
            CurrentExp = 0;
            ExpToNextLevel = CalculateExpToNextLevel(100);

            // คำนวณ stats ใหม่สำหรับ level 100

            // Force update network state
            if (HasStateAuthority)
            {
                character.ForceUpdateNetworkState();
            }

            // บันทึกลง Firebase

            // แจ้ง events
            OnLevelUp?.Invoke(character, CurrentLevel);
            Character.RaiseOnStatsChanged();

            Debug.Log($"[LevelManager] ✅ Character is now Level 100!");
            Debug.Log($"  HP: {character.MaxHp}, ATK: {character.AttackDamage}, LifeSteal: {character.LifeSteal:F1}%");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LevelManager] ❌ Error setting level 100: {e.Message}");
        }
    }

    [ContextMenu("🎯 Give Max Stat Points")]
    public void GiveMaxStatPoints()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[LevelManager] This only works in Play Mode!");
            return;
        }

        try
        {
            string activeCharacterType = PersistentPlayerData.Instance.GetCurrentActiveCharacter();
            var characterData = PersistentPlayerData.Instance.GetOrCreateCharacterData(activeCharacterType);

            if (characterData != null)
            {
                // ให้ stat points เต็ม (100 points)
                characterData.availableStatPoints = 100;
                characterData.totalStatPointsEarned = 100;

                Debug.Log($"[LevelManager] 🎯 Gave 100 stat points to {activeCharacterType}");

                // บันทึกลง Firebase
                PersistentPlayerData.Instance.SavePlayerDataAsync();

                Debug.Log($"[LevelManager] ✅ Character now has {characterData.availableStatPoints} stat points!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LevelManager] ❌ Error giving stat points: {e.Message}");
        }
    }

    [ContextMenu("💰 Give Money")]
    public void GiveMoneyCheat()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[LevelManager] This only works in Play Mode!");
            return;
        }

        try
        {
            var currencyManager = CurrencyManager.FindCurrencyManager();
            if (currencyManager != null)
            {
                // ให้เงิน 1,000,000 และ เพชร 10,000
                currencyManager.AddGold(1000000);
                currencyManager.AddGems(10000);

                Debug.Log("[LevelManager] 💰 Added 1,000,000 gold and 10,000 gems!");
            }
            else
            {
                Debug.LogError("[LevelManager] ❌ CurrencyManager not found!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LevelManager] ❌ Error giving money: {e.Message}");
        }
    }
}