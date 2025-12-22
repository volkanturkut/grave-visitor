using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StarterAssets;

[RequireComponent(typeof(StarterAssetsInputs))]
public class PlayerInteract : MonoBehaviour
{
    private StarterAssetsInputs _input;

    private readonly Collider[] _colliderResults = new Collider[10];

    private void Start()
    {
        _input = GetComponent<StarterAssetsInputs>();
    }

    private void Update()
    {
        if (_input.interact)
        {
            _input.interact = false;
            IInteractable interactable = GetInteractableObject();

            interactable?.Interact(transform);
        }
    }

    public IInteractable GetInteractableObject()
    {
        float interactRange = 3f;
        int numFound = Physics.OverlapSphereNonAlloc(transform.position, interactRange, _colliderResults);

        IInteractable closestInteractable = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < numFound; i++)
        {
            if (_colliderResults[i].TryGetComponent(out IInteractable interactable))
            {
                float dist = Vector3.Distance(transform.position, interactable.GetTransform().position);

                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestInteractable = interactable;
                }
            }
        }

        return closestInteractable;
    }
}