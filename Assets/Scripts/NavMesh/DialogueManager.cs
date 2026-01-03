using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem; // For Gamepad button A
using StarterAssets; // To access ThirdPersonController

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI Components")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;

    [Header("Player Reference")]
    // Assign the Player object here to lock movement
    public ThirdPersonController playerController;

    private Queue<string> _sentences;
    private bool _isDialogueActive = false;
    private GhostNPC _currentInteractingGhost;

    private void Awake()
    {
        // Singleton pattern to access this from anywhere
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _sentences = new Queue<string>();
        dialoguePanel.SetActive(false);
    }

    private void Update()
    {
        if (!_isDialogueActive) return;

        // Check for Gamepad 'A' (Button South) OR Keyboard 'E' to Next/Close
        if (WasSubmitPressed())
        {
            DisplayNextSentence();
        }
    }

    private bool WasSubmitPressed()
    {
        // Check Gamepad South (A on Xbox, X on PS)
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) return true;
        // Check Keyboard E (or Enter)
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) return true;

        return false;
    }

    public void StartDialogue(DialogueData data, GhostNPC ghost)
    {
        _isDialogueActive = true;
        _currentInteractingGhost = ghost;

        // 1. Lock Player Movement using the method in ThirdPersonController.cs
        if (playerController) playerController.LockInput(true);

        // 2. Setup UI
        dialoguePanel.SetActive(true);
        nameText.text = data.speakerName;
        _sentences.Clear();

        foreach (string sentence in data.sentences)
        {
            _sentences.Enqueue(sentence);
        }

        DisplayNextSentence();
    }

    public void DisplayNextSentence()
    {
        if (_sentences.Count == 0)
        {
            EndDialogue();
            return;
        }

        string sentence = _sentences.Dequeue();
        dialogueText.text = sentence;
    }

    private void EndDialogue()
    {
        _isDialogueActive = false;
        dialoguePanel.SetActive(false);

        // 1. Unlock Player Movement
        if (playerController) playerController.LockInput(false);

        // 2. Tell the ghost to resume wandering
        if (_currentInteractingGhost != null)
        {
            _currentInteractingGhost.ResumeWandering();
            _currentInteractingGhost = null;
        }
    }
}