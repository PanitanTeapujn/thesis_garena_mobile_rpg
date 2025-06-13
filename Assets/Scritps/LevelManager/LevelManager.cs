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
            character.Armor = characterData.totalArmor;
            character.CriticalChance = characterData.totalCriticalChance;
            character.MoveSpeed = characterData.totalMoveSpeed;

            character.ForceUpdateNetworkState();

            Debug.Log($"✅ Refreshed character data for {activeCharacterType}");
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
        // Get current active character data instead of general player data
        string activeCharacterType = PersistentPlayerData.Instance.GetCurrentActiveCharacter();
        CharacterProgressData characterData = PersistentPlayerData.Instance.GetCharacterData(activeCharacterType);

        if (characterData != null && HasInputAuthority)
        {
            // ส่งข้อมูลไปยัง server
            RPC_ApplyFirebaseStats(
                characterData.currentLevel,
                characterData.currentExp,
                characterData.expToNextLevel,
                characterData.totalMaxHp,
                characterData.totalMaxMana,
                characterData.totalAttackDamage,
                characterData.totalArmor,
                characterData.totalCriticalChance,
                characterData.totalMoveSpeed
            );

            Debug.Log($"✅ Applied Firebase data for {activeCharacterType}: Level {characterData.currentLevel}");
        }
        else
        {
            Debug.LogWarning($"[LevelManager] No character data found for {activeCharacterType}");
            FallbackToDefault();
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

        // Save to specific character instead of general player data
        string activeCharacterType = PersistentPlayerData.Instance.GetCurrentActiveCharacter();

        if (PersistentPlayerData.Instance.multiCharacterData != null)
        {
            PersistentPlayerData.Instance.multiCharacterData.UpdateCharacterStats(
                activeCharacterType,
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

            // Also update the currentPlayerData for compatibility
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
            character.Armor = characterData.totalArmor;
            character.CriticalChance = characterData.totalCriticalChance;
            character.MoveSpeed = characterData.totalMoveSpeed;

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