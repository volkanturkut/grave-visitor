using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string sceneName;

    public void Interact(Transform interactorTransform)
    {
        SceneManager.LoadScene(sceneName);
    }

    public Transform GetTransform()
    {
        return transform;
    }
}