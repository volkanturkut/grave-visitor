using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;

namespace Core.Events.Tests
{
    [TestFixture]
    public class DailyScheduleTests
    {
        private DailySchedule _dailySchedule;
        private GameEvent _gameEvent;
        private GameObject _listenerObject;
        private GameEventListener _listener;
        private bool _eventRaised;

        [SetUp]
        public void SetUp()
        {
            _dailySchedule = ScriptableObject.CreateInstance<DailySchedule>();
            _dailySchedule.events = new List<DailySchedule.ScheduledEvent>();

            _gameEvent = ScriptableObject.CreateInstance<GameEvent>();

            _listenerObject = new GameObject("TestListener");
            _listener = _listenerObject.AddComponent<GameEventListener>();
            _listener.Event = _gameEvent;
            _listener.Response = new UnityEvent();

            _eventRaised = false;
            _listener.Response.AddListener(() => _eventRaised = true);

            // Manually register the listener since OnEnable might not run automatically in EditMode tests
            // or to be explicit about the setup.
            _gameEvent.RegisterListener(_listener);
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

            if (_dailySchedule != null)
            {
                Object.DestroyImmediate(_dailySchedule);
            }

            if (_gameEvent != null)
            {
                Object.DestroyImmediate(_gameEvent);
            }
        }

        [Test]
        public void CheckSchedule_CorrectHour_RaisesEvent()
        {
            // Arrange
            int testHour = 8;
            var scheduledEvent = new DailySchedule.ScheduledEvent
            {
                hour = testHour,
                eventToRaise = _gameEvent,
                description = "Test Event"
            };
            _dailySchedule.events.Add(scheduledEvent);

            // Act
            _dailySchedule.CheckSchedule(testHour);

            // Assert
            Assert.IsTrue(_eventRaised, "Event should be raised when the hour matches.");
        }

        [Test]
        public void CheckSchedule_IncorrectHour_DoesNotRaiseEvent()
        {
            // Arrange
            int eventHour = 8;
            int checkHour = 9;
            var scheduledEvent = new DailySchedule.ScheduledEvent
            {
                hour = eventHour,
                eventToRaise = _gameEvent,
                description = "Test Event"
            };
            _dailySchedule.events.Add(scheduledEvent);

            // Act
            _dailySchedule.CheckSchedule(checkHour);

            // Assert
            Assert.IsFalse(_eventRaised, "Event should not be raised when the hour does not match.");
        }

        [Test]
        public void CheckSchedule_NullEvent_DoesNotRaiseEvent()
        {
            // Arrange
            int testHour = 8;
            var scheduledEvent = new DailySchedule.ScheduledEvent
            {
                hour = testHour,
                eventToRaise = null,
                description = "Test Event"
            };
            _dailySchedule.events.Add(scheduledEvent);

            // Act
            // Should not throw exception
            Assert.DoesNotThrow(() => _dailySchedule.CheckSchedule(testHour));
        }

        [Test]
        public void CheckSchedule_MultipleEvents_RaisesCorrectEvent()
        {
            // Arrange
            int hour1 = 8;
            int hour2 = 9;

            var event1 = ScriptableObject.CreateInstance<GameEvent>();
            var event2 = ScriptableObject.CreateInstance<GameEvent>();

            bool event1Raised = false;
            bool event2Raised = false;

            var listenerObj1 = new GameObject("Listener1");
            var listener1 = listenerObj1.AddComponent<GameEventListener>();
            listener1.Event = event1;
            listener1.Response = new UnityEvent();
            listener1.Response.AddListener(() => event1Raised = true);
            event1.RegisterListener(listener1);

            var listenerObj2 = new GameObject("Listener2");
            var listener2 = listenerObj2.AddComponent<GameEventListener>();
            listener2.Event = event2;
            listener2.Response = new UnityEvent();
            listener2.Response.AddListener(() => event2Raised = true);
            event2.RegisterListener(listener2);

            _dailySchedule.events.Add(new DailySchedule.ScheduledEvent { hour = hour1, eventToRaise = event1 });
            _dailySchedule.events.Add(new DailySchedule.ScheduledEvent { hour = hour2, eventToRaise = event2 });

            // Act
            _dailySchedule.CheckSchedule(hour1);

            // Assert
            Assert.IsTrue(event1Raised, "Event 1 should be raised for hour 1.");
            Assert.IsFalse(event2Raised, "Event 2 should not be raised for hour 1.");

            // Cleanup
            event1.UnregisterListener(listener1);
            event2.UnregisterListener(listener2);
            Object.DestroyImmediate(listenerObj1);
            Object.DestroyImmediate(listenerObj2);
            Object.DestroyImmediate(event1);
            Object.DestroyImmediate(event2);
        }
    }
}
