using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System;

[System.Serializable]
public class LevelUpStats
{
    [Header("Stats Bonus Per Level")]
    public int hpBonusPerLevel = 10;           // +10 HP ต่อ level
    public int manaBonusPerLevel = 5;          // +5 Mana ต่อ level
    public int attackDamageBonusPerLevel = 2;  // +2 Attack ต่อ level
    public int armorBonusPerLevel = 1;         // +1 Armor ต่อ level
    public float criticalChanceBonusPerLevel = 0.5f; // +0.5% Crit ต่อ level
    public float moveSpeedBonusPerLevel = 0.1f;     // +0.1 Speed ต่อ level
}

[System.Serializable]
public class ExpSettings
{
    [Header("Experience Settings")]
    public int baseExpToNextLevel = 100;     // exp ที่ต้องการสำหรับ level 2
    public float expGrowthRate = 1.2f;       // เพิ่มขึ้น 20% ต่อ level
    public int maxLevel = 100;               // level สูงสุด

    [Header("Enemy Exp Rewards")]
    public int baseEnemyExp = 25;            // exp พื้นฐานจาก enemy
    public float enemyExpLevelMultiplier = 1.1f; // exp เพิ่มตาม level ของ enemy
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

    // ========== Events ==========
    public static event Action<Character, int> OnLevelUp;           // character, newLevel
    public static event Action<Character, int, int> OnExpGain;      // character, expGained, totalExp
    public static event Action<Character, LevelUpStats> OnStatsIncreased; // character, statBonus

    // ========== Component References ==========
    private Character character;
    private EquipmentManager equipmentManager;

    protected virtual void Awake()
    {
        character = GetComponent<Character>();
        equipmentManager = GetComponent<EquipmentManager>();
    }

    protected virtual void Start()
    {
        // Subscribe to death events สำหรับ exp gain
        CombatManager.OnCharacterDeath += HandleCharacterDeath;

        if (HasStateAuthority)
        {
            // Initialize first level requirements
            ExpToNextLevel = CalculateExpToNextLevel(CurrentLevel);
        }
    }

    protected virtual void OnDestroy()
    {
        CombatManager.OnCharacterDeath -= HandleCharacterDeath;
    }

    public override void Spawned()
    {
        if (HasStateAuthority && CurrentLevel == 1)
        {
            // Apply level 1 base stats
            ApplyLevelUpStats(true); // isInitialSetup = true
        }
    }

    // ========== Experience System ==========
    public virtual void GainExp(int expAmount)
    {
        if (!HasStateAuthority && !HasInputAuthority) return;

        int oldExp = CurrentExp;
        CurrentExp += expAmount;

        Debug.Log($"[Exp Gain] {character.CharacterName} gained {expAmount} exp! ({oldExp} -> {CurrentExp})");

        // Sync to network
        if (HasStateAuthority)
        {
            RPC_BroadcastExpGain(expAmount, CurrentExp);
        }
        else if (HasInputAuthority)
        {
            RPC_UpdateExp(CurrentExp);
        }

        // Check for level up
        CheckLevelUp();

        // Fire event
        OnExpGain?.Invoke(character, expAmount, CurrentExp);
    }

    private void CheckLevelUp()
    {
        while (CurrentExp >= ExpToNextLevel && CurrentLevel < expSettings.maxLevel)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        CurrentExp -= ExpToNextLevel;
        CurrentLevel++;

        // Calculate new exp requirement
        ExpToNextLevel = CalculateExpToNextLevel(CurrentLevel);

        Debug.Log($"🎉 [LEVEL UP!] {character.CharacterName} reached Level {CurrentLevel}!");

        // Apply stat bonuses
        ApplyLevelUpStats(false);

        // Sync to network
        if (HasStateAuthority)
        {
            RPC_BroadcastLevelUp(CurrentLevel, CurrentExp, ExpToNextLevel);
        }

        // Fire event
        OnLevelUp?.Invoke(character, CurrentLevel);
        OnStatsIncreased?.Invoke(character, levelUpStats);

        // Visual/Audio effects
        RPC_PlayLevelUpEffects();
    }

