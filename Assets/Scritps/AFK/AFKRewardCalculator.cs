using UnityEngine;
using System;

public static class AFKRewardCalculator
{
    private const int MAX_AFK_HOURS = 8;
    private const int GOLD_PER_HOUR = 100;

    /// <summary>
    /// คำนวณรางวัล AFK จากเวลาที่ผ่านไป
    /// </summary>
    public static AFKRewardResult CalculateReward(string lastLoginTime, int dummyLevel, int playerLevel)
    {
        var result = new AFKRewardResult();

        try
        {
            // Parse เวลา
            if (!DateTime.TryParse(lastLoginTime, out DateTime lastLogin))
            {
                Debug.LogWarning("[AFKRewardCalculator] Invalid last login time format");
                return result;
            }

            DateTime now = DateTime.Now;
            TimeSpan afkDuration = now - lastLogin;

            // คำนวณจำนวนชั่วโมงที่ AFK (สูงสุด 8 ชั่วโมง)
            float totalHours = (float)afkDuration.TotalHours;
            float clampedHours = Mathf.Min(totalHours, MAX_AFK_HOURS);

            // ถ้า AFK น้อยกว่า 1 นาที ไม่ได้รางวัล
            if (totalHours < (1f / 60f))
            {
                Debug.Log("[AFKRewardCalculator] AFK time too short (< 1 minute)");
                return result;
            }

            result.hasReward = true;
            result.afkHours = clampedHours;
            result.dummyLevel = dummyLevel;
            result.playerLevel = playerLevel;

            // คำนวณเงินพื้นฐาน (100 gold ต่อชั่วโมง)
            result.baseGold = (long)(clampedHours * GOLD_PER_HOUR);

            // ✅ โบนัสจาก Dummy Level = Base Gold * Dummy Level
            result.dummyBonus = result.baseGold * dummyLevel;

            // ✅ โบนัสจาก Player Level (1% ต่อ level) คำนวณจาก (Base + Dummy Bonus)
            float playerMultiplier = playerLevel * 0.01f;
            result.playerLevelBonus = (long)((result.baseGold + result.dummyBonus) * playerMultiplier);

            // เงินรวมทั้งหมด
            result.totalGold = result.baseGold + result.dummyBonus + result.playerLevelBonus;

            Debug.Log($"[AFKRewardCalculator] ✅ Calculated AFK reward:");
            Debug.Log($"  AFK Time: {result.GetAfkTimeDisplay()} (actual: {totalHours:F2}h, capped: {clampedHours:F2}h)");
            Debug.Log($"  Dummy Level: {dummyLevel} (x{dummyLevel})");
            Debug.Log($"  Player Level: {playerLevel} (+{playerMultiplier * 100:F0}%)");
            Debug.Log($"  Base Gold: {result.baseGold}");
            Debug.Log($"  Dummy Bonus: +{result.dummyBonus} ({result.baseGold} x {dummyLevel})");
            Debug.Log($"  Player Bonus: +{result.playerLevelBonus}");
            Debug.Log($"  💰 Total: {result.totalGold} gold");

            return result;
        }
        catch (Exception e)
        {
            Debug.LogError($"[AFKRewardCalculator] ❌ Error calculating reward: {e.Message}");
            return result;
        }
    }

    /// <summary>
    /// ตรวจสอบว่ามีรางวัล AFK รอรับหรือไม่
    /// </summary>
    public static bool HasPendingReward(string lastLoginTime)
    {
        if (!DateTime.TryParse(lastLoginTime, out DateTime lastLogin))
            return false;

        TimeSpan afkDuration = DateTime.Now - lastLogin;
        return afkDuration.TotalMinutes >= 1; // อย่างน้อย 1 นาที
    }
}