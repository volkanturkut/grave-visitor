using UnityEngine;

public class BedInteractable : MonoBehaviour, IInteractable
{
    public void Interact(Transform interactorTransform)
    {
        GameUIManager.Instance.OpenBedPanel();
    }

    public Transform GetTransform()
    {
        return transform;
    }
}