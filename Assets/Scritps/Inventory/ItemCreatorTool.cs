#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class ItemCreatorTool : EditorWindow
{
    private Vector2 scrollPosition;
    private bool showWeapons = true;
    private bool showArmor = true;
    private bool showConsumables = true;
    private bool showDatabase = true;

    [MenuItem("Tools/Item Creator")]
    public static void ShowWindow()
    {
        ItemCreatorTool window = GetWindow<ItemCreatorTool>("Item Creator");
        window.minSize = new Vector2(400, 600);
    }

    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("Item Creator Tool", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Create folders section
        EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);
        if (GUILayout.Button("Create Item Folders"))
        {
            CreateItemFolders();
        }
        GUILayout.Space(10);

        // Weapons section
        showWeapons = EditorGUILayout.Foldout(showWeapons, "Weapons");
        if (showWeapons)
        {
            EditorGUI.indentLevel++;
            if (GUILayout.Button("Create All Sample Weapons"))
            {
                CreateSampleWeapons();
            }
            if (GUILayout.Button("Create Iron Sword"))
            {
                CreateIronSword();
            }
            if (GUILayout.Button("Create Steel Sword"))
            {
                CreateSteelSword();
            }
            if (GUILayout.Button("Create Flame Blade"))
            {
                CreateFlameBlade();
            }
            if (GUILayout.Button("Create Dragon Slayer"))
            {
                CreateDragonSlayer();
            }
            if (GUILayout.Button("Create Excalibur"))
            {
                CreateExcalibur();
            }
            EditorGUI.indentLevel--;
        }
        GUILayout.Space(5);

        // Armor section
        showArmor = EditorGUILayout.Foldout(showArmor, "Armor & Equipment");
        if (showArmor)
        {
            EditorGUI.indentLevel++;
            if (GUILayout.Button("Create All Sample Armor"))
            {
                CreateSampleArmor();
            }
            if (GUILayout.Button("Create Helmets"))
            {
                CreateHelmets();
            }
            if (GUILayout.Button("Create Armor"))
            {
                CreateChestArmor();
            }
            if (GUILayout.Button("Create Pants"))
            {
                CreatePants();
            }
            if (GUILayout.Button("Create Boots"))
            {
                CreateBoots();
            }
            EditorGUI.indentLevel--;
        }
        GUILayout.Space(5);

        // Consumables section
        showConsumables = EditorGUILayout.Foldout(showConsumables, "Consumables");
        if (showConsumables)
        {
            EditorGUI.indentLevel++;
            if (GUILayout.Button("Create All Sample Consumables"))
            {
                CreateSampleConsumables();
            }
            if (GUILayout.Button("Create Health Potions"))
            {
                CreateHealthPotions();
            }
            if (GUILayout.Button("Create Mana Potions"))
            {
                CreateManaPotions();
            }
            if (GUILayout.Button("Create Special Potions"))
            {
                CreateSpecialPotions();
            }
            EditorGUI.indentLevel--;
        }
        GUILayout.Space(5);

        // Database section
        showDatabase = EditorGUILayout.Foldout(showDatabase, "Database");
        if (showDatabase)
        {
            EditorGUI.indentLevel++;
            if (GUILayout.Button("Create Item Database"))
            {
                CreateItemDatabase();
            }
            if (GUILayout.Button("Refresh Database"))
            {
                RefreshItemDatabase();
            }
            if (GUILayout.Button("Validate All Items"))
            {
                ValidateAllItems();
            }
            EditorGUI.indentLevel--;
        }
        GUILayout.Space(10);

        // Utility section
        EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
        if (GUILayout.Button("Create Everything"))
        {
            CreateEverything();
        }
        if (GUILayout.Button("Clear All Items"))
        {
            ClearAllItems();
        }

        EditorGUILayout.EndScrollView();
    }

    void CreateItemFolders()
    {
        string[] folders = {
            "Assets/Items",
            "Assets/Items/Weapons",
            "Assets/Items/Helmets",
            "Assets/Items/Armors",
            "Assets/Items/Pants",
            "Assets/Items/Boots",
            "Assets/Items/Consumables",
            "Assets/Items/Materials"
        };

        foreach (string folder in folders)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
                Debug.Log($"Created folder: {folder}");
            }
        }
        AssetDatabase.Refresh();
    }

    void CreateSampleWeapons()
    {
        CreateIronSword();
        CreateSteelSword();
        CreateFlameBlade();
        CreateDragonSlayer();
        CreateExcalibur();
        Debug.Log("All sample weapons created!");
    }

    void CreateIronSword()
    {
        CreateWeapon("iron_sword", "Iron Sword", "A basic iron sword for beginners",
            ItemRarity.Common, 15, 0, 0, 0, 0, 0, 0, 0, 100, 50);
    }

    void CreateSteelSword()
    {
        CreateWeapon("steel_sword", "Steel Sword", "A sturdy steel sword with improved damage",
            ItemRarity.Uncommon, 25, 0, 0, 0, 0, 0.1f, 0, 0, 200, 100);
    }

    void CreateFlameBlade()
    {
        CreateWeapon("flame_blade", "Flame Blade", "A sword imbued with fire magic that burns enemies",
            ItemRarity.Rare, 35, 0, 0, 0, 0, 0.15f, 5, 10, 500, 250);
    }

    void CreateDragonSlayer()
    {
        CreateWeapon("dragon_slayer", "Dragon Slayer", "Legendary sword forged to slay dragons",
            ItemRarity.Epic, 50, 0, 0, 0, 0.2f, 0.2f, 10, 25, 1000, 500);
    }

    void CreateExcalibur()
    {
        CreateWeapon("excalibur", "Excalibur", "The ultimate holy sword wielded by legendary heroes",
            ItemRarity.Legendary, 75, 0, 50, 25, 0.3f, 0.3f, 20, 50, 2000, 1000);
    }

    void CreateSampleArmor()
    {
        CreateHelmets();
        CreateChestArmor();
        CreatePants();
        CreateBoots();
        Debug.Log("All sample armor created!");
    }

    void CreateHelmets()
    {
        CreateArmor("leather_helmet", "Leather Helmet", "Basic leather head protection",
            EquipmentType.Helmet, ItemRarity.Common, 0, 5, 10, 0, 0, 0, 0, 0, 50, 25);

        CreateArmor("iron_helmet", "Iron Helmet", "Sturdy iron helmet with good protection",
            EquipmentType.Helmet, ItemRarity.Uncommon, 0, 10, 25, 0, 0, 0, 0, 0, 100, 50);

        CreateArmor("steel_helmet", "Steel Helmet", "Heavy steel helmet for warriors",
            EquipmentType.Helmet, ItemRarity.Rare, 0, 18, 40, 0, -0.05f, 0, 0, 0, 200, 100);

        CreateArmor("dragon_helmet", "Dragon Helmet", "Helmet forged from dragon scales",
            EquipmentType.Helmet, ItemRarity.Epic, 0, 25, 75, 25, 0, 0, 0, 0, 500, 250);
    }

    void CreateChestArmor()
    {
        CreateArmor("leather_armor", "Leather Armor", "Basic leather chest protection",
            EquipmentType.Armor, ItemRarity.Common, 0, 8, 15, 0, 0, 0, 0, 0, 75, 35);

        CreateArmor("chainmail", "Chainmail", "Flexible chainmail armor",
            EquipmentType.Armor, ItemRarity.Uncommon, 0, 15, 30, 0, 0, 0, 0, 0, 150, 75);

        CreateArmor("plate_armor", "Plate Armor", "Heavy plate armor for maximum protection",
            EquipmentType.Armor, ItemRarity.Rare, 0, 25, 50, 0, -0.1f, 0, 0, 0, 300, 150);

        CreateArmor("dragon_armor", "Dragon Scale Armor", "Armor made from ancient dragon scales",
            EquipmentType.Armor, ItemRarity.Epic, 0, 40, 100, 50, 0, 0, 5, 0, 800, 400);
    }

    void CreatePants()
    {
        CreateArmor("leather_pants", "Leather Pants", "Basic leather leg protection",
            EquipmentType.Pants, ItemRarity.Common, 0, 3, 8, 0, 0, 0, 0, 0, 40, 20);

        CreateArmor("iron_greaves", "Iron Greaves", "Iron leg armor for better protection",
            EquipmentType.Pants, ItemRarity.Uncommon, 0, 8, 20, 0, 0, 0, 0, 0, 80, 40);

        CreateArmor("steel_leggings", "Steel Leggings", "Heavy steel leg protection",
            EquipmentType.Pants, ItemRarity.Rare, 0, 15, 35, 0, -0.05f, 0, 0, 0, 160, 80);
    }

    void CreateBoots()
    {
        CreateArmor("leather_boots", "Leather Boots", "Basic leather footwear",
            EquipmentType.Boots, ItemRarity.Common, 0, 2, 5, 0, 0.1f, 0, 0, 0, 30, 15);

        CreateArmor("iron_boots", "Iron Boots", "Heavy iron boots",
            EquipmentType.Boots, ItemRarity.Uncommon, 0, 5, 12, 0, 0, 0, 0, 0, 60, 30);

        CreateArmor("speed_boots", "Boots of Speed", "Magical boots that enhance movement",
            EquipmentType.Boots, ItemRarity.Rare, 0, 5, 10, 0, 0.5f, 0, 0, 0, 200, 100);

        CreateArmor("shadow_boots", "Shadow Boots", "Mysterious boots that grant stealth",
            EquipmentType.Boots, ItemRarity.Epic, 0, 8, 20, 0, 0.3f, 0, 0, 0, 400, 200);
    }

    void CreateSampleConsumables()
    {
        CreateHealthPotions();
        CreateManaPotions();
        CreateSpecialPotions();
        Debug.Log("All sample consumables created!");
    }

    void CreateHealthPotions()
    {
        CreateConsumable("health_potion_small", "Small Health Potion", "Restores 50 HP",
            ItemRarity.Common, 50, 0, 0, 20, 10);

        CreateConsumable("health_potion_medium", "Health Potion", "Restores 100 HP",
            ItemRarity.Uncommon, 100, 0, 0, 50, 25);

        CreateConsumable("health_potion_large", "Large Health Potion", "Restores 200 HP",
            ItemRarity.Rare, 200, 0, 0, 100, 50);

        CreateConsumable("health_potion_super", "Super Health Potion", "Restores 500 HP",
            ItemRarity.Epic, 500, 0, 0, 250, 125);
    }

    void CreateManaPotions()
    {
        CreateConsumable("mana_potion_small", "Small Mana Potion", "Restores 30 MP",
            ItemRarity.Common, 0, 30, 0, 25, 12);

        CreateConsumable("mana_potion_medium", "Mana Potion", "Restores 60 MP",
            ItemRarity.Uncommon, 0, 60, 0, 60, 30);

        CreateConsumable("mana_potion_large", "Large Mana Potion", "Restores 120 MP",
            ItemRarity.Rare, 0, 120, 0, 120, 60);

        CreateConsumable("mana_potion_super", "Super Mana Potion", "Restores 300 MP",
            ItemRarity.Epic, 0, 300, 0, 300, 150);
    }

    void CreateSpecialPotions()
    {
        CreateConsumable("full_restore", "Full Restore Potion", "Fully restores HP and MP",
            ItemRarity.Epic, 9999, 9999, 0, 500, 250);

        CreateConsumable("strength_potion", "Strength Potion", "Temporarily increases attack power",
            ItemRarity.Rare, 0, 0, 60, 150, 75);

        CreateConsumable("speed_potion", "Speed Potion", "Temporarily increases movement speed",
            ItemRarity.Rare, 0, 0, 60, 150, 75);

        CreateConsumable("elixir_of_life", "Elixir of Life", "Ultimate healing potion",
            ItemRarity.Legendary, 9999, 9999, 0, 1000, 500);
    }

    void CreateWeapon(string id, string itemName, string description, ItemRarity rarity,
        int attackDamage, int armor, int maxHp, int maxMana, float moveSpeed,
        float attackSpeed, float critChance, float critDamage, int buyPrice, int sellPrice)
    {
        ItemData item = CreateInstance<ItemData>();
        item.itemId = id;
        item.itemName = itemName;
        item.description = description;
        item.itemType = ItemType.Equipment;
        item.equipmentType = EquipmentType.Weapon;
        item.rarity = rarity;
        item.isStackable = false;
        item.maxStackSize = 1;

        item.stats = new ItemStats
        {
            attackDamage = attackDamage,
            armor = armor,
            maxHp = maxHp,
            maxMana = maxMana,
            moveSpeed = moveSpeed,
            attackSpeed = attackSpeed,
            criticalChance = critChance,
            criticalDamage = critDamage
        };

        item.buyPrice = buyPrice;
        item.sellPrice = sellPrice;

        string path = $"Assets/Items/Weapons/{itemName}.asset";
        CreateDirectoryIfNotExists(path);
        AssetDatabase.CreateAsset(item, path);
        Debug.Log($"Created weapon: {itemName}");
    }

    void CreateArmor(string id, string itemName, string description, EquipmentType equipType,
        ItemRarity rarity, int attackDamage, int armor, int maxHp, int maxMana,
        float moveSpeed, float attackSpeed, float critChance, float critDamage,
        int buyPrice, int sellPrice)
    {
        ItemData item = CreateInstance<ItemData>();
        item.itemId = id;
        item.itemName = itemName;
        item.description = description;
        item.itemType = ItemType.Equipment;
        item.equipmentType = equipType;
        item.rarity = rarity;
        item.isStackable = false;
        item.maxStackSize = 1;

        item.stats = new ItemStats
        {
            attackDamage = attackDamage,
            armor = armor,
            maxHp = maxHp,
            maxMana = maxMana,
            moveSpeed = moveSpeed,
            attackSpeed = attackSpeed,
            criticalChance = critChance,
            criticalDamage = critDamage
        };

        item.buyPrice = buyPrice;
        item.sellPrice = sellPrice;

        string folderName = equipType.ToString() + "s";
        string path = $"Assets/Items/{folderName}/{itemName}.asset";
        CreateDirectoryIfNotExists(path);
        AssetDatabase.CreateAsset(item, path);
        Debug.Log($"Created {equipType}: {itemName}");
    }

    void CreateConsumable(string id, string itemName, string description, ItemRarity rarity,
        int healAmount, int manaAmount, float buffDuration, int buyPrice, int sellPrice)
    {
        ItemData item = CreateInstance<ItemData>();
        item.itemId = id;
        item.itemName = itemName;
        item.description = description;
        item.itemType = ItemType.Consumable;
        item.rarity = rarity;
        item.isStackable = true;
        item.maxStackSize = 99;

        item.healAmount = healAmount;
        item.manaAmount = manaAmount;
        item.buffDuration = buffDuration;
        item.buyPrice = buyPrice;
        item.sellPrice = sellPrice;

        string path = $"Assets/Items/Consumables/{itemName}.asset";
        CreateDirectoryIfNotExists(path);
        AssetDatabase.CreateAsset(item, path);
        Debug.Log($"Created consumable: {itemName}");
    }

    void CreateDirectoryIfNotExists(string filePath)
    {
        string directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    void CreateItemDatabase()
    {
        ItemDatabase database = CreateInstance<ItemDatabase>();

        // Load all items from Assets/Items folder
        string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/Items" });
        ItemData[] items = new ItemData[guids.Length];

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            items[i] = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        }

        database.allItems = items;

        string dbPath = "Assets/Items/ItemDatabase.asset";
        AssetDatabase.CreateAsset(database, dbPath);

        Debug.Log($"Created ItemDatabase with {items.Length} items at {dbPath}");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    void RefreshItemDatabase()
    {
        string dbPath = "Assets/Items/ItemDatabase.asset";
        ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(dbPath);

        if (database == null)
        {
            Debug.LogWarning("ItemDatabase not found! Creating new one...");
            CreateItemDatabase();
            return;
        }

        // Refresh items
        string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/Items" });
        ItemData[] items = new ItemData[guids.Length];

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            items[i] = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        }

        database.allItems = items;
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();

        Debug.Log($"Refreshed ItemDatabase with {items.Length} items");
    }

    void ValidateAllItems()
    {
        string dbPath = "Assets/Items/ItemDatabase.asset";
        ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(dbPath);

        if (database != null)
        {
            database.ValidateDatabase();
        }
        else
        {
            Debug.LogError("ItemDatabase not found! Please create it first.");
        }
    }

    void CreateEverything()
    {
        CreateItemFolders();
        CreateSampleWeapons();
        CreateSampleArmor();
        CreateSampleConsumables();
        CreateItemDatabase();

        Debug.Log("=== ALL ITEMS CREATED SUCCESSFULLY! ===");
        Debug.Log("You can now find all items in Assets/Items/ folder");
        Debug.Log("ItemDatabase is available at Assets/Items/ItemDatabase.asset");
    }

    void ClearAllItems()
    {
        if (EditorUtility.DisplayDialog("Clear All Items",
            "Are you sure you want to delete all items? This cannot be undone!",
            "Yes, Delete All", "Cancel"))
        {
            if (Directory.Exists("Assets/Items"))
            {
                Directory.Delete("Assets/Items", true);
                AssetDatabase.Refresh();
                Debug.Log("All items cleared!");
            }
        }
    }
}
#endif