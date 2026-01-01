using UnityEngine;

public class ToggleEffectsInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private ParticleSystem targetParticle;
    [SerializeField] private Light targetLight;

    private bool _isOn = true;

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

    public Transform GetTransform()
    {
        return transform;
    }
}