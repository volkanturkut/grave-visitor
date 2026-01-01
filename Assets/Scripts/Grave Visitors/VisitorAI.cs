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
    public float wanderRadius = 10f; // How far they can roam
    public float wanderWaitTime = 5f; // How long they wait between moving

    private GravePoint _targetGrave;
    private Vector3 _despawnPoint;
    private bool _isLeaving = false;

    // Animation IDs
    private int _animIDSpeed;
    private int _animIDVisiting; // Bool: IsVisiting

    public void Initialize(DayNightController timeController, Vector3 despawnPos)
    {
        _timeController = timeController;
        _despawnPoint = despawnPos;
    }

    private void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();

        // Ensure stopping distance is close enough to look good but not inside the object
        _agent.stoppingDistance = 0.5f;

        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDVisiting = Animator.StringToHash("IsVisiting"); //

        // Start the Main Loop
        StartCoroutine(VisitorLoopRoutine());
    }

    private void Update()
    {
        // 1. Sync Animation Speed
        _animator.SetFloat(_animIDSpeed, _agent.velocity.magnitude);

        // Optional: Ensure MotionSpeed is set for StarterAssets
        _animator.SetFloat("MotionSpeed", 1f);

        // 2. Force Leave Check (5 AM - 11 AM)
        if (!_isLeaving && _timeController != null)
        {
            float t = _timeController.currentTime; //
            if (t >= 5f && t < 11f)
            {
                StopAllCoroutines();
                StartCoroutine(LeaveRoutine());
            }
        }
    }

    private IEnumerator VisitorLoopRoutine()
    {
        // Loop forever until the Update() function forces them to leave
        while (true)
        {
            // STATE 1: Wander / Loiter
            // Pick a random point near current position
            Vector3 wanderPos = GetRandomNavMeshPosition(transform.position, wanderRadius);
            _agent.SetDestination(wanderPos);

            // Wait until arrived at wander spot
            while (_agent.pathPending || _agent.remainingDistance > _agent.stoppingDistance)
            {
                yield return null;
            }

            // Wait/Idle for a bit (Simulates looking around or waiting)
            yield return new WaitForSeconds(Random.Range(3f, wanderWaitTime));

            // STATE 2: Try to find a Grave
            _targetGrave = GetRandomEmptyGrave();

            if (_targetGrave != null)
            {
                // Found a spot! Go there.
                _agent.SetDestination(_targetGrave.GetPosition());

                // Wait until arrived
                while (_agent.pathPending || _agent.remainingDistance > _agent.stoppingDistance)
                {
                    yield return null;
                }

                // Double check it's still ours (safety check)
                if (_targetGrave.IsOccupied)
                {
                    // STATE 3: Visit (Sit/Pray)
                    _animator.SetBool(_animIDVisiting, true);
                    yield return new WaitForSeconds(Random.Range(minVisitDuration, maxVisitDuration));
                    _animator.SetBool(_animIDVisiting, false);

                    // Done visiting, release the grave
                    _targetGrave.SetOccupied(false);
                    _targetGrave = null;
                }
            }
            // If no grave was found (null), the loop just restarts
            // causing them to wander to a new random spot (State 1)
        }
    }

    private IEnumerator LeaveRoutine()
    {
        _isLeaving = true;
        // Release grave if we are holding one
        if (_targetGrave != null)
        {
            _targetGrave.SetOccupied(false);
            _animator.SetBool(_animIDVisiting, false);
        }

        _agent.SetDestination(_despawnPoint);

        // Wait until reached exit
        while (_agent.pathPending || _agent.remainingDistance > 2.0f)
        {
            yield return null;
        }

        Destroy(gameObject);
    }

    // --- HELPER FUNCTIONS ---

    private Vector3 GetRandomNavMeshPosition(Vector3 origin, float dist)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;
        NavMeshHit navHit;
        // Sample the NavMesh to find a valid point close to the random sphere
        NavMesh.SamplePosition(randDirection, out navHit, dist, NavMesh.AllAreas);
        return navHit.position;
    }

    private GravePoint GetRandomEmptyGrave()
    {
        var emptyGraves = new List<GravePoint>();

        // Find all graves that are NOT occupied
        foreach (var grave in GravePoint.AllGraves) //
        {
            if (!grave.IsOccupied) emptyGraves.Add(grave);
        }

        if (emptyGraves.Count == 0) return null;

        // Pick one randomly
        GravePoint selected = emptyGraves[Random.Range(0, emptyGraves.Count)];

        // Mark it as occupied immediately so others don't pick it
        selected.SetOccupied(true);
        return selected;
    }
}