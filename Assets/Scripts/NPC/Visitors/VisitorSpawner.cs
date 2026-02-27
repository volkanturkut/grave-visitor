using System.Collections.Generic;
using UnityEngine;

public class VisitorSpawner : MonoBehaviour
{
    [Header("References")]
    public DayNightController dayNightController; //
    public GameObject visitorPrefab;
    public Transform spawnPoint;   // Place this "outside"
    public Transform despawnPoint; // Usually same as spawn point

    [Header("Spawning Settings")]
    public int maxConcurrentVisitors = 10;
    public float minSpawnInterval = 5f;
    public float maxSpawnInterval = 15f;

    [Header("Time Settings")]
    [Tooltip("Start Hour (24h format, e.g., 12 for Noon)")]
    public float openHour = 12f;
    [Tooltip("End Hour (24h format, e.g., 5 for 5 AM)")]
    public float closeHour = 5f;

    private float _spawnTimer;
    private float _cleanupTimer;
    private const float CLEANUP_INTERVAL = 1.0f; // Cleanup every second instead of every frame
    private readonly List<VisitorAI> _activeVisitors = new List<VisitorAI>();

    // Object pooling
    private ObjectPool<VisitorAI> _visitorPool;

    /// <summary>
    /// Initializes the object pool and pre-warms visitors.
    /// </summary>
    private void Start()
    {
        // Initialize object pool
        _visitorPool = new ObjectPool<VisitorAI>(
            visitorPrefab,
            initialSize: 5,
            maxSize: maxConcurrentVisitors,
            parent: transform,
            allowGrowth: false
        );

        DebugLogger.Log($"[VisitorSpawner] Pool initialized with {_visitorPool.TotalCount} visitors");
    }

    /// <summary>
    /// Handles spawning logic and periodic cleanup of the visitor list.
    /// </summary>
    private void Update()
    {
        // Periodic cleanup instead of every frame (5x performance improvement)
        _cleanupTimer -= Time.deltaTime;
        if (_cleanupTimer <= 0f)
        {
            _activeVisitors.RemoveAll(v => v == null);
            _cleanupTimer = CLEANUP_INTERVAL;
        }

        if (IsVisitingHours() && _activeVisitors.Count < maxConcurrentVisitors)
        {
            _spawnTimer -= Time.deltaTime;

            if (_spawnTimer <= 0f)
            {
                SpawnVisitor();
                _spawnTimer = Random.Range(minSpawnInterval, maxSpawnInterval);
            }
        }
    }

    /// <summary>
    /// Checks if the current time is within the designated visiting hours.
    /// Handles both standard days (e.g., 9AM-5PM) and overnight schedules (e.g., 9PM-5AM).
    /// </summary>
    /// <returns>True if currently within visiting hours.</returns>
    private bool IsVisitingHours()
    {
        // If open=12 and close=5:
        // Valid times are [12...24] OR [0...5]
        float t = dayNightController.currentTime; //

        if (openHour < closeHour)
        {
            // Simple range (e.g., 10 AM to 5 PM)
            return t >= openHour && t < closeHour;
        }
        else
        {
            // Overnight range (e.g., 12 PM to 5 AM)
            return t >= openHour || t < closeHour;
        }
    }

    /// <summary>
    /// Retrieves a visitor from the object pool, positions it at the spawn point, and initializes it.
    /// </summary>
    private void SpawnVisitor()
    {
        // Get visitor from pool instead of instantiating
        VisitorAI ai = _visitorPool.Get();

        if (ai == null)
        {
            DebugLogger.LogWarning("[VisitorSpawner] Pool exhausted, cannot spawn more visitors");
            return;
        }

        // Position at spawn point
        ai.transform.position = spawnPoint.position;
        ai.transform.rotation = spawnPoint.rotation;

        // Initialize visitor
        ai.Initialize(this, dayNightController, despawnPoint.position, openHour, closeHour);
        _activeVisitors.Add(ai);

        DebugLogger.Log($"[VisitorSpawner] Spawned visitor (Active: {_activeVisitors.Count}, Pool: {_visitorPool.ActiveCount}/{_visitorPool.TotalCount})");
    }

    /// <summary>
    /// Returns a visitor to the object pool for reuse
    /// </summary>
    /// <param name="visitor">Visitor to return</param>
    public void ReturnVisitorToPool(VisitorAI visitor)
    {
        if (visitor == null) return;

        _activeVisitors.Remove(visitor);
        _visitorPool.Return(visitor);

        DebugLogger.Log($"[VisitorSpawner] Visitor returned to pool (Active: {_activeVisitors.Count}, Pool: {_visitorPool.ActiveCount}/{_visitorPool.TotalCount})");
    }

}