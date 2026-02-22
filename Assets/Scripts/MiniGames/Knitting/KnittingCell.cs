using UnityEngine;
using UnityEngine.UI;

public class KnittingCell : MonoBehaviour
{
    public Image fillImage;
    public Image selectionOutline;
    public Image errorIcon;

    private int _targetColorIndex;
    private bool _isKnitted = false;

    public void Setup(int targetColorIdx, Color targetColor)
    {
        _targetColorIndex = targetColorIdx;
        _isKnitted = false;

        // FIX: Make sure the GameObjects are actually turned on
        fillImage.gameObject.SetActive(true);
        errorIcon.gameObject.SetActive(false); // Hide error initially

        // Create the "Ghost" pattern (Guidance)
        Color ghostColor = targetColor;
        ghostColor.a = 0.2f; // Low opacity
        fillImage.color = ghostColor;

        SetSelected(false);
    }

    public void SetSelected(bool isSelected)
    {
        if (selectionOutline != null)
        {
            // FIX: Enable the GameObject, not just the Image component
            selectionOutline.gameObject.SetActive(isSelected);
        }
    }

    public bool TryKnit(int selectedColorIdx, Color displayColor)
    {
        if (_isKnitted) return true;

        if (selectedColorIdx == _targetColorIndex)
        {
            // SUCCESS
            _isKnitted = true;

            // FIX: Ensure GameObject is on
            fillImage.gameObject.SetActive(true);
            errorIcon.gameObject.SetActive(false);

            // Make color solid
            Color solidColor = displayColor;
            solidColor.a = 1f;
            fillImage.color = solidColor;

            return true;
        }
        else
        {
            // FAIL
            // FIX: Enable the Error GameObject
            errorIcon.gameObject.SetActive(true);
            return false;
        }
    }
}