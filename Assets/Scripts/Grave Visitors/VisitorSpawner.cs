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
    private int _currentVisitorCount = 0;

    private void Update()
    {
        // Clean up nulls in case visitors were destroyed
        // (A simple way to keep count valid without complex events)
        _currentVisitorCount = CountActiveVisitors();

        if (IsVisitingHours() && _currentVisitorCount < maxConcurrentVisitors)
        {
            _spawnTimer -= Time.deltaTime;

            if (_spawnTimer <= 0f)
            {
                SpawnVisitor();
                _spawnTimer = Random.Range(minSpawnInterval, maxSpawnInterval);
            }
        }
    }

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

    private void SpawnVisitor()
    {
        GameObject newVisitor = Instantiate(visitorPrefab, spawnPoint.position, spawnPoint.rotation);

        // Initialize the AI
        VisitorAI ai = newVisitor.GetComponent<VisitorAI>();
        if (ai != null)
        {
            ai.Initialize(dayNightController, despawnPoint.position);
        }
    }

    private int CountActiveVisitors()
    {
        // This finds all objects with the VisitorAI script
        // For production, a List<VisitorAI> managed by this script is faster, 
        // but this is robust for now.
        return FindObjectsOfType<VisitorAI>().Length;
    }
}