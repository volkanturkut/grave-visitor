using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GraveVisitor.Inventory.Tests
{
    public class InventoryDropDistanceTests
    {
        [Test]
        public void CalculateDropDistance_NormalDistance_SubtractsPadding()
        {
            // Arrange
            float hitDistance = 1.0f;

            // Act
            float result = InventoryManager.CalculateDropDistance(hitDistance);

            // Assert
            Assert.AreEqual(0.8f, result, 0.001f);
        }

        [Test]
        public void CalculateDropDistance_SmallDistance_Minimum02()
        {
            // Arrange
            float hitDistance = 0.3f;

            // Act
            float result = InventoryManager.CalculateDropDistance(hitDistance);

            // Assert
            Assert.AreEqual(0.2f, result, 0.001f);
        }

        [Test]
        public void CalculateDropDistance_HitDistanceLessThan02_CappedAtHitDistanceMinus005()
        {
            // Arrange
            float hitDistance = 0.1f;

            // Act
            float result = InventoryManager.CalculateDropDistance(hitDistance);

            // Assert
            Assert.AreEqual(0.05f, result, 0.001f);
        }

        [Test]
        public void CalculateDropDistance_HitDistanceExactly02_Returns02()
        {
            // Arrange
            float hitDistance = 0.2f;

            // Act
            float result = InventoryManager.CalculateDropDistance(hitDistance);

            // Assert
            Assert.AreEqual(0.2f, result, 0.001f);
        }

        [Test]
        public void CalculateDropDistance_HitDistanceExtremelySmall_Returns0()
        {
            // Arrange
            float hitDistance = 0.02f;

            // Act
            float result = InventoryManager.CalculateDropDistance(hitDistance);

            // Assert
            Assert.AreEqual(0f, result, 0.001f);
        }
    }
}
