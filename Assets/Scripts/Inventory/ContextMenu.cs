using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ContextMenu : MonoBehaviour
{
    [Header("Buttons")]
    public Button useButton;
    public Button dropButton;
    public Button splitButton; // "Grab 1"
    public Button favButton;

    private InventoryManager manager;
    private int currentSlotIndex;

    public void Setup(InventoryManager invManager)
    {
        manager = invManager;
        gameObject.SetActive(false);
    }

    public void OpenMenu(ItemData item, int slotIndex, Vector2 position, int quantity = 1)
    {
        currentSlotIndex = slotIndex;
        transform.position = position;

        // 1. Toggle Buttons
        useButton.gameObject.SetActive(item.canUse);
        dropButton.gameObject.SetActive(item.canDrop);

        // Only Tools can be favorited
        bool isTool = item.itemType == ItemData.ItemType.Tool;
        favButton.gameObject.SetActive(item.canFavorite && isTool);

        // Update Favorite Button Text to show current state
        if (favButton.gameObject.activeSelf)
        {
            UpdateFavoriteButtonText(slotIndex);
        }

        splitButton.gameObject.SetActive(item.isStackable && quantity > 1);

        gameObject.SetActive(true);

        // 2. Setup Listeners
        useButton.onClick.RemoveAllListeners();
        useButton.onClick.AddListener(() =>
        {
            manager.OnUseItem(currentSlotIndex);
            manager.CloseContextMenu();
        });

        dropButton.onClick.RemoveAllListeners();
        dropButton.onClick.AddListener(() =>
        {
            manager.OnDropItem(currentSlotIndex);
            manager.CloseContextMenu();
        });

        splitButton.onClick.RemoveAllListeners();
        splitButton.onClick.AddListener(() =>
        {
            manager.OnSplitItem(currentSlotIndex);
            manager.CloseContextMenu();
        });

        favButton.onClick.RemoveAllListeners();
        favButton.onClick.AddListener(() =>
        {
            // Call logic but DO NOT close menu, so player can see the change (Fav 1 -> Fav 2...)
            manager.OnFavoriteItem(currentSlotIndex);
            UpdateFavoriteButtonText(currentSlotIndex);
        });

        // 3. Auto Select
        if (useButton.gameObject.activeSelf) useButton.Select();
        else if (dropButton.gameObject.activeSelf) dropButton.Select();
        else if (splitButton.gameObject.activeSelf) splitButton.Select();
    }

    private void UpdateFavoriteButtonText(int slotIndex)
    {
        int currentFav = manager.GetFavoriteIndex(slotIndex);
        TextMeshProUGUI btnText = favButton.GetComponentInChildren<TextMeshProUGUI>();

        if (btnText != null)
        {
            if (currentFav == -1) btnText.text = "Favorite";
            else btnText.text = $"Fav: {currentFav + 1}"; // Display 1-4
        }
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}