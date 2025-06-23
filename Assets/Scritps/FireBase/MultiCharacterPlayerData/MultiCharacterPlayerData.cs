using UnityEngine;
using System.Collections.Generic;
using System;

[System.Serializable]
public class MultiCharacterPlayerData
{
    #region Variables and Properties  ตัวแปรทั้งหมดรวมถึงข้อมูลผู้เล่น, ตัวละคร, และระบบเพื่อน
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
    #endregion

    #region Constructor and Initialization Constructor และฟังก์ชันสร้างตัวละครเริ่มต้น

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

            // 🔧 ✅ แก้ไข: ใช้ค่าคงที่แทนค่าจาก ScriptableObject
            defaultAssassin.totalCriticalDamageBonus = assassinStats.criticalDamageBonus;

            defaultAssassin.totalMoveSpeed = assassinStats.moveSpeed;
            defaultAssassin.totalAttackRange = assassinStats.attackRange;
            defaultAssassin.totalAttackCooldown = assassinStats.attackCoolDown;
            defaultAssassin.totalHitRate = assassinStats.hitRate;
            defaultAssassin.totalEvasionRate = assassinStats.evasionRate;
            defaultAssassin.totalAttackSpeed = assassinStats.attackSpeed;
            defaultAssassin.totalReductionCoolDown = assassinStats.reductionCoolDown;

            Debug.Log($"✅ Default Assassin created with Critical Multiplier: {defaultAssassin.totalCriticalDamageBonus}");
        }

        characters.Add(defaultAssassin);
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

            // 🔧 ✅ แก้ไข: ใช้ค่าคงที่สำหรับทุกตัวละคร
            newCharacter.totalCriticalDamageBonus = characterStats.criticalDamageBonus; // ค่าที่ต้องการ

            newCharacter.totalMoveSpeed = characterStats.moveSpeed;
            newCharacter.totalAttackRange = characterStats.attackRange;
            newCharacter.totalAttackCooldown = characterStats.attackCoolDown;
            newCharacter.totalHitRate = characterStats.hitRate;
            newCharacter.totalEvasionRate = characterStats.evasionRate;
            newCharacter.totalAttackSpeed = characterStats.attackSpeed;
            newCharacter.totalReductionCoolDown = characterStats.reductionCoolDown;

            Debug.Log($"✅ Created {characterType} with Critical Multiplier: {newCharacter.totalCriticalDamageBonus}");
        }
        return newCharacter;
    }
    #endregion

    #region Character Management การจัดการตัวละคร (ดึงข้อมูล, สร้าง, เปลี่ยน, อัปเดต)
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

    public void UpdateCharacterStats(string characterType, int level, int exp, int expToNext,
        int maxHp, int maxMana, int attackDamage, int magicDamage, int armor, float critChance,
        float critDamageBonus, float moveSpeed, float hitRate, float evasion, float attackSpeed,
        float reductionCoolDown)
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
        character.totalCriticalDamageBonus = critDamageBonus;
        character.totalMoveSpeed = moveSpeed;
        character.totalHitRate = hitRate;
        character.totalEvasionRate = evasion;
        character.totalAttackSpeed = attackSpeed;
        character.totalReductionCoolDown = reductionCoolDown;
    }
    #endregion

    #region Data Validation and Utility  ฟังก์ชันตรวจสอบความถูกต้องของข้อมูล
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(playerName) &&
               !string.IsNullOrEmpty(currentActiveCharacter) &&
               characters.Count > 0;
    }
    #endregion

    #region Debug Methods ฟังก์ชันสำหรับ debug และแสดงข้อมูล
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
    #endregion
}

[System.Serializable]
public class CharacterProgressData
{
    #region Character Identity
    public string characterType;
    #endregion ชื่อประเภทตัวละคร

    #region Level and Experience เลเวลและประสบการณ์
    [Header("Level Progress")]
    public int currentLevel = 1;
    public int currentExp = 0;
    public int expToNextLevel = 100;
    #endregion

    #region Basic Combat Stats สถานะพื้นฐาน (HP, Mana, Attack, Magic, Armor)
    [Header("Basic Stats")]
    public int totalMaxHp;
    public int totalMaxMana;
    public int totalAttackDamage;
    public int totalMagicDamage;
    public int totalArmor;
    #endregion

    #region Critical Strike Stats สถานะ Critical Strike
    [Header("Critical Strike")]
    public float totalCriticalChance;
    public float totalCriticalDamageBonus;
    #endregion

    #region Movement and Attack Stats ความเร็ว, ระยะโจมตี, คูลดาวน์
    [Header("Movement & Attack")]
    public float totalMoveSpeed;
    public float totalAttackRange;
    public float totalAttackCooldown;
    public float totalAttackSpeed;
    #endregion

    #region Accuracy and Defense Stats  Hit Rate และ Evasion Rate
    [Header("Accuracy & Defense")]
    public float totalHitRate;
    public float totalEvasionRate;
    #endregion

    #region Special Stats สถานะพิเศษอื่นๆ
    [Header("Special Stats")]
    public float totalReductionCoolDown;
    #endregion
}