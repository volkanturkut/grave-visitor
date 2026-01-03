using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(GhostWander))]
public class GhostNPC : MonoBehaviour, IInteractable
{
    [Header("Dialogue Config")]
    public DialogueData conversation;

    private GhostWander _wanderScript;
    private NavMeshAgent _agent;
    private Animator _animator;
    private Transform _playerTransform;
    private bool _isTalking;

    private void Start()
    {
        _wanderScript = GetComponent<GhostWander>();
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        // If talking, rotate to face the player smoothly
        if (_isTalking && _playerTransform != null)
        {
            Vector3 direction = (_playerTransform.position - transform.position).normalized;
            direction.y = 0; // Keep rotation flat
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    // Required by IInteractable interface
    public void Interact(Transform interactorTransform)
    {
        if (_isTalking) return;

        _playerTransform = interactorTransform;
        StartTalking();
    }

    // Required by IInteractable interface
    public Transform GetTransform()
    {
        return transform;
    }

    private void StartTalking()
    {
        _isTalking = true;

        // 1. Stop the Ghost Wander Script
        if (_wanderScript) _wanderScript.enabled = false;

        // 2. Stop the Agent physically
        if (_agent)
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
        }

        // 3. Optional: Play Idle Animation (stop flying/moving anim)
        // if (_animator) _animator.SetFloat("Speed", 0f);

        // 4. Send Data to Manager
        DialogueManager.Instance.StartDialogue(conversation, this);
    }

    public void ResumeWandering()
    {
        _isTalking = false;

        // 1. Enable Agent
        if (_agent) _agent.isStopped = false;

        // 2. Re-enable Wander Script (it will pick up where it left off)
        if (_wanderScript) _wanderScript.enabled = true;
    }
}