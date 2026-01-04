using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class VisitorAI : MonoBehaviour
{
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

    // UPDATED: Now takes open/close hours to sync with Spawner
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
        _agent.stoppingDistance = 0.5f;

        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDVisiting = Animator.StringToHash("IsVisiting");

        StartCoroutine(VisitorLoopRoutine());
    }

    private void Update()
    {
        // 1. Sync Animation
        _animator.SetFloat(_animIDSpeed, _agent.velocity.magnitude);
        _animator.SetFloat("MotionSpeed", 1f);

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

            yield return new WaitForSeconds(Random.Range(3f, wanderWaitTime));

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
        while (_agent.pathPending || _agent.remainingDistance > 2.0f)
        {
            yield return null;
        }

        Destroy(gameObject);
    }

    private Vector3 GetRandomNavMeshPosition(Vector3 origin, float dist)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;
        NavMeshHit navHit;
        NavMesh.SamplePosition(randDirection, out navHit, dist, NavMesh.AllAreas);
        return navHit.position;
    }

    private GravePoint GetRandomEmptyGrave()
    {
        var emptyGraves = new List<GravePoint>();
        foreach (var grave in GravePoint.AllGraves)
        {
            if (!grave.IsOccupied) emptyGraves.Add(grave);
        }

        if (emptyGraves.Count == 0) return null;

        GravePoint selected = emptyGraves[Random.Range(0, emptyGraves.Count)];
        selected.SetOccupied(true);
        return selected;
    }
}