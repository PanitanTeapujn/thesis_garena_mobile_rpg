using Fusion;

[System.Serializable]
public struct NetworkInventoryItem : INetworkStruct
{
    public NetworkString<_64> itemId;
    public int quantity;

    public NetworkInventoryItem(string id, int qty)
    {
        itemId = id ?? "";
        quantity = qty;
    }
}

[System.Serializable]
public struct NetworkEquippedItem : INetworkStruct
{
    public NetworkString<_64> itemId;
    public EquipmentType equipmentType;

    public NetworkEquippedItem(string id, EquipmentType type)
    {
        itemId = id ?? "";
        equipmentType = type;
    }
}
