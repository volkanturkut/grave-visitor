using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Schedule", menuName = "Events/Daily Schedule")]
public class DailySchedule : ScriptableObject
{
    [System.Serializable]
    public struct ScheduledEvent
    {
        public string description; // E.g. "Lights On"
        [Range(0, 23)] public int hour;
        public GameEvent eventToRaise;
    }

    public List<ScheduledEvent> events;

    public void CheckSchedule(int currentHour)
    {
        foreach (var item in events)
        {
            if (item.hour == currentHour && item.eventToRaise != null)
            {
                item.eventToRaise.Raise();
            }
        }
    }
}