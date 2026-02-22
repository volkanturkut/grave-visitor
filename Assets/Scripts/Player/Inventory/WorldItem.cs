using UnityEngine;
using TMPro;

public class WorldItem : MonoBehaviour
{
    public ItemData itemData;
    public int quantity = 1;

    [Header("UI Feedback")]
    public GameObject nameTagUI; // Assign a Canvas/Text prefab above the item
    public TextMeshProUGUI nameText;

    private void Start()
    {
        if (nameTagUI) nameTagUI.SetActive(false);
        if (nameText && itemData) nameText.text = itemData.itemName;
    }

    public void ShowInfo(bool show)
    {
        if (nameTagUI) nameTagUI.SetActive(show);
    }
}