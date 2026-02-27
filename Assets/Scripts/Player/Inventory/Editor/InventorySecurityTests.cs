using NUnit.Framework;
using UnityEngine;
using GraveVisitor.Inventory;
using System.Collections;
using UnityEngine.TestTools;

namespace GraveVisitor.Inventory.Tests
{
    [TestFixture]
    public class InventorySecurityTests
    {
        private InventoryModel _inventoryModel;
        private const int MaxSlots = 5;

        [SetUp]
        public void SetUp()
        {
            _inventoryModel = new InventoryModel(MaxSlots);
        }

        [Test, Timeout(1000)] // Fail if the test takes longer than 1 second (infinite loop)
        public void AddItem_ZeroMaxStack_DoesNotHangOrFillInventory()
        {
            // Arrange
            ItemData item = ScriptableObject.CreateInstance<ItemData>();
            item.itemName = "Malicious Item";
            item.isStackable = true;
            item.maxStack = 0; // Malicious value

            // Act
            bool result = _inventoryModel.AddItem(item, 5);

            // Assert
            // The expectation depends on the desired behavior.
            // Ideally, it should either reject the item or treat it as maxStack=1.
            // If the vulnerability exists, the loop `while (amount > 0)` will run until inventory is full,
            // because `amount` is decremented by `Mathf.Min(amount, item.maxStack)`, which is 0.

            // If the vulnerability is present, this test will likely pass but the inventory will be full of 0-quantity items,
            // or it might hang if FindEmptySlotIndex logic allows it (it doesn't, it returns -1 eventually).
            // Wait, if maxStack is 0, amountToAdd is 0.
            // slot.quantity becomes 0.
            // amount -= 0, so amount remains 5.
            // It finds the next empty slot.
            // It fills all slots with quantity 0.
            // Then FindEmptySlotIndex returns -1.
            // Then it returns false.

            // So it won't be an INFINITE loop in the sense of hanging forever,
            // but it will instantaneously fill the entire inventory with garbage.

            // We want to assert that it does NOT fill the inventory with garbage.

            int filledSlots = 0;
            foreach(var slot in _inventoryModel.Slots)
            {
                if (!slot.IsEmpty) filledSlots++;
            }

            // If we fix it by clamping maxStack to 1, it should fill 1 slot with 5 items (if maxStack was 1)
            // or 5 slots with 1 item each (if maxStack was 1).
            // Wait, if we clamp maxStack to 1:
            // amountToAdd = min(5, 1) = 1.
            // slot.quantity = 1.
            // amount = 4.
            // Next slot...
            // So it would fill 5 slots.

            // However, the vulnerability report says "Infinite Loop Vulnerability".
            // Let's look at the code again.
            /*
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
            */
            // If item.maxStack is 0:
            // amountToAdd = 0.
            // amount -= 0.
            // Loop continues.
            // FindEmptySlotIndex() finds the next empty slot.
            // Eventually all slots are filled with quantity 0.
            // FindEmptySlotIndex returns -1.
            // Returns false.

            // So it terminates. It is NOT an infinite loop that hangs the thread,
            // but it is a "logical infinite loop" that consumes all available resources (slots) instantaneously.

            // UNLESS item.maxStack is negative?
            // If item.maxStack is -1:
            // amountToAdd = -1.
            // amount -= -1 => amount increases!
            // valid slot is filled with -1.
            // It fills all slots with -1.
            // Still terminates when inventory is full.

            // Okay, so the "Infinite Loop" description might be slightly inaccurate regarding "hanging",
            // but it describes the unbounded consumption of slots.

            // However, we want to prevent this "fill everything" behavior.
            // If I pass maxStack=0, I expect it to behave safely (e.g. treat as 1, or reject).
            // If I treat it as 1, it will fill 5 slots with 1 item each.

            // Wait, if the fix is `Mathf.Max(1, item.maxStack)`, then passing 0 becomes 1.
            // If I pass 5 items with maxStack 0 (clamped to 1), it will fill 5 slots.
            // This is "safer" than filling them with 0 quantity?

            // Actually, a quantity of 0 is usually considered "Empty" in many systems.
            // If `slot.IsEmpty` checks `itemData == null`, then a slot with itemData but quantity 0 is NOT empty.
            // So it bricks the inventory slots.

            // Let's assert that we don't end up with 0-quantity items.

            bool hasZeroQuantitySlots = false;
            foreach(var slot in _inventoryModel.Slots)
            {
                if (!slot.IsEmpty && slot.quantity <= 0)
                {
                    hasZeroQuantitySlots = true;
                    break;
                }
            }

            Assert.IsFalse(hasZeroQuantitySlots, "Inventory should not contain slots with <= 0 quantity after addition.");
        }
    }
}
