using UnityEngine;

/// <summary>
/// Interactable object that opens the bed/sleep interface when activated
/// </summary>
public class BedInteractable : MonoBehaviour, IInteractable
{
    /// <summary>
    /// Opens the bed panel UI for sleeping/saving
    /// </summary>
    /// <param name="interactorTransform">Transform of the interacting player</param>
    public void Interact(Transform interactorTransform)
    {
        GameUIManager.Instance.OpenBedPanel();
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