using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;

namespace Core.Events.Tests
{
    [TestFixture]
    public class GameEventTests
    {
        private GameEvent _gameEvent;
        private GameObject _listenerObject;
        private GameEventListener _listener;
        private bool _eventRaised;

        [SetUp]
        public void SetUp()
        {
            _gameEvent = ScriptableObject.CreateInstance<GameEvent>();
            _listenerObject = new GameObject("TestListener");
            _listener = _listenerObject.AddComponent<GameEventListener>();
            _listener.Event = _gameEvent;
            _listener.Response = new UnityEvent();
            _eventRaised = false;
            _listener.Response.AddListener(() => _eventRaised = true);
        }

        [TearDown]
        public void TearDown()
        {
            if (_gameEvent != null && _listener != null)
            {
                _gameEvent.UnregisterListener(_listener);
            }
            if (_listenerObject != null)
            {
                Object.DestroyImmediate(_listenerObject);
            }
            if (_gameEvent != null)
            {
                Object.DestroyImmediate(_gameEvent);
            }
        }

        [Test]
        public void Raise_InvokesListenerResponse()
        {
            _gameEvent.RegisterListener(_listener);
            _gameEvent.Raise();
            Assert.IsTrue(_eventRaised, "Listener response should be invoked when event is raised.");
        }

        [Test]
        public void RegisterListener_AddsListener()
        {
            _gameEvent.RegisterListener(_listener);
            _gameEvent.Raise();
            Assert.IsTrue(_eventRaised, "Listener should be registered and receive event.");
        }

        [Test]
        public void UnregisterListener_RemovesListener()
        {
            _gameEvent.RegisterListener(_listener);
            _gameEvent.UnregisterListener(_listener);
            _gameEvent.Raise();
            Assert.IsFalse(_eventRaised, "Listener should be unregistered and not receive event.");
        }

        [Test]
        public void RegisterListener_NullListener_DoesNotThrowOnRaise()
        {
            Assert.DoesNotThrow(() =>
            {
                _gameEvent.RegisterListener(null);
                _gameEvent.Raise();
            }, "Registering a null listener should not cause an exception when the event is raised.");
        }

        [Test]
        public void UnregisterListener_NullListener_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                _gameEvent.UnregisterListener(null);
            }, "Unregistering a null listener should not throw an exception.");
        }

        [Test]
        public void RegisterListener_DuplicateRegistration_InvokesOnce()
        {
            int callCount = 0;
            _listener.Response.RemoveAllListeners();
            _listener.Response.AddListener(() => callCount++);

            _gameEvent.RegisterListener(_listener);
            _gameEvent.RegisterListener(_listener); // Duplicate registration

            _gameEvent.Raise();

            Assert.AreEqual(1, callCount, "Listener should only be invoked once despite duplicate registration.");
        }

        [Test]
        public void Raise_MultipleListeners_InvokesAll()
        {
            var listenerObj2 = new GameObject("TestListener2");
            var listener2 = listenerObj2.AddComponent<GameEventListener>();
            listener2.Event = _gameEvent;
            listener2.Response = new UnityEvent();
            bool event2Raised = false;
            listener2.Response.AddListener(() => event2Raised = true);

            _gameEvent.RegisterListener(_listener);
            _gameEvent.RegisterListener(listener2);

            _gameEvent.Raise();

            Assert.IsTrue(_eventRaised, "First listener should be invoked.");
            Assert.IsTrue(event2Raised, "Second listener should be invoked.");

            Object.DestroyImmediate(listenerObj2);
        }

        [Test]
        public void Raise_NoListeners_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                _gameEvent.Raise();
            }, "Calling Raise with no listeners should not throw an exception.");
        }

        [Test]
        public void Raise_ListenerUnregistersDuringRaise_IteratesSafely()
        {
            var listenerObj2 = new GameObject("TestListener2");
            var listener2 = listenerObj2.AddComponent<GameEventListener>();
            listener2.Event = _gameEvent;
            listener2.Response = new UnityEvent();
            bool event2Raised = false;
            listener2.Response.AddListener(() => event2Raised = true);

            var listenerObj3 = new GameObject("TestListener3");
            var listener3 = listenerObj3.AddComponent<GameEventListener>();
            listener3.Event = _gameEvent;
            listener3.Response = new UnityEvent();
            bool event3Raised = false;
            listener3.Response.AddListener(() => event3Raised = true);

            // Listener 2 unregisters itself when the event is raised
            listener2.Response.AddListener(() => _gameEvent.UnregisterListener(listener2));

            _gameEvent.RegisterListener(_listener);
            _gameEvent.RegisterListener(listener2);
            _gameEvent.RegisterListener(listener3);

            Assert.DoesNotThrow(() => _gameEvent.Raise(), "Raise should not throw when a listener unregisters itself.");

            Assert.IsTrue(_eventRaised, "First listener should be invoked.");
            Assert.IsTrue(event2Raised, "Second listener (the one unregistering) should be invoked.");
            Assert.IsTrue(event3Raised, "Third listener should be invoked.");

            Object.DestroyImmediate(listenerObj2);
            Object.DestroyImmediate(listenerObj3);
        }
    }
}
