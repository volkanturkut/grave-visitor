using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(GhostWander))]
public class GhostNPC : MonoBehaviour, IInteractable
{
    [Header("Dialogues")]
    [Tooltip("The dialogue for the FIRST interaction")]
    public DialogueData normalDialogue;

    [Tooltip("The dialogue for the SECOND interaction (Triggers Ascension)")]
    public DialogueData missionEndDialogue;

    [Header("Settings")]
    public GhostAscension ascensionEffect;

    // State tracking: Has the player talked to me once?
    private bool _hasTalkedOnce = false;

    // Components
    private GhostWander _wanderScript;
    private NavMeshAgent _agent;
    private Transform _playerTransform;
    private bool _isTalking;

    private void Start()
    {
        _wanderScript = GetComponent<GhostWander>();
        _agent = GetComponent<NavMeshAgent>();

        // Auto-find ascension script if not assigned manually
        if (ascensionEffect == null) ascensionEffect = GetComponent<GhostAscension>();
    }

    private void Update()
    {
        // Smoothly rotate to face the player while talking
        if (_isTalking && _playerTransform != null)
        {
            Vector3 direction = (_playerTransform.position - transform.position).normalized;
            direction.y = 0; // Keep rotation flat
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
            }
        }
    }

    // Called by PlayerInteract.cs when pressing Interact button
    public void Interact(Transform interactorTransform)
    {
        if (_isTalking) return; // Don't interrupt if already talking

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

        // 2. Stop the Agent physically so it doesn't drift
        if (_agent)
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
        }

        // --- LOGIC: CHOOSE WHICH DIALOGUE TO PLAY ---

        if (!_hasTalkedOnce)
        {
            // --- CASE A: FIRST TIME ---
            // Play Normal Dialogue, NO Ascension
            if (normalDialogue != null)
            {
                DialogueManager.Instance.StartDialogue(normalDialogue, this);
                _hasTalkedOnce = true; // Remember that we have met
            }
            else
            {
                Debug.LogWarning("GhostNPC: 'Normal Dialogue' is not assigned in the Inspector!");
                ResumeWandering();
            }
        }
        else
        {
            // --- CASE B: SECOND TIME (or more) ---
            // Play Mission End Dialogue AND trigger Ascension callback
            if (missionEndDialogue != null)
            {
                // We pass a 'Callback' function (Action) that runs ONLY after the text finishes
                DialogueManager.Instance.StartDialogue(missionEndDialogue, this, () =>
                {
                    if (ascensionEffect != null)
                    {
                        ascensionEffect.StartAscension();
                    }
                    else
                    {
                        Debug.LogError("GhostNPC: You forgot to attach the 'GhostAscension' script!");
                        ResumeWandering(); // Fallback if script is missing
                    }
                });
            }
            else
            {
                Debug.LogWarning("GhostNPC: 'Mission End Dialogue' is not assigned in the Inspector!");
                ResumeWandering();
            }
        }
    }

    // Called by DialogueManager when text closes (unless we Ascended)
    public void ResumeWandering()
    {
        _isTalking = false;

        // 1. Enable Agent
        if (_agent) _agent.isStopped = false;

        // 2. Re-enable Wander Script (it will pick up where it left off)
        if (_wanderScript) _wanderScript.enabled = true;
    }
}