using UnityEngine;

/// <summary>
/// Interactable object that opens the info/notification panel when activated
/// </summary>
public class InfoPanelInteractable : MonoBehaviour, IInteractable
{
    /// <summary>
    /// Opens the simple info panel UI
    /// </summary>
    /// <param name="interactorTransform">Transform of the interacting player</param>
    public void Interact(Transform interactorTransform)
    {
        GameUIManager.Instance.OpenSimplePanel();
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