using UnityEngine;
public static class PlayerSelectionData
{
    // ประเภทตัวละครที่มีในเกม
    public enum CharacterType
    {
        BloodKnight,
        Archer,
        Assassin,
        IronJuggernaut
    }

    // ค่าเริ่มต้น (ถ้าไม่มีการเลือกตัวละคร)
    private const CharacterType DEFAULT_CHARACTER = CharacterType.IronJuggernaut;

    // บันทึกการเลือกตัวละคร
    public static void SaveCharacterSelection(CharacterType character)
    {
        PlayerPrefs.SetInt("SelectedCharacter", (int)character);
        PlayerPrefs.Save();
    }

    // ดึงข้อมูลตัวละครที่เลือก
    public static CharacterType GetSelectedCharacter()
    {
        if (PlayerPrefs.HasKey("SelectedCharacter"))
        {
            return (CharacterType)PlayerPrefs.GetInt("SelectedCharacter");
        }
        return DEFAULT_CHARACTER;
    }
}