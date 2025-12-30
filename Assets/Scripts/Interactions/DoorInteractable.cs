using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string sceneName;

    public void Interact(Transform interactorTransform)
    {
        // Pass the interactor (player) to a coroutine
        StartCoroutine(OpenDoorRoutine(interactorTransform));
    }

    private IEnumerator OpenDoorRoutine(Transform player)
    {
        // 1. Trigger Animation on player
        if (player.TryGetComponent(out ThirdPersonController controller))
        {
            controller.TriggerActionAnimation("OpenDoor");
        }

        // 2. Wait for animation to finish (e.g., 1 second)
        yield return new WaitForSeconds(1.0f);

        // 3. Load Scene
        SceneManager.LoadScene(sceneName);
    }

    public Transform GetTransform()
    {
        return transform;
    }
}