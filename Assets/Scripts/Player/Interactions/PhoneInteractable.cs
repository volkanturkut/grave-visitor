using UnityEngine;

/// <summary>
/// Interactable object that opens the phone/shop interface when activated
/// </summary>
public class PhoneInteractable : MonoBehaviour, IInteractable
{
    /// <summary>
    /// Opens the phone panel UI for shop/upgrades
    /// </summary>
    /// <param name="interactorTransform">Transform of the interacting player</param>
    public void Interact(Transform interactorTransform)
    {
        GameUIManager.Instance.OpenPhonePanel();
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