using UnityEngine;

/// <summary>
/// Interactable object that toggles particle effects and lights on/off
/// </summary>
public class ToggleEffectsInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private ParticleSystem targetParticle;
    [SerializeField] private Light targetLight;

    private bool _isOn = true;

    /// <summary>
    /// Toggles the particle system and light on or off
    /// </summary>
    /// <param name="interactorTransform">Transform of the interacting player</param>
    public void Interact(Transform interactorTransform)
    {
        _isOn = !_isOn;

        if (targetParticle != null)
        {
            if (_isOn) targetParticle.Play();
            else targetParticle.Stop();
        }

        if (targetLight != null)
        {
            targetLight.enabled = _isOn;
        }
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