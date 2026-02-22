using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Controls day/night cycle, skybox transitions, lighting, and scheduled events
/// </summary>
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

    [Header("Event System (Scriptable Objects)")]
    // Drag your "NormalDaySchedule" asset here
    public DailySchedule activeSchedule;

    [Header("Skybox Settings")]
    public Material skyboxMaterial;
    public List<SkyPhase> skyPhases;

    [Header("Cloud Animation")]
    public AnimationCurve skyTiltCurve;
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

    // Constants for lighting thresholds
    private const float LIGHT_ACTIVE_THRESHOLD = 0.01f;

    // Constants for time calculations
    private const float HOURS_IN_DAY = 24f;
    private const int MINUTES_IN_HOUR = 60;
    private const int MINUTE_ROUNDING = 10;
    private const int NOON_HOUR = 12;

    // Constants for sun angle calculations  
    private const float DAWN_START = 5f;
    private const float DAWN_END = 8f;
    private const float MORNING_END = 11f;
    private const float NOON = 12f;
    private const float AFTERNOON_END = 14f;
    private const float EVENING_START = 16f;
    private const float EVENING_END = 18f;
    private const float DUSK_END = 19f;
    private const float NIGHT_START = 22f;
    private const float MIDNIGHT_START = 0f;
    private const float MIDNIGHT_END = 4f;
    private const float DEEP_NIGHT_END = 5f;

    // Sun rotation angles
    private const float ANGLE_DAWN_START = 270f;
    private const float ANGLE_DAWN_END = 315f;
    private const float ANGLE_NOON_START = 315f;
    private const float ANGLE_NOON_END = 360f;
    private const float ANGLE_AFTERNOON_START = 0f;
    private const float ANGLE_AFTERNOON_MID = 45f;
    private const float ANGLE_EVENING_END = 90f;
    private const float ANGLE_DUSK = 270f;
    private const float ANGLE_NIGHT_MID = 180f;
    private const float ANGLE_MIDNIGHT_END = 90f;

    private float timeMultiplier;
    private int _lastHour = -1; // Tracks when the hour changes

    private void Start()
    {
        timeMultiplier = 1f / (realSecondsPerInterval * 6f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        skyPhases.Sort((p1, p2) => p1.hour.CompareTo(p2.hour));

        // Initialize lastHour so we don't trigger events immediately on Start
        _lastHour = Mathf.FloorToInt(currentTime);
    }

    private void Update()
    {
        // Increment time
        currentTime += Time.deltaTime * timeMultiplier;
        if (currentTime >= HOURS_IN_DAY) currentTime %= HOURS_IN_DAY;

        // 2. Check for Event Triggers (Hour Change)
        CheckForScheduleEvents();

        // 3. Visual Updates
        UpdateSkyboxBlend();
        UpdateLighting();
        UpdateRotation();
        UpdateUI();
    }

    /// <summary>
    /// Checks if the hour has changed and triggers scheduled events if necessary.
    /// </summary>
    private void CheckForScheduleEvents()
    {
        int currentHourInt = Mathf.FloorToInt(currentTime);

        // Only run this logic if the hour has changed since the last frame
        if (currentHourInt != _lastHour)
        {
            _lastHour = currentHourInt;

            // Notify the ScriptableObject Schedule
            if (activeSchedule != null)
            {
                activeSchedule.CheckSchedule(currentHourInt);
            }
        }
    }

    /// <summary>
    /// Updates the skybox material properties based on the current time and sky phases.
    /// Handles blending between phases, tilting, and rotation.
    /// </summary>
    private void UpdateSkyboxBlend()
    {
        if (skyboxMaterial == null || skyPhases.Count < 2) return;

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

        skyboxMaterial.SetTexture("_TexA", currentPhase.skyTexture);
        skyboxMaterial.SetTexture("_TexB", nextPhase.skyTexture);
        skyboxMaterial.SetFloat("_Blend", blend);
        skyboxMaterial.SetColor("_Tint", Color.Lerp(currentPhase.tintColor, nextPhase.tintColor, blend));

        float tiltValue = skyTiltCurve.Evaluate(currentTime / 24f);
        skyboxMaterial.SetFloat("_Tilt", tiltValue);

        currentSpin += Time.deltaTime * skySpinSpeed;
        skyboxMaterial.SetFloat("_Rotation", currentSpin % 360f);
    }

    /// <summary>
    /// Updates sun and moon intensity/color and ambient lighting based on current time.
    /// </summary>
    private void UpdateLighting()
    {
        float time01 = currentTime / 24f;

        if (sunLight != null)
        {
            sunLight.color = sunColor.Evaluate(time01);
            sunLight.intensity = Mathf.Clamp(sunIntensity.Evaluate(time01), 0f, 1f);

            // Optimization: Disable GameObject when light is off to save performance
            bool isSunActive = sunLight.intensity > LIGHT_ACTIVE_THRESHOLD;
            if (sunLight.gameObject.activeSelf != isSunActive)
                sunLight.gameObject.SetActive(isSunActive);
        }

        if (moonLight != null)
        {
            moonLight.color = moonColor.Evaluate(time01);
            moonLight.intensity = Mathf.Clamp(moonIntensity.Evaluate(time01), 0f, 1f);

            bool isMoonActive = moonLight.intensity > LIGHT_ACTIVE_THRESHOLD;
            if (moonLight.gameObject.activeSelf != isMoonActive)
                moonLight.gameObject.SetActive(isMoonActive);
        }

        RenderSettings.ambientLight = ambientColor.Evaluate(time01);
    }

    /// <summary>
    /// Updates the rotation of the sun and moon directional lights.
    /// </summary>
    private void UpdateRotation()
    {
        float baseY = CalculateBaseYAngle();

        // Optional: I kept your vibration logic, but consider removing it if shadows jitter too much
        float vibration = Mathf.Sin(Time.time * vibrationSpeed) * vibrationAmount;
        float finalY = baseY + vibration;

        if (sunLight != null) sunLight.transform.rotation = Quaternion.Euler(fixedXRotation, finalY, fixedZRotation);
        if (moonLight != null) moonLight.transform.rotation = Quaternion.Euler(fixedXRotation, finalY, fixedZRotation);
    }

    /// <summary>
    /// Calculates the Y-axis rotation angle for the sun/moon based on specific time ranges.
    /// </summary>
    /// <returns>The calculated Y angle in degrees.</returns>
    private float CalculateBaseYAngle()
    {
        float angle = 0f;
        if (currentTime >= DAWN_START && currentTime < DAWN_END)
            angle = Remap(currentTime, DAWN_START, DAWN_END, ANGLE_DAWN_START, ANGLE_DAWN_END);
        else if (currentTime >= DAWN_END && currentTime < MORNING_END)
            angle = ANGLE_NOON_START;
        else if (currentTime >= MORNING_END && currentTime < NOON)
            angle = Remap(currentTime, MORNING_END, NOON, ANGLE_NOON_START, ANGLE_NOON_END);
        else if (currentTime >= NOON && currentTime < AFTERNOON_END)
            angle = Remap(currentTime, NOON, AFTERNOON_END, ANGLE_AFTERNOON_START, ANGLE_AFTERNOON_MID);
        else if (currentTime >= AFTERNOON_END && currentTime < EVENING_START)
            angle = ANGLE_AFTERNOON_MID;
        else if (currentTime >= EVENING_START && currentTime < EVENING_END)
            angle = Remap(currentTime, EVENING_START, EVENING_END, ANGLE_AFTERNOON_MID, ANGLE_EVENING_END);
        else if (currentTime >= EVENING_END && currentTime < DUSK_END)
            angle = Remap(currentTime, EVENING_END, DUSK_END, ANGLE_EVENING_END, ANGLE_DUSK);
        else if (currentTime >= DUSK_END && currentTime < NIGHT_START)
            angle = Remap(currentTime, DUSK_END, NIGHT_START, ANGLE_DUSK, ANGLE_NIGHT_MID);
        else if (currentTime >= NIGHT_START && currentTime < HOURS_IN_DAY)
            angle = ANGLE_NIGHT_MID;
        else if (currentTime >= MIDNIGHT_START && currentTime < MIDNIGHT_END)
            angle = Remap(currentTime, MIDNIGHT_START, MIDNIGHT_END, ANGLE_NIGHT_MID, ANGLE_MIDNIGHT_END);
        else if (currentTime >= MIDNIGHT_END && currentTime < DEEP_NIGHT_END)
            angle = Remap(currentTime, MIDNIGHT_END, DEEP_NIGHT_END, ANGLE_MIDNIGHT_END, ANGLE_DUSK);
        return angle;
    }

    /// <summary>
    /// Updates the UI text display with the current time (AM/PM format).
    /// </summary>
    private void UpdateUI()
    {
        if (timeDisplay == null) return;
        int hours = Mathf.FloorToInt(currentTime);
        int minutes = Mathf.FloorToInt((currentTime - hours) * MINUTES_IN_HOUR);
        minutes = (minutes / MINUTE_ROUNDING) * MINUTE_ROUNDING;
        string period = hours >= NOON_HOUR ? "PM" : "AM";
        int displayHour = hours % NOON_HOUR;
        if (displayHour == 0) displayHour = NOON_HOUR;
        timeDisplay.text = string.Format("{0}:{1:00} {2}", displayHour, minutes, period);
    }

    /// <summary>
    /// Remaps a value from one range to another.
    /// </summary>
    /// <param name="value">The value to remap.</param>
    /// <param name="from1">Original range min.</param>
    /// <param name="to1">Original range max.</param>
    /// <param name="from2">Target range min.</param>
    /// <param name="to2">Target range max.</param>
    /// <returns>The remapped value.</returns>
    private float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
}