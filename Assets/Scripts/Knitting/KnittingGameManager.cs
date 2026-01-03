using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class KnittingGameManager : MonoBehaviour
{
    [Header("Game Config")]
    public int width = 17;
    public int height = 23;
    public float maxTime = 60f;
    public float penaltyTime = 5f;

    [Header("References")]
    public Texture2D patternTexture;
    public Color[] availableColors;
    public Transform gridContainer;
    public GameObject cellPrefab;
    public Image[] yarnSelectionImages;
    public Slider timerSlider;

    // State
    private int _currentX = 0;
    private int _currentY = 0;
    private int _selectedColorIndex = 0;
    private float _timer;
    private bool _gameActive = true;
    private KnittingCell[,] _gridCells;
    private int[,] _patternData;

    // Input
    private InputAction _knitAction;
    private InputAction _selectColorAction;
    private InputActionMap _knittingMap;

    void Start()
    {
        // 1. Setup Input
        var playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            // Ensure we switch to the Knitting map
            playerInput.SwitchCurrentActionMap("Knitting");
            _knittingMap = playerInput.actions.FindActionMap("Knitting");
        }

        if (_knittingMap != null)
        {
            _knitAction = _knittingMap.FindAction("Knit");
            _selectColorAction = _knittingMap.FindAction("SelectColor");

            _knitAction.performed += OnKnitPressed;
            _selectColorAction.performed += OnSelectColorPressed;
            _knittingMap.Enable();
        }
        else
        {
            Debug.LogError("Knitting Input Map not found! Check PlayerInput component.");
        }

        // 2. Load Data
        _timer = maxTime;
        LoadPattern();
        GenerateGrid();
        UpdateSelectionUI();

        // 3. Highlight First Cell
        if (_gridCells.Length > 0 && _gridCells[0, 0] != null)
        {
            _gridCells[0, 0].SetSelected(true);
        }
    }

    // ... (Update and Input methods remain same, skipping to save space) ...
    // COPY THE "Update", "OnSelectColorPressed", "UpdateSelectionUI" methods from previous response if needed
    // The critical changes are below:

    void Update()
    {
        if (!_gameActive) return;
        _timer -= Time.deltaTime;
        if (timerSlider) timerSlider.value = _timer / maxTime;
    }

    private void OnSelectColorPressed(InputAction.CallbackContext ctx)
    {
        if (!_gameActive) return;
        float val = ctx.ReadValue<float>();
        if (val > 0) _selectedColorIndex++;
        else if (val < 0) _selectedColorIndex--;

        if (_selectedColorIndex >= availableColors.Length) _selectedColorIndex = 0;
        if (_selectedColorIndex < 0) _selectedColorIndex = availableColors.Length - 1;
        UpdateSelectionUI();
    }

    private void UpdateSelectionUI()
    {
        for (int i = 0; i < yarnSelectionImages.Length; i++)
        {
            yarnSelectionImages[i].transform.localScale = (i == _selectedColorIndex) ? Vector3.one * 1.2f : Vector3.one;
        }
    }

    private void OnKnitPressed(InputAction.CallbackContext ctx)
    {
        if (!_gameActive) return;

        // Try to knit the current cell
        KnittingCell currentCell = _gridCells[_currentX, _currentY];

        // We pass the current selected color to the cell to check if it matches
        bool success = currentCell.TryKnit(_selectedColorIndex, availableColors[_selectedColorIndex]);

        if (success)
        {
            // ONLY move cursor if knit was successful
            currentCell.SetSelected(false);
            AdvanceCursor();
        }
        else
        {
            _timer -= penaltyTime;
            // Cursor stays here until you get it right!
        }
    }

    private void AdvanceCursor()
    {
        _currentX++;
        if (_currentX >= width)
        {
            _currentX = 0;
            _currentY++;
        }

        if (_currentY >= height)
        {
            Debug.Log("You Win!");
            _gameActive = false;
            return;
        }

        _gridCells[_currentX, _currentY].SetSelected(true);
    }

    private void LoadPattern()
    {
        _patternData = new int[width, height];
        if (patternTexture == null) return;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixelColor = patternTexture.GetPixel(x, y);
                // If pixel is transparent, default to 0
                if (pixelColor.a == 0) pixelColor = availableColors[0];

                _patternData[x, y] = GetClosestColorIndex(pixelColor);
            }
        }
    }

    private int GetClosestColorIndex(Color target)
    {
        float minDiff = float.MaxValue;
        int index = 0;
        for (int i = 0; i < availableColors.Length; i++)
        {
            float diff = Vector4.Distance((Vector4)target, (Vector4)availableColors[i]);
            if (diff < minDiff) { minDiff = diff; index = i; }
        }
        return index;
    }

    private void GenerateGrid()
    {
        _gridCells = new KnittingCell[width, height];

        // Clear old cells
        foreach (Transform child in gridContainer) Destroy(child.gameObject);

        // FIX: Loop Y from TOP (height-1) down to BOTTOM (0)
        // This matches the UI which starts at the Top-Left
        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject obj = Instantiate(cellPrefab, gridContainer);
                KnittingCell cell = obj.GetComponent<KnittingCell>();

                int colorIndex = _patternData[x, y];

                // Safety check in case your pattern has a color not in your list
                if (colorIndex >= availableColors.Length) colorIndex = 0;

                Color colorVisual = availableColors[colorIndex];
                cell.Setup(colorIndex, colorVisual);

                _gridCells[x, y] = cell;
            }
        }
    }
}