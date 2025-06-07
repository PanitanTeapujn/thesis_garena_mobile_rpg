using UnityEngine;
using Fusion;

public class CharacterEquipmentSystem : NetworkBehaviour
{
    [Header("Equipment Visual Slots")]
    public Transform weaponSlot;
    public Transform helmetSlot;
    public Transform armorSlot;
    public Transform pantsSlot;
    public Transform bootsSlot;

    [Header("Equipment Prefabs")]
    public GameObject[] weaponPrefabs;
    public GameObject[] helmetPrefabs;
    public GameObject[] armorPrefabs;
    public GameObject[] pantsPrefabs;
    public GameObject[] bootsPrefabs;

    private InventoryManager inventoryManager;
    private Character character;
    private ItemDatabase itemDatabase;

    // Currently equipped visual objects
    private GameObject currentWeapon;
    private GameObject currentHelmet;
    private GameObject currentArmor;
    private GameObject currentPants;
    private GameObject currentBoots;

    public override void Spawned()
    {
        inventoryManager = GetComponent<InventoryManager>();
        character = GetComponent<Character>();

        if (inventoryManager != null)
        {
            itemDatabase = inventoryManager.itemDatabase;

            // Subscribe to equipment changes
            inventoryManager.OnItemEquipped += OnItemEquipped;
            inventoryManager.OnItemUnequipped += OnItemUnequipped;

            // Apply initial equipment stats
            if (HasInputAuthority)
            {
                ApplyEquipmentStats();
            }
        }
    }

    private void OnItemEquipped(EquippedItem equippedItem)
    {
        UpdateEquipmentVisual(equippedItem.equipmentType, equippedItem.itemId);

        if (HasInputAuthority)
        {
            ApplyEquipmentStats();
        }
    }

    private void OnItemUnequipped(EquipmentType equipmentType)
    {
        UpdateEquipmentVisual(equipmentType, null);

        if (HasInputAuthority)
        {
            ApplyEquipmentStats();
        }
    }

    private void UpdateEquipmentVisual(EquipmentType equipmentType, string itemId)
    {
        GameObject prefabToSpawn = null;
        Transform targetSlot = null;
        GameObject currentEquipped = null;

        // Get the appropriate prefab and slot
        if (!string.IsNullOrEmpty(itemId))
        {
            ItemData itemData = itemDatabase.GetItem(itemId);
            if (itemData != null)
            {
                prefabToSpawn = GetEquipmentPrefab(equipmentType, itemData);
            }
        }

        // Get target slot and current equipped item
        switch (equipmentType)
        {
            case EquipmentType.Weapon:
                targetSlot = weaponSlot;
                currentEquipped = currentWeapon;
                break;
            case EquipmentType.Helmet:
                targetSlot = helmetSlot;
                currentEquipped = currentHelmet;
                break;
            case EquipmentType.Armor:
                targetSlot = armorSlot;
                currentEquipped = currentArmor;
                break;
            case EquipmentType.Pants:
                targetSlot = pantsSlot;
                currentEquipped = currentPants;
                break;
            case EquipmentType.Boots:
                targetSlot = bootsSlot;
                currentEquipped = currentBoots;
                break;
        }

        // Remove current equipped visual
        if (currentEquipped != null)
        {
            Destroy(currentEquipped);
        }

        // Spawn new equipment visual
        if (prefabToSpawn != null && targetSlot != null)
        {
            GameObject newEquipment = Instantiate(prefabToSpawn, targetSlot);
            newEquipment.transform.localPosition = Vector3.zero;
            newEquipment.transform.localRotation = Quaternion.identity;

            // Store reference
            switch (equipmentType)
            {
                case EquipmentType.Weapon:
                    currentWeapon = newEquipment;
                    break;
                case EquipmentType.Helmet:
                    currentHelmet = newEquipment;
                    break;
                case EquipmentType.Armor:
                    currentArmor = newEquipment;
                    break;
                case EquipmentType.Pants:
                    currentPants = newEquipment;
                    break;
                case EquipmentType.Boots:
                    currentBoots = newEquipment;
                    break;
            }
        }
        else
        {
            // Clear reference
            switch (equipmentType)
            {
                case EquipmentType.Weapon:
                    currentWeapon = null;
                    break;
                case EquipmentType.Helmet:
                    currentHelmet = null;
                    break;
                case EquipmentType.Armor:
                    currentArmor = null;
                    break;
                case EquipmentType.Pants:
                    currentPants = null;
                    break;
                case EquipmentType.Boots:
                    currentBoots = null;
                    break;
            }
        }
    }

