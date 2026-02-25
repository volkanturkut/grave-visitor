using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GraveVisitor.Inventory
{
    /// <summary>
    /// View component of the Inventory MVC system.
    /// Handles UI rendering, slot instantiation, and visual feedback.
    /// </summary>
    public class InventoryView : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject inventoryPanel;
        public Transform gridContainer;
        public GameObject slotPrefab;
        public GameObject tooltipPanel;
        public TextMeshProUGUI tooltipText;
        public GameObject contextMenuPanel;

        [Header("Configuration")]
        public Vector2 tooltipOffset = new Vector2(50, 50);

        // Internal UI mapping
        private List<InventorySlotUI> _uiSlots = new List<InventorySlotUI>();

        // Callbacks
        public System.Action<int> OnSlotSelected;
        public System.Action<int> OnSlotClicked;

        private void Awake()
        {
            if (inventoryPanel) inventoryPanel.SetActive(false);
            if (tooltipPanel) tooltipPanel.SetActive(false);
            if (contextMenuPanel) contextMenuPanel.SetActive(false);
        }

        /// <summary>
        /// Initializes the UI grid based on the maximum number of slots.
        /// </summary>
        public void InitializeGrid(int maxSlots, InventoryManager managerInstance) // Temporary dependency on Manager for UI script setup
        {
            // Clear existing
            foreach (Transform child in gridContainer) Destroy(child.gameObject);
            _uiSlots.Clear();

            for (int i = 0; i < maxSlots; i++)
            {
                GameObject newSlot = Instantiate(slotPrefab, gridContainer);
                if (newSlot.TryGetComponent(out InventorySlotUI uiScript))
                {
                    // For now, we reuse the existing InventorySlotUI which expects the Manager
                    // In a full refactor, InventorySlotUI should be decoupled too
                    uiScript.Setup(i, managerInstance);
                    _uiSlots.Add(uiScript);
                }
            }
        }

        /// <summary>
        /// Updates the entire UI based on the provided model data.
        /// </summary>
        public void RefreshDisplay(List<InventorySlotData> slots)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (i < _uiSlots.Count)
                {
                    _uiSlots[i].UpdateSlot(slots[i]);
                }
            }
        }

        /// <summary>
        /// Shows or hides the main inventory panel.
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (inventoryPanel) inventoryPanel.SetActive(visible);
            if (!visible)
            {
                HideTooltip();
                // Close context menu logic here
            }
        }

        private RectTransform _tooltipRect;

        public void ShowTooltip(string text, Vector2 position)
        {
            if (tooltipPanel && tooltipText)
            {
                if (_tooltipRect == null) tooltipPanel.TryGetComponent(out _tooltipRect);

                if (tooltipText.text != text || !tooltipPanel.activeSelf)
                {
                    tooltipPanel.SetActive(true);
                    tooltipText.text = text;
                    if (_tooltipRect) LayoutRebuilder.ForceRebuildLayoutImmediate(_tooltipRect);
                }

                tooltipPanel.transform.rotation = Quaternion.Euler(-180, 0, 0);
                tooltipPanel.transform.position = position;
                tooltipPanel.transform.Translate(tooltipOffset.x, tooltipOffset.y, 0, Space.Self);
            }
        }

        public void HideTooltip()
        {
            if (tooltipPanel) tooltipPanel.SetActive(false);
        }

        /// <summary>
        /// Highlights a specific slot (e.g., for drag and drop).
        /// </summary>
        public void HighlightSlot(int index, Color color)
        {
            if (index >= 0 && index < _uiSlots.Count)
            {
                // Assuming InventorySlotUI has a specific way to show highlight, 
                // or we access its image directly.
                // For this refactor step, we might need to expose the Image from InventorySlotUI
                // but let's assume we can access it via the component for now
                if (_uiSlots[index].iconImage)
                {
                    _uiSlots[index].iconImage.color = color;
                }
            }
        }
    }
}
