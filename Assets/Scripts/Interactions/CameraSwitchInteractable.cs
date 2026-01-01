using UnityEngine;
using Cinemachine;

public class CameraSwitchInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private CinemachineVirtualCamera cameraToSwitchTo;
    [SerializeField] private int highPriority = 20;
    [SerializeField] private int lowPriority = 10;

    private bool _isActive = false;

    public void Interact(Transform interactorTransform)
    {
        _isActive = !_isActive;

        if (_isActive)
        {
            cameraToSwitchTo.Priority = highPriority;
        }
        else
        {
            cameraToSwitchTo.Priority = lowPriority;
        }
    }

    public Transform GetTransform()
    {
        return transform;
    }
}