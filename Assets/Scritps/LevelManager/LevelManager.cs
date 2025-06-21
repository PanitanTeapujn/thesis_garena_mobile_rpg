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

        string activeCharacterType = PersistentPlayerData.Instance.GetCurrentActiveCharacter();
        CharacterProgressData characterData = PersistentPlayerData.Instance.GetCharacterData(activeCharacterType);

        if (characterData != null && IsCorrectCharacter())
        {
            // Apply updated character data
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

            // 🔧 ✅ แก้ไข: ใช้ค่าคงที่แทนการโหลดจาก Firebase
            character.UpdateCriticalDamageBonus(characterData.totalCriticalDamageBonus, true);

            character.MoveSpeed = characterData.totalMoveSpeed;
            character.HitRate = characterData.totalHitRate;
            character.EvasionRate = characterData.totalEvasionRate;
            character.AttackSpeed = characterData.totalAttackSpeed;
            character.ReductionCoolDown = characterData.totalReductionCoolDown;

            character.ForceUpdateNetworkState();

            Debug.Log($"✅ Refreshed character data for {activeCharacterType} with fixed Critical Multiplier");
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
                characterData.totalReductionCoolDown
            );;

            Debug.Log($"✅ Applied Firebase data for {activeCharacterType}: Level {characterData.currentLevel}");
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
     int attackDamage, int magicDamage, int armor, float critChance,float critDamageBonus, float moveSpeed,
     float hitRate, float evasionRate, float attackSpeed, float reductionCoolDown)
    {
        CurrentLevel = level;
        CurrentExp = exp;
        ExpToNextLevel = expToNext;

        character.MaxHp = maxHp;
        character.CurrentHp = maxHp;
        character.MaxMana = maxMana;
        character.CurrentMana = maxMana;
        character.AttackDamage = attackDamage;
        character.MagicDamage = magicDamage;
        character.Armor = armor;
        character.CriticalChance = critChance;

        // 🔧 ✅ แก้ไข: ไม่ให้ override criticalMultiplier จาก Firebase
        // ใช้ค่าคงที่ 0.1f แทน
        character.UpdateCriticalDamageBonus(critDamageBonus, false);

        character.MoveSpeed = moveSpeed;
        character.HitRate = hitRate;
        character.EvasionRate = evasionRate;
        character.AttackSpeed = attackSpeed;
        character.ReductionCoolDown = reductionCoolDown;

        character.ForceUpdateNetworkState();
        IsInitialized = true;

        Debug.Log($"🔧 Applied Firebase stats with fixed Critical Multiplier: 0.1f");

        // Broadcast to all clients
        RPC_BroadcastStats(level, exp, expToNext, maxHp, maxMana, attackDamage, magicDamage, armor, critChance, critDamageBonus, moveSpeed,
            hitRate, evasionRate, attackSpeed, reductionCoolDown);
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastStats(int level, int exp, int expToNext, int maxHp, int maxMana,
   int attackDamage, int magicDamage, int armor, float critChance,float critDamageBonus, float moveSpeed,
   float hitRate, float evasionRate, float attackSpeed, float reductionCoolDown)
    {
        CurrentLevel = level;
        CurrentExp = exp;
        ExpToNextLevel = expToNext;

        character.MaxHp = maxHp;
        character.CurrentHp = maxHp;
        character.MaxMana = maxMana;
        character.CurrentMana = maxMana;
        character.AttackDamage = attackDamage;
        character.MagicDamage = magicDamage;
        character.Armor = armor;
        character.CriticalChance = critChance;

        // 🔧 ✅ แก้ไข: ตั้งค่า criticalMultiplier ที่ถูกต้อง
        character.UpdateCriticalDamageBonus(critDamageBonus, false);

        character.MoveSpeed = moveSpeed;
        character.HitRate = hitRate;
        character.EvasionRate = evasionRate;
        character.AttackSpeed = attackSpeed;
        character.ReductionCoolDown = reductionCoolDown;

        IsInitialized = true;

        Debug.Log($"🔧 Broadcasted stats with fixed Critical Multiplier: 0.1f");
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

        // ✅ Full restore on level up
        character.CurrentHp = character.MaxHp;
        character.CurrentMana = character.MaxMana;

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
            // Update โดยตรงใน MultiCharacterPlayerData
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
                character.ReductionCoolDown
            );
        }

        Debug.Log($"💾 Quick saved {activeCharacterType} - Level {CurrentLevel}");
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
    public void ForceSaveToFirebase() => QuickSave();
    public void ForceLoadFromFirebase() => TryLoadFromFirebase();

    // ========== Debug Methods ==========
   

    [ContextMenu("Level Up Now")]
    public void TestLevelUp() => GainExp(ExpToNextLevel);

   
}