using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using StarterAssets;
using System;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI Components")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;

    [Header("Player Reference")]
    public ThirdPersonController playerController;

    private Queue<string> _sentences;
    private bool _isDialogueActive = false;
    private GhostNPC _currentInteractingGhost;
    private Action _onDialogueComplete;

    // FIX: Add a timer to prevent instant skipping
    private float _inputCooldown = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _sentences = new Queue<string>();
        dialoguePanel.SetActive(false);
    }

    private void Update()
    {
        if (!_isDialogueActive) return;

        // FIX: If cooldown is active, reduce it and ignore input
        if (_inputCooldown > 0f)
        {
            _inputCooldown -= Time.deltaTime;
            return;
        }

        // Check for Input
        if (WasSubmitPressed())
        {
            DisplayNextSentence();
        }
    }

    private bool WasSubmitPressed()
    {
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) return true;
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) return true;
        return false;
    }

    public void StartDialogue(DialogueData data, GhostNPC ghost, Action onComplete = null)
    {
        _isDialogueActive = true;
        _currentInteractingGhost = ghost;
        _onDialogueComplete = onComplete;

        // FIX: Set a small cooldown (e.g. 0.5 seconds) so the 'Interact' press doesn't skip text
        _inputCooldown = 0.5f;

        if (playerController) playerController.LockInput(true);

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

        if (playerController) playerController.LockInput(false);

        if (_onDialogueComplete != null)
        {
            _onDialogueComplete.Invoke();
            _onDialogueComplete = null;
        }
        else if (_currentInteractingGhost != null)
        {
            _currentInteractingGhost.ResumeWandering();
        }
        _currentInteractingGhost = null;
    }
}