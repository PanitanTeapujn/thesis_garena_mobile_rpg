using UnityEngine;

public class QuestProgressHandler : MonoBehaviour
{
    void Start()
    {
        // เมื่อโหลด lobby scene ให้ flush progress ทันที
        DailyQuestTracker.FlushProgressToQuests();
    }
}