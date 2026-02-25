using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Core.Tests
{
    [TestFixture]
    public class ObjectPoolTests
    {
        private GameObject _poolContainer;
        private GameObject _prefab;
        private ObjectPool<TestPoolableComponent> _pool;

        [SetUp]
        public void SetUp()
        {
            _poolContainer = new GameObject("PoolContainer");
            _prefab = new GameObject("Prefab");
            _prefab.AddComponent<TestPoolableComponent>();

            // Initialize pool with size 2, max size 5
            _pool = new ObjectPool<TestPoolableComponent>(_prefab, 2, 5, _poolContainer.transform, true);
        }

        [TearDown]
        public void TearDown()
        {
            if (_pool != null)
            {
                _pool.Clear();
            }
            if (_poolContainer != null)
            {
                Object.DestroyImmediate(_poolContainer);
            }
            if (_prefab != null)
            {
                Object.DestroyImmediate(_prefab);
            }
        }

        [Test]
        public void Return_ValidObject_ReturnsToPoolAndDeactivates()
        {
            // Arrange
            TestPoolableComponent obj = _pool.Get();
            int initialAvailable = _pool.AvailableCount;

            // Act
            _pool.Return(obj);

            // Assert
            Assert.IsFalse(obj.gameObject.activeSelf, "Returned object should be deactivated");
            Assert.AreEqual(initialAvailable + 1, _pool.AvailableCount, "Available count should increase by 1");
        }

        [Test]
        public void Return_NullObject_DoesNotCrash()
        {
            // Arrange
            int initialAvailable = _pool.AvailableCount;

            // Act
            _pool.Return(null);

            // Assert
            Assert.AreEqual(initialAvailable, _pool.AvailableCount, "Pool state should not change when returning null");
        }

        [Test]
        public void Return_ObjectNotFromPool_DoesNotAddToPool()
        {
            // Arrange
            GameObject otherObj = new GameObject("OtherObject");
            TestPoolableComponent otherComponent = otherObj.AddComponent<TestPoolableComponent>();
            int initialAvailable = _pool.AvailableCount;

            // Act
            _pool.Return(otherComponent);

            // Assert
            Assert.AreEqual(initialAvailable, _pool.AvailableCount, "Should not accept objects not from the pool");
            Assert.IsTrue(otherObj.activeSelf, "Object not from pool should remain active");

            // Cleanup
            Object.DestroyImmediate(otherObj);
        }

        [Test]
        public void Return_PoolableObject_CallsOnReturnToPool()
        {
            // Arrange
            TestPoolableComponent obj = _pool.Get();
            Assert.IsTrue(obj.OnSpawnCalled, "OnSpawnFromPool should be called on Get");

            // Reset flags to verify Return specifically
            obj.ResetFlags();

            // Act
            _pool.Return(obj);

            // Assert
            Assert.IsTrue(obj.OnReturnCalled, "OnReturnToPool should be called on Return");
        }
    }

    public class TestPoolableComponent : MonoBehaviour, IPoolable
    {
        public bool OnSpawnCalled { get; private set; }
        public bool OnReturnCalled { get; private set; }

        public void OnSpawnFromPool()
        {
            OnSpawnCalled = true;
        }

        public void OnReturnToPool()
        {
            OnReturnCalled = true;
        }

        public void ResetFlags()
        {
            OnSpawnCalled = false;
            OnReturnCalled = false;
        }
    }
}
