using UnityEngine;

public class PhoneInteractable : MonoBehaviour, IInteractable
{
    public void Interact(Transform interactorTransform)
    {
        GameUIManager.Instance.OpenPhonePanel();
    }

    public Transform GetTransform()
    {
        return transform;
    }
}