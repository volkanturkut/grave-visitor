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
    }
}
