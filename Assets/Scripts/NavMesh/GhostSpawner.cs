using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class GhostSpawner : MonoBehaviour
{
    [Header("Graveyard Settings")]
    public Transform graveyardCenter;
    public float graveyardRadius = 15f;

    [Header("Generic Ghosts")]
    public GameObject genericGhostPrefab;
    public int numberOfGenerics = 5;

    [Header("Unique Characters")]
    // Drag your specific ghosts (Ghost1, Ghost2, etc.) here
    public List<GameObject> uniqueGhostPrefabs;

    // Internal list to keep track of living ghosts
    private List<GameObject> _activeGhosts = new List<GameObject>();

    public void SpawnGhosts()
    {
        if (_activeGhosts.Count > 0) return;

        // 1. Spawn Generic Ghosts
        if (genericGhostPrefab != null)
        {
            for (int i = 0; i < numberOfGenerics; i++)
            {
                SpawnGhost(genericGhostPrefab);
            }
        }

        // 2. Spawn Unique Characters (One of each)
        foreach (GameObject uniquePrefab in uniqueGhostPrefabs)
        {
            if (uniquePrefab != null)
            {
                SpawnGhost(uniquePrefab);
            }
        }
    }

    public void DespawnGhosts()
    {
        foreach (GameObject ghost in _activeGhosts)
        {
            if (ghost != null) Destroy(ghost);
        }
        _activeGhosts.Clear();
    }

    private void SpawnGhost(GameObject prefabToSpawn)
    {
        Vector3 randomPoint = GetRandomPointOnNavMesh();
        Quaternion randomRot = Quaternion.Euler(0, Random.Range(0, 360), 0);

        GameObject newGhost = Instantiate(prefabToSpawn, randomPoint, randomRot);

        // Initialize Wander Script so they stay in the graveyard
        GhostWander wanderScript = newGhost.GetComponent<GhostWander>();
        if (wanderScript != null)
        {
            wanderScript.Initialize(graveyardCenter.position, graveyardRadius);
        }

        _activeGhosts.Add(newGhost);
    }

    private Vector3 GetRandomPointOnNavMesh()
    {
        Vector3 randomDir = Random.insideUnitSphere * graveyardRadius;
        randomDir += graveyardCenter.position;
        NavMeshHit hit;

        if (NavMesh.SamplePosition(randomDir, out hit, graveyardRadius, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return graveyardCenter.position;
    }

    private void OnDrawGizmosSelected()
    {
        if (graveyardCenter != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(graveyardCenter.position, graveyardRadius);
        }
    }
}