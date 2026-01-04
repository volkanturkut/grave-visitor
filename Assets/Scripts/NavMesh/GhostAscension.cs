using UnityEngine;
using UnityEngine.AI; // Needed to disable Agent
using System.Collections;
using StarterAssets;

public class GhostAscension : MonoBehaviour
{
    [Header("Visual Effects")]
    public ParticleSystem soulParticle;
    public float scaleDuration = 1.5f;

    [Header("Ascension Movement")]
    public float ghostLiftSpeed = 1.5f; // New: How fast the ghost body rises
    public float particleLiftDuration = 3.0f;
    public float particleLiftSpeed = 2.0f;

    public void StartAscension()
    {
        StartCoroutine(AscensionRoutine());
    }

    private IEnumerator AscensionRoutine()
    {
        // 1. Find Player and Start Waving
        ThirdPersonController player = FindObjectOfType<ThirdPersonController>();
        if (player != null)
        {
            player.LockInput(true);
            player.SetWaveAnimation(true);
        }

        // 2. DISABLE NAVMESH AGENT (Crucial Fix)
        // This un-glues the ghost from the NavMesh height so we can lift it.
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        // 3. Detach Particle
        if (soulParticle != null)
        {
            soulParticle.gameObject.SetActive(true);
            soulParticle.transform.SetParent(null);
            soulParticle.Play();
        }

        // 4. Scale Ghost Down AND Move Up
        Vector3 initialScale = transform.localScale;
        float timer = 0f;

        while (timer < scaleDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / scaleDuration;

            // Shrink
            transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, progress);

            // LIFT: Move the ghost object up while it shrinks
            transform.position += Vector3.up * ghostLiftSpeed * Time.deltaTime;

            yield return null;
        }
        transform.localScale = Vector3.zero;

        // 5. Move Particle Upward
        float liftTimer = 0f;
        while (liftTimer < particleLiftDuration)
        {
            if (soulParticle != null)
                soulParticle.transform.position += Vector3.up * particleLiftSpeed * Time.deltaTime;

            liftTimer += Time.deltaTime;
            yield return null;
        }

        // 6. Cleanup
        if (player != null)
        {
            player.SetWaveAnimation(false);
            player.LockInput(false);
        }

        if (soulParticle != null) Destroy(soulParticle.gameObject);
        Destroy(gameObject);
    }
}