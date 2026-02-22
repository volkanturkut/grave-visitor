using StarterAssets;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Interactable door that triggers animation and scene transition
/// </summary>
public class DoorInteractable : MonoBehaviour, IInteractable
{
    // Constants
    private const float FADE_DURATION = 1.0f;

    [SerializeField] private string sceneName;

    /// <summary>
    /// Initiates door opening sequence with animation, fade, and scene load
    /// </summary>
    /// <param name="interactorTransform">Transform of the interacting player</param>
    public void Interact(Transform interactorTransform)
    {
        StartCoroutine(OpenDoorRoutine(interactorTransform));
    }

    private IEnumerator OpenDoorRoutine(Transform player)
    {
        // 1. Trigger Animation
        if (player.TryGetComponent(out ThirdPersonController controller))
        {
            controller.TriggerActionAnimation("OpenDoor");
        }

        // Start fade  to black (runs in parallel with animation)
        StartCoroutine(GameUIManager.Instance.FadeToBlackRoutine(FADE_DURATION));

        // Wait for animation and fade to complete
        yield return new WaitForSeconds(FADE_DURATION);

        // 4. Load Scene
        SceneManager.LoadScene(sceneName);
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