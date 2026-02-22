using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GraveVisitor.Inventory
{
    [System.Serializable]
    public class InventoryItemAddedEvent : UnityEvent<ItemData, int> { }
    [System.Serializable]
    public class InventoryItemRemovedEvent : UnityEvent<int> { }
    [System.Serializable]
    public class InventoryUpdatedEvent : UnityEvent { }

    /// <summary>
    /// Model component of the Inventory MVC system.
    /// Handles data storage, validation, and core logic (add, remove, split, etc.).
    /// </summary>
    public class InventoryModel
    {
        // Data
        private List<InventorySlotData> _slots;
        private int _maxSlots;
        private int[] _favoriteSlots = { -1, -1, -1, -1 };

        // Events
        public InventoryUpdatedEvent OnInventoryUpdated = new InventoryUpdatedEvent();

        public List<InventorySlotData> Slots => _slots;
        public int MaxSlots => _maxSlots;

        public InventoryModel(int maxSlots)
        {
            _maxSlots = maxSlots;
            _slots = new List<InventorySlotData>();
            for (int i = 0; i < maxSlots; i++)
            {
                _slots.Add(new InventorySlotData());
            }
        }

        /// <summary>
        /// Adds an item to the inventory.
        /// </summary>
        public bool AddItem(ItemData item, int amount = 1)
        {
            if (item.isStackable)
            {
                foreach (var slot in _slots)
                {
                    if (!slot.IsEmpty && slot.itemData == item && slot.quantity < item.maxStack)
                    {
                        int spaceRemaining = item.maxStack - slot.quantity;
                        int amountToAdd = Mathf.Min(spaceRemaining, amount);
                        slot.quantity += amountToAdd;
                        amount -= amountToAdd;
                        if (amount <= 0)
                        {
                            OnInventoryUpdated.Invoke();
                            return true;
                        }
                    }
                }
            }

            while (amount > 0)
            {
                int emptyIndex = FindEmptySlotIndex();
                if (emptyIndex == -1)
                {
                    OnInventoryUpdated.Invoke();
                    return false; // Inventory full
                }

                var slot = _slots[emptyIndex];
                slot.itemData = item;
                int amountToAdd = Mathf.Min(amount, item.maxStack);
                slot.quantity = amountToAdd;
                amount -= amountToAdd;
            }

            OnInventoryUpdated.Invoke();
            return true;
        }

        /// <summary>
        /// Removes an item from a specific slot.
        /// </summary>
        public void RemoveItem(int index)
        {
            if (IsValidIndex(index))
            {
                _slots[index] = new InventorySlotData();
                OnInventoryUpdated.Invoke();
            }
        }

        /// <summary>
        /// Swaps items between two slots.
        /// </summary>
        public void SwapItems(int indexA, int indexB)
        {
            if (IsValidIndex(indexA) && IsValidIndex(indexB))
            {
                (_slots[indexA], _slots[indexB]) = (_slots[indexB], _slots[indexA]);

                // Update favorites mapping
                UpdateFavoritesOnSwap(indexA, indexB);

                OnInventoryUpdated.Invoke();
            }
        }

        /// <summary>
        /// Updates favorite slot references after a swap.
        /// </summary>
        private void UpdateFavoritesOnSwap(int indexA, int indexB)
        {
            for (int i = 0; i < 4; i++)
            {
                if (_favoriteSlots[i] == indexA) _favoriteSlots[i] = indexB;
                else if (_favoriteSlots[i] == indexB) _favoriteSlots[i] = indexA;
            }
        }

        private int FindEmptySlotIndex()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].IsEmpty) return i;
            }
            return -1;
        }

        private bool IsValidIndex(int index)
        {
            return index >= 0 && index < _slots.Count;
        }

        public InventorySlotData GetSlot(int index)
        {
            if (IsValidIndex(index)) return _slots[index];
            return null;
        }

        public int[] GetFavoriteSlots() => _favoriteSlots;

        public void SetFavoriteSlot(int favIndex, int inventoryIndex)
        {
            if (favIndex >= 0 && favIndex < _favoriteSlots.Length)
            {
                _favoriteSlots[favIndex] = inventoryIndex;
            }
        }
    }
}
