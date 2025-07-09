using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class GachaMachine : MonoBehaviour
{
    [Header("Machine Settings")]
    public string machineId = "";
    public string machineName = "";
    public GachaPoolData gachaPool;

    [Header("Guarantee Tracking")]
    public bool trackGuarantee = true;
    private int rollsSinceLastRare = 0;

    [Header("Visual Settings")]
    public Animator machineAnimator;
    public ParticleSystem rollEffect;
    public AudioSource audioSource;
    public AudioClip rollSound;
    public AudioClip rareItemSound;

    #region Events
    public static event Action<GachaMachine, List<GachaReward>> OnGachaRolled;
    public static event Action<GachaMachine, GachaReward> OnRareItemObtained;
    public static event Action<GachaMachine, string> OnGachaError;
    #endregion

    #region Properties
    public GachaPoolData Pool => gachaPool;
    public int RollsSinceLastRare => rollsSinceLastRare;
    public bool IsGuaranteeReady => trackGuarantee && gachaPool != null && gachaPool.hasGuarantee && rollsSinceLastRare >= gachaPool.guaranteeCount;
    #endregion

    #region Initialization
    void Awake()
    {
        if (string.IsNullOrEmpty(machineId))
        {
            machineId = $"machine_{name.Replace(" ", "_").ToLower()}";
        }

        if (string.IsNullOrEmpty(machineName))
        {
            machineName = gachaPool?.poolName ?? name;
        }
    }

    void Start()
    {
        ValidateSetup();
    }

    private void ValidateSetup()
    {
        if (gachaPool == null)
        {
            Debug.LogError($" GachaMachine '{machineName}' has no gacha pool assigned!");
            return;
        }

        if (gachaPool.GachaItems.Count == 0)
        {
            Debug.LogError($" GachaMachine '{machineName}' pool has no items!");
            return;
        }

        Debug.Log($" GachaMachine '{machineName}' initialized with {gachaPool.GachaItems.Count} items");
    }
    #endregion

    #region Gacha Operations
    public bool CanRoll(int rollCount = 1)
    {
        if (gachaPool == null) return false;
        if (gachaPool.GachaItems.Count == 0) return false;

        // TODO: ตรวจสอบ currency ที่นี่
        return true;
    }

    public List<GachaReward> RollSingle()
    {
        return Roll(1);
    }

    public List<GachaReward> RollTen()
    {
        return Roll(10);
    }

    public List<GachaReward> Roll(int count)
    {
        if (!CanRoll(count))
        {
            string error = "Cannot perform gacha roll";
            Debug.LogWarning($" {error}");
            OnGachaError?.Invoke(this, error);
            return new List<GachaReward>();
        }

        List<GachaReward> rewards = new List<GachaReward>();

        Debug.Log($"🎰 Rolling {count} times on machine '{machineName}'");

        for (int i = 0; i < count; i++)
        {
            GachaReward reward = PerformSingleRoll();
            if (reward != null && reward.IsValid())
            {
                rewards.Add(reward);
            }
        }

        // แจ้งผลลัพธ์
        OnGachaRolled?.Invoke(this, rewards);

        // เล่นเสียงและ effects
        PlayRollEffects(rewards);

        return rewards;
    }

    private GachaReward PerformSingleRoll()
    {
        if (gachaPool == null) return null;

        GachaItemEntry selectedEntry = null;
        bool isGuaranteed = false;

        // ตรวจสอบ guarantee
        if (IsGuaranteeReady)
        {
            selectedEntry = gachaPool.GetGuaranteedItem(gachaPool.guaranteeTier);
            isGuaranteed = true;
            rollsSinceLastRare = 0;
            Debug.Log($" Guarantee activated! Got {selectedEntry?.itemData?.ItemName}");
        }
        else
        {
            selectedEntry = gachaPool.GetRandomItem();
            rollsSinceLastRare++;
        }

        if (selectedEntry == null || !selectedEntry.IsValid())
        {
            // แสดงสถานะแต่ละ entry
            for (int i = 0; i < gachaPool.GachaItems.Count; i++)
            {
                var e = gachaPool.GachaItems[i];
                Debug.LogError(
                    $"[GachaMachine:{machineName}] Entry {i} — " +
                    $"itemData={(e.itemData == null ? "null" : e.itemData.ItemName)}; " +
                    $"IsValid={e.IsValid()}"
                );
            }
            
            Debug.LogError(" Failed to get valid gacha item");
            return null;
        }

        // สร้าง reward
        int quantity = selectedEntry.GetRandomQuantity();
        GachaReward reward = new GachaReward(selectedEntry.itemData, quantity, isGuaranteed);

        // ตรวจสอบว่าเป็น rare item หรือไม่
        if (selectedEntry.itemData.Tier >= ItemTier.Rare || selectedEntry.isRareItem)
        {
            rollsSinceLastRare = 0; // reset guarantee counter
            OnRareItemObtained?.Invoke(this, reward);
        }

        Debug.Log($" Rolled: {reward.GetRewardText()} (Tier: {selectedEntry.itemData.GetTierText()})");

        return reward;
    }
    #endregion

    #region Effects & Animation
    private void PlayRollEffects(List<GachaReward> rewards)
    {
        // เล่น animation
        if (machineAnimator != null)
        {
            machineAnimator.SetTrigger("Roll");
        }

        // เล่น particle effect
        if (rollEffect != null)
        {
            rollEffect.Play();
        }

        // เล่นเสียง
        bool hasRareItem = rewards.Any(r => r.itemData.Tier >= ItemTier.Rare);
        AudioClip soundToPlay = hasRareItem && rareItemSound != null ? rareItemSound : rollSound;

        if (audioSource != null && soundToPlay != null)
        {
            audioSource.PlayOneShot(soundToPlay);
        }
    }
    #endregion

    #region Guarantee Management
    public void ResetGuaranteeCounter()
    {
        rollsSinceLastRare = 0;
        Debug.Log($" Reset guarantee counter for machine '{machineName}'");
    }

    public void SetGuaranteeCounter(int value)
    {
        rollsSinceLastRare = Mathf.Max(0, value);
        Debug.Log($" Set guarantee counter to {rollsSinceLastRare} for machine '{machineName}'");
    }
    #endregion

    #region Debug
    [ContextMenu("Test Single Roll")]
    public void TestSingleRoll()
    {
        var rewards = RollSingle();
        foreach (var reward in rewards)
        {
            Debug.Log($" Test Roll Result: {reward.GetRewardText()}");
        }
    }

    [ContextMenu("Test Ten Rolls")]
    public void TestTenRolls()
    {
        var rewards = RollTen();
        Debug.Log($" Test 10 Rolls completed. Got {rewards.Count} items");
    }

    [ContextMenu("Debug Machine Info")]
    public void DebugMachineInfo()
    {
        Debug.Log($" Machine: {machineName} ({machineId})");
        Debug.Log($" Pool: {gachaPool?.poolName ?? "None"}");
        Debug.Log($" Guarantee: {rollsSinceLastRare}/{(gachaPool?.guaranteeCount ?? 0)} rolls");
        Debug.Log($" Guarantee Ready: {IsGuaranteeReady}");
    }
    #endregion
}