    private void ApplyLevelUpStats(bool isInitialSetup)
    {
        // คำนวณ stats bonus จาก level
        int levelBonus = isInitialSetup ? 0 : 1; // ถ้าเป็น initial setup ไม่บวก bonus
        int totalLevels = isInitialSetup ? CurrentLevel - 1 : CurrentLevel - 1 + levelBonus;

        if (totalLevels <= 0) return;

        // คำนวณ stats ที่ได้จาก level
        int hpBonus = totalLevels * levelUpStats.hpBonusPerLevel;
        int manaBonus = totalLevels * levelUpStats.manaBonusPerLevel;
        int attackBonus = totalLevels * levelUpStats.attackDamageBonusPerLevel;
        int armorBonus = totalLevels * levelUpStats.armorBonusPerLevel;
        float critBonus = totalLevels * levelUpStats.criticalChanceBonusPerLevel;
        float speedBonus = totalLevels * levelUpStats.moveSpeedBonusPerLevel;

        if (!isInitialSetup)
        {
            // เฉพาะเมื่อ level up เท่านั้น ถึงจะบวก stats
            character.MaxHp += levelUpStats.hpBonusPerLevel;
            character.MaxMana += levelUpStats.manaBonusPerLevel;
            character.AttackDamage += levelUpStats.attackDamageBonusPerLevel;
            character.Armor += levelUpStats.armorBonusPerLevel;
            character.CriticalChance += levelUpStats.criticalChanceBonusPerLevel;
            character.MoveSpeed += levelUpStats.moveSpeedBonusPerLevel;

            // รักษาเลือดและ mana เต็มเมื่อ level up
            character.CurrentHp = character.MaxHp;
            character.CurrentMana = character.MaxMana;

            Debug.Log($"[Level Up Stats] {character.CharacterName} gained: HP+{levelUpStats.hpBonusPerLevel}, ATK+{levelUpStats.attackDamageBonusPerLevel}, etc.");
        }
        else
        {
            // Setup เริ่มต้น - apply stats รวมจาก level ปัจจุบัน
            character.MaxHp += hpBonus;
            character.MaxMana += manaBonus;
            character.AttackDamage += attackBonus;
            character.Armor += armorBonus;
            character.CriticalChance += critBonus;
            character.MoveSpeed += speedBonus;

            character.CurrentHp = character.MaxHp;
            character.CurrentMana = character.MaxMana;

            Debug.Log($"[Initial Level Stats] {character.CharacterName} Level {CurrentLevel}: Total bonus applied");
        }

        // Force update network state
        character.ForceUpdateNetworkState();
    }

    // ========== Enemy Death Handler ==========
    private void HandleCharacterDeath(Character deadCharacter)
    {
        // ให้ exp เฉพาะเมื่อ enemy ตาย
        if (deadCharacter.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            // หา Heroes ที่อยู่ใกล้ๆ ให้ exp
            Hero[] nearbyHeroes = FindNearbyHeroes(deadCharacter.transform.position, 10f);

            if (nearbyHeroes.Length > 0)
            {
                int expReward = CalculateEnemyExpReward(deadCharacter);
                int expPerHero = Mathf.Max(1, expReward / nearbyHeroes.Length); // แบ่ง exp

                foreach (Hero hero in nearbyHeroes)
                {
                    if (hero != null && hero.IsSpawned)
                    {
                        LevelManager heroLevelManager = hero.GetComponent<LevelManager>();
                        if (heroLevelManager != null)
                        {
                            heroLevelManager.GainExp(expPerHero);
                        }
                    }
                }

                Debug.Log($"[Enemy Death] {deadCharacter.CharacterName} gave {expPerHero} exp to {nearbyHeroes.Length} heroes");
            }
        }
    }

