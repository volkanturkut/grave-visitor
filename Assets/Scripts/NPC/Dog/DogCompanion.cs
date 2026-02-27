using UnityEngine;
using UnityEngine.AI;
using StarterAssets;
using Cinemachine;

/// <summary>
/// Dog companion AI that follows the player, sits when idle, and can be petted for camera interaction
/// </summary>
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
    public float rotationSpeed = 5f;

    [Tooltip("How directly must the player face the dog? 0.5 is approx 60 degrees.")]
    public float interactFaceThreshold = 0.5f;

    // Constants
    private const float DIRECTION_CHECK_DISTANCE = 0.5f;
    private const float ROTATION_ANGLE_THRESHOLD = 5f;
    private const float MOVEMENT_THRESHOLD = 0.1f;
    private const float IDLE_SPEED_THRESHOLD = 0.1f;
    private const float ANIMATOR_BLEND_TIME = 0.1f;
    private const float MOVING_ANIMATOR_SPEED = 1.0f;

    private NavMeshAgent _agent;
    private Animator _animator;
    private AudioSource _audioSource;
    private PlayerInputs _playerInputs;
    private CharacterController _playerCharacterController;

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

        _agent.updateRotation = false;
        _agent.stoppingDistance = stopDistance;

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        if (playerTransform != null)
        {
            playerController = playerTransform.GetComponent<ThirdPersonController>();
            _playerInputs = playerTransform.GetComponent<PlayerInputs>();
            _playerCharacterController = playerTransform.GetComponent<CharacterController>();
        }

        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDSit = Animator.StringToHash("Sit");

        if (dogCamera != null) dogCamera.Priority = 0;
    }

    void Update()
    {
        if (playerTransform == null) return;

        MoveLogic();
        RotationLogic();
        SitLogic();
    }

    private void MoveLogic()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        bool isSprinting = _playerInputs != null && _playerInputs.sprint;
        bool isMovingInput = _playerInputs != null && _playerInputs.move != Vector2.zero;
        float targetSpeed = (isSprinting && isMovingInput) ? runSpeed : walkSpeed;

        if (distanceToPlayer > stopDistance)
        {
            _agent.isStopped = false;
            _agent.SetDestination(playerTransform.position);
            _agent.speed = targetSpeed;
        }
        else
        {
            _agent.isStopped = true;
        }
    }

    private void RotationLogic()
    {
        Vector3 targetDirection = Vector3.zero;
        bool isMoving = _agent.velocity.magnitude > 0.1f;
        float animatorSpeed = 0f;

        if (isMoving)
        {
            targetDirection = _agent.velocity.normalized;
            animatorSpeed = _agent.velocity.magnitude;
        }
        else
        {
            Vector3 directionToPlayer = playerTransform.position - transform.position;
            if (directionToPlayer.magnitude > DIRECTION_CHECK_DISTANCE)
            {
                targetDirection = directionToPlayer.normalized;
            }
        }

        if (targetDirection != Vector3.zero)
        {
            targetDirection.y = 0;
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

            if (!isMoving && angleDifference > ROTATION_ANGLE_THRESHOLD)
            {
                animatorSpeed = MOVING_ANIMATOR_SPEED;
            }
        }

        _animator.SetFloat(_animIDSpeed, animatorSpeed, ANIMATOR_BLEND_TIME, Time.deltaTime);
    }

    private void SitLogic()
    {
        if (_animator.GetFloat(_animIDSpeed) > IDLE_SPEED_THRESHOLD)
        {
            _sitTimer = 0f;
            if (_isSitting)
            {
                _isSitting = false;
                _animator.SetBool(_animIDSit, false);
            }
            return;
        }

        float playerSpeed = _playerCharacterController != null ? _playerCharacterController.velocity.magnitude : 0f;
        if (playerSpeed < MOVEMENT_THRESHOLD)
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

    /// <summary>
    /// Handles player interaction - toggles dog camera and plays bark sound if player is facing the dog
    /// </summary>
    /// <param name="interactorTransform">Transform of the interacting player</param>
    public void Interact(Transform interactorTransform)
    {
        // 1. Calculate direction from Player to Dog
        Vector3 dirToDog = (transform.position - interactorTransform.position).normalized;

        // 2. Get Player's Forward direction
        Vector3 playerForward = interactorTransform.forward;

        // 3. Ignore Height (Y axis) for a fair check on uneven ground
        dirToDog.y = 0;
        playerForward.y = 0;

        // 4. Dot Product Check
        // 1.0 means looking exactly at dog. 0.0 means looking 90 degrees away.
        // 0.5f is roughly a 60-degree cone in front of the player.
        if (Vector3.Dot(playerForward.normalized, dirToDog.normalized) < interactFaceThreshold)
        {
            return; // Player is not facing the dog! Ignore interaction.
        }

        // --- Interaction Logic ---
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

    /// <summary>
    /// Gets the transform of this interactable object
    /// </summary>
    /// <returns>Transform component</returns>
    public Transform GetTransform()
    {
        return transform;
    }
}