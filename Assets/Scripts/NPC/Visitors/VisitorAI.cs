using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// AI controller for cemetery visitors - handles wandering, grave visiting, and departure
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class VisitorAI : MonoBehaviour, IPoolable
{
    // Constants
    private const float STOPPING_DISTANCE = 0.5f;
    private const float MIN_WANDER_WAIT = 3f;
    private const float DESPAWN_DISTANCE = 2.0f;
    private const float MOTION_SPEED = 1f;

    private NavMeshAgent _agent;
    private Animator _animator;
    private DayNightController _timeController;

    [Header("Settings")]
    public float minVisitDuration = 10f;
    public float maxVisitDuration = 30f;
    public float wanderRadius = 10f;
    public float wanderWaitTime = 5f;

    // Time settings passed from Spawner
    private float _openHour;
    private float _closeHour;

    private GravePoint _targetGrave;
    private Vector3 _despawnPoint;
    private bool _isLeaving = false;

    // Animation IDs
    private int _animIDSpeed;
    private int _animIDVisiting;

    /// <summary>
    /// Initializes the visitor with time controller and spawn/despawn settings
    /// </summary>
    /// <param name="timeController">Day/night cycle controller</param>
    /// <param name="despawnPos">Position where visitor will leave</param>
    /// <param name="openTime">Hour when visiting starts</param>
    /// <param name="closeTime">Hour when visiting ends</param>
    public void Initialize(DayNightController timeController, Vector3 despawnPos, float openTime, float closeTime)
    {
        _timeController = timeController;
        _despawnPoint = despawnPos;
        _openHour = openTime;
        _closeHour = closeTime;
    }

    private void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
        _agent.stoppingDistance = STOPPING_DISTANCE;

        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDVisiting = Animator.StringToHash("IsVisiting");

        StartCoroutine(VisitorLoopRoutine());
    }

    private void Update()
    {
        // 1. Sync Animation
        _animator.SetFloat(_animIDSpeed, _agent.velocity.magnitude);
        _animator.SetFloat("MotionSpeed", MOTION_SPEED);

        // 2. UPDATED: Dynamic Leave Check
        // If we are NOT leaving yet, check if visiting hours are over
        if (!_isLeaving && _timeController != null)
        {
            if (!IsVisitingHours())
            {
                // Time is up! Force leave immediately.
                StopAllCoroutines();
                StartCoroutine(LeaveRoutine());
            }
        }
    }

    // Helper to check if we are allowed to stay
    private bool IsVisitingHours()
    {
        float t = _timeController.currentTime;

        if (_openHour < _closeHour)
        {
            // Standard day shift (e.g., 08 to 17)
            return t >= _openHour && t < _closeHour;
        }
        else
        {
            // Night shift (e.g., 12 to 05)
            return t >= _openHour || t < _closeHour;
        }
    }

    private IEnumerator VisitorLoopRoutine()
    {
        while (true)
        {
            // STATE 1: Wander
            Vector3 wanderPos = GetRandomNavMeshPosition(transform.position, wanderRadius);
            _agent.SetDestination(wanderPos);

            while (_agent.pathPending || _agent.remainingDistance > _agent.stoppingDistance)
            {
                yield return null;
            }

            yield return new WaitForSeconds(Random.Range(MIN_WANDER_WAIT, wanderWaitTime));

            // STATE 2: Try Visit Grave
            _targetGrave = GetRandomEmptyGrave();

            if (_targetGrave != null)
            {
                _agent.SetDestination(_targetGrave.GetPosition());

                while (_agent.pathPending || _agent.remainingDistance > _agent.stoppingDistance)
                {
                    yield return null;
                }

                if (_targetGrave.IsOccupied)
                {
                    _animator.SetBool(_animIDVisiting, true);
                    yield return new WaitForSeconds(Random.Range(minVisitDuration, maxVisitDuration));
                    _animator.SetBool(_animIDVisiting, false);
                    _targetGrave.SetOccupied(false);
                    _targetGrave = null;
                }
            }
        }
    }

    private IEnumerator LeaveRoutine()
    {
        _isLeaving = true;

        if (_targetGrave != null)
        {
            _targetGrave.SetOccupied(false);
            _animator.SetBool(_animIDVisiting, false);
        }

        _agent.SetDestination(_despawnPoint);

        // Wait until close to exit
        while (_agent.pathPending || _agent.remainingDistance > DESPAWN_DISTANCE)
        {
            yield return null;
        }

        // Return to pool instead of destroying
        ReturnToPool();
    }

    /// <summary>
    /// Returns this visitor to the object pool for reuse
    /// </summary>
    private void ReturnToPool()
    {
        // Clean up state
        if (_targetGrave != null)
        {
            _targetGrave.SetOccupied(false);
            _targetGrave = null;
        }

        // Find the spawner and return to its pool
        VisitorSpawner spawner = FindObjectOfType<VisitorSpawner>();
        if (spawner != null)
        {
            spawner.ReturnVisitorToPool(this);
        }
        else
        {
            // Fallback: just disable
            DebugLogger.LogWarning("[VisitorAI] Could not find spawner to return to pool");
            gameObject.SetActive(false);
        }
    }

    #region IPoolable Implementation

    /// <summary>
    /// Called when visitor is spawned from pool
    /// </summary>
    public void OnSpawnFromPool()
    {
        _isLeaving = false;
        _targetGrave = null;

        // Restart the visitor loop
        StopAllCoroutines();
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(VisitorLoopRoutine());
        }
    }

    /// <summary>
    /// Called when visitor is returned to pool
    /// </summary>
    public void OnReturnToPool()
    {
        // Stop all activity
        StopAllCoroutines();

        // Reset state
        _isLeaving = false;
        if (_targetGrave != null)
        {
            _targetGrave.SetOccupied(false);
            _targetGrave = null;
        }

        // Reset animator
        if (_animator != null)
        {
            _animator.SetBool(_animIDVisiting, false);
            _animator.SetFloat(_animIDSpeed, 0f);
        }

        // Stop agent
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }
    }

    #endregion

    private Vector3 GetRandomNavMeshPosition(Vector3 origin, float dist)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;
        NavMeshHit navHit;
        NavMesh.SamplePosition(randDirection, out navHit, dist, NavMesh.AllAreas);
        return navHit.position;
    }

    // Shared buffer to avoid allocations per call
    private static List<GravePoint> _searchBuffer = new List<GravePoint>();

    private GravePoint GetRandomEmptyGrave()
    {
        _searchBuffer.Clear();
        foreach (var grave in GravePoint.AllGraves)
        {
            if (!grave.IsOccupied) _searchBuffer.Add(grave);
        }

        if (_searchBuffer.Count == 0) return null;

        GravePoint selected = _searchBuffer[Random.Range(0, _searchBuffer.Count)];
        selected.SetOccupied(true);
        return selected;
    }
}