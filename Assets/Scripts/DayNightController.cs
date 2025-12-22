using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DayNightController : MonoBehaviour
{
    [System.Serializable]
    public struct SkyPhase
    {
        public string phaseName;
        [Range(0, 24)] public float hour;
        public Cubemap skyTexture;
        public Color tintColor;
    }

    [Header("Time Settings")]
    public float realSecondsPerInterval = 7f;
    [Range(0, 24)] public float currentTime = 5f;

    [Header("Skybox Settings")]
    public Material skyboxMaterial;
    public List<SkyPhase> skyPhases;

    [Header("Cloud Animation")]
    [Tooltip("Controls the Tilt (Up/Down) of clouds over time. Set keys between 30 and 50.")]
    public AnimationCurve skyTiltCurve;
    [Tooltip("How fast the clouds spin (Wind speed).")]
    public float skySpinSpeed = 0.5f;
    private float currentSpin = 0f;

    [Header("Sun & Moon")]
    public Light sunLight;
    public Light moonLight;
    public Gradient sunColor;
    public AnimationCurve sunIntensity;
    public Gradient moonColor;
    public AnimationCurve moonIntensity;

    [Header("Ambient")]
    public Gradient ambientColor;

    [Header("Rotation Settings")]
    public float fixedXRotation = 50f;
    public float fixedZRotation = 0f;
    public float vibrationAmount = 2.0f;
    public float vibrationSpeed = 1.0f;

    [Header("UI")]
    public TextMeshProUGUI timeDisplay;

    private float timeMultiplier;

    private void Start()
    {
        timeMultiplier = 1f / (realSecondsPerInterval * 6f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        skyPhases.Sort((p1, p2) => p1.hour.CompareTo(p2.hour));
    }

    private void Update()
    {
        currentTime += Time.deltaTime * timeMultiplier;
        if (currentTime >= 24f) currentTime %= 24f;

        UpdateSkyboxBlend();
        UpdateLighting();
        UpdateRotation();
        UpdateUI();
    }

    private void UpdateSkyboxBlend()
    {
        if (skyboxMaterial == null || skyPhases.Count < 2) return;

        // 1. Calculate Phase Blending (Existing Logic)
        SkyPhase currentPhase = skyPhases[0];
        SkyPhase nextPhase = skyPhases[0];

        for (int i = 0; i < skyPhases.Count; i++)
        {
            if (currentTime >= skyPhases[i].hour)
            {
                currentPhase = skyPhases[i];
                if (i + 1 < skyPhases.Count) nextPhase = skyPhases[i + 1];
                else nextPhase = skyPhases[0];
            }
        }

        float duration;
        float timeSincePhaseStart;

        if (nextPhase.hour > currentPhase.hour)
        {
            duration = nextPhase.hour - currentPhase.hour;
            timeSincePhaseStart = currentTime - currentPhase.hour;
        }
        else
        {
            duration = (24f - currentPhase.hour) + nextPhase.hour;
            timeSincePhaseStart = currentTime >= currentPhase.hour ?
                                  currentTime - currentPhase.hour :
                                  (24f - currentPhase.hour) + currentTime;
        }

        float blend = Mathf.Clamp01(timeSincePhaseStart / duration);

        // 2. Apply Textures & Tint
        skyboxMaterial.SetTexture("_TexA", currentPhase.skyTexture);
        skyboxMaterial.SetTexture("_TexB", nextPhase.skyTexture);
        skyboxMaterial.SetFloat("_Blend", blend);
        skyboxMaterial.SetColor("_Tint", Color.Lerp(currentPhase.tintColor, nextPhase.tintColor, blend));

        // A. Tilt (Up/Down movement based on Hour)
        float tiltValue = skyTiltCurve.Evaluate(currentTime / 24f);
        skyboxMaterial.SetFloat("_Tilt", tiltValue);

        // B. Spin (Wind movement)
        // Increases continuously so clouds drift sideways
        currentSpin += Time.deltaTime * skySpinSpeed;
        skyboxMaterial.SetFloat("_Rotation", currentSpin % 360f);
    }

    private void UpdateLighting()
    {
        float time01 = currentTime / 24f;

        if (sunLight != null)
        {
            sunLight.color = sunColor.Evaluate(time01);
            sunLight.intensity = Mathf.Clamp(sunIntensity.Evaluate(time01), 0f, 1f);
            sunLight.gameObject.SetActive(sunLight.intensity > 0.01f);
        }

        if (moonLight != null)
        {
            moonLight.color = moonColor.Evaluate(time01);
            moonLight.intensity = Mathf.Clamp(moonIntensity.Evaluate(time01), 0f, 1f);
            moonLight.gameObject.SetActive(moonLight.intensity > 0.01f);
        }

        RenderSettings.ambientLight = ambientColor.Evaluate(time01);
    }

    private void UpdateRotation()
    {
        float baseY = CalculateBaseYAngle();
        float vibration = Mathf.Sin(Time.time * vibrationSpeed) * vibrationAmount;
        float finalY = baseY + vibration;

        if (sunLight != null) sunLight.transform.rotation = Quaternion.Euler(fixedXRotation, finalY, fixedZRotation);
        if (moonLight != null) moonLight.transform.rotation = Quaternion.Euler(fixedXRotation, finalY, fixedZRotation);
    }

    private float CalculateBaseYAngle()
    {
        float angle = 0f;
        if (currentTime >= 5f && currentTime < 8f) angle = Remap(currentTime, 5f, 8f, 270f, 315f);
        else if (currentTime >= 8f && currentTime < 11f) angle = 315f;
        else if (currentTime >= 11f && currentTime < 12f) angle = Remap(currentTime, 11f, 12f, 315f, 360f);
        else if (currentTime >= 12f && currentTime < 14f) angle = Remap(currentTime, 12f, 14f, 0f, 45f);
        else if (currentTime >= 14f && currentTime < 16f) angle = 45f;
        else if (currentTime >= 16f && currentTime < 18f) angle = Remap(currentTime, 16f, 18f, 45f, 90f);
        else if (currentTime >= 18f && currentTime < 19f) angle = Remap(currentTime, 18f, 19f, 90f, 270f);
        else if (currentTime >= 19f && currentTime < 22f) angle = Remap(currentTime, 19f, 22f, 270f, 180f);
        else if (currentTime >= 22f && currentTime < 24f) angle = 180f;
        else if (currentTime >= 0f && currentTime < 4f) angle = Remap(currentTime, 0f, 4f, 180f, 90f);
        else if (currentTime >= 4f && currentTime < 5f) angle = Remap(currentTime, 4f, 5f, 90f, 270f);
        return angle;
    }

    private void UpdateUI()
    {
        if (timeDisplay == null) return;
        int hours = Mathf.FloorToInt(currentTime);
        int minutes = Mathf.FloorToInt((currentTime - hours) * 60);
        minutes = (minutes / 10) * 10;
        string period = hours >= 12 ? "PM" : "AM";
        int displayHour = hours % 12;
        if (displayHour == 0) displayHour = 12;
        timeDisplay.text = string.Format("{0}:{1:00} {2}", displayHour, minutes, period);
    }

    private float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
}