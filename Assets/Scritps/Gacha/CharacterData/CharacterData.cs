using UnityEngine;

[System.Serializable]
public enum CharacterRarity
{
    Common = 1,
    Uncommon = 2,
    Rare = 3,
    Epic = 4,
    Legendary = 5
}

[CreateAssetMenu(fileName = "NewCharacterData", menuName = "Gacha System/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Basic Information")]
    public string characterId;
    public string characterName;
    public CharacterRarity rarity = CharacterRarity.Common;

    [Header("Visual")]
    public Sprite characterIcon;
    public Sprite fullArtwork;
    public RuntimeAnimatorController animatorController;
    public GameObject characterPrefab;

    [Header("Character Stats")]
    public CharacterStats baseStats;

    [Header("Description")]
    [TextArea(3, 5)]
    public string description;
    [TextArea(2, 3)]
    public string lore;

    [Header("Gacha Settings")]
    public bool isLimitedCharacter = false;
    public bool isFeatured = false;

    #region Properties
    public Color GetRarityColor()
    {
        return rarity switch
        {
            CharacterRarity.Common => Color.white,
            CharacterRarity.Uncommon => Color.green,
            CharacterRarity.Rare => Color.blue,
            CharacterRarity.Epic => Color.magenta,
            CharacterRarity.Legendary => Color.yellow,
            _ => Color.white
        };
    }

    public string GetRarityText()
    {
        return rarity switch
        {
            CharacterRarity.Common => "Common",
            CharacterRarity.Uncommon => "Uncommon",
            CharacterRarity.Rare => "Rare",
            CharacterRarity.Epic => "Epic",
            CharacterRarity.Legendary => "Legendary",
            _ => "Unknown"
        };
    }

    public int GetRarityStars()
    {
        return (int)rarity;
    }
    #endregion

    #region Validation
    void OnValidate()
    {
        if (string.IsNullOrEmpty(characterId))
        {
            characterId = $"char_{name.Replace(" ", "_").ToLower()}";
        }

        if (string.IsNullOrEmpty(characterName))
        {
            characterName = name;
        }
    }
    #endregion
}