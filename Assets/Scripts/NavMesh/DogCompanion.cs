using UnityEngine;
using UnityEngine.AI;
using StarterAssets;
using Cinemachine;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
public class DogCompanion : MonoBehaviour, IInteractable
{
    [Header("References")]
    public Transform playerTransform;
    public ThirdPersonController playerController;
    public CinemachineVirtualCamera dogCamera;
    public AudioClip barkSound;

    [Header("Settings")]
    public float walkSpeed = 2f;
    public float runSpeed = 5.5f;
    public float stopDistance = 2.5f;
    public float sitDelay = 10f;
    public float rotationSpeed = 5f; // New setting for turn smoothness

    private NavMeshAgent _agent;
    private Animator _animator;
    private AudioSource _audioSource;
    private StarterAssetsInputs _playerInputs;

    private float _sitTimer;
    private bool _isSitting;
    private bool _isDogCamActive;

    // Animator IDs
    private int _animIDSpeed;
    private int _animIDSit;

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
        _audioSource = GetComponent<AudioSource>();

        // IMPORTANT: We handle rotation manually to prevent sliding
        _agent.updateRotation = false;
        _agent.stoppingDistance = stopDistance;

        // Auto-find references
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        if (playerTransform != null)
        {
            playerController = playerTransform.GetComponent<ThirdPersonController>();
            _playerInputs = playerTransform.GetComponent<StarterAssetsInputs>();
        }

        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDSit = Animator.StringToHash("Sit");

        if (dogCamera != null) dogCamera.Priority = 0;
    }

    void Update()
    {
        if (playerTransform == null) return;

        MoveLogic();
        RotationLogic(); // New Function
        SitLogic();
    }

    private void MoveLogic()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // Input Checks
        bool isSprinting = _playerInputs != null && _playerInputs.sprint;
        bool isMovingInput = _playerInputs != null && _playerInputs.move != Vector2.zero;

        // 1. Determine Target Speed
        float targetSpeed = (isSprinting && isMovingInput) ? runSpeed : walkSpeed;

        // 2. Move Agent
        if (distanceToPlayer > stopDistance)
        {
            _agent.isStopped = false;
            _agent.SetDestination(playerTransform.position);
            _agent.speed = targetSpeed;
        }
        else
        {
            _agent.isStopped = true;
            // Note: We do NOT set animator speed to 0 here immediately
            // We let RotationLogic handle it in case we are turning in place
        }
    }

    private void RotationLogic()
    {
        Vector3 targetDirection = Vector3.zero;
        bool isMoving = _agent.velocity.magnitude > 0.1f;
        float animatorSpeed = 0f;

        // Case A: Moving - Look where we are going
        if (isMoving)
        {
            targetDirection = _agent.velocity.normalized;
            animatorSpeed = _agent.velocity.magnitude;
        }
        // Case B: Stopped - Look at the Player
        else
        {
            Vector3 directionToPlayer = playerTransform.position - transform.position;
            // Only try to look if player is slightly away to avoid jitter
            if (directionToPlayer.magnitude > 0.5f)
            {
                targetDirection = directionToPlayer.normalized;
            }
        }

        // Apply Rotation
        if (targetDirection != Vector3.zero)
        {
            targetDirection.y = 0; // Keep dog upright
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

            // Calculate how much we need to turn this frame
            float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);

            // Smoothly rotate
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

            // FOOT SHUFFLE TRICK:
            // If we are NOT moving (velocity is 0) but we ARE turning (angle > 10 degrees),
            // force the walk animation to play slowly so the feet move.
            if (!isMoving && angleDifference > 5f)
            {
                animatorSpeed = 1.0f; // Fake walk speed to make feet shuffle
            }
        }

        // Send final speed to Animator (Smoothly)
        _animator.SetFloat(_animIDSpeed, animatorSpeed, 0.1f, Time.deltaTime);
    }

    private void SitLogic()
    {
        // If moving or turning significantly, reset sit
        if (_animator.GetFloat(_animIDSpeed) > 0.1f)
        {
            _sitTimer = 0f;
            if (_isSitting)
            {
                _isSitting = false;
                _animator.SetBool(_animIDSit, false);
            }
            return;
        }

        // If Player is also stopped, count down
        float playerSpeed = playerController.GetComponent<CharacterController>().velocity.magnitude;
        if (playerSpeed < 0.1f)
        {
            _sitTimer += Time.deltaTime;
            if (_sitTimer >= sitDelay && !_isSitting)
            {
                _isSitting = true;
                _animator.SetBool(_animIDSit, true);
            }
        }
        else
        {
            _sitTimer = 0f;
        }
    }

    // --- IInteractable Implementation ---
    public void Interact(Transform interactorTransform)
    {
        if (dogCamera != null)
        {
            _isDogCamActive = !_isDogCamActive;
            dogCamera.Priority = _isDogCamActive ? 20 : 0;
        }

        if (_audioSource != null && barkSound != null)
        {
            _audioSource.PlayOneShot(barkSound);
        }
    }

    public Transform GetTransform()
    {
        return transform;
    }
}   