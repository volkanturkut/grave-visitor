using System.Collections;
using UnityEngine;
using UnityEngine.VFX;
using StarterAssets;

public class GraveDirtController : MonoBehaviour, IInteractable
{
    [Header("Dirt Settings")]
    [Tooltip("Time in seconds for the grave to become 100% dirty.")]
    public float fullDirtTime = 1200f;

    [Tooltip("Material Index for the Dirt Material.")]
    public int dirtMaterialIndex = 1;

    [Tooltip("The EXACT name of the item required to clean this.")]
    public string requiredToolName = "Brush";

    [Header("Cleaning Configuration")]
    [Tooltip("How much faster should the cleaning animation play? 1 = Normal, 2 = Double Speed.")]
    public float animationSpeedMultiplier = 2.0f;

    [Tooltip("How long to wait (in seconds) before the dirt disappears and particles show.")]
    public float delayBeforeEffect = 1.5f;

    [Tooltip("How fast the dirt fades away once the effect starts.")]
    public float dirtFadeDuration = 0.5f;

    [Header("VFX")]
    public VisualEffect cleaningVFX;

    // Internal variables
    private Material _dirtMaterialInstance;
    private float _currentDirtAmount = 0f;
    private float _dirtAccumulationRate;
    private bool _isBeingCleaned = false;

    private void Start()
    {
        InitializeMaterial();
        // Slight randomness to dirt accumulation
        float randomness = Random.Range(0.8f, 1.2f);
        _dirtAccumulationRate = (1f / fullDirtTime) * randomness;
    }

    private void Update()
    {
        if (_dirtMaterialInstance == null || _isBeingCleaned) return;

        // Increase dirt over time if not being cleaned
        if (_currentDirtAmount < 1.0f)
        {
            _currentDirtAmount += _dirtAccumulationRate * Time.deltaTime;
            _currentDirtAmount = Mathf.Clamp01(_currentDirtAmount);
            SetDirtAlpha(_currentDirtAmount);
        }
    }

    private void InitializeMaterial()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null && rend.materials.Length > dirtMaterialIndex)
        {
            _dirtMaterialInstance = rend.materials[dirtMaterialIndex];
            SetDirtAlpha(0f);
        }
        else
        {
            Debug.LogError($"[GraveDirtController] Could not find dirt material at index {dirtMaterialIndex} on {name}");
            enabled = false;
        }
    }

    private void SetDirtAlpha(float alpha)
    {
        if (_dirtMaterialInstance != null)
        {
            Color color = _dirtMaterialInstance.GetColor("_BaseColor");
            color.a = alpha;
            _dirtMaterialInstance.SetColor("_BaseColor", color);
        }
    }

    // --- IInteractable Implementation ---
    public Transform GetTransform() { return transform; }

    public void Interact(Transform interactorTransform)
    {
        if (_isBeingCleaned) return;

        InventoryManager inventory = FindObjectOfType<InventoryManager>();

        if (inventory != null)
        {
            ItemData equippedItem = inventory.GetCurrentEquippedItem();

            if (equippedItem != null && equippedItem.itemName == requiredToolName)
            {
                StartCoroutine(CleaningRoutine(interactorTransform));
            }
            else
            {
                Debug.Log($"You need a Brush! (Held: {(equippedItem != null ? equippedItem.itemName : "Nothing")})");
            }
        }
        else
        {
            Debug.LogError("CRITICAL: No InventoryManager found in the scene!");
        }
    }

    private IEnumerator CleaningRoutine(Transform playerTransform)
    {
        _isBeingCleaned = true;

        ThirdPersonController controller = playerTransform.GetComponentInParent<ThirdPersonController>();
        Animator anim = controller != null ? controller.GetComponent<Animator>() : null;
        int rightArmIndex = -1;
        float originalAnimSpeed = 1f;

        if (controller != null && anim != null)
        {
            // 1. Lock Movement
            controller.LockInput(true);

            // 2. Disable Right Arm Layer (to fix animation conflict)
            rightArmIndex = anim.GetLayerIndex("RightArmLayer");
            if (rightArmIndex != -1) anim.SetLayerWeight(rightArmIndex, 0f);

            // 3. Speed up the Animator temporarily
            originalAnimSpeed = anim.speed;
            anim.speed = animationSpeedMultiplier;

            // 4. Trigger the Animation
            controller.TriggerActionAnimation("Clean");
        }

        // 5. WAIT: Do nothing while the animation plays
        // This is the key change: The dirt stays dirty here!
        yield return new WaitForSeconds(delayBeforeEffect);

        // 6. Restore Animation Speed
        if (anim != null) anim.speed = originalAnimSpeed;

        // 7. NOW Play VFX (Particles)
        if (cleaningVFX != null)
        {
            cleaningVFX.gameObject.SetActive(true);
            cleaningVFX.Play();
        }

        // 8. Clean the Dirt (Fast Fade)
        float timer = 0f;
        float startDirt = _currentDirtAmount;

        while (timer < dirtFadeDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / dirtFadeDuration;
            _currentDirtAmount = Mathf.Lerp(startDirt, 0f, progress);
            SetDirtAlpha(_currentDirtAmount);
            yield return null;
        }

        // Finalize Clean
        _currentDirtAmount = 0f;
        SetDirtAlpha(0f);

        if (cleaningVFX != null) cleaningVFX.Stop();

        // 9. Restore Player State
        if (controller != null)
        {
            controller.LockInput(false);

            // Restore Right Arm Layer (Hold the brush again)
            if (rightArmIndex != -1 && anim != null)
            {
                anim.SetLayerWeight(rightArmIndex, 1f);
            }
        }

        _isBeingCleaned = false;
    }
}