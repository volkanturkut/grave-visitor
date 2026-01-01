using StarterAssets;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string sceneName;

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

        // 2. Start Fade on UI (Opacity 0 -> 100)
        // We do not wait for the fade to finish immediately, we run it in parallel 
        // or we can wait for the specific duration.
        float fadeDuration = 1.0f;
        StartCoroutine(GameUIManager.Instance.FadeToBlackRoutine(fadeDuration));

        // 3. Wait for animation + fade
        yield return new WaitForSeconds(fadeDuration);

        // 4. Load Scene
        SceneManager.LoadScene(sceneName);
    }

    public Transform GetTransform()
    {
        return transform;
    }
}