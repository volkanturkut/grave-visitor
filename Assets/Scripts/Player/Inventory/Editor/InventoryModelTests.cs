using NUnit.Framework;
using UnityEngine;
using GraveVisitor.Inventory;

namespace GraveVisitor.Inventory.Tests
{
    [TestFixture]
    public class InventoryModelTests
    {
        private InventoryModel _inventoryModel;
        private const int MaxSlots = 10;

        [SetUp]
        public void SetUp()
        {
            _inventoryModel = new InventoryModel(MaxSlots);
        }

        [Test]
        public void RemoveItem_ValidIndex_ClearsSlot()
        {
            // Arrange
            ItemData item = ScriptableObject.CreateInstance<ItemData>();
            item.itemName = "Test Item";
            _inventoryModel.AddItem(item, 5);

            int slotIndex = 0;
            Assert.IsFalse(_inventoryModel.GetSlot(slotIndex).IsEmpty, "Slot should not be empty after adding item.");

            // Act
            _inventoryModel.RemoveItem(slotIndex);

            // Assert
            Assert.IsTrue(_inventoryModel.GetSlot(slotIndex).IsEmpty, "Slot should be empty after removing item.");
            Assert.IsNull(_inventoryModel.GetSlot(slotIndex).itemData, "ItemData should be null after removal.");
            Assert.AreEqual(0, _inventoryModel.GetSlot(slotIndex).quantity, "Quantity should be 0 after removal.");
        }

        [Test]
        public void RemoveItem_InvalidIndex_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _inventoryModel.RemoveItem(-1));
            Assert.DoesNotThrow(() => _inventoryModel.RemoveItem(MaxSlots));
            Assert.DoesNotThrow(() => _inventoryModel.RemoveItem(999));
        }

        [Test]
        public void RemoveItem_ValidIndex_InvokesUpdateEvent()
        {
            // Arrange
            bool eventInvoked = false;
            _inventoryModel.OnInventoryUpdated.AddListener(() => eventInvoked = true);

            ItemData item = ScriptableObject.CreateInstance<ItemData>();
            _inventoryModel.AddItem(item, 1);
            eventInvoked = false; // Reset after AddItem invoke

            // Act
            _inventoryModel.RemoveItem(0);

            // Assert
            Assert.IsTrue(eventInvoked, "OnInventoryUpdated should be invoked when an item is removed.");
        }

        [Test]
        public void RemoveItem_EmptySlot_StillInvokesUpdateEvent()
        {
            // Arrange
            bool eventInvoked = false;
            _inventoryModel.OnInventoryUpdated.AddListener(() => eventInvoked = true);

            // Act
            _inventoryModel.RemoveItem(0);

            // Assert
            Assert.IsTrue(eventInvoked, "OnInventoryUpdated should be invoked even if the slot was already empty.");
        }

        [Test]
        public void SwapItems_ValidIndices_SwapsItems()
        {
            // Arrange
            ItemData itemA = ScriptableObject.CreateInstance<ItemData>();
            itemA.itemName = "Item A";
            _inventoryModel.AddItem(itemA, 1);

            ItemData itemB = ScriptableObject.CreateInstance<ItemData>();
            itemB.itemName = "Item B";
            _inventoryModel.AddItem(itemB, 1);

            int indexA = 0;
            int indexB = 1;

            Assert.AreEqual(itemA, _inventoryModel.GetSlot(indexA).itemData);
            Assert.AreEqual(itemB, _inventoryModel.GetSlot(indexB).itemData);

            // Act
            _inventoryModel.SwapItems(indexA, indexB);

            // Assert
            Assert.AreEqual(itemB, _inventoryModel.GetSlot(indexA).itemData, "Slot A should contain Item B after swap.");
            Assert.AreEqual(itemA, _inventoryModel.GetSlot(indexB).itemData, "Slot B should contain Item A after swap.");
        }

        [Test]
        public void SwapItems_WithEmptySlot_SwapsCorrectly()
        {
            // Arrange
            ItemData item = ScriptableObject.CreateInstance<ItemData>();
            item.itemName = "Item";
            _inventoryModel.AddItem(item, 1);

            int itemIndex = 0;
            int emptyIndex = 1;

            Assert.IsFalse(_inventoryModel.GetSlot(itemIndex).IsEmpty);
            Assert.IsTrue(_inventoryModel.GetSlot(emptyIndex).IsEmpty);

            // Act
            _inventoryModel.SwapItems(itemIndex, emptyIndex);

            // Assert
            Assert.IsTrue(_inventoryModel.GetSlot(itemIndex).IsEmpty, "Original item slot should be empty.");
            Assert.AreEqual(item, _inventoryModel.GetSlot(emptyIndex).itemData, "Target slot should contain the item.");
        }

        [Test]
        public void SwapItems_UpdatesFavoritesMapping()
        {
            // Arrange
            ItemData item = ScriptableObject.CreateInstance<ItemData>();
            _inventoryModel.AddItem(item, 1);
            int originalIndex = 0;
            int newIndex = 1;

            // Set favorite: Slot 0 is assigned to Favorite 0
            _inventoryModel.SetFavoriteSlot(0, originalIndex);
            Assert.AreEqual(originalIndex, _inventoryModel.GetFavoriteSlots()[0]);

            // Act
            _inventoryModel.SwapItems(originalIndex, newIndex);

            // Assert
            Assert.AreEqual(newIndex, _inventoryModel.GetFavoriteSlots()[0], "Favorite mapping should follow the item to the new slot.");
        }

        [Test]
        public void SwapItems_InvalidIndices_DoesNothing()
        {
            // Arrange
            ItemData item = ScriptableObject.CreateInstance<ItemData>();
            _inventoryModel.AddItem(item, 1);
            var originalSlotData = _inventoryModel.GetSlot(0);

            // Act
            _inventoryModel.SwapItems(0, -1);
            _inventoryModel.SwapItems(0, 999);

            // Assert
            Assert.AreEqual(item, _inventoryModel.GetSlot(0).itemData, "Inventory should not change on invalid swap.");
        }

        [Test]
        public void SwapItems_InvokesUpdateEvent()
        {
            // Arrange
            bool eventInvoked = false;
            _inventoryModel.OnInventoryUpdated.AddListener(() => eventInvoked = true);

            // Act
            _inventoryModel.SwapItems(0, 1);

            // Assert
            Assert.IsTrue(eventInvoked, "OnInventoryUpdated should be invoked after swap.");
        }
    }
}
