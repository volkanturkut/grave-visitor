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
    [SerializeField] private StarterAssetsInputs playerInputs;
    [SerializeField] private ThirdPersonController playerController;
    [SerializeField] private IsometricCameraRotator cameraRotator;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

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

    private bool IsAnyPanelOpen()
    {
        return bedPanel.activeSelf || phonePanel.activeSelf || simpleInfoPanel.activeSelf;
    }

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

    public void CloseAllPanels()
    {
        bedPanel.SetActive(false);
        phonePanel.SetActive(false);
        simpleInfoPanel.SetActive(false);

        EventSystem.current.SetSelectedGameObject(null);

        SetGameState(false);
    }

    public void OpenBedPanel()
    {
        SetGameState(true);
        bedPanel.SetActive(true);
        SelectButton(bedFirstButton);
    }

    public void OpenPhonePanel()
    {
        SetGameState(true);
        phonePanel.SetActive(true);
        SelectButton(phoneFirstButton);
    }

    public void OpenSimplePanel()
    {
        SetGameState(true);
        simpleInfoPanel.SetActive(true);
        SelectButton(simpleFirstButton);
    }

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
        Debug.Log("Saving and Sleeping...");
        CloseAllPanels();
    }

    public void OnBedNoClicked()
    {
        CloseAllPanels();
    }

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