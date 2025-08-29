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

    [Header("Character Gacha Pool")]
    public CharacterGachaPoolData characterPool;
    public bool isCharacterMachine = false;

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
    public CharacterGachaPoolData CharacterPool => characterPool;
    public int RollsSinceLastRare => rollsSinceLastRare;
    public bool IsGuaranteeReady => trackGuarantee && GetGuaranteeCount() > 0 && rollsSinceLastRare >= GetGuaranteeCount();

    private int GetGuaranteeCount()
    {
        if (isCharacterMachine && characterPool != null)
            return characterPool.guaranteeCount;
        else if (gachaPool != null)
            return gachaPool.guaranteeCount;
        return 0;
    }
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
            if (isCharacterMachine && characterPool != null)
                machineName = characterPool.poolName;
            else
                machineName = gachaPool?.poolName ?? name;
        }
    }

    void Start()
    {
        ValidateSetup();
    }

    private void ValidateSetup()
    {
        if (isCharacterMachine)
        {
            if (characterPool == null)
            {
                Debug.LogError($" GachaMachine '{machineName}' is set as character machine but has no character pool assigned!");
                return;
            }

            if (characterPool.CharacterEntries.Count == 0)
            {
                Debug.LogError($" GachaMachine '{machineName}' character pool has no characters!");
                return;
            }

            Debug.Log($" Character GachaMachine '{machineName}' initialized with {characterPool.CharacterEntries.Count} characters");
        }
        else
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
    }
    #endregion

    #region Gacha Operations
    public bool CanRoll(int rollCount = 1)
    {
        if (isCharacterMachine)
        {
            if (characterPool == null) return false;
            if (characterPool.CharacterEntries.Count == 0) return false;
        }
        else
        {
            if (gachaPool == null) return false;
            if (gachaPool.GachaItems.Count == 0) return false;
        }

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
            Debug.LogWarning($"Gacha {error}");
            OnGachaError?.Invoke(this, error);
            return new List<GachaReward>();
        }

        List<GachaReward> rewards = new List<GachaReward>();

        Debug.Log($" Rolling {count} times on machine '{machineName}'{(isCharacterMachine ? " (Character)" : "")}");

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
        if (isCharacterMachine)
        {
            return PerformCharacterRoll();
        }
        else
        {
            return PerformItemRoll();
        }
    }

    private GachaReward PerformItemRoll()
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

    private GachaReward PerformCharacterRoll()
    {
        if (characterPool == null) return null;

        CharacterGachaEntry selectedEntry = null;
        bool isGuaranteed = false;

        // ตรวจสอบ guarantee
        if (IsGuaranteeReady)
        {
            selectedEntry = characterPool.GetGuaranteedCharacter(characterPool.guaranteeRarity);
            isGuaranteed = true;
            rollsSinceLastRare = 0;
            Debug.Log($" Character guarantee activated! Got {selectedEntry?.characterData?.characterName}");
        }
        else
        {
            selectedEntry = characterPool.GetRandomCharacter();
            rollsSinceLastRare++;
        }

        if (selectedEntry == null || !selectedEntry.IsValid())
        {
            Debug.LogError(" Failed to get valid gacha character");
            return null;
        }

        // ตรวจสอบว่าเป็นตัวละครซ้ำหรือไม่ (ต้องมี CharacterCollection)
        bool isDuplicate = false;
        CharacterCollection collection = FindObjectOfType<CharacterCollection>();
        if (collection != null)
        {
            isDuplicate = collection.HasCharacter(selectedEntry.characterData);
        }

        // สร้าง reward
        GachaReward reward;

        if (isDuplicate && selectedEntry.convertDuplicates && selectedEntry.duplicateRewardItem != null)
        {
            // แปลงตัวละครซ้ำเป็น item
            reward = new GachaReward(selectedEntry.duplicateRewardItem, selectedEntry.duplicateRewardAmount, isGuaranteed);
            reward.isDuplicate = true;
            Debug.Log($" Duplicate character converted to: {reward.GetRewardText()}");
        }
        else
        {
            // ตัวละครปกติ
            reward = new GachaReward(selectedEntry.characterData, isGuaranteed, isDuplicate);
        }

        // ตรวจสอบว่าเป็น rare character หรือไม่
        if (selectedEntry.characterData.rarity >= CharacterRarity.Rare)
        {
            rollsSinceLastRare = 0; // reset guarantee counter
            OnRareItemObtained?.Invoke(this, reward);
        }

        Debug.Log($" Rolled: {reward.GetRewardText()} (Rarity: {selectedEntry.characterData.GetRarityText()})");
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
        bool hasRareItem = rewards.Any(r => r.IsRareReward());
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
        Debug.Log($" Test 10 Rolls completed. Got {rewards.Count} rewards");

        foreach (var reward in rewards)
        {
            Debug.Log($"  - {reward.GetRewardText()}");
        }
    }

    [ContextMenu("Debug Machine Info")]
    public void DebugMachineInfo()
    {
        Debug.Log($" Machine: {machineName} ({machineId})");
        Debug.Log($" Type: {(isCharacterMachine ? "Character" : "Item")} Machine");

        if (isCharacterMachine && characterPool != null)
        {
            Debug.Log($" Character Pool: {characterPool.poolName}");
            Debug.Log($" Characters: {characterPool.CharacterEntries.Count}");
        }
        else if (gachaPool != null)
        {
            Debug.Log($" Item Pool: {gachaPool.poolName}");
            Debug.Log($" Items: {gachaPool.GachaItems.Count}");
        }

        Debug.Log($" Guarantee: {rollsSinceLastRare}/{GetGuaranteeCount()} rolls");
        Debug.Log($" Guarantee Ready: {IsGuaranteeReady}");
    }

    [ContextMenu("Force Guarantee Next Roll")]
    public void ForceGuaranteeNextRoll()
    {
        rollsSinceLastRare = GetGuaranteeCount();
        Debug.Log($" Forced guarantee for next roll on '{machineName}'");
    }
    #endregion
}