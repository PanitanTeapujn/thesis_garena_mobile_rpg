using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DailyQuestItemUI : MonoBehaviour
{
    [Header("Quest Info UI")]
    public Image questIcon;
    public TextMeshProUGUI questNameText;
    public TextMeshProUGUI questDescriptionText;

    [Header("Progress UI")]
    public Slider progressBar;
    public TextMeshProUGUI progressText;

    [Header("Reward UI")]
    public Transform rewardContainer;
    public GameObject goldRewardPrefab;
    public GameObject gemsRewardPrefab;
    public GameObject expRewardPrefab;

    [Header("Button UI")]
    public Button claimButton;
    public TextMeshProUGUI buttonText;

    [Header("Status UI")]
    public GameObject completedMark;
    public GameObject expiredMark;
    public Image backgroundImage;

    [Header("Quest Type Icons")]
    public Sprite loginIcon;
    public Sprite combatIcon;
    public Sprite collectIcon;
    public Sprite stageIcon;

    [Header("Colors")]
    public Color availableColor = Color.white;
    public Color completedColor = Color.green;
    public Color claimedColor = Color.gray;
    public Color expiredColor = Color.red;

    private DailyQuestData currentQuest;
    private DailyQuest questManager;

    public void SetupQuest(DailyQuestData quest, DailyQuest manager)
    {
        currentQuest = quest;
        questManager = manager;

        UpdateQuestDisplay();
        SetupButton();
    }

    void UpdateQuestDisplay()
    {
        if (currentQuest == null) return;

        // Update quest info
        questNameText.text = currentQuest.questName;
        questDescriptionText.text = currentQuest.questDescription;

        // Update quest icon
        UpdateQuestIcon();

        // Update progress
        UpdateProgress();

        // Update rewards
        UpdateRewardDisplay();

        // Update status
        UpdateQuestStatus();
    }

    void UpdateQuestIcon()
    {
        if (questIcon == null) return;

        Sprite iconToUse = null;

        switch (currentQuest.questType)
        {
            case QuestType.DailyLogin:
                iconToUse = loginIcon;
                break;
            case QuestType.KillEnemies:
                iconToUse = combatIcon;
                break;
            case QuestType.CollectItems:
                iconToUse = collectIcon;
                break;
            case QuestType.CompleteStages:
                iconToUse = stageIcon;
                break;
        }

        if (iconToUse != null)
        {
            questIcon.sprite = iconToUse;
            questIcon.color = Color.white;
        }
    }

    void UpdateProgress()
    {
        if (progressBar != null)
        {
            float progress = currentQuest.GetProgressPercentage();
            progressBar.value = progress;
        }

        if (progressText != null)
        {
            progressText.text = $"{currentQuest.currentProgress}/{currentQuest.targetValue}";
        }
    }

    void UpdateRewardDisplay()
    {
        if (rewardContainer == null) return;

        // Clear existing reward displays
        foreach (Transform child in rewardContainer)
        {
            Destroy(child.gameObject);
        }

        var reward = currentQuest.reward;

        // Show gold reward
        if (reward.goldReward > 0)
        {
            CreateRewardItem(goldRewardPrefab, reward.goldReward.ToString(), "💰");
        }

        // Show gems reward
        if (reward.gemsReward > 0)
        {
            CreateRewardItem(gemsRewardPrefab, reward.gemsReward.ToString(), "💎");
        }

        // Show exp reward
        if (reward.expReward > 0)
        {
            CreateRewardItem(expRewardPrefab, reward.expReward.ToString(), "⭐");
        }
    }

    void CreateRewardItem(GameObject prefab, string amount, string icon)
    {
        if (prefab == null) return;

        GameObject rewardItem = Instantiate(prefab, rewardContainer);

        // Try to find text components in the reward item
        var amountText = rewardItem.GetComponentInChildren<TextMeshProUGUI>();
        if (amountText != null)
        {
            amountText.text = amount;
        }

        // Try to find image component for icon
        var iconImage = rewardItem.GetComponentInChildren<Image>();
        if (iconImage != null)
        {
            // You might want to set appropriate sprites here
            iconImage.color = Color.white;
        }
    }

    void UpdateQuestStatus()
    {
        Color statusColor = availableColor;
        string buttonTextString = "In Progress";
        bool buttonInteractable = false;

        if (currentQuest.IsExpired())
        {
            statusColor = expiredColor;
            buttonTextString = "Expired";
            buttonInteractable = false;

            if (expiredMark != null)
                expiredMark.SetActive(true);
            if (completedMark != null)
                completedMark.SetActive(false);
        }
        else if (currentQuest.isRewardClaimed)
        {
            statusColor = claimedColor;
            buttonTextString = "Claimed";
            buttonInteractable = false;

            if (completedMark != null)
                completedMark.SetActive(true);
            if (expiredMark != null)
                expiredMark.SetActive(false);
        }
        else if (currentQuest.isCompleted)
        {
            statusColor = completedColor;
            buttonTextString = "Claim Reward";
            buttonInteractable = true;

            if (completedMark != null)
                completedMark.SetActive(true);
            if (expiredMark != null)
                expiredMark.SetActive(false);
        }
        else
        {
            statusColor = availableColor;
            buttonTextString = $"{currentQuest.currentProgress}/{currentQuest.targetValue}";
            buttonInteractable = false;

            if (completedMark != null)
                completedMark.SetActive(false);
            if (expiredMark != null)
                expiredMark.SetActive(false);
        }

        // Update background color
        if (backgroundImage != null)
        {
            backgroundImage.color = statusColor;
        }

        // Update button
        if (claimButton != null)
        {
            claimButton.interactable = buttonInteractable;
        }

        if (buttonText != null)
        {
            buttonText.text = buttonTextString;
        }
    }

    void SetupButton()
    {
        if (claimButton != null)
        {
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(OnClaimButtonClicked);
        }
    }

    void OnClaimButtonClicked()
    {
        if (currentQuest == null || questManager == null) return;

        if (currentQuest.CanClaimReward())
        {
            questManager.ClaimQuestReward(currentQuest.questId);

            // Play claim animation or sound effect here
            PlayClaimEffect();
        }
        else
        {
            Debug.LogWarning($"[DailyQuestItemUI] Cannot claim reward for quest: {currentQuest.questName}");
        }
    }

    void PlayClaimEffect()
    {
        // Add your claim effect here
        // For example: particle effects, sound, animation

        Debug.Log($"[DailyQuestItemUI] Played claim effect for: {currentQuest.questName}");

        // Simple scale animation
        StartCoroutine(ClaimAnimation());
    }

    System.Collections.IEnumerator ClaimAnimation()
    {
        Vector3 originalScale = transform.localScale;

        // Scale up
        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            transform.localScale = Vector3.Lerp(originalScale, originalScale * 1.1f, progress);
            yield return null;
        }

        // Scale back
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            transform.localScale = Vector3.Lerp(originalScale * 1.1f, originalScale, progress);
            yield return null;
        }

        transform.localScale = originalScale;
    }

    // Public method to refresh the display
    public void RefreshDisplay()
    {
        UpdateQuestDisplay();
    }

    // Public method to get current quest data
    public DailyQuestData GetQuestData()
    {
        return currentQuest;
    }
}