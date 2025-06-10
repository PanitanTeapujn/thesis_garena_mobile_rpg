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
    public int armorBonusPerLevel = 1;
    public float criticalChanceBonusPerLevel = 0.5f;
    public float moveSpeedBonusPerLevel = 0.1f;
}

[System.Serializable]
public class ExpSettings
{
    [Header("Experience Settings")]
    public int baseExpToNextLevel = 100;
    public float expGrowthRate = 1.2f;
    public int maxLevel = 100;

    [Header("Enemy Exp Rewards")]
    public int baseEnemyExp = 25;
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
            InitializeBasicLevelSystem();
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

    private void ApplyFirebaseData()
    {
        PlayerProgressData data = PersistentPlayerData.Instance.GetPlayerData();
        if (data?.IsValid() == true && HasInputAuthority)
        {
            // ส่งข้อมูลไปยัง server
            RPC_ApplyFirebaseStats(
                data.currentLevel,
                data.currentExp,
                data.expToNextLevel,
                data.totalMaxHp,
                data.totalMaxMana,
                data.totalAttackDamage,
                data.totalArmor,
                data.totalCriticalChance,
                data.totalMoveSpeed
            );

            Debug.Log($"✅ Applied Firebase data: Level {data.currentLevel}");
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
    private void RPC_ApplyFirebaseStats(int level, int exp, int expToNext, int maxHp, int maxMana, int attackDamage, int armor, float critChance, float moveSpeed)
    {
        CurrentLevel = level;
        CurrentExp = exp;
        ExpToNextLevel = expToNext;

        character.MaxHp = maxHp;
        character.CurrentHp = maxHp;
        character.MaxMana = maxMana;
        character.CurrentMana = maxMana;
        character.AttackDamage = attackDamage;
        character.Armor = armor;
        character.CriticalChance = critChance;
        character.MoveSpeed = moveSpeed;

        character.ForceUpdateNetworkState();
        IsInitialized = true;

        // Broadcast to all clients
        RPC_BroadcastStats(level, exp, expToNext, maxHp, maxMana, attackDamage, armor, critChance, moveSpeed);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastStats(int level, int exp, int expToNext, int maxHp, int maxMana, int attackDamage, int armor, float critChance, float moveSpeed)
    {
        CurrentLevel = level;
        CurrentExp = exp;
        ExpToNextLevel = expToNext;

        character.MaxHp = maxHp;
        character.CurrentHp = maxHp;
        character.MaxMana = maxMana;
        character.CurrentMana = maxMana;
        character.AttackDamage = attackDamage;
        character.Armor = armor;
        character.CriticalChance = critChance;
        character.MoveSpeed = moveSpeed;

        IsInitialized = true;
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
            character.Armor = character.characterStats.arrmor;
            character.CriticalChance = character.characterStats.criticalChance;
            character.MoveSpeed = character.characterStats.moveSpeed;
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
        character.Armor += levelUpStats.armorBonusPerLevel;
        character.CriticalChance += levelUpStats.criticalChanceBonusPerLevel;
        character.MoveSpeed += levelUpStats.moveSpeedBonusPerLevel;

        // Full restore on level up
        character.CurrentHp = character.MaxHp;
        character.CurrentMana = character.MaxMana;

        character.ForceUpdateNetworkState();

        Debug.Log($"🎉 {character.CharacterName} reached Level {CurrentLevel}!");

        // Fire events
        OnLevelUp?.Invoke(character, CurrentLevel);
        OnStatsIncreased?.Invoke(character, levelUpStats);

        // Quick save
        QuickSave();
    }

    // ========== Quick Save (Non-blocking) ==========
    private void QuickSave()
    {
        if (!HasInputAuthority) return;

        PersistentPlayerData.Instance.UpdateLevelAndStats(
            CurrentLevel,
            CurrentExp,
            ExpToNextLevel,
            character.MaxHp,
            character.MaxMana,
            character.AttackDamage,
            character.Armor,
            character.CriticalChance,
            character.MoveSpeed
        );
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
    [ContextMenu("Gain 50 Exp")]
    public void TestGainExp() => GainExp(50);

    [ContextMenu("Level Up Now")]
    public void TestLevelUp() => GainExp(ExpToNextLevel);

    public void LogLevelInfo()
    {
        Debug.Log($"=== {character.CharacterName} Level Info ===");
        Debug.Log($"📊 Level: {CurrentLevel}, Exp: {CurrentExp}/{ExpToNextLevel}");
        Debug.Log($"❤️ HP: {character.CurrentHp}/{character.MaxHp}");
        Debug.Log($"⚔️ Attack: {character.AttackDamage}");
        Debug.Log($"🔄 Initialized: {IsInitialized}");
    }
}