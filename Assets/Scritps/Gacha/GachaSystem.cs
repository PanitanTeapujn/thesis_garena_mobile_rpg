using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Define an item that can be drawn in the gacha
[CreateAssetMenu(fileName = "GachaItem", menuName = "Gacha/Item")]
public class GachaItem : ScriptableObject
{
    [Tooltip("Unique identifier for the item")] public string itemId;
    [Tooltip("Icon or prefab representing this item")] public Sprite icon;
    [Tooltip("Relative probability weight for this item")] public int weight;
}

// Core gacha system for random draws and basic UI handling
public class GachaSystem : MonoBehaviour
{
    [Header("Gacha Pool")]
    [Tooltip("List of items available in this gacha")] public List<GachaItem> items;

    [Header("UI Components")]
    [Tooltip("Button to trigger a single gacha draw")] public Button drawButton;
    [Tooltip("Button to trigger a 10x gacha draw")] public Button multiDrawButton;
    [Tooltip("Image to display the drawn item's icon")] public Image resultImage;
    [Tooltip("Text to display the drawn item's ID")] public Text resultText;

    private int totalWeight;

    private void Awake()
    {
        CalculateTotalWeight();
    }

    private void Start()
    {
        // Register listeners if buttons are assigned
        if (drawButton != null)
            drawButton.onClick.AddListener(OnDrawButtonClicked);
        if (multiDrawButton != null)
            multiDrawButton.onClick.AddListener(OnMultiDrawButtonClicked);
    }

    // Compute the sum of all weights in the pool
    private void CalculateTotalWeight()
    {
        totalWeight = 0;
        foreach (var item in items)
            totalWeight += Mathf.Max(0, item.weight);
    }

    /// <summary>
    /// Public method to call via Inspector on a UI Button
    /// Triggers a single draw
    /// </summary>
    public void OnDrawButtonClicked()
    {
        var item = Draw();
        if (item != null)
            UpdateUI(item);
    }

    /// <summary>
    /// Public method to call via Inspector on a UI Button
    /// Triggers a 10x draw
    /// </summary>
    public void OnMultiDrawButtonClicked()
    {
        var itemsDrawn = MultiDraw(10);
        // For simplicity display first result
        if (itemsDrawn.Count > 0)
            UpdateUI(itemsDrawn[0]);
    }

    // Update UI elements with the drawn item
    private void UpdateUI(GachaItem item)
    {
        if (resultImage != null)
            resultImage.sprite = item.icon;
        if (resultText != null)
            resultText.text = item.itemId;
    }

    /// <summary>
    /// Perform a single gacha draw based on weighted probabilities and log the result.
    /// </summary>
    public GachaItem Draw()
    {
        if (items == null || items.Count == 0)
        {
            Debug.LogWarning("Gacha pool is empty.");
            return null;
        }

        int randomWeight = Random.Range(0, totalWeight);
        int current = 0;

        foreach (var item in items)
        {
            current += Mathf.Max(0, item.weight);
            if (randomWeight < current)
            {
                Debug.Log($"Gacha Draw: got {item.itemId}");
                return item;
            }
        }

        var fallback = items[items.Count - 1];
        Debug.Log($"Gacha Draw (fallback): got {fallback.itemId}");
        return fallback;
    }

    /// <summary>
    /// Perform multiple draws at once (e.g., 10x pull) and log each step.
    /// </summary>
    public List<GachaItem> MultiDraw(int count)
    {
        Debug.Log($"Gacha MultiDraw: performing {count} pulls");
        List<GachaItem> results = new List<GachaItem>();
        for (int i = 0; i < count; i++)
        {
            var item = Draw();
            results.Add(item);
        }
        Debug.Log($"Gacha MultiDraw: results count = {results.Count}");
        return results;
    }
}

