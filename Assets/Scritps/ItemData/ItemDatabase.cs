using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory System/Item Database")]
public class ItemDatabase : ScriptableObject
{
    #region Database
    [Header("All Items")]
    [SerializeField] private List<ItemData> allItems = new List<ItemData>();

    [Header("🗂️ Organized by Type")]
    [SerializeField] private List<ItemData> weapons = new List<ItemData>();
    [SerializeField] private List<ItemData> headItems = new List<ItemData>();
    [SerializeField] private List<ItemData> armorItems = new List<ItemData>();
    [SerializeField] private List<ItemData> pantsItems = new List<ItemData>();
    [SerializeField] private List<ItemData> shoesItems = new List<ItemData>();
    [SerializeField] private List<ItemData> runes = new List<ItemData>();
    [SerializeField] private List<ItemData> potions = new List<ItemData>();
    [SerializeField] private List<ItemData> materials = new List<ItemData>();
    [SerializeField] private List<ItemData> miscItems = new List<ItemData>();

    [Header("⚔️ Weapons by Tier")]
    [SerializeField] private List<ItemData> weaponsCommon = new List<ItemData>();
    [SerializeField] private List<ItemData> weaponsUncommon = new List<ItemData>();
    [SerializeField] private List<ItemData> weaponsRare = new List<ItemData>();
    [SerializeField] private List<ItemData> weaponsEpic = new List<ItemData>();
    [SerializeField] private List<ItemData> weaponsLegendary = new List<ItemData>();

    [Header("⛑️ Head Items by Tier")]
    [SerializeField] private List<ItemData> headCommon = new List<ItemData>();
    [SerializeField] private List<ItemData> headUncommon = new List<ItemData>();
    [SerializeField] private List<ItemData> headRare = new List<ItemData>();
    [SerializeField] private List<ItemData> headEpic = new List<ItemData>();
    [SerializeField] private List<ItemData> headLegendary = new List<ItemData>();

    [Header("🛡️ Armor by Tier")]
    [SerializeField] private List<ItemData> armorCommon = new List<ItemData>();
    [SerializeField] private List<ItemData> armorUncommon = new List<ItemData>();
    [SerializeField] private List<ItemData> armorRare = new List<ItemData>();
    [SerializeField] private List<ItemData> armorEpic = new List<ItemData>();
    [SerializeField] private List<ItemData> armorLegendary = new List<ItemData>();

    [Header("👖 Pants by Tier")]
    [SerializeField] private List<ItemData> pantsCommon = new List<ItemData>();
    [SerializeField] private List<ItemData> pantsUncommon = new List<ItemData>();
    [SerializeField] private List<ItemData> pantsRare = new List<ItemData>();
    [SerializeField] private List<ItemData> pantsEpic = new List<ItemData>();
    [SerializeField] private List<ItemData> pantsLegendary = new List<ItemData>();

    [Header("👠 Shoes by Tier")]
    [SerializeField] private List<ItemData> shoesCommon = new List<ItemData>();
    [SerializeField] private List<ItemData> shoesUncommon = new List<ItemData>();
    [SerializeField] private List<ItemData> shoesRare = new List<ItemData>();
    [SerializeField] private List<ItemData> shoesEpic = new List<ItemData>();
    [SerializeField] private List<ItemData> shoesLegendary = new List<ItemData>();

    [Header("🔮 Runes by Tier")]
    [SerializeField] private List<ItemData> runesCommon = new List<ItemData>();
    [SerializeField] private List<ItemData> runesUncommon = new List<ItemData>();
    [SerializeField] private List<ItemData> runesRare = new List<ItemData>();
    [SerializeField] private List<ItemData> runesEpic = new List<ItemData>();
    [SerializeField] private List<ItemData> runesLegendary = new List<ItemData>();

    [Header("🧪 Potions by Tier")]
    [SerializeField] private List<ItemData> potionsCommon = new List<ItemData>();
    [SerializeField] private List<ItemData> potionsUncommon = new List<ItemData>();
    [SerializeField] private List<ItemData> potionsRare = new List<ItemData>();
    [SerializeField] private List<ItemData> potionsEpic = new List<ItemData>();
    [SerializeField] private List<ItemData> potionsLegendary = new List<ItemData>();

