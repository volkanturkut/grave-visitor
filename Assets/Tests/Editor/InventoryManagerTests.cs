using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Reflection;
using GraveVisitor.Inventory;

public class InventoryManagerTests
{
    private InventoryManager manager;
    private InventoryModel model;

    [SetUp]
    public void Setup()
    {
        GameObject go = new GameObject("InventoryManager");
        manager = go.AddComponent<InventoryManager>();

        // Initialize Model manually via reflection
        model = new InventoryModel(10); // 10 slots

        FieldInfo modelField = typeof(InventoryManager).GetField("_model", BindingFlags.NonPublic | BindingFlags.Instance);
        if (modelField == null)
        {
            Debug.LogError("_model field not found!");
            return;
        }
        modelField.SetValue(manager, model);

        // Ensure favoriteSlots is initialized
    }

    [TearDown]
    public void Teardown()
    {
        if (manager != null)
            Object.DestroyImmediate(manager.gameObject);
    }

    [Test]
    public void OnFavoriteItem_NonTool_DoesNotFavorite()
    {
        // Arrange
        ItemData nonToolItem = ScriptableObject.CreateInstance<ItemData>();
        nonToolItem.itemType = ItemData.ItemType.Consumable; // Not Tool

        // Ensure slot 0 has this item
        // Accessing Slots directly from model which is exposed in InventoryModel class
        model.Slots[0].itemData = nonToolItem;
        model.Slots[0].quantity = 1;

        // Act
        manager.OnFavoriteItem(0);

        // Assert
        int favIndex = manager.GetFavoriteIndex(0);
        Assert.AreEqual(-1, favIndex, "Non-tool item should not be favorited");
    }

    [Test]
    public void OnFavoriteItem_Tool_Favorites()
    {
        // Arrange
        ItemData toolItem = ScriptableObject.CreateInstance<ItemData>();
        toolItem.itemType = ItemData.ItemType.Tool;

        model.Slots[0].itemData = toolItem;
        model.Slots[0].quantity = 1;

        // Act
        manager.OnFavoriteItem(0);

        // Assert
        int favIndex = manager.GetFavoriteIndex(0);
        Assert.AreEqual(0, favIndex, "Tool item should be favorited to slot 0 (first slot)");
    }

    [Test]
    public void OnFavoriteItem_CycleFavorites()
    {
        // Arrange
        ItemData toolItem = ScriptableObject.CreateInstance<ItemData>();
        toolItem.itemType = ItemData.ItemType.Tool;

        model.Slots[0].itemData = toolItem;
        model.Slots[0].quantity = 1;

        // Act 1: Favorite first time -> Slot 0
        manager.OnFavoriteItem(0);
        Assert.AreEqual(0, manager.GetFavoriteIndex(0));

        // Act 2: Favorite second time -> Slot 1
        manager.OnFavoriteItem(0);
        Assert.AreEqual(1, manager.GetFavoriteIndex(0));

        // Act 3: Favorite third time -> Slot 2
        manager.OnFavoriteItem(0);
        Assert.AreEqual(2, manager.GetFavoriteIndex(0));

        // Act 4: Favorite fourth time -> Slot 3
        manager.OnFavoriteItem(0);
        Assert.AreEqual(3, manager.GetFavoriteIndex(0));

        // Act 5: Favorite fifth time -> Removed
        manager.OnFavoriteItem(0);
        Assert.AreEqual(-1, manager.GetFavoriteIndex(0));
    }
}
