using UnityEngine;

/// <summary>
/// Component สำหรับกำหนดประเภทของ Enemy
/// ใช้ในระบบเงื่อนไขการผ่านด่าน
/// </summary>
public enum EnemyTag
{
    Normal,         // ศัตรูธรรมดา - นับในเงื่อนไข ✅
    Boss,           // บอส - นับในเงื่อนไข ✅
    MiniBoss,       // มินิบอส - นับในเงื่อนไข ✅
    Summoned,       // ลูกน้องที่ถูกเสก - ไม่นับ ❌ ⭐ สำคัญ
    Elite,          // ศัตรู Elite - นับในเงื่อนไข ✅
    Environmental,  // ศัตรูสิ่งแวดล้อม - ไม่นับ ❌
    Dummy           // Dummy ฝึกซ้อม - ไม่นับ ❌
}

public class EnemyTagComponent : MonoBehaviour
{
    [Header("🏷️ Enemy Classification")]
    [Tooltip("ประเภทของศัตรู - กำหนดว่านับในเงื่อนไขการผ่านด่านหรือไม่")]
    public EnemyTag enemyTag = EnemyTag.Normal;

    [Header("📋 Display Info")]
    [Tooltip("ชื่อที่แสดงในระบบ (ถ้าเว้นว่างจะใช้ชื่อ GameObject)")]
    public string displayName = "";

    [Tooltip("คำอธิบายสำหรับ Game Designer")]
    [TextArea(2, 4)]
    public string description = "";

    private void Start()
    {
        if (string.IsNullOrEmpty(displayName))
        {
            displayName = gameObject.name;
        }

        Debug.Log($"[EnemyTag] {displayName} - Tag: {enemyTag}, Counts: {CountsForStageClear()}");
    }

    /// <summary>
    /// เช็คว่า Enemy ตัวนี้นับในเงื่อนไขการผ่านด่านหรือไม่
    /// </summary>
    public bool CountsForStageClear()
    {
        switch (enemyTag)
        {
            case EnemyTag.Normal:
            case EnemyTag.Boss:
            case EnemyTag.MiniBoss:
            case EnemyTag.Elite:
                return true; // นับ

            case EnemyTag.Summoned:
            case EnemyTag.Environmental:
            case EnemyTag.Dummy:
                return false; // ไม่นับ

            default:
                return true;
        }
    }

    /// <summary>
    /// เปลี่ยน Tag (ใช้เมื่อ Boss เสกลูกน้อง)
    /// </summary>
    public void SetTag(EnemyTag newTag)
    {
        enemyTag = newTag;
        Debug.Log($"[EnemyTag] {displayName} tag changed to: {newTag}");
    }
}