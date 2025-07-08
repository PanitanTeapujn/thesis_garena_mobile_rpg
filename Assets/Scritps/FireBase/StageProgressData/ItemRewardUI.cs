using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Script สำหรับ Item Reward UI Element - แก้ไข layout issues
/// </summary>
public class ItemRewardUI : MonoBehaviour
{
    [Header("UI Components")]
    public Image itemIcon;
    public Image backgroundImage;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI quantityText;
    public GameObject rarityGlow;

    [Header("🔧 Layout Settings")]
    public float preferredWidth = 120f;
    public float preferredHeight = 140f;

    [Header("Tier Colors")]
    public Color commonColor = Color.white;
    public Color uncommonColor = Color.green;
    public Color rareColor = Color.blue;
    public Color epicColor = Color.magenta;
    public Color legendaryColor = Color.yellow;

    private void Awake()
    {
        // 🔧 ตั้งค่า Layout Element เพื่อป้องกันการทับกัน
        LayoutElement layoutElement = GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.preferredWidth = preferredWidth;
        layoutElement.preferredHeight = preferredHeight;
        layoutElement.flexibleWidth = 0;
        layoutElement.flexibleHeight = 0;

        // 🔧 ตั้งค่า RectTransform
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(preferredWidth, preferredHeight);
        }
    }

    /// <summary>
    /// ตั้งค่าข้อมูลไอเทม
    /// </summary>
    public void SetItemData(ItemRewardInfo itemReward)
    {
        if (itemReward == null) return;

        // ตั้งค่าไอคอน
        if (itemIcon != null && itemReward.itemIcon != null)
        {
            itemIcon.sprite = itemReward.itemIcon;

            // 🔧 ตั้งค่าให้ไอคอนไม่ยืดหดตามขนาด
            itemIcon.preserveAspect = true;
        }

        // ตั้งค่าชื่อไอเทม
        if (itemNameText != null)
        {
            itemNameText.text = itemReward.itemName;

            // 🔧 จำกัดความยาวชื่อ
            if (itemReward.itemName.Length > 12)
            {
                itemNameText.text = itemReward.itemName.Substring(0, 10) + "...";
            }
        }

        // ตั้งค่าจำนวน
        if (quantityText != null)
        {
            if (itemReward.quantity > 1)
            {
                quantityText.text = $"x{itemReward.quantity}";
                quantityText.gameObject.SetActive(true);
            }
            else
            {
                quantityText.gameObject.SetActive(false);
            }
        }

        // ตั้งค่าสีตาม tier
        SetTierVisuals(itemReward.itemTier);
    }

    /// <summary>
    /// ตั้งค่าสีและเอฟเฟกต์ตาม tier
    /// </summary>
    private void SetTierVisuals(ItemTier tier)
    {
        Color tierColor = GetTierColor(tier);

        // ตั้งค่าสี background
        if (backgroundImage != null)
        {
            backgroundImage.color = tierColor * 0.3f; // สีอ่อนลง
        }

        // ตั้งค่าสีข้อความ
        if (itemNameText != null)
        {
            itemNameText.color = tierColor;
        }

        if (quantityText != null)
        {
            quantityText.color = tierColor;
        }

        // แสดง glow effect สำหรับ tier สูง
        if (rarityGlow != null)
        {
            bool shouldGlow = tier >= ItemTier.Rare;
            rarityGlow.SetActive(shouldGlow);

            if (shouldGlow)
            {
                Image glowImage = rarityGlow.GetComponent<Image>();
                if (glowImage != null)
                {
                    glowImage.color = tierColor * 0.5f;
                }
            }
        }
    }

    /// <summary>
    /// ดึงสีตาม tier
    /// </summary>
    private Color GetTierColor(ItemTier tier)
    {
        switch (tier)
        {
            case ItemTier.Common: return commonColor;
            case ItemTier.Uncommon: return uncommonColor;
            case ItemTier.Rare: return rareColor;
            case ItemTier.Epic: return epicColor;
            case ItemTier.Legendary: return legendaryColor;
            default: return commonColor;
        }
    }

    /// <summary>
    /// Animation เมื่อเริ่มแสดง
    /// </summary>
    public void PlayAppearAnimation()
    {
        StartCoroutine(AppearAnimationCoroutine());
    }

    private System.Collections.IEnumerator AppearAnimationCoroutine()
    {
        Vector3 originalScale = transform.localScale;
        transform.localScale = Vector3.zero;

        float duration = 0.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;

            // Bounce effect
            float scale = Mathf.Lerp(0f, 1.2f, progress);
            if (progress > 0.8f)
            {
                scale = Mathf.Lerp(1.2f, 1f, (progress - 0.8f) / 0.2f);
            }

            transform.localScale = originalScale * scale;
            yield return null;
        }

        transform.localScale = originalScale;
    }

    /// <summary>
    /// 🔧 Debug method สำหรับตรวจสอบ layout
    /// </summary>
    [ContextMenu("🔧 Fix Layout")]
    public void FixLayout()
    {
        Awake(); // เรียก setup ใหม่
        Debug.Log($"[ItemRewardUI] Layout fixed: {preferredWidth}x{preferredHeight}");
    }
}