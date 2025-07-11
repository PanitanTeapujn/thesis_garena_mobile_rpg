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
                InitializeBasicLevelSystem();
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
                Debug.Log($"[LevelManager] 🔄 Simple refresh for {activeCharacterType}...");

                // Apply level และ exp
                CurrentLevel = characterData.currentLevel;
                CurrentExp = characterData.currentExp;
                ExpToNextLevel = characterData.expToNextLevel;

                // ใช้ total stats จาก Firebase โดยตรง (ไม่คำนวณใหม่)
                character.MaxHp = characterData.totalMaxHp;
                character.CurrentHp = characterData.totalMaxHp;
                character.MaxMana = characterData.totalMaxMana;
                character.CurrentMana = characterData.totalMaxMana;
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
                character.LifeSteal = characterData.totalLifeSteal; // ✅ Apply LifeSteal

                character.ForceUpdateNetworkState();

                Debug.Log($"[LevelManager] ✅ Applied Firebase total stats directly:");
                Debug.Log($"  HP={character.MaxHp}, ATK={character.AttackDamage}, LifeSteal={character.LifeSteal:F1}%");

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
                characterData.totalLifeSteal // ✅ เพิ่ม LifeSteal
            );

            Debug.Log($"✅ Applied Firebase data for {activeCharacterType}: Level {characterData.currentLevel}, LifeSteal {characterData.totalLifeSteal:F1}%");
        }
    }


    private void FallbackToDefault()
    {
        if (!IsInitialized && HasInputAuthority)
        {
            Debug.Log("Using fallback default stats");
            InitializeBasicLevelSystem();
        }
    }


    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_ApplyFirebaseStats(int level, int exp, int expToNext, int maxHp, int maxMana,
     int attackDamage, int magicDamage, int armor, float critChance, float critDamageBonus, float moveSpeed,
     float hitRate, float evasionRate, float attackSpeed, float reductionCoolDown, float lifeSteal) // ✅ เพิ่ม LifeSteal parameter
    {
        // Apply base stats รวม LifeSteal
        ApplyBaseStatsOnly(level, exp, expToNext, maxHp, maxMana, attackDamage, magicDamage, armor,
                           critChance, critDamageBonus, moveSpeed, hitRate, evasionRate, attackSpeed, reductionCoolDown, lifeSteal);

        Debug.Log($"🔧 Applied Firebase base stats including LifeSteal: {lifeSteal:F1}%");

        // Broadcast to all clients
        RPC_BroadcastStats(level, exp, expToNext, maxHp, maxMana, attackDamage, magicDamage, armor, critChance, critDamageBonus, moveSpeed,
            hitRate, evasionRate, attackSpeed, reductionCoolDown, lifeSteal);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastStats(int level, int exp, int expToNext, int maxHp, int maxMana,
        int attackDamage, int magicDamage, int armor, float critChance, float critDamageBonus, float moveSpeed,
        float hitRate, float evasionRate, float attackSpeed, float reductionCoolDown, float lifeSteal) // ✅ เพิ่ม LifeSteal parameter
    {
        // Apply base stats รวม LifeSteal
        ApplyBaseStatsOnly(level, exp, expToNext, maxHp, maxMana, attackDamage, magicDamage, armor,
                           critChance, critDamageBonus, moveSpeed, hitRate, evasionRate, attackSpeed, reductionCoolDown, lifeSteal);

        Debug.Log($"🔧 Broadcasted base stats including LifeSteal: {lifeSteal:F1}%");
    }
    private System.Collections.IEnumerator DelayedEquipmentStatsApplication()
    {
        Debug.Log("[LevelManager] ⚠️ DelayedEquipmentStatsApplication is now DISABLED");
        Debug.Log("[LevelManager] ✅ Equipment stats are handled by Character component directly");
        yield break;
    }
    private void RecalculateStatsWithEquipment()
    {
        Debug.Log("[LevelManager] ⚠️ RecalculateStatsWithEquipment is now DISABLED to prevent stats bugs");
        Debug.Log("[LevelManager] ✅ Using Firebase total stats directly - no recalculation needed");

        // เฉพาะแจ้ง stats changed
        Character.RaiseOnStatsChanged();

        // ไม่ทำ complex calculation ที่ทำให้ stats บัค
        return;
    }
    private void SaveTotalStatsAfterRecalculation()
    {
        try
        {
            if (!HasInputAuthority) return;

            Debug.Log("[LevelManager] 💾 Saving total stats after equipment recalculation...");

            string activeCharacterType = PersistentPlayerData.Instance.GetCurrentActiveCharacter();

            if (PersistentPlayerData.Instance.multiCharacterData != null)
            {
                // อัปเดต total stats (รวม equipment bonuses) ลง Firebase
                PersistentPlayerData.Instance.UpdateLevelAndStats(
                    CurrentLevel,
                    CurrentExp,
                    ExpToNextLevel,
                    character.MaxHp,                    // total HP (รวม equipment)
                    character.MaxMana,                  // total Mana (รวม equipment)
                    character.AttackDamage,            // total Attack (รวม equipment)
                    character.MagicDamage,             // total Magic (รวม equipment)
                    character.Armor,                   // total Armor (รวม equipment)
                    character.CriticalChance,          // total Crit (รวม equipment)
                    character.CriticalDamageBonus,     // total Crit Damage (รวม equipment)
                    character.MoveSpeed,               // total Move Speed (รวม equipment)
                    character.HitRate,                 // total Hit Rate (รวม equipment)
                    character.EvasionRate,             // total Evasion (รวม equipment)
                    character.AttackSpeed,             // total Attack Speed (รวม equipment)
                    character.ReductionCoolDown ,       // total CDR (รวม equipment)
                    character.LifeSteal
                ); 

                Debug.Log($"[LevelManager] ✅ Total stats saved to Firebase for {activeCharacterType}");
                Debug.Log($"  Saved: HP={character.MaxHp}, ATK={character.AttackDamage}, ARM={character.Armor}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LevelManager] ❌ Error saving total stats after recalculation: {e.Message}");
        }
    }
    private void SaveUpdatedStatsToFirebase()
    {
        try
        {
            if (!HasInputAuthority) return;

            Debug.Log("[LevelManager] 💾 Saving updated stats to Firebase...");

            string activeCharacterType = PersistentPlayerData.Instance.GetCurrentActiveCharacter();

            if (PersistentPlayerData.Instance.multiCharacterData != null)
            {
                // อัปเดต total stats ลง Firebase
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
                    character.LifeSteal
                );

                Debug.Log($"[LevelManager] ✅ Updated stats saved for {activeCharacterType}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LevelManager] ❌ Error saving updated stats: {e.Message}");
        }
    }

    /// <summary>
    /// 🆕 Apply เฉพาะ base stats (ไม่รวม equipment bonuses)
    /// </summary>
    // 🆕 แก้ไข ApplyBaseStatsOnly ให้คำนวณ base stats ก่อน
    private void ApplyBaseStatsOnly(int level, int exp, int expToNext, int maxHp, int maxMana,
     int attackDamage, int magicDamage, int armor, float critChance, float critDamageBonus,
     float moveSpeed, float hitRate, float evasionRate, float attackSpeed, float reductionCoolDown, float lifeSteal) // ✅ เพิ่ม LifeSteal parameter
    {
        CurrentLevel = level;
        CurrentExp = exp;
        ExpToNextLevel = expToNext;

        // เก็บ HP/Mana percentage ก่อนเปลี่ยน stats
        float hpPercentage = character.MaxHp > 0 ? (float)character.CurrentHp / character.MaxHp : 1f;
        float manaPercentage = character.MaxMana > 0 ? (float)character.CurrentMana / character.MaxMana : 1f;

        // คำนวณ base stats จาก ScriptableObject + Level bonuses
        CalculateBaseStatsFromLevel();

        // บันทึก base stats ใน CharacterProgressData
        SaveBaseStatsToFirebase();

        // ใช้ total stats จาก Firebase (รวม equipment แล้ว)
        character.MaxHp = maxHp;
        character.MaxMana = maxMana;
        character.AttackDamage = attackDamage;
        character.MagicDamage = magicDamage;
        character.Armor = armor;
        character.CriticalChance = critChance;
        character.UpdateCriticalDamageBonus(critDamageBonus, false);
        character.MoveSpeed = moveSpeed;
        character.HitRate = hitRate;
        character.EvasionRate = evasionRate;
        character.AttackSpeed = attackSpeed;
        character.ReductionCoolDown = reductionCoolDown;
        character.LifeSteal = lifeSteal; // ✅ Apply LifeSteal

        // ปรับ currentHp และ currentMana ตามเปอร์เซ็นต์เดิม
        character.CurrentHp = Mathf.RoundToInt(character.MaxHp * hpPercentage);
        character.CurrentMana = Mathf.RoundToInt(character.MaxMana * manaPercentage);
        character.CurrentHp = Mathf.Clamp(character.CurrentHp, 1, character.MaxHp);
        character.CurrentMana = Mathf.Clamp(character.CurrentMana, 0, character.MaxMana);

        character.ForceUpdateNetworkState();
        IsInitialized = true;

        Debug.Log($"[LevelManager] ✅ Applied stats: Base calculated, Total from Firebase, LifeSteal={lifeSteal:F1}%");
    }
    private void SaveBaseStatsToFirebase()
    {
        try
        {
            string characterType = PersistentPlayerData.Instance.GetCurrentActiveCharacter();
            var characterData = PersistentPlayerData.Instance.GetOrCreateCharacterData(characterType);

            // บันทึก base stats
            characterData.UpdateBaseStats(
                baseMaxHp, baseMaxMana, baseAttackDamage, baseMagicDamage, baseArmor,
                baseCriticalChance, baseCriticalDamageBonus, baseMoveSpeed,
                baseHitRate, baseEvasionRate, baseAttackSpeed, baseReductionCoolDown,baseLifeSteal
            );

            Debug.Log($"[LevelManager] 💾 Base stats saved to Firebase");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LevelManager] ❌ Error saving base stats: {e.Message}");
        }
    }
    private void CalculateBaseStatsFromLevel()
    {
        CalculateBaseStatsWithUpgrades();
    }


    private void InitializeBasicLevelSystem()
    {
        if (IsInitialized) return;

        // Use ScriptableObject stats as fallback
        if (character.characterStats != null)
        {
            character.MaxHp = character.characterStats.maxHp;
            character.CurrentHp = character.MaxHp;
            character.MaxMana = character.characterStats.maxMana;
            character.CurrentMana = character.MaxMana;
            character.AttackDamage = character.characterStats.attackDamage;
            character.MagicDamage = character.characterStats.magicDamage;
            character.Armor = character.characterStats.arrmor;
            character.CriticalChance = character.characterStats.criticalChance;
            character.CriticalDamageBonus = character.characterStats.criticalDamageBonus;
            character.MoveSpeed = character.characterStats.moveSpeed;
            character.HitRate = character.characterStats.hitRate;
            character.EvasionRate = character.characterStats.evasionRate;
            character.AttackSpeed = character.characterStats.attackSpeed;
            character.ReductionCoolDown = character.characterStats.reductionCoolDown;
        }

        CurrentLevel = 1;
        CurrentExp = 0;
        ExpToNextLevel = expSettings.baseExpToNextLevel;
        IsInitialized = true;

        Debug.Log($"✅ Initialized basic level system for {character.CharacterName}");
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

    private void LevelUp()
    {
        CurrentExp -= ExpToNextLevel;
        CurrentLevel++;
        ExpToNextLevel = CalculateExpToNextLevel(CurrentLevel);

        // Apply stat bonuses
        character.MaxHp += levelUpStats.hpBonusPerLevel;
        character.MaxMana += levelUpStats.manaBonusPerLevel;
        character.AttackDamage += levelUpStats.attackDamageBonusPerLevel;
        character.MagicDamage += levelUpStats.magicDamageBonusPerLevel;
        character.Armor += levelUpStats.armorBonusPerLevel;
        character.CriticalChance += levelUpStats.criticalChanceBonusPerLevel;
        character.MoveSpeed += levelUpStats.moveSpeedBonusPerLevel;
        character.LifeSteal += levelUpStats.lifeStealBonusPerLevel;

        // ✅ Full restore on level up
        character.CurrentHp = character.MaxHp;
        character.CurrentMana = character.MaxMana;

        // 🆕 เพิ่ม stat point เมื่อ level up
        AddStatPointOnLevelUp();

        // ✅ แก้ไข: Force sync network state หลัง level up
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

        // Quick save
        QuickSave();
    }
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
                character.LifeSteal
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
            

            IsInitialized = true;
            Debug.Log($"✅ Initialized LevelManager for {characterType} - Level {CurrentLevel}");
        }
        else
        {
            Debug.LogWarning($"[LevelManager] No data found for {characterType}, using defaults");
            InitializeBasicLevelSystem();
        }
    }


    private void CalculateAndStoreBaseStats()
    {
        if (character?.characterStats == null)
        {
            Debug.LogError("[LevelManager] No characterStats available for base calculation!");
            return;
        }

        // คำนวณ base stats = ScriptableObject stats + Level bonuses
        int levelBonusHp = (CurrentLevel - 1) * levelUpStats.hpBonusPerLevel;
        int levelBonusMana = (CurrentLevel - 1) * levelUpStats.manaBonusPerLevel;
        int levelBonusAttack = (CurrentLevel - 1) * levelUpStats.attackDamageBonusPerLevel;
        int levelBonusMagic = (CurrentLevel - 1) * levelUpStats.magicDamageBonusPerLevel;
        int levelBonusArmor = (CurrentLevel - 1) * levelUpStats.armorBonusPerLevel;
        float levelBonusCrit = (CurrentLevel - 1) * levelUpStats.criticalChanceBonusPerLevel;
        float levelBonusSpeed = (CurrentLevel - 1) * levelUpStats.moveSpeedBonusPerLevel;

        // เก็บ base stats (ScriptableObject + Level bonuses)
        baseMaxHp = character.characterStats.maxHp + levelBonusHp;
        baseMaxMana = character.characterStats.maxMana + levelBonusMana;
        baseAttackDamage = character.characterStats.attackDamage + levelBonusAttack;
        baseMagicDamage = character.characterStats.magicDamage + levelBonusMagic;
        baseArmor = character.characterStats.arrmor + levelBonusArmor;
        baseCriticalChance = character.characterStats.criticalChance + levelBonusCrit;
        baseCriticalDamageBonus = character.characterStats.criticalDamageBonus;
        baseMoveSpeed = character.characterStats.moveSpeed + levelBonusSpeed;
        baseHitRate = character.characterStats.hitRate;
        baseEvasionRate = character.characterStats.evasionRate;
        baseAttackSpeed = character.characterStats.attackSpeed;
        baseReductionCoolDown = character.characterStats.reductionCoolDown;

       
    }
    private void CalculateBaseStatsWithUpgrades()
    {
        if (character?.characterStats == null) return;

        // คำนวณ base stats = ScriptableObject + Level bonuses
        int levelBonus = CurrentLevel - 1;

        int baseHp = character.characterStats.maxHp + (levelBonus * levelUpStats.hpBonusPerLevel);
        int baseAttack = character.characterStats.attackDamage + (levelBonus * levelUpStats.attackDamageBonusPerLevel);
        int baseMagic = character.characterStats.magicDamage + (levelBonus * levelUpStats.magicDamageBonusPerLevel);
        int baseMana = character.characterStats.maxMana + (levelBonus * levelUpStats.manaBonusPerLevel);
        float baseCrit = character.characterStats.criticalChance + (levelBonus * levelUpStats.criticalChanceBonusPerLevel);
        float baseCritDmg = character.characterStats.criticalDamageBonus;
        float baseSpeed = character.characterStats.moveSpeed + (levelBonus * levelUpStats.moveSpeedBonusPerLevel);
        float baseAtkSpeed = character.characterStats.attackSpeed;
        float baseEvasion = character.characterStats.evasionRate;
        float baseCdr = character.characterStats.reductionCoolDown;
        float baseHit = character.characterStats.hitRate;
        float baseLifeSteal = character.characterStats.lifeSteal;

        // 🆕 เพิ่ม stat bonuses จาก upgrades
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
                baseLifeSteal += lifeStealBonus;
                baseSpeed += speedBonus;

                Debug.Log($"[LevelManager] 🎯 Applied stat upgrade bonuses: STR={characterData.upgradedSTR}, DEX={characterData.upgradedDEX}, INT={characterData.upgradedINT}, MAS={characterData.upgradedMAS}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LevelManager] ❌ Error applying stat bonuses: {e.Message}");
        }

        // บันทึก base stats ที่คำนวณแล้ว
        baseMaxHp = baseHp;
        baseMaxMana = baseMana;
        baseAttackDamage = baseAttack;
        baseMagicDamage = baseMagic;
        baseArmor = character.characterStats.arrmor + (levelBonus * levelUpStats.armorBonusPerLevel);
        baseCriticalChance = baseCrit;
        baseCriticalDamageBonus = baseCritDmg;
        baseMoveSpeed = baseSpeed;
        baseAttackSpeed = baseAtkSpeed;
        baseEvasionRate = baseEvasion;
        baseReductionCoolDown = baseCdr;
        baseHitRate = baseHit;
        baseLifeSteal = baseLifeSteal;

        Debug.Log($"[LevelManager] 📊 Final base stats with upgrades: HP={baseHp}, ATK={baseAttack}, MAGIC={baseMagic}");
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
            CurrentLevel = 100;
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