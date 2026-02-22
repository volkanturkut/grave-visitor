using UnityEngine;
using UnityEngine.UI;
using StarterAssets;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance;

    [Header("Panels")]
    [SerializeField] private GameObject bedPanel;
    [SerializeField] private GameObject phonePanel;
    [SerializeField] private GameObject simpleInfoPanel;
    [SerializeField] private Image transitionPanel;

    [Header("First Selected Buttons")]
    [SerializeField] private GameObject bedFirstButton;
    [SerializeField] private GameObject phoneFirstButton;
    [SerializeField] private GameObject simpleFirstButton;

    [Header("Player References")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private PlayerInputs playerInputs;
    [SerializeField] private ThirdPersonController playerController;
    [SerializeField] private IsometricCameraRotator cameraRotator;

    private void Awake()
    {
        // Singleton pattern with persistence
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        CloseAllPanels();

        if (transitionPanel != null) transitionPanel.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (IsAnyPanelOpen())
        {
            if ((Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame) ||
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseAllPanels();
            }
        }
    }

    /// <summary>
    /// Checks if any critical UI panel is currently active.
    /// </summary>
    /// <returns>True if Bed, Phone, or Simple panels are open.</returns>
    private bool IsAnyPanelOpen()
    {
        return (bedPanel != null && bedPanel.activeSelf) ||
               (phonePanel != null && phonePanel.activeSelf) ||
               (simpleInfoPanel != null && simpleInfoPanel.activeSelf);
    }

    /// <summary>
    /// configures input mapping, cursor state, and camera control based on whether a menu is open.
    /// </summary>
    /// <param name="isMenuOpen">True to enable UI mode, False for Gameplay mode.</param>
    private void SetGameState(bool isMenuOpen)
    {
        Cursor.visible = isMenuOpen;
        Cursor.lockState = isMenuOpen ? CursorLockMode.None : CursorLockMode.Locked;

        if (playerInputs != null)
        {
            playerInputs.move = Vector2.zero;
            playerInputs.look = Vector2.zero;
            playerInputs.sprint = false;
        }

        if (playerInput != null)
        {
            if (isMenuOpen)
            {
                playerInput.SwitchCurrentActionMap("UIInventory");
            }
            else
            {
                playerInput.SwitchCurrentActionMap("Player");
            }
        }

        if (playerController != null)
        {
            playerController.LockInput(isMenuOpen);
        }

        if (cameraRotator != null)
        {
            cameraRotator.enabled = !isMenuOpen;
        }
    }

    /// <summary>
    /// Closes all UI panels and restores game state
    /// </summary>
    public void CloseAllPanels()
    {
        if (bedPanel != null) bedPanel.SetActive(false);
        if (phonePanel != null) phonePanel.SetActive(false);
        if (simpleInfoPanel != null) simpleInfoPanel.SetActive(false);

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        SetGameState(false);
    }

    /// <summary>
    /// Opens the bed interaction panel
    /// </summary>
    public void OpenBedPanel()
    {
        if (bedPanel == null)
        {
            DebugLogger.LogError("GameUIManager: bedPanel is not assigned!");
            return;
        }

        SetGameState(true);
        bedPanel.SetActive(true);
        SelectButton(bedFirstButton);
    }

    /// <summary>
    /// Opens the phone interaction panel
    /// </summary>
    public void OpenPhonePanel()
    {
        if (phonePanel == null)
        {
            DebugLogger.LogError("GameUIManager: phonePanel is not assigned!");
            return;
        }

        SetGameState(true);
        phonePanel.SetActive(true);
        SelectButton(phoneFirstButton);
    }

    /// <summary>
    /// Opens a simple info panel
    /// </summary>
    public void OpenSimplePanel()
    {
        if (simpleInfoPanel == null)
        {
            DebugLogger.LogError("GameUIManager: simpleInfoPanel is not assigned!");
            return;
        }

        SetGameState(true);
        simpleInfoPanel.SetActive(true);
        SelectButton(simpleFirstButton);
    }

    /// <summary>
    /// Sets the currently selected UI object for gamepad/keyboard navigation.
    /// </summary>
    /// <param name="btn">The GameObject to select.</param>
    private void SelectButton(GameObject btn)
    {
        if (EventSystem.current != null && btn != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(btn);
        }
    }

    public void OnBedSleepClicked()
    {
        DebugLogger.Log("Saving and Sleeping...");
        CloseAllPanels();
    }

    public void OnBedNoClicked()
    {
        CloseAllPanels();
    }

    /// <summary>
    /// Coroutine to fade the screen to black over a specified duration.
    /// Used for transitions like sleeping.
    /// </summary>
    /// <param name="duration">Duration of the fade in seconds.</param>
    public IEnumerator FadeToBlackRoutine(float duration)
    {
        if (transitionPanel != null)
        {
            transitionPanel.gameObject.SetActive(true);
            float t = 0;
            Color startColor = transitionPanel.color;
            startColor.a = 0;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 1);

            while (t < duration)
            {
                t += Time.deltaTime;
                transitionPanel.color = Color.Lerp(startColor, endColor, t / duration);
                yield return null;
            }
            transitionPanel.color = endColor;
        }
    }
}