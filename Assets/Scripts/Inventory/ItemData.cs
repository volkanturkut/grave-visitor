using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    [Header("Info")]
    public string itemName;
    public Sprite icon;
    public bool isStackable;
    public int maxStack = 1;

    [Header("Types")]
    public ItemType itemType;
    public enum ItemType { Consumable, Tool, Material }

    [Header("Prefabs")]
    public GameObject prefab;          // World (Drop)
    public GameObject equippedPrefab;  // Hand (Equip)

    [Header("Equip Settings")] // <--- NEW SECTION
    public Vector3 gripPosition;
    public Vector3 gripRotation;

    [Header("Action Settings")]
    public bool canUse;
    public bool canDrop;
    public bool canFavorite;
}