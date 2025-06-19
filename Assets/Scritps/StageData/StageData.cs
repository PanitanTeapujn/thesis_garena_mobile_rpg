using UnityEngine;

[CreateAssetMenu(fileName = "StageData", menuName = "Game/Stage Data")]
public class StageData : ScriptableObject
{
    [Header("Stage Information")]
    public string stageName;           // ชื่อ substage เช่น "PlayRoom1_1"
    public string sceneNameToLoad;     // Scene ที่จะโหลด เช่น "PlayRoom1_1"
    public int stageOrder;            // ลำดับ substage (0, 1, 2, ...)

    [Header("Unlock Conditions")]
    public int requiredEnemyKills = 10;  // จำนวน Enemy ที่ต้องกำจัด
    public string[] requiredPreviousStages; // substage ที่ต้องผ่านมาก่อน

    [Header("UI Display")]
    public Sprite stageIcon;
    public string stageDescription;
}
