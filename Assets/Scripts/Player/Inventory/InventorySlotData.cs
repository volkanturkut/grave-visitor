using UnityEngine;

[System.Serializable]
public class InventorySlotData
{
    public ItemData itemData;
    public int quantity;

    public bool IsEmpty => itemData == null;

    public InventorySlotData()
    {
        itemData = null;
        quantity = 0;
    }

    public InventorySlotData(ItemData item, int qty)
    {
        itemData = item;
        quantity = qty;
    }
}
