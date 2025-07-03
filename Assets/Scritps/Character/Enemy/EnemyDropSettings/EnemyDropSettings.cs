using UnityEngine;

/// <summary>
/// ตั้งค่าการ drop ของ enemy แต่ละประเภท (เฉพาะเงินและเพชร)
/// </summary>
[CreateAssetMenu(fileName = "New Enemy Drop Settings", menuName = "Game/Enemy Drop Settings")]
public class EnemyDropSettings : ScriptableObject
{
    [Header("💰 Currency Drops")]
    [Tooltip("จำนวนเงินขั้นต่ำที่จะ drop")]
    public long minGoldDrop = 10;

    [Tooltip("จำนวนเงินสูงสุดที่จะ drop")]
    public long maxGoldDrop = 50;

    [Tooltip("โอกาสที่จะ drop เงิน (0-100%)")]
    [Range(0f, 100f)]
    public float goldDropChance = 80f;

    [Space]
    [Tooltip("จำนวนเพชรขั้นต่ำที่จะ drop")]
    public int minGemsDrop = 0;

    [Tooltip("จำนวนเพชรสูงสุดที่จะ drop")]
    public int maxGemsDrop = 3;

    [Tooltip("โอกาสที่จะ drop เพชร (0-100%)")]
    [Range(0f, 100f)]
    public float gemsDropChance = 5f;

    [Header("🎯 Level Scaling")]
    [Tooltip("เพิ่มเงินตาม level ของ enemy (% per level)")]
    [Range(0f, 100f)]
    public float goldLevelBonus = 10f;

    [Tooltip("เพิ่มโอกาส drop ตาม level ของ enemy (% per level)")]
    [Range(0f, 10f)]
    public float dropChanceLevelBonus = 2f;

    [Header("🔧 Debug")]
    [Tooltip("แสดง log เมื่อมีการ drop")]
    public bool showDropLogs = true;

    [Tooltip("บังคับ drop ทุกอย่างเพื่อทดสอบ")]
    public bool guaranteedDropsForTesting = false;

    public long CalculateGoldDrop(int enemyLevel)
    {
        if (Random.Range(0f, 100f) > GetEffectiveGoldDropChance(enemyLevel) && !guaranteedDropsForTesting)
            return 0;

        long baseGold = Random.Range((int)minGoldDrop, (int)maxGoldDrop + 1);
        float levelMultiplier = 1f + (goldLevelBonus / 100f) * (enemyLevel - 1);
        long finalGold = Mathf.RoundToInt(baseGold * levelMultiplier);

        return finalGold;
    }

    public int CalculateGemsDrop(int enemyLevel)
    {
        if (Random.Range(0f, 100f) > GetEffectiveGemsDropChance(enemyLevel) && !guaranteedDropsForTesting)
            return 0;

        return Random.Range(minGemsDrop, maxGemsDrop + 1);
    }

    private float GetEffectiveGoldDropChance(int enemyLevel)
    {
        return Mathf.Min(100f, goldDropChance + (dropChanceLevelBonus * (enemyLevel - 1)));
    }

    private float GetEffectiveGemsDropChance(int enemyLevel)
    {
        return Mathf.Min(100f, gemsDropChance + (dropChanceLevelBonus * (enemyLevel - 1)));
    }

    [ContextMenu("Create Weak Enemy Preset")]
    public void CreateWeakEnemyPreset()
    {
        minGoldDrop = 5; maxGoldDrop = 15; goldDropChance = 70f;
        minGemsDrop = 0; maxGemsDrop = 1; gemsDropChance = 3f;
        goldLevelBonus = 5f; dropChanceLevelBonus = 1f;
    }

    [ContextMenu("Create Normal Enemy Preset")]
    public void CreateNormalEnemyPreset()
    {
        minGoldDrop = 15; maxGoldDrop = 40; goldDropChance = 80f;
        minGemsDrop = 0; maxGemsDrop = 2; gemsDropChance = 5f;
        goldLevelBonus = 10f; dropChanceLevelBonus = 2f;
    }

    [ContextMenu("Create Boss Enemy Preset")]
    public void CreateBossEnemyPreset()
    {
        minGoldDrop = 100; maxGoldDrop = 300; goldDropChance = 100f;
        minGemsDrop = 3; maxGemsDrop = 10; gemsDropChance = 80f;
        goldLevelBonus = 25f; dropChanceLevelBonus = 5f;
    }
}

