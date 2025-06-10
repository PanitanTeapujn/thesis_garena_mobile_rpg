using UnityEngine;
using System;

[System.Serializable]
public class PlayerProgressData
{
    [Header("Player Info")]
    public string playerName;
    public string password;
    public string lastCharacterSelected;
    public string registrationDate;
    public string lastLoginDate;

    [Header("Level Progress")]
    public int currentLevel = 1;
    public int currentExp = 0;
    public int expToNextLevel = 100;

    [Header("Character Stats (Base + Level Bonus)")]
    public int totalMaxHp;
    public int totalMaxMana;
    public int totalAttackDamage;
    public int totalArmor;
    public float totalCriticalChance;
    public float totalCriticalMultiplier;
    public float totalMoveSpeed;
    public float totalAttackRange;
    public float totalAttackCooldown;

    [Header("Base Stats (from ScriptableObject)")]
    public string baseCharacterStatsId; // ใช้เป็น reference ไปยัง ScriptableObject

    // Constructor
    public PlayerProgressData()
    {
        // Default values
        playerName = "";
        password = "";
        lastCharacterSelected = "Assasins";
        registrationDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        lastLoginDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        currentLevel = 1;
        currentExp = 0;
        expToNextLevel = 100;

        // Stats จะถูกกำหนดจาก CharacterStats ใน InitializeFromCharacterStats
    }

    // Initialize stats จาก CharacterStats ScriptableObject
    public void InitializeFromCharacterStats(CharacterStats characterStats, int level = 1)
    {
        if (characterStats == null) return;

        baseCharacterStatsId = characterStats.name; // เก็บชื่อ ScriptableObject

        // Base stats จาก ScriptableObject
        int baseMaxHp = characterStats.maxHp;
        int baseMaxMana = characterStats.maxMana;
        int baseAttackDamage = characterStats.attackDamage;
        int baseArmor = characterStats.arrmor;
        float baseCriticalChance = characterStats.criticalChance;
        float baseCriticalMultiplier = characterStats.criticalMultiplier;
        float baseMoveSpeed = characterStats.moveSpeed;
        float baseAttackRange = characterStats.attackRange;
        float baseAttackCooldown = characterStats.attackCoolDown;

        // คำนวณ level bonus (level - 1 เพราะ level 1 ไม่มี bonus)
        int levelBonus = level - 1;

        // Stats เริ่มต้นสำหรับการ level up (ควรตรงกับ LevelManager)
        int hpBonusPerLevel = 10;
        int manaBonusPerLevel = 5;
        int attackBonusPerLevel = 2;
        int armorBonusPerLevel = 1;
        float critBonusPerLevel = 0.5f;
        float speedBonusPerLevel = 0.1f;

        // คำนวณ total stats
        totalMaxHp = baseMaxHp + (levelBonus * hpBonusPerLevel);
        totalMaxMana = baseMaxMana + (levelBonus * manaBonusPerLevel);
        totalAttackDamage = baseAttackDamage + (levelBonus * attackBonusPerLevel);
        totalArmor = baseArmor + (levelBonus * armorBonusPerLevel);
        totalCriticalChance = baseCriticalChance + (levelBonus * critBonusPerLevel);
        totalCriticalMultiplier = baseCriticalMultiplier; // ไม่เปลี่ยน
        totalMoveSpeed = baseMoveSpeed + (levelBonus * speedBonusPerLevel);
        totalAttackRange = baseAttackRange; // ไม่เปลี่ยน
        totalAttackCooldown = baseAttackCooldown; // ไม่เปลี่ยน

        currentLevel = level;
    }

    // Update stats เมื่อ level up
    public void UpdateStatsOnLevelUp(LevelUpStats levelUpStats)
    {
        totalMaxHp += levelUpStats.hpBonusPerLevel;
        totalMaxMana += levelUpStats.manaBonusPerLevel;
        totalAttackDamage += levelUpStats.attackDamageBonusPerLevel;
        totalArmor += levelUpStats.armorBonusPerLevel;
        totalCriticalChance += levelUpStats.criticalChanceBonusPerLevel;
        totalMoveSpeed += levelUpStats.moveSpeedBonusPerLevel;
    }

    // Validate data integrity
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(playerName) &&
               !string.IsNullOrEmpty(lastCharacterSelected) &&
               currentLevel > 0 &&
               totalMaxHp > 0 &&
               totalMaxMana > 0;
    }

    // Create from simple player data (backward compatibility)
    public static PlayerProgressData FromSimplePlayerData(FirebaseLoginManager.SimplePlayerData simpleData, CharacterStats characterStats)
    {
        PlayerProgressData progressData = new PlayerProgressData();
        progressData.playerName = simpleData.playerName;
        progressData.password = simpleData.password;
        progressData.lastCharacterSelected = simpleData.lastCharacterSelected;
        progressData.registrationDate = simpleData.registrationDate;
        progressData.lastLoginDate = simpleData.lastLoginDate;

        // Initialize with base stats
        progressData.InitializeFromCharacterStats(characterStats, 1);

        return progressData;
    }

    // Debug info
    public void LogProgressInfo()
    {
        Debug.Log($"=== Player Progress Info ===");
        Debug.Log($"👤 Player: {playerName}");
        Debug.Log($"🎭 Character: {lastCharacterSelected}");
        Debug.Log($"📊 Level: {currentLevel}");
        Debug.Log($"⭐ Exp: {currentExp}/{expToNextLevel}");
        Debug.Log($"❤️ HP: {totalMaxHp}");
        Debug.Log($"💙 Mana: {totalMaxMana}");
        Debug.Log($"⚔️ Attack: {totalAttackDamage}");
        Debug.Log($"🛡️ Armor: {totalArmor}");
        Debug.Log($"💥 Crit: {totalCriticalChance}%");
        Debug.Log($"🏃 Speed: {totalMoveSpeed}");
    }
}