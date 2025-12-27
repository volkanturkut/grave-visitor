using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IDropHandler, IEndDragHandler
{
    [Header("UI References")]
    public Image iconImage;
    public TextMeshProUGUI qtyText;

    private int slotIndex;
    private InventoryManager manager;
    private ItemData currentItem;

    private Transform originalParent;
    private Transform originalTextParent;
    private Canvas parentCanvas;
    private RectTransform inventoryPanelRect;
    private CanvasGroup iconCanvasGroup;

    public void Setup(int index, InventoryManager invManager)
    {
        slotIndex = index;
        manager = invManager;

        if (manager && manager.inventoryPanel)
        {
            manager.inventoryPanel.TryGetComponent(out inventoryPanelRect);
        }
    }

    public void UpdateSlot(InventoryManager.InventorySlotData slotData)
    {
        if (slotData != null && slotData.itemData != null)
        {
            currentItem = slotData.itemData;
            iconImage.sprite = currentItem.icon;
            iconImage.enabled = true;

            if (slotData.quantity > 1)
            {
                qtyText.text = slotData.quantity.ToString();
                qtyText.enabled = true;
            }
            else
            {
                qtyText.enabled = false;
            }
        }
        else
        {
            ClearSlot();
        }
    }

    public void ClearSlot()
    {
        currentItem = null;
        iconImage.enabled = false;
        qtyText.enabled = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Cursor.visible) EventSystem.current.SetSelectedGameObject(null);
        ShowNameTooltip();
    }
    public void OnPointerExit(PointerEventData eventData) => manager.HideTooltip();

    public void OnSelect(BaseEventData eventData)
    {
        if (manager.contextMenuPanel.activeSelf)
        {
            manager.CloseContextMenu(false);
        }
        ShowNameTooltip();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && currentItem != null)
        {
            Vector2 menuPos = (Vector2)transform.position + manager.contextMenuOffset;
            manager.OpenContextMenu(slotIndex, menuPos);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem == null) return;

        manager.CloseContextMenu();

        originalParent = iconImage.transform.parent;

        if (qtyText)
        {
            originalTextParent = qtyText.transform.parent;
            qtyText.transform.SetParent(iconImage.transform);
        }

        if (!parentCanvas) parentCanvas = GetComponentInParent<Canvas>();

        iconImage.transform.SetParent(manager.inventoryPanel.transform);

        if (!iconCanvasGroup)
        {
            if (!iconImage.TryGetComponent(out iconCanvasGroup))
            {
                iconCanvasGroup = iconImage.gameObject.AddComponent<CanvasGroup>();
            }
        }

        iconCanvasGroup.blocksRaycasts = false;
        iconImage.raycastTarget = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (currentItem == null) return;

        if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            iconImage.transform.position = Input.mousePosition;
        }
        else
        {
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                inventoryPanelRect,
                eventData.position,
                parentCanvas.worldCamera,
                out Vector3 mousePos
            );
            iconImage.transform.position = mousePos;

            Vector3 localPos = iconImage.transform.localPosition;
            localPos.z = 0;
            iconImage.transform.localPosition = localPos;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (currentItem == null) return;

        if (qtyText && originalTextParent)
        {
            qtyText.transform.SetParent(originalTextParent);
        }

        iconImage.transform.SetParent(originalParent);
        iconImage.rectTransform.anchoredPosition = Vector2.zero;

        if (iconCanvasGroup)
        {
            iconCanvasGroup.blocksRaycasts = true;
        }
        iconImage.raycastTarget = false;

        manager.RefreshUI();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag && eventData.pointerDrag.TryGetComponent(out InventorySlotUI sourceSlot))
        {
            manager.SwapItems(sourceSlot.slotIndex, slotIndex);
            EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }

    private void ShowNameTooltip()
    {
        if (currentItem != null) manager.ShowTooltip(currentItem.itemName, transform.position);
        else manager.HideTooltip();
    }
}