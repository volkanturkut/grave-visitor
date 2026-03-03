using UnityEngine;
using StarterAssets;

public class PlayerInteract : MonoBehaviour
{
    [Header("Settings")]
    public float interactRange = 3.0f;
    public LayerMask interactLayer; // Set this to "Everything" or "Default" to test

    [Header("Debug")]
    public bool showDebugLines = true;

    private PlayerInputs _input;
    private readonly Collider[] _colliderResults = new Collider[10];

    // Throttling for performance optimization
    private float _nextCheckTime;
    private float _checkInterval = 0.1f;
    private IInteractable _cachedInteractable;

    private void Start()
    {
        _input = GetComponent<PlayerInputs>();
    }

    private void Update()
    {
        // 1. Constantly find the closest grave (now throttled)
        IInteractable target = GetInteractableObject();

        // 2. VISUAL DEBUGGING (Draw lines in Scene View)
        if (showDebugLines && target != null)
        {
            // Draw a RED line from Player to Grave
            Debug.DrawLine(transform.position + Vector3.up, target.GetTransform().position, Color.red);
        }

        // 3. Handle Input
        if (_input.interact)
        {
            _input.interact = false; // Reset the button flag

            if (target != null)
            {
                DebugLogger.Log($"[PlayerInteract] SUCCESS! Interacting with {target.GetTransform().name}");
                target.Interact(transform);
            }
            else
            {
                DebugLogger.LogWarning("[PlayerInteract] FAIL: Interact pressed, but no object found in range/layer.");
            }
        }
    }

    public IInteractable GetInteractableObject()
    {
        // Performance Optimization: Return cached result if interval hasn't passed
        if (Time.time < _nextCheckTime)
        {
            return _cachedInteractable;
        }

        // Update the next check time
        _nextCheckTime = Time.time + _checkInterval;

        // Find everything inside the sphere
        int numFound = Physics.OverlapSphereNonAlloc(transform.position, interactRange, _colliderResults, interactLayer);

        IInteractable closestInteractable = null;
        float closestDistanceSqr = float.MaxValue;

        for (int i = 0; i < numFound; i++)
        {
            // IMPORTANT: Look for script on the object AND its parents
            var interactable = _colliderResults[i].GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                float distSqr = (transform.position - interactable.GetTransform().position).sqrMagnitude;
                if (distSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distSqr;
                    closestInteractable = interactable;
                }
            }
        }

        // Cache the result
        _cachedInteractable = closestInteractable;
        return closestInteractable;
    }

    // Draws the detection range in the Editor
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}