    [Header("⚒️ Materials by Tier")]
    [SerializeField] private List<ItemData> materialsCommon = new List<ItemData>();
    [SerializeField] private List<ItemData> materialsUncommon = new List<ItemData>();
    [SerializeField] private List<ItemData> materialsRare = new List<ItemData>();
    [SerializeField] private List<ItemData> materialsEpic = new List<ItemData>();
    [SerializeField] private List<ItemData> materialsLegendary = new List<ItemData>();

    [Header("📦 Misc Items by Tier")]
    [SerializeField] private List<ItemData> miscCommon = new List<ItemData>();
    [SerializeField] private List<ItemData> miscUncommon = new List<ItemData>();
    [SerializeField] private List<ItemData> miscRare = new List<ItemData>();
    [SerializeField] private List<ItemData> miscEpic = new List<ItemData>();
    [SerializeField] private List<ItemData> miscLegendary = new List<ItemData>();

    #endregion

    #region Singleton Access
    private static ItemDatabase _instance;
    public static ItemDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<ItemDatabase>("ItemDatabase");
                if (_instance == null)
                {
                    Debug.LogError("❌ ItemDatabase not found in Resources folder!");
                }
            }
            return _instance;
        }
    }
    #endregion

    #region Initialization
    void OnValidate()
    {
        if (Application.isEditor)
        {
            RefreshItemLists();
        }
    }

    [ContextMenu("Refresh Item Lists")]
    public void RefreshItemLists()
    {
        // Clear all lists
        ClearAllLists();

        // Sort all items by name first
        allItems = allItems.OrderBy(item => item?.ItemName ?? "").ToList();

        // Organize by type and tier
        foreach (var item in allItems)
        {
            if (item == null) continue;

            // Add to type lists
            AddToTypeList(item);

            // Add to tier lists
            AddToTierList(item);
        }

        // Sort all tier lists by name similarity
        SortTierListsBySimilarity();
    }

    private void ClearAllLists()
    {
        // Clear type lists
        weapons.Clear();
        headItems.Clear();
        armorItems.Clear();
        pantsItems.Clear();
        shoesItems.Clear();
        runes.Clear();
        potions.Clear();
        materials.Clear();
        miscItems.Clear();

        // Clear tier lists
        ClearWeaponTierLists();
        ClearHeadTierLists();
        ClearArmorTierLists();
        ClearPantsTierLists();
        ClearShoesTierLists();
        ClearRunesTierLists();
        ClearPotionsTierLists();
        ClearMaterialsTierLists();
        ClearMiscTierLists();
    }

    private void ClearWeaponTierLists()
    {
        weaponsCommon.Clear();
        weaponsUncommon.Clear();
        weaponsRare.Clear();
        weaponsEpic.Clear();
        weaponsLegendary.Clear();
    }

    private void ClearHeadTierLists()
    {
        headCommon.Clear();
        headUncommon.Clear();
        headRare.Clear();
        headEpic.Clear();
        headLegendary.Clear();
    }

    private void ClearArmorTierLists()
    {
        armorCommon.Clear();
        armorUncommon.Clear();
        armorRare.Clear();
        armorEpic.Clear();
        armorLegendary.Clear();
    }

    private void ClearPantsTierLists()
    {
        pantsCommon.Clear();
        pantsUncommon.Clear();
        pantsRare.Clear();
        pantsEpic.Clear();
        pantsLegendary.Clear();
    }

    private void ClearShoesTierLists()
    {
        shoesCommon.Clear();
        shoesUncommon.Clear();
        shoesRare.Clear();
        shoesEpic.Clear();
        shoesLegendary.Clear();
    }

    private void ClearRunesTierLists()
    {
        runesCommon.Clear();
        runesUncommon.Clear();
        runesRare.Clear();
        runesEpic.Clear();
        runesLegendary.Clear();
    }

    private void ClearPotionsTierLists()
    {
        potionsCommon.Clear();
        potionsUncommon.Clear();
        potionsRare.Clear();
        potionsEpic.Clear();
        potionsLegendary.Clear();
    }

    private void ClearMaterialsTierLists()
    {
        materialsCommon.Clear();
        materialsUncommon.Clear();
        materialsRare.Clear();
        materialsEpic.Clear();
        materialsLegendary.Clear();
    }

    private void ClearMiscTierLists()
    {
        miscCommon.Clear();
        miscUncommon.Clear();
        miscRare.Clear();
        miscEpic.Clear();
        miscLegendary.Clear();
    }

    private void AddToTypeList(ItemData item)
    {
        switch (item.ItemType)
        {
            case ItemType.Weapon:
                weapons.Add(item);
                break;
            case ItemType.Head:
                headItems.Add(item);
                break;
            case ItemType.Armor:
                armorItems.Add(item);
                break;
            case ItemType.Pants:
                pantsItems.Add(item);
                break;
            case ItemType.Shoes:
                shoesItems.Add(item);
                break;
            case ItemType.Rune:
                runes.Add(item);
                break;
            case ItemType.Potion:
                potions.Add(item);
                break;
            case ItemType.Material:
                materials.Add(item);
                break;
            case ItemType.Misc:
                miscItems.Add(item);
                break;
        }
    }

    private void AddToTierList(ItemData item)
    {
        switch (item.ItemType)
        {
            case ItemType.Weapon:
                AddWeaponToTierList(item);
                break;
            case ItemType.Head:
                AddHeadToTierList(item);
                break;
            case ItemType.Armor:
                AddArmorToTierList(item);
                break;
            case ItemType.Pants:
                AddPantsToTierList(item);
                break;
            case ItemType.Shoes:
                AddShoesToTierList(item);
                break;
            case ItemType.Rune:
                AddRuneToTierList(item);
                break;
            case ItemType.Potion:
                AddPotionToTierList(item);
                break;
            case ItemType.Material:
                AddMaterialToTierList(item);
                break;
            case ItemType.Misc:
                AddMiscToTierList(item);
                break;
        }
    }

    private void AddWeaponToTierList(ItemData item)
    {
        switch (item.Tier)
        {
            case ItemTier.Common: weaponsCommon.Add(item); break;
            case ItemTier.Uncommon: weaponsUncommon.Add(item); break;
            case ItemTier.Rare: weaponsRare.Add(item); break;
            case ItemTier.Epic: weaponsEpic.Add(item); break;
            case ItemTier.Legendary: weaponsLegendary.Add(item); break;
        }
    }

    private void AddHeadToTierList(ItemData item)
    {
        switch (item.Tier)
        {
            case ItemTier.Common: headCommon.Add(item); break;
            case ItemTier.Uncommon: headUncommon.Add(item); break;
            case ItemTier.Rare: headRare.Add(item); break;
            case ItemTier.Epic: headEpic.Add(item); break;
            case ItemTier.Legendary: headLegendary.Add(item); break;
        }
    }

    private void AddArmorToTierList(ItemData item)
    {
        switch (item.Tier)
        {
            case ItemTier.Common: armorCommon.Add(item); break;
            case ItemTier.Uncommon: armorUncommon.Add(item); break;
            case ItemTier.Rare: armorRare.Add(item); break;
            case ItemTier.Epic: armorEpic.Add(item); break;
            case ItemTier.Legendary: armorLegendary.Add(item); break;
        }
    }

    private void AddPantsToTierList(ItemData item)
    {
        switch (item.Tier)
        {
            case ItemTier.Common: pantsCommon.Add(item); break;
            case ItemTier.Uncommon: pantsUncommon.Add(item); break;
            case ItemTier.Rare: pantsRare.Add(item); break;
            case ItemTier.Epic: pantsEpic.Add(item); break;
            case ItemTier.Legendary: pantsLegendary.Add(item); break;
        }
    }

    private void AddShoesToTierList(ItemData item)
    {
        switch (item.Tier)
        {
            case ItemTier.Common: shoesCommon.Add(item); break;
            case ItemTier.Uncommon: shoesUncommon.Add(item); break;
            case ItemTier.Rare: shoesRare.Add(item); break;
            case ItemTier.Epic: shoesEpic.Add(item); break;
            case ItemTier.Legendary: shoesLegendary.Add(item); break;
        }
    }

    private void AddRuneToTierList(ItemData item)
    {
        switch (item.Tier)
        {
            case ItemTier.Common: runesCommon.Add(item); break;
            case ItemTier.Uncommon: runesUncommon.Add(item); break;
            case ItemTier.Rare: runesRare.Add(item); break;
            case ItemTier.Epic: runesEpic.Add(item); break;
            case ItemTier.Legendary: runesLegendary.Add(item); break;
        }
    }

    private void AddPotionToTierList(ItemData item)
    {
        switch (item.Tier)
        {
            case ItemTier.Common: potionsCommon.Add(item); break;
            case ItemTier.Uncommon: potionsUncommon.Add(item); break;
            case ItemTier.Rare: potionsRare.Add(item); break;
            case ItemTier.Epic: potionsEpic.Add(item); break;
            case ItemTier.Legendary: potionsLegendary.Add(item); break;
        }
    }

    private void AddMaterialToTierList(ItemData item)
    {
        switch (item.Tier)
        {
            case ItemTier.Common: materialsCommon.Add(item); break;
            case ItemTier.Uncommon: materialsUncommon.Add(item); break;
            case ItemTier.Rare: materialsRare.Add(item); break;
            case ItemTier.Epic: materialsEpic.Add(item); break;
            case ItemTier.Legendary: materialsLegendary.Add(item); break;
        }
    }

    private void AddMiscToTierList(ItemData item)
    {
        switch (item.Tier)
        {
            case ItemTier.Common: miscCommon.Add(item); break;
            case ItemTier.Uncommon: miscUncommon.Add(item); break;
            case ItemTier.Rare: miscRare.Add(item); break;
            case ItemTier.Epic: miscEpic.Add(item); break;
            case ItemTier.Legendary: miscLegendary.Add(item); break;
        }
    }

    private void SortTierListsBySimilarity()
    {
        // Sort each tier list by name similarity
        SortListBySimilarNames(weaponsCommon);
        SortListBySimilarNames(weaponsUncommon);
        SortListBySimilarNames(weaponsRare);
        SortListBySimilarNames(weaponsEpic);
        SortListBySimilarNames(weaponsLegendary);

        SortListBySimilarNames(headCommon);
        SortListBySimilarNames(headUncommon);
        SortListBySimilarNames(headRare);
        SortListBySimilarNames(headEpic);
        SortListBySimilarNames(headLegendary);

        SortListBySimilarNames(armorCommon);
        SortListBySimilarNames(armorUncommon);
        SortListBySimilarNames(armorRare);
        SortListBySimilarNames(armorEpic);
        SortListBySimilarNames(armorLegendary);

        SortListBySimilarNames(pantsCommon);
        SortListBySimilarNames(pantsUncommon);
        SortListBySimilarNames(pantsRare);
        SortListBySimilarNames(pantsEpic);
        SortListBySimilarNames(pantsLegendary);

        SortListBySimilarNames(shoesCommon);
        SortListBySimilarNames(shoesUncommon);
        SortListBySimilarNames(shoesRare);
        SortListBySimilarNames(shoesEpic);
        SortListBySimilarNames(shoesLegendary);

        SortListBySimilarNames(runesCommon);
        SortListBySimilarNames(runesUncommon);
        SortListBySimilarNames(runesRare);
        SortListBySimilarNames(runesEpic);
        SortListBySimilarNames(runesLegendary);

        SortListBySimilarNames(potionsCommon);
        SortListBySimilarNames(potionsUncommon);
        SortListBySimilarNames(potionsRare);
        SortListBySimilarNames(potionsEpic);
        SortListBySimilarNames(potionsLegendary);

        SortListBySimilarNames(materialsCommon);
        SortListBySimilarNames(materialsUncommon);
        SortListBySimilarNames(materialsRare);
        SortListBySimilarNames(materialsEpic);
        SortListBySimilarNames(materialsLegendary);

        SortListBySimilarNames(miscCommon);
        SortListBySimilarNames(miscUncommon);
        SortListBySimilarNames(miscRare);
        SortListBySimilarNames(miscEpic);
        SortListBySimilarNames(miscLegendary);
    }

    private void SortListBySimilarNames(List<ItemData> list)
    {
        if (list.Count <= 1) return;

        // Group by similar names and sort
        var groups = new List<List<ItemData>>();
        var remaining = new List<ItemData>(list);

        while (remaining.Count > 0)
        {
            var current = remaining[0];
            var similarItems = new List<ItemData> { current };
            remaining.RemoveAt(0);

            // Find items with similar names
            for (int i = remaining.Count - 1; i >= 0; i--)
            {
                if (AreNamesSimilar(current.ItemName, remaining[i].ItemName))
                {
                    similarItems.Add(remaining[i]);
                    remaining.RemoveAt(i);
                }
            }

            // Sort within group
            similarItems.Sort((a, b) => string.Compare(a.ItemName, b.ItemName, System.StringComparison.OrdinalIgnoreCase));
            groups.Add(similarItems);
        }

        // Sort groups by first item name
        groups.Sort((a, b) => string.Compare(a[0].ItemName, b[0].ItemName, System.StringComparison.OrdinalIgnoreCase));

        // Rebuild list
        list.Clear();
        foreach (var group in groups)
        {
            list.AddRange(group);
        }
    }

    private bool AreNamesSimilar(string name1, string name2)
    {
        if (string.IsNullOrEmpty(name1) || string.IsNullOrEmpty(name2))
            return false;

        // Convert to lowercase for comparison
        name1 = name1.ToLower();
        name2 = name2.ToLower();

        // Check if they share common words
        var words1 = name1.Split(' ', '_', '-').Where(w => w.Length > 2).ToArray();
        var words2 = name2.Split(' ', '_', '-').Where(w => w.Length > 2).ToArray();

        // If they share at least one significant word, consider them similar
        foreach (var word1 in words1)
        {
            foreach (var word2 in words2)
            {
                if (word1 == word2 ||
                    word1.Contains(word2) ||
                    word2.Contains(word1) ||
                    CalculateLevenshteinDistance(word1, word2) <= 2)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private int CalculateLevenshteinDistance(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1)) return s2?.Length ?? 0;
        if (string.IsNullOrEmpty(s2)) return s1.Length;

        int[,] matrix = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++)
            matrix[i, 0] = i;
        for (int j = 0; j <= s2.Length; j++)
            matrix[0, j] = j;

        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                matrix[i, j] = System.Math.Min(
                    System.Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[s1.Length, s2.Length];
    }
    #endregion

    #region Query Methods (เดิม - ไม่เปลี่ยนแปลง)
    public ItemData GetItemById(string itemId)
    {
        return allItems.FirstOrDefault(item => item.ItemId == itemId);
    }

    public ItemData GetItemByName(string itemName)
    {
        return allItems.FirstOrDefault(item => item.ItemName == itemName);
    }

    public List<ItemData> GetItemsByType(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Weapon: return new List<ItemData>(weapons);
            case ItemType.Head: return new List<ItemData>(headItems);
            case ItemType.Armor: return new List<ItemData>(armorItems);
            case ItemType.Pants: return new List<ItemData>(pantsItems);
            case ItemType.Shoes: return new List<ItemData>(shoesItems);
            case ItemType.Rune: return new List<ItemData>(runes);
            case ItemType.Potion: return new List<ItemData>(potions);
            case ItemType.Material: return new List<ItemData>(materials);
            case ItemType.Misc: return new List<ItemData>(miscItems);
            default: return new List<ItemData>();
        }
    }

    public List<ItemData> GetItemsByTier(ItemTier tier)
    {
        return allItems.Where(item => item.Tier == tier).ToList();
    }

    public List<ItemData> GetAllItems()
    {
        return new List<ItemData>(allItems);
    }

    public ItemData GetRandomItem()
    {
        if (allItems.Count == 0) return null;
        int randomIndex = Random.Range(0, allItems.Count);
        return allItems[randomIndex];
    }

    public List<ItemData> GetSellableItems()
    {
        return allItems.Where(item => item.IsSellable).ToList();
    }

    public List<ItemData> GetAllMaterials()
    {
        return allItems.Where(item => item.IsMaterial()).ToList();
    }

    public ItemData GetRandomItemByType(ItemType itemType)
    {
        var itemsOfType = GetItemsByType(itemType);
        if (itemsOfType.Count == 0) return null;
        int randomIndex = Random.Range(0, itemsOfType.Count);
        return itemsOfType[randomIndex];
    }

    public ItemData GetRandomItemByTier(ItemTier tier)
    {
        var itemsOfTier = GetItemsByTier(tier);
        if (itemsOfTier.Count == 0) return null;
        int randomIndex = Random.Range(0, itemsOfTier.Count);
        return itemsOfTier[randomIndex];
    }
    #endregion

    #region Management Methods (เดิม - ไม่เปลี่ยนแปลง)
    public void AddItem(ItemData item)
    {
        if (item == null) return;

        if (!allItems.Contains(item))
        {
            allItems.Add(item);
            RefreshItemLists();
            Debug.Log($"✅ Added item: {item.ItemName}");
        }
    }

    public void RemoveItem(ItemData item)
    {
        if (item == null) return;

        if (allItems.Contains(item))
        {
            allItems.Remove(item);
            RefreshItemLists();
            Debug.Log($"❌ Removed item: {item.ItemName}");
        }
    }

    public void RemoveItemById(string itemId)
    {
        var item = GetItemById(itemId);
        if (item != null)
        {
            RemoveItem(item);
        }
    }
    #endregion
}