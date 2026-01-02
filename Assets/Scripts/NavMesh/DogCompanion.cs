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
    public float rotationSpeed = 5f;

    [Tooltip("How directly must the player face the dog? 0.5 is approx 60 degrees.")]
    public float interactFaceThreshold = 0.5f; // NEW SETTING

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
            if (directionToPlayer.magnitude > 0.5f)
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

            if (!isMoving && angleDifference > 5f)
            {
                animatorSpeed = 1.0f;
            }
        }

        _animator.SetFloat(_animIDSpeed, animatorSpeed, 0.1f, Time.deltaTime);
    }

    private void SitLogic()
    {
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

    // --- UPDATED INTERACT METHOD ---
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

    public Transform GetTransform()
    {
        return transform;
    }
}