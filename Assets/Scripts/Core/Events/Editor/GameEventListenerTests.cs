using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;

namespace Core.Events.Tests
{
    [TestFixture]
    public class GameEventListenerTests
    {
        private GameObject _listenerObject;
        private GameEventListener _listener;
        private bool _responseInvoked;

        [SetUp]
        public void SetUp()
        {
            _listenerObject = new GameObject("TestListener");
            _listener = _listenerObject.AddComponent<GameEventListener>();
            _listener.Response = new UnityEvent();
            _responseInvoked = false;
            _listener.Response.AddListener(() => _responseInvoked = true);
        }

        [TearDown]
        public void TearDown()
        {
            if (_listenerObject != null)
            {
                Object.DestroyImmediate(_listenerObject);
            }
        }

        [Test]
        public void OnEventRaised_InvokesResponse()
        {
            // Act
            _listener.OnEventRaised();

            // Assert
            Assert.IsTrue(_responseInvoked, "Listener response should be invoked when OnEventRaised is called.");
        }
    }
}
