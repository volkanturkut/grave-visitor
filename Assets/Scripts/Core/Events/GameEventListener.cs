using UnityEngine;
using UnityEngine.Events;

public class GameEventListener : MonoBehaviour
{
    [Tooltip("The Event SO to listen for")]
    public GameEvent Event;

    [Tooltip("Response to trigger when Event is raised")]
    public UnityEvent Response;

    private void OnEnable()
    {
        if (Event != null) Event.RegisterListener(this);
    }

    private void OnDisable()
    {
        if (Event != null) Event.UnregisterListener(this);
    }

    public void OnEventRaised()
    {
        Response.Invoke();
    }
}