    private GameObject GetEquipmentPrefab(EquipmentType equipmentType, ItemData itemData)
    {
        // This is a simple implementation - you might want to use the itemData.itemId 
        // to determine which specific prefab to use

        GameObject[] prefabArray = null;

        switch (equipmentType)
        {
            case EquipmentType.Weapon:
                prefabArray = weaponPrefabs;
                break;
            case EquipmentType.Helmet:
                prefabArray = helmetPrefabs;
                break;
            case EquipmentType.Armor:
                prefabArray = armorPrefabs;
                break;
            case EquipmentType.Pants:
                prefabArray = pantsPrefabs;
                break;
            case EquipmentType.Boots:
                prefabArray = bootsPrefabs;
                break;
        }

        if (prefabArray != null && prefabArray.Length > 0)
        {
            // Simple implementation: use hash of item name to select prefab
            int index = Mathf.Abs(itemData.itemId.GetHashCode()) % prefabArray.Length;
            return prefabArray[index];
        }

        return null;
    }

    private void ApplyEquipmentStats()
    {
        if (character == null || inventoryManager == null) return;

        // Get total equipment stats
        ItemStats totalStats = inventoryManager.GetTotalEquipmentStats();

        // Apply stats to character (add to base stats)
        int baseAttackDamage = character.characterStats.attackDamage;
        int baseArmor = character.characterStats.arrmor;
        int baseMaxHp = character.characterStats.maxHp;
        int baseMaxMana = character.characterStats.maxMana;
        float baseMoveSpeed = character.characterStats.moveSpeed;

        // Update character stats with equipment bonuses
        character.AttackDamage = baseAttackDamage + totalStats.attackDamage;
        character.Armor = baseArmor + totalStats.armor;
        character.MaxHp = baseMaxHp + totalStats.maxHp;
        character.MaxMana = baseMaxMana + totalStats.maxMana;
        character.MoveSpeed = baseMoveSpeed + totalStats.moveSpeed;

        // If current HP/Mana is higher than new max, keep it (don't reduce)
        // If it's lower, the new max allows for more
        character.CurrentHp = Mathf.Min(character.CurrentHp, character.MaxHp);
        character.CurrentMana = Mathf.Min(character.CurrentMana, character.MaxMana);

        Debug.Log($"Equipment Stats Applied - ATK: {character.AttackDamage}, DEF: {character.Armor}, HP: {character.MaxHp}, MP: {character.MaxMana}, SPD: {character.MoveSpeed}");

        // Save updated stats to Firebase
        if (inventoryManager.TryGetComponent<FirebaseInventorySync>(out var firebaseSync))
        {
            firebaseSync.SavePlayerStats(totalStats);
        }
    }

    // Public method to refresh equipment visuals (useful for initialization)
    public void RefreshAllEquipmentVisuals()
    {
        if (inventoryManager == null) return;

        // Update all equipment slots
        for (int i = 0; i < 5; i++)
        {
            EquipmentType equipType = (EquipmentType)i;
            var equippedItem = inventoryManager.GetEquippedItem(equipType);

            if (equippedItem != null)
            {
                UpdateEquipmentVisual(equipType, equippedItem.itemId);
            }
            else
            {
                UpdateEquipmentVisual(equipType, null);
            }
        }
    }

    // Get current weapon for combat system
    public GameObject GetCurrentWeapon()
    {
        return currentWeapon;
    }

    // Check if specific equipment is equipped
    public bool HasEquipmentType(EquipmentType equipmentType)
    {
        if (inventoryManager == null) return false;

        var equippedItem = inventoryManager.GetEquippedItem(equipmentType);
        return equippedItem != null;
    }

    // Get equipped item data
    public ItemData GetEquippedItemData(EquipmentType equipmentType)
    {
        if (inventoryManager == null) return null;

        var equippedItem = inventoryManager.GetEquippedItem(equipmentType);
        if (equippedItem != null)
        {
            return itemDatabase.GetItem(equippedItem.itemId);
        }

        return null;
    }

    void OnDestroy()
    {
        if (inventoryManager != null)
        {
            inventoryManager.OnItemEquipped -= OnItemEquipped;
            inventoryManager.OnItemUnequipped -= OnItemUnequipped;
        }
    }
}
