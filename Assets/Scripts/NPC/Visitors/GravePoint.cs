using System.Collections.Generic;
using UnityEngine;

public class GravePoint : MonoBehaviour
{
    // A global static list of all graves in the scene
    public static List<GravePoint> AllGraves = new List<GravePoint>();

    // Is an NPC currently standing here?
    public bool IsOccupied { get; private set; } = false;

    private void OnEnable()
    {
        AllGraves.Add(this);
    }

    private void OnDisable()
    {
        AllGraves.Remove(this);
    }

    public void SetOccupied(bool state)
    {
        IsOccupied = state;
    }

    public Vector3 GetPosition()
    {
        return transform.position;
    }
}