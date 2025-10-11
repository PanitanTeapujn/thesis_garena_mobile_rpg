using UnityEngine;

public static class PlayerSelectionData
{
    // ✅ กำหนดค่า explicit เพื่อป้องกัน index mismatch
    public enum CharacterType
    {
        BloodKnight = 0,
        Archer = 1,
        Assassin = 2,
        IronJuggernaut = 3
    }

    // ✅ เปลี่ยน default เป็น Assassin เพื่อ test
    private const CharacterType DEFAULT_CHARACTER = CharacterType.Assassin;

    // บันทึกการเลือกตัวละคร
   /* public static void SaveCharacterSelection(CharacterType character)
    {
        PlayerPrefs.SetInt("SelectedCharacter", (int)character);
        PlayerPrefs.Save();
        Debug.Log($"[PlayerSelectionData] Saved character: {character} (index: {(int)character})");
    }*/

    // ดึงข้อมูลตัวละครที่เลือก
   /* public static CharacterType GetSelectedCharacter()
    {
        if (PlayerPrefs.HasKey("SelectedCharacter"))
        {
            int savedIndex = PlayerPrefs.GetInt("SelectedCharacter");
            CharacterType character = (CharacterType)savedIndex;
            Debug.Log($"[PlayerSelectionData] Loaded character: {character} (index: {savedIndex})");
            return character;
        }

        Debug.Log($"[PlayerSelectionData] No saved character, using default: {DEFAULT_CHARACTER}");
        return DEFAULT_CHARACTER;
    }*/

    // ✅ เพิ่ม method สำหรับ clear saved data
    public static void ClearSavedSelection()
    {
        if (PlayerPrefs.HasKey("SelectedCharacter"))
        {
            PlayerPrefs.DeleteKey("SelectedCharacter");
            PlayerPrefs.Save();
            Debug.Log("[PlayerSelectionData] Cleared saved character selection");
        }
    }

    // ✅ เพิ่ม method สำหรับ debug
    public static void DebugCurrentSelection()
    {
        if (PlayerPrefs.HasKey("SelectedCharacter"))
        {
            int savedIndex = PlayerPrefs.GetInt("SelectedCharacter");
            CharacterType character = (CharacterType)savedIndex;
            Debug.Log($"[PlayerSelectionData] Current saved: {character} (index: {savedIndex})");
        }
        else
        {
            Debug.Log($"[PlayerSelectionData] No saved selection (will use default: {DEFAULT_CHARACTER})");
        }
    }
}