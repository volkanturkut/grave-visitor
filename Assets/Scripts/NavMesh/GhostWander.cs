using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class GhostWander : MonoBehaviour
{
    private NavMeshAgent _agent;
    private Animator _animator;

    [Header("Flying Settings")]
    public float flyingHeight = 1.5f; // How high they float (Y axis)
    public float moveSpeed = 2.0f;

    [Header("Wander Settings")]
    public float minWaitTime = 2f;
    public float maxWaitTime = 5f;

    // These are set automatically by the Spawner
    private Vector3 _wanderCenter;
    private float _wanderRadius;
    private bool _isWaiting;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();

        // 1. MAKE THEM FLY
        // baseOffset visually lifts the mesh while the agent stays on the ground
        _agent.baseOffset = flyingHeight;
        _agent.speed = moveSpeed;

        // Randomize animation speed so they don't all flap wings in sync
        if (_animator) _animator.speed = Random.Range(0.8f, 1.2f);
    }

    private void Update()
    {
        // Check if reached destination
        if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
        {
            if (!_isWaiting)
            {
                StartCoroutine(WaitAndPickNewSpot());
            }
        }
    }

    // Called by the Spawner to tell the ghost where the graveyard is
    public void Initialize(Vector3 center, float radius)
    {
        _wanderCenter = center;
        _wanderRadius = radius;
        MoveToRandomPoint();
    }

    private IEnumerator WaitAndPickNewSpot()
    {
        _isWaiting = true;
        yield return new WaitForSeconds(Random.Range(minWaitTime, maxWaitTime));
        MoveToRandomPoint();
        _isWaiting = false;
    }

    private void MoveToRandomPoint()
    {
        // 2. WANDER ONLY IN GRAVEYARD
        // Pick a random point inside the sphere
        Vector3 randomDirection = Random.insideUnitSphere * _wanderRadius;
        randomDirection += _wanderCenter;

        NavMeshHit hit;
        // Find the closest valid point on the NavMesh
        if (NavMesh.SamplePosition(randomDirection, out hit, _wanderRadius, NavMesh.AllAreas))
        {
            _agent.SetDestination(hit.position);
        }
    }
}