    private Hero[] FindNearbyHeroes(Vector3 position, float range)
    {
        List<Hero> nearbyHeroes = new List<Hero>();
        Hero[] allHeroes = FindObjectsOfType<Hero>();

        foreach (Hero hero in allHeroes)
        {
            if (hero != null && hero.IsSpawned)
            {
                float distance = Vector3.Distance(position, hero.transform.position);
                if (distance <= range)
                {
                    nearbyHeroes.Add(hero);
                }
            }
        }

        return nearbyHeroes.ToArray();
    }

    private int CalculateEnemyExpReward(Character enemy)
    {
        // Base exp
        int baseExp = expSettings.baseEnemyExp;

        // Bonus จาก level ของ enemy (ถ้ามี LevelManager)
        LevelManager enemyLevelManager = enemy.GetComponent<LevelManager>();
        if (enemyLevelManager != null)
        {
            float levelMultiplier = Mathf.Pow(expSettings.enemyExpLevelMultiplier, enemyLevelManager.CurrentLevel - 1);
            baseExp = Mathf.RoundToInt(baseExp * levelMultiplier);
        }

        return baseExp;
    }

    // ========== Utility Methods ==========
    private int CalculateExpToNextLevel(int level)
    {
        if (level >= expSettings.maxLevel) return int.MaxValue;

        float expRequired = expSettings.baseExpToNextLevel * Mathf.Pow(expSettings.expGrowthRate, level - 1);
        return Mathf.RoundToInt(expRequired);
    }

    // ========== Network RPCs ==========
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_UpdateExp(int newExp)
    {
        CurrentExp = newExp;
        RPC_BroadcastExpGain(0, newExp); // broadcast without showing gain
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastExpGain(int expGained, int totalExp)
    {
        CurrentExp = totalExp;
        // Update UI here if needed
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastLevelUp(int newLevel, int remainingExp, int expToNext)
    {
        CurrentLevel = newLevel;
        CurrentExp = remainingExp;
        ExpToNextLevel = expToNext;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayLevelUpEffects()
    {
        // Play level up VFX/SFX
        Debug.Log($"🎉✨ {character.CharacterName} LEVEL UP EFFECTS! ✨🎉");

        // TODO: Add particle effects, sound, screen flash, etc.
    }

    // ========== Public Query Methods ==========
    public float GetExpProgress()
    {
        if (ExpToNextLevel == 0) return 1f;
        return (float)CurrentExp / ExpToNextLevel;
    }

    public int GetTotalStatsFromLevel()
    {
        return (CurrentLevel - 1) * (levelUpStats.hpBonusPerLevel + levelUpStats.manaBonusPerLevel +
                                   levelUpStats.attackDamageBonusPerLevel + levelUpStats.armorBonusPerLevel);
    }

    public bool IsMaxLevel()
    {
        return CurrentLevel >= expSettings.maxLevel;
    }

    // ========== Debug/Testing Methods ==========
    [ContextMenu("Gain 50 Exp")]
    public void TestGainExp()
    {
        GainExp(50);
    }

    [ContextMenu("Level Up Now")]
    public void TestLevelUp()
    {
        GainExp(ExpToNextLevel);
    }

    [ContextMenu("Set Level 10")]
    public void TestSetLevel10()
    {
        while (CurrentLevel < 10)
        {
            GainExp(ExpToNextLevel);
        }
    }

    public void LogLevelInfo()
    {
        Debug.Log($"=== {character.CharacterName} Level Info ===");
        Debug.Log($"📊 Level: {CurrentLevel}/{expSettings.maxLevel}");
        Debug.Log($"⭐ Exp: {CurrentExp}/{ExpToNextLevel} ({GetExpProgress() * 100:F1}%)");
        Debug.Log($"💪 Total Stats Bonus: {GetTotalStatsFromLevel()}");
        Debug.Log($"❤️ HP: {character.CurrentHp}/{character.MaxHp}");
        Debug.Log($"💙 Mana: {character.CurrentMana}/{character.MaxMana}");
        Debug.Log($"⚔️ Attack: {character.AttackDamage}");
        Debug.Log($"🛡️ Armor: {character.Armor}");
        Debug.Log($"💥 Crit: {character.CriticalChance:F1}%");
    }
}