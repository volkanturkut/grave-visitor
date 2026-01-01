using UnityEngine;

public class GenericPanelInteractable : MonoBehaviour, IInteractable
{
    public void Interact(Transform interactorTransform)
    {
        GameUIManager.Instance.OpenSimplePanel();
    }

    public Transform GetTransform()
    {
        return transform;
    }
}