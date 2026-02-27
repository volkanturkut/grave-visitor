using NUnit.Framework;
using UnityEngine;
using GraveVisitor.Inventory;

namespace GraveVisitor.Inventory.Tests
{
    [TestFixture]
    public class InventorySlotDataTests
    {
        [Test]
        public void IsEmpty_DefaultConstructor_ReturnsTrue()
        {
            // Arrange
            InventorySlotData slotData = new InventorySlotData();

            // Assert
            Assert.IsTrue(slotData.IsEmpty, "New InventorySlotData should be empty by default.");
            Assert.AreEqual(0, slotData.quantity, "New InventorySlotData should have 0 quantity.");
        }

        [Test]
        public void IsEmpty_ConstructorWithNullItem_ReturnsTrue()
        {
            // Arrange
            InventorySlotData slotData = new InventorySlotData(null, 5);

            // Assert
            Assert.IsTrue(slotData.IsEmpty, "InventorySlotData with null item should be empty.");
        }

        [Test]
        public void IsEmpty_ConstructorWithItem_ReturnsFalse()
        {
            // Arrange
            ItemData item = ScriptableObject.CreateInstance<ItemData>();
            InventorySlotData slotData = new InventorySlotData(item, 1);

            // Assert
            Assert.IsFalse(slotData.IsEmpty, "InventorySlotData with valid item should not be empty.");
        }

        [Test]
        public void IsEmpty_SetItemDataNull_ReturnsTrue()
        {
            // Arrange
            ItemData item = ScriptableObject.CreateInstance<ItemData>();
            InventorySlotData slotData = new InventorySlotData(item, 1);

            // Act
            slotData.itemData = null;

            // Assert
            Assert.IsTrue(slotData.IsEmpty, "InventorySlotData should be empty after setting itemData to null.");
        }

        [Test]
        public void IsEmpty_SetItemDataNotNull_ReturnsFalse()
        {
            // Arrange
            InventorySlotData slotData = new InventorySlotData();
            ItemData item = ScriptableObject.CreateInstance<ItemData>();

            // Act
            slotData.itemData = item;

            // Assert
            Assert.IsFalse(slotData.IsEmpty, "InventorySlotData should not be empty after setting itemData.");
        }
    }
}
