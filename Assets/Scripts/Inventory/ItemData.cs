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

    // YENİ: El Tipi Seçimi
    public enum EquipHand { Right, Left }

    [Header("Prefabs")]
    public GameObject prefab;          // World (Drop)
    public GameObject equippedPrefab;  // Hand (Equip)

    [Header("Equip Settings")]
    public EquipHand handType = EquipHand.Right; // Varsayılan Sağ El
    public Vector3 gripPosition;
    public Vector3 gripRotation;

    [Tooltip("Animator'da tetiklenecek bool parametre adı (Örn: IsHoldingLantern)")]
    public string holdAnimBool;

    [Header("Action Settings")]
    public bool canUse;
    public bool canDrop;
    public bool canFavorite;
}