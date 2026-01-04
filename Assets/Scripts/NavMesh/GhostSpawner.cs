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
    public List<GameObject> uniqueGhostPrefabs;

    private List<GameObject> _activeGhosts = new List<GameObject>();

    public void SpawnGhosts()
    {
        if (_activeGhosts.Count > 0) return;

        // 1. Spawn Generics
        if (genericGhostPrefab != null)
        {
            for (int i = 0; i < numberOfGenerics; i++)
            {
                SpawnGhost(genericGhostPrefab);
            }
        }

        // 2. Spawn Uniques
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

    // Inside GhostSpawner.cs

    private void SpawnGhost(GameObject prefabToSpawn)
    {
        Vector3 targetPos = Vector3.zero;
        bool foundPoint = false;

        // 1. Try to find a random point
        for (int i = 0; i < 10; i++)
        {
            if (GetRandomPointOnNavMesh(out targetPos))
            {
                foundPoint = true;
                break;
            }
        }

        // 2. If random failed, try to snap the Center point to the mesh
        if (!foundPoint)
        {
            NavMeshHit hit;
            // Check within 5.0f units of the center for the ground
            if (NavMesh.SamplePosition(graveyardCenter.position, out hit, 5.0f, NavMesh.AllAreas))
            {
                targetPos = hit.position;
            }
            else
            {
                Debug.LogError($"[GhostSpawner] Could not place ghost! GraveyardCenter is too far from NavMesh.");
                return; // Abort spawning to prevent errors
            }
        }

        Quaternion randomRot = Quaternion.Euler(0, Random.Range(0, 360), 0);

        // 3. Instantiate (Agent MUST be disabled in Prefab)
        GameObject newGhost = Instantiate(prefabToSpawn, targetPos, randomRot);

        // 4. Setup Agent safely
        NavMeshAgent agent = newGhost.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            // Double-check: ensure the agent is definitely turned off before moving
            if (agent.enabled) agent.enabled = false;

            // Force position
            newGhost.transform.position = targetPos;

            // NOW enable. Since we used SamplePosition for everything, this is guaranteed valid.
            agent.enabled = true;
        }

        // 5. Initialize Logic
        GhostWander wanderScript = newGhost.GetComponent<GhostWander>();
        if (wanderScript != null)
        {
            wanderScript.Initialize(graveyardCenter.position, graveyardRadius);
        }

        _activeGhosts.Add(newGhost);
    }

    private bool GetRandomPointOnNavMesh(out Vector3 result)
    {
        Vector3 randomDir = Random.insideUnitSphere * graveyardRadius;
        randomDir += graveyardCenter.position;
        NavMeshHit hit;

        if (NavMesh.SamplePosition(randomDir, out hit, graveyardRadius, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }

        result = Vector3.zero;
        return false;
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