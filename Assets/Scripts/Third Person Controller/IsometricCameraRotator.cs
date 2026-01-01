using UnityEngine;
using StarterAssets; // Required to access StarterAssetsInputs

public class IsometricCameraRotator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag your Player (the object with StarterAssetsInputs) here")]
    public StarterAssetsInputs inputSource;

    [Header("Settings")]
    [Tooltip("Adjust speed. The Input System already scales gamepad stick by 300, so keep this low (e.g., 1).")]
    public float rotationSpeed = 1.0f;

    private void Update()
    {
        if (inputSource == null) return;

        // "look" comes from the Right Stick (mapped in Input Actions) [cite: 5]
        // Left is negative, Right is positive.
        float yawInput = inputSource.look.x;

        if (Mathf.Abs(yawInput) > 0.01f)
        {
            // We multiply by Time.deltaTime because the gamepad value is high (~300) [cite: 52]
            float rotationAmount = yawInput * rotationSpeed * Time.deltaTime;

            // Apply rotation directly to this Camera's Y axis
            // This satisfies: "Left is negative Y, Right is + Y"
            transform.Rotate(0f, rotationAmount, 0f, Space.World);
        }
    }
}