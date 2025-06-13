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
    public string currentActiveCharacter = "Assassin"; // Default character

    [Header("Character Data")]
    public List<CharacterProgressData> characters = new List<CharacterProgressData>();

    // Constructor
    public MultiCharacterPlayerData()
    {
        playerName = "";
        password = "";
        registrationDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        lastLoginDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        currentActiveCharacter = "Assassin";

        // Initialize with default Assassin character
        InitializeDefaultCharacter();
    }

    private void InitializeDefaultCharacter()
    {
        CharacterProgressData defaultAssassin = new CharacterProgressData();
        defaultAssassin.characterType = "Assassin";
        defaultAssassin.currentLevel = 1;
        defaultAssassin.currentExp = 0;
        defaultAssassin.expToNextLevel = 100;

        // Default Assassin stats
        defaultAssassin.totalMaxHp = 70;
        defaultAssassin.totalMaxMana = 40;
        defaultAssassin.totalAttackDamage = 35;
        defaultAssassin.totalArmor = 2;
        defaultAssassin.totalCriticalChance = 5f;
        defaultAssassin.totalCriticalMultiplier = 2f;
        defaultAssassin.totalMoveSpeed = 6.5f;
        defaultAssassin.totalAttackRange = 2f;
        defaultAssassin.totalAttackCooldown = 1f;

        characters.Add(defaultAssassin);
    }

    // Get character data by type
    public CharacterProgressData GetCharacterData(string characterType)
    {
        return characters.Find(c => c.characterType == characterType);
    }

    // Get or create character data
    public CharacterProgressData GetOrCreateCharacterData(string characterType)
    {
        CharacterProgressData existing = GetCharacterData(characterType);
        if (existing != null)
            return existing;

        // Create new character with default stats
        CharacterProgressData newCharacter = CreateDefaultCharacterData(characterType);
        characters.Add(newCharacter);
        return newCharacter;
    }

    // Get current active character data
    public CharacterProgressData GetActiveCharacterData()
    {
        return GetOrCreateCharacterData(currentActiveCharacter);
    }

    // Switch active character
    public void SwitchActiveCharacter(string characterType)
    {
        currentActiveCharacter = characterType;
        // Ensure character exists
        GetOrCreateCharacterData(characterType);
    }

    // Create default character data based on type
    public CharacterProgressData CreateDefaultCharacterData(string characterType)
    {
        CharacterProgressData newCharacter = new CharacterProgressData();
        newCharacter.characterType = characterType;
        newCharacter.currentLevel = 1;
        newCharacter.currentExp = 0;
        newCharacter.expToNextLevel = 100;

        // Apply default stats based on character type
        switch (characterType)
        {
            case "BloodKnight":
                newCharacter.totalMaxHp = 120;
                newCharacter.totalMaxMana = 60;
                newCharacter.totalAttackDamage = 25;
                newCharacter.totalArmor = 8;
                newCharacter.totalMoveSpeed = 5.2f;
                break;
            case "Archer":
                newCharacter.totalMaxHp = 80;
                newCharacter.totalMaxMana = 80;
                newCharacter.totalAttackDamage = 30;
                newCharacter.totalArmor = 3;
                newCharacter.totalMoveSpeed = 5.8f;
                break;
            case "Assassin":
                newCharacter.totalMaxHp = 70;
                newCharacter.totalMaxMana = 40;
                newCharacter.totalAttackDamage = 35;
                newCharacter.totalArmor = 2;
                newCharacter.totalMoveSpeed = 6.5f;
                break;
            case "IronJuggernaut":
                newCharacter.totalMaxHp = 150;
                newCharacter.totalMaxMana = 40;
                newCharacter.totalAttackDamage = 20;
                newCharacter.totalArmor = 12;
                newCharacter.totalMoveSpeed = 4.5f;
                break;
            default:
                // Default to Assassin stats
                newCharacter.totalMaxHp = 70;
                newCharacter.totalMaxMana = 40;
                newCharacter.totalAttackDamage = 35;
                newCharacter.totalArmor = 2;
                newCharacter.totalMoveSpeed = 6.5f;
                break;
        }

        // Common default stats
        newCharacter.totalCriticalChance = 5f;
        newCharacter.totalCriticalMultiplier = 2f;
        newCharacter.totalAttackRange = 2f;
        newCharacter.totalAttackCooldown = 1f;

        return newCharacter;
    }

    // Update character stats
    public void UpdateCharacterStats(string characterType, int level, int exp, int expToNext,
        int maxHp, int maxMana, int attackDamage, int armor, float critChance, float moveSpeed)
    {
        CharacterProgressData character = GetOrCreateCharacterData(characterType);
        character.currentLevel = level;
        character.currentExp = exp;
        character.expToNextLevel = expToNext;
        character.totalMaxHp = maxHp;
        character.totalMaxMana = maxMana;
        character.totalAttackDamage = attackDamage;
        character.totalArmor = armor;
        character.totalCriticalChance = critChance;
        character.totalMoveSpeed = moveSpeed;
    }

    // Validate data
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(playerName) &&
               !string.IsNullOrEmpty(currentActiveCharacter) &&
               characters.Count > 0;
    }

    // Debug info
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
    public float totalCriticalChance;
    public float totalCriticalMultiplier;
    public float totalMoveSpeed;
    public float totalAttackRange;
    public float totalAttackCooldown;

    // Convert to PlayerProgressData for backward compatibility
    public PlayerProgressData ToPlayerProgressData(string playerName)
    {
        PlayerProgressData progressData = new PlayerProgressData();
        progressData.playerName = playerName;
        progressData.lastCharacterSelected = characterType;
        progressData.currentLevel = currentLevel;
        progressData.currentExp = currentExp;
        progressData.expToNextLevel = expToNextLevel;
        progressData.totalMaxHp = totalMaxHp;
        progressData.totalMaxMana = totalMaxMana;
        progressData.totalAttackDamage = totalAttackDamage;
        progressData.totalArmor = totalArmor;
        progressData.totalCriticalChance = totalCriticalChance;
        progressData.totalCriticalMultiplier = totalCriticalMultiplier;
        progressData.totalMoveSpeed = totalMoveSpeed;
        progressData.totalAttackRange = totalAttackRange;
        progressData.totalAttackCooldown = totalAttackCooldown;

        return progressData;
    }
}