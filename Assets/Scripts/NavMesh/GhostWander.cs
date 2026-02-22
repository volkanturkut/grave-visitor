using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class GhostWander : MonoBehaviour
{
    private NavMeshAgent _agent;
    private Animator _animator;

    [Header("Flying Settings")]
    public float flyingHeight = 1.5f;
    public float moveSpeed = 2.0f;

    [Header("Wander Settings")]
    public float minWaitTime = 2f;
    public float maxWaitTime = 5f;

    private Vector3 _wanderCenter;
    private float _wanderRadius;
    private bool _isWaiting;
    private float _waitTimer;
    private bool _isInitialized = false;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();

        if (_agent != null)
        {
            _agent.baseOffset = flyingHeight;
            _agent.speed = moveSpeed;
        }

        if (_animator) _animator.speed = Random.Range(0.8f, 1.2f);
    }

    private void Update()
    {
        // --- SAFETY CHECK ---
        // If agent is missing, disabled, or not on NavMesh, DO NOT RUN LOGIC
        if (_agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh) return;

        // Wait until Initialize is called
        if (!_isInitialized) return;

        if (_isWaiting)
        {
            _waitTimer -= Time.deltaTime;
            if (_waitTimer <= 0f)
            {
                _isWaiting = false;
                MoveToRandomPoint();
            }
        }
        else if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
        {
            _isWaiting = true;
            _waitTimer = Random.Range(minWaitTime, maxWaitTime);
        }
    }

    public void Initialize(Vector3 center, float radius)
    {
        _wanderCenter = center;
        _wanderRadius = radius;
        _isInitialized = true;

        // Only move if we are safely on the mesh
        if (_agent != null && _agent.isOnNavMesh)
        {
            MoveToRandomPoint();
        }
    }

    private void MoveToRandomPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * _wanderRadius;
        randomDirection += _wanderCenter;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, _wanderRadius, NavMesh.AllAreas))
        {
            // Final safety check
            if (_agent.isActiveAndEnabled && _agent.isOnNavMesh)
            {
                _agent.SetDestination(hit.position);
            }
        }
    }
}
