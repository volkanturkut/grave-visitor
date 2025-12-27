using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

[RequireComponent(typeof(StarterAssetsInputs))]
public class PlayerInteraction : MonoBehaviour
{
    [Header("Settings")]
    public float interactRange = 3f;
    public float interactAngle = 60f; // NEW: Field of View for pickup
    public LayerMask itemLayer;
    public Transform playerCamera; // Ensure this is assigned in Inspector

    [Header("References")]
    public InventoryManager inventoryManager;

    private WorldItem currentTargetItem;
    private StarterAssetsInputs _input;

    // Cache array for non-alloc physics
    private readonly Collider[] _hitColliders = new Collider[10];

    private void Start()
    {
        _input = GetComponent<StarterAssetsInputs>();
        if (playerCamera == null) playerCamera = transform; // Fallback if camera not assigned
    }

    private void Update()
    {
        DetectNearbyItems();

        if (_input != null && _input.pickup)
        {
            PickupItem();
            _input.pickup = false;
        }
    }

    void DetectNearbyItems()
    {
        // Use camera position/forward for Raycasting if available, otherwise player body
        Vector3 origin = playerCamera != null ? playerCamera.position : transform.position;
        Vector3 forward = playerCamera != null ? playerCamera.forward : transform.forward;

        Debug.DrawRay(origin, forward * interactRange, Color.red);

        int numHits = Physics.OverlapSphereNonAlloc(transform.position, interactRange, _hitColliders, itemLayer);

        WorldItem bestTarget = null;
        float closestDist = float.MaxValue;
        float bestAngle = interactAngle; // Start with max allowed angle

        for (int i = 0; i < numHits; i++)
        {
            var hit = _hitColliders[i];

            if (!hit.TryGetComponent(out WorldItem itemScript))
            {
                continue;
            }

            Vector3 directionToItem = (hit.transform.position - origin).normalized;
            float dist = Vector3.Distance(origin, hit.transform.position);
            float angle = Vector3.Angle(forward, directionToItem);

            // Logic: 
            // 1. Must be within range
            // 2. Must be within the viewing angle (in front of player)
            // 3. Prioritize items closer to the center of the screen (smaller angle)

            if (angle < interactAngle)
            {
                // We prioritize looking directly at the item (low angle) over pure distance
                // But we still want it reasonably close.
                // Simple score: combine distance and angle.

                if (dist < closestDist)
                {
                    closestDist = dist;
                    bestTarget = itemScript;
                }
            }
        }

        if (bestTarget != currentTargetItem)
        {
            if (currentTargetItem != null) currentTargetItem.ShowInfo(false);
            currentTargetItem = bestTarget;
            if (currentTargetItem != null)
            {
                currentTargetItem.ShowInfo(true);
            }
        }
    }

    void PickupItem()
    {
        if (currentTargetItem != null)
        {
            bool added = inventoryManager.AddItem(currentTargetItem.itemData, currentTargetItem.quantity);
            if (added)
            {
                Destroy(currentTargetItem.gameObject);
                currentTargetItem = null;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}