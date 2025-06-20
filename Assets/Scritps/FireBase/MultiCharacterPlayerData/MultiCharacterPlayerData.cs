using UnityEngine;
using System.Collections.Generic;
using System;

[System.Serializable]
public class MultiCharacterPlayerData
{
    [Header("Player Info")]
    public string playerName;
    public string password;
    public string registrationDate;
    public string lastLoginDate;
    public string currentActiveCharacter = "Assassin";

    [Header("Character Data")]
    public List<CharacterProgressData> characters = new List<CharacterProgressData>();

    [Header("Stage Progress")]
    public StageProgressData stageProgress = new StageProgressData();
    [Header("Friends System")]
    public List<string> friends = new List<string>();
    public List<string> pendingFriendRequests = new List<string>();
    public MultiCharacterPlayerData()
    {
        playerName = "";
        password = "";
        registrationDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        lastLoginDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        currentActiveCharacter = "Assassin";
        stageProgress = new StageProgressData();

        InitializeDefaultCharacter();
    }

    private void InitializeDefaultCharacter()
    {
        CharacterProgressData defaultAssassin = new CharacterProgressData();
        defaultAssassin.characterType = "Assassin";
        defaultAssassin.currentLevel = 1;
        defaultAssassin.currentExp = 0;
        defaultAssassin.expToNextLevel = 100;

        CharacterStats assassinStats = Resources.Load<CharacterStats>("Characters/AssassinStats");
        if (assassinStats != null)
        {
            defaultAssassin.totalMaxHp = assassinStats.maxHp;
            defaultAssassin.totalMaxMana = assassinStats.maxMana;
            defaultAssassin.totalAttackDamage = assassinStats.attackDamage;
            defaultAssassin.totalMagicDamage = assassinStats.magicDamage;
            defaultAssassin.totalArmor = assassinStats.arrmor;
            defaultAssassin.totalCriticalChance = assassinStats.criticalChance;
            defaultAssassin.totalCriticalMultiplier = assassinStats.criticalMultiplier;
            defaultAssassin.totalMoveSpeed = assassinStats.moveSpeed;
            defaultAssassin.totalAttackRange = assassinStats.attackRange;
            defaultAssassin.totalAttackCooldown = assassinStats.attackCoolDown;
            defaultAssassin.totalHitRate = assassinStats.hitRate;
            defaultAssassin.totalEvasionRate = assassinStats.evasionRate;
            defaultAssassin.totalAttackSpeed = assassinStats.attackSpeed;
            defaultAssassin.totalReductionCoolDown = assassinStats.reductionCoolDown;
        }
        

        characters.Add(defaultAssassin);
    }

    public CharacterProgressData GetCharacterData(string characterType)
    {
        return characters.Find(c => c.characterType == characterType);
    }

    public CharacterProgressData GetOrCreateCharacterData(string characterType)
    {
        CharacterProgressData existing = GetCharacterData(characterType);
        if (existing != null)
            return existing;

        CharacterProgressData newCharacter = CreateDefaultCharacterData(characterType);
        characters.Add(newCharacter);
        return newCharacter;
    }

    public CharacterProgressData GetActiveCharacterData()
    {
        return GetOrCreateCharacterData(currentActiveCharacter);
    }

    public void SwitchActiveCharacter(string characterType)
    {
        currentActiveCharacter = characterType;
        GetOrCreateCharacterData(characterType);
    }

    public CharacterProgressData CreateDefaultCharacterData(string characterType)
    {
        CharacterProgressData newCharacter = new CharacterProgressData();
        newCharacter.characterType = characterType;
        newCharacter.currentLevel = 1;
        newCharacter.currentExp = 0;
        newCharacter.expToNextLevel = 100;

        CharacterStats characterStats = null;

        switch (characterType)
        {
            case "BloodKnight":
                characterStats = Resources.Load<CharacterStats>("Characters/BloodKnightStats");
                break;
            case "Archer":
                characterStats = Resources.Load<CharacterStats>("Characters/ArcherStats");
                break;
            case "Assassin":
                characterStats = Resources.Load<CharacterStats>("Characters/AssassinStats");
                break;
            case "IronJuggernaut":
                characterStats = Resources.Load<CharacterStats>("Characters/IronJuggernautStats");
                break;
        }

        if (characterStats != null)
        {
            newCharacter.totalMaxHp = characterStats.maxHp;
            newCharacter.totalMaxMana = characterStats.maxMana;
            newCharacter.totalAttackDamage = characterStats.attackDamage;
            newCharacter.totalMagicDamage = characterStats.magicDamage;
            newCharacter.totalArmor = characterStats.arrmor;
            newCharacter.totalCriticalChance = characterStats.criticalChance;
            newCharacter.totalCriticalMultiplier = characterStats.criticalMultiplier;
            newCharacter.totalMoveSpeed = characterStats.moveSpeed;
            newCharacter.totalAttackRange = characterStats.attackRange;
            newCharacter.totalAttackCooldown = characterStats.attackCoolDown;
            newCharacter.totalHitRate = characterStats.hitRate;
            newCharacter.totalEvasionRate = characterStats.evasionRate;
            newCharacter.totalAttackSpeed = characterStats.attackSpeed;
            newCharacter.totalReductionCoolDown = characterStats.reductionCoolDown;
            Debug.Log($"✅ Used ScriptableObject stats for {characterType}");
        }
        return newCharacter;
    }

    public void UpdateCharacterStats(string characterType, int level, int exp, int expToNext,
        int maxHp, int maxMana, int attackDamage,int magicDamage ,int armor, float critChance, float moveSpeed,
        float hitRate, float evasion, float attackSpeed,float reductionCoolDown)
    {
        CharacterProgressData character = GetOrCreateCharacterData(characterType);
        character.currentLevel = level;
        character.currentExp = exp;
        character.expToNextLevel = expToNext;
        character.totalMaxHp = maxHp;
        character.totalMaxMana = maxMana;
        character.totalAttackDamage = attackDamage;
        character.totalMagicDamage = magicDamage;
        character.totalArmor = armor;
        character.totalCriticalChance = critChance;
        character.totalMoveSpeed = moveSpeed;
        character.totalHitRate = hitRate;
        character.totalEvasionRate = evasion;
        character.totalAttackSpeed = attackSpeed;
        character.totalReductionCoolDown = reductionCoolDown;
    }

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(playerName) &&
               !string.IsNullOrEmpty(currentActiveCharacter) &&
               characters.Count > 0;
    }

    public void LogAllCharacters()
    {
        Debug.Log($"=== {playerName}'s Characters ===");
        Debug.Log($"🎯 Active: {currentActiveCharacter}");

        foreach (var character in characters)
        {
            Debug.Log($"🎭 {character.characterType} - Level {character.currentLevel} " +
                     $"(HP: {character.totalMaxHp}, ATK: {character.totalAttackDamage})");
        }
    }
}

[System.Serializable]
public class CharacterProgressData
{
    public string characterType;

    [Header("Level Progress")]
    public int currentLevel = 1;
    public int currentExp = 0;
    public int expToNextLevel = 100;

    [Header("Character Stats")]
    public int totalMaxHp;
    public int totalMaxMana;
    public int totalAttackDamage;
    public int totalArmor;
    public int totalMagicDamage;
    public float totalCriticalChance;
    public float totalCriticalMultiplier;
    public float totalMoveSpeed;
    public float totalAttackRange;
    public float totalAttackCooldown;
    public float totalHitRate;
    public float totalEvasionRate;
    public float totalAttackSpeed;
    public float totalReductionCoolDown;
}