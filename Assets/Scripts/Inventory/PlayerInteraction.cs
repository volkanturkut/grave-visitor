using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;
using System.Collections;

[RequireComponent(typeof(StarterAssetsInputs))]
public class PlayerInteraction : MonoBehaviour
{
    [Header("Settings")]
    public float interactRange = 3f;
    public float interactAngle = 60f;
    public LayerMask itemLayer;
    public Transform playerCamera;

    [Header("Animation Sync")]
    [Tooltip("Time in seconds WHEN the item should disappear from ground (e.g. when hand touches it).")]
    public float pickupGrabTime = 0.5f;

    [Tooltip("TOTAL length of the pickup animation clip. Player is locked until this time passes.")]
    public float pickupTotalDuration = 1.5f;

    [Header("References")]
    public InventoryManager inventoryManager;

    private WorldItem currentTargetItem;
    private StarterAssetsInputs _input;
    private ThirdPersonController _controller;
    private readonly Collider[] _hitColliders = new Collider[10];
    private bool isPickingUp = false;

    private void Start()
    {
        _input = GetComponent<StarterAssetsInputs>();
        _controller = GetComponent<ThirdPersonController>();
        if (playerCamera == null) playerCamera = transform;
    }

    private void Update()
    {
        if (!isPickingUp) DetectNearbyItems();

        if (_input != null && _input.pickup)
        {
            PickupItem();
            _input.pickup = false;
        }
    }

    void DetectNearbyItems()
    {
        Vector3 origin = playerCamera != null ? playerCamera.position : transform.position;
        Vector3 forward = playerCamera != null ? playerCamera.forward : transform.forward;
        int numHits = Physics.OverlapSphereNonAlloc(transform.position, interactRange, _hitColliders, itemLayer);

        WorldItem bestTarget = null;
        float closestDist = float.MaxValue;

        for (int i = 0; i < numHits; i++)
        {
            var hit = _hitColliders[i];
            if (!hit.TryGetComponent(out WorldItem itemScript)) continue;

            Vector3 directionToItem = (hit.transform.position - origin).normalized;
            float dist = Vector3.Distance(origin, hit.transform.position);
            float angle = Vector3.Angle(forward, directionToItem);

            if (angle < interactAngle && dist < closestDist)
            {
                closestDist = dist;
                bestTarget = itemScript;
            }
        }

        if (bestTarget != currentTargetItem)
        {
            if (currentTargetItem != null) currentTargetItem.ShowInfo(false);
            currentTargetItem = bestTarget;
            if (currentTargetItem != null) currentTargetItem.ShowInfo(true);
        }
    }

    void PickupItem()
    {
        if (currentTargetItem != null && !isPickingUp)
        {
            StartCoroutine(PickupRoutine());
        }
    }

    private IEnumerator PickupRoutine()
    {
        isPickingUp = true;

        // 1. Lock Movement IMMEDIATELY
        if (_controller) _controller.LockInput(true);

        // 2. Trigger Animation
        if (_controller) _controller.TriggerActionAnimation("Pickup");

        // 3. Wait until the hand visually touches the object
        yield return new WaitForSeconds(pickupGrabTime);

        // 4. Logic: Add Item to Inventory / Destroy World Object
        if (currentTargetItem != null)
        {
            bool added = inventoryManager.AddItem(currentTargetItem.itemData, currentTargetItem.quantity);
            if (added)
            {
                Destroy(currentTargetItem.gameObject);
                currentTargetItem = null;
            }
        }

        // 5. Calculate remaining time to wait (Total - GrabTime)
        float remainingTime = pickupTotalDuration - pickupGrabTime;
        if (remainingTime > 0) yield return new WaitForSeconds(remainingTime);

        // 6. Unlock Movement ONLY after full animation
        if (_controller) _controller.LockInput(false);

        isPickingUp = false;
    }
}