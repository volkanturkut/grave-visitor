using Cinemachine;
using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

using GraveVisitor.Inventory; // Import new MVC components

public class InventoryManager : MonoBehaviour
{
    // InventoryManager Refactored - Namespace Fixed
    public static InventoryManager Instance { get; private set; }

    [Header("Tooltip Settings")]
    public Vector2 tooltipOffset = new(50, 50);

    [Header("Context Menu Settings")]
    public Vector2 contextMenuOffset = new(10, -10);

    [Header("Configuration")]
    public int columns = 10;
    public int rows = 3;
    public float cameraBlendTime = 0.5f;

    [Header("UI References")]
    public GameObject inventoryPanel;
    public Transform gridContainer;
    public GameObject slotPrefab;
    public GameObject contextMenuPanel;

    [Header("Scene References")]
    public CinemachineVirtualCamera playerCam;
    public CinemachineVirtualCamera inventoryCam;

    [Header("Hand References")]
    [Tooltip("Assign the Right Hand bone from the Inspector")]
    public Transform rightHandTransform;
    [Tooltip("Assign the Left Hand bone from the Inspector")]
    public Transform leftHandTransform;

    [Header("Animator Layer Names")]
    [Tooltip("Must EXACTLY match the layer names in your Animator")]
    public string rightArmLayerName = "RightArmLayer";
    public string leftArmLayerName = "LeftArmLayer";

    [Header("New UI Components")]
    public ContextMenu contextMenuScript;
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;

    // Internal Data
    private InventoryModel _model;
    private InventoryView _view;
    private List<InventorySlotData> inventorySlots => _model?.Slots;
    private readonly List<GameObject> uiSlotObjects = new();
    private readonly List<InventorySlotUI> uiSlotScripts = new();
    private readonly List<Image> slotImages = new();
    private readonly int[] favoriteSlots = { -1, -1, -1, -1 };

    private int currentEquippedFavIndex = -1;
    private int currentEquippedSlot = -1;
    private GameObject currentEquippedObject;

    // Animation tracking
    private string currentActiveAnimParam = "";

    // State
    private bool isInventoryOpen;
    private bool isTransitioning;
    private int lastSelectedSlot = -1;
    private int savedSelectionIndex = 0;
    private CanvasGroup gridCanvasGroup;
    private Transform playerTransform;

    // Gamepad Rearrange State
    private int gamepadSourceIndex = -1;
    private Transform gamepadOriginalParent;
    private Transform gamepadOriginalTextParent;
    private Image gamepadMovingIcon;
    private TextMeshProUGUI gamepadMovingText;

    // Input Logic Variables
    private bool isHoldingButton;
    private bool isDragging;
    private float holdTimer;
    private const float DRAG_THRESHOLD = 0.25f;
    private bool dpadPressed;

    private PlayerInput _playerInput;
    private PlayerInputs _starterInputs;
    private InputAction _openAction;
    private InputAction _closeAction;
    private InputAction _contextAction;

    private StarterAssets.ThirdPersonController playerController;

    // Data class is now in internal file: Assets/Scripts/Player/Inventory/InventorySlotData.cs
    // Uses GraveVisitor.Inventory namespace


    private void Start()
    {
        // Initialize singleton with persistence
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            DebugLogger.LogWarning("Multiple InventoryManager instances found. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        // Initialize MVC View
        _view = GetComponent<InventoryView>();
        if (_view == null) _view = gameObject.AddComponent<InventoryView>();

        // Transfer references to View
        _view.inventoryPanel = inventoryPanel;
        _view.gridContainer = gridContainer;
        _view.slotPrefab = slotPrefab;
        _view.tooltipPanel = tooltipPanel;
        _view.tooltipText = tooltipText;
        _view.contextMenuPanel = contextMenuPanel;
        if (_view) _view.tooltipOffset = tooltipOffset;

        InitializeSystem();
        InitializeSlots();

        if (contextMenuScript)
        {
            contextMenuScript.Setup(this);
        }

        inventoryPanel.SetActive(false);
        if (contextMenuPanel) contextMenuPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_openAction != null) _openAction.performed -= OnOpenInput;
        if (_closeAction != null) _closeAction.performed -= OnCloseInput;
        _contextAction?.Disable();
    }

    private void Update()
    {
        if (isInventoryOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (_playerInput)
            {
                var uiMap = _playerInput.actions.FindActionMap("UI");
                uiMap?.Enable();
            }

            HandleInputLogic();
        }
        else
        {
            HandleHotbarInput();
        }
    }

    /// <summary>
    /// Initializes core system references like the player, input action map, and hand transforms.
    /// </summary>
    private void InitializeSystem()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            playerTransform = player.transform;
            playerController = player.GetComponent<StarterAssets.ThirdPersonController>();

            if (player.TryGetComponent(out _playerInput))
            {
                _openAction = _playerInput.actions.FindAction("Player/InventoryToggle");
                _closeAction = _playerInput.actions.FindAction("UIInventory/Cancel");
                _contextAction = _playerInput.actions.FindAction("UIInventory/Context");

                _contextAction?.Enable();

                if (_openAction != null) _openAction.performed += OnOpenInput;
                if (_closeAction != null) _closeAction.performed += OnCloseInput;
            }
            player.TryGetComponent(out _starterInputs);
        }

        // Hand transform validation
        if (rightHandTransform == null || leftHandTransform == null)
        {
            DebugLogger.LogWarning("InventoryManager: Please assign Right and Left hand transforms!");
        }
        else
        {
            rightHandTransform.gameObject.SetActive(true);
            leftHandTransform.gameObject.SetActive(true);
        }

        if (gridContainer)
            gridContainer.TryGetComponent(out gridCanvasGroup);
    }

    /// <summary>
    /// Creates and initializes the inventory slots and their UI representations.
    /// Clears any existing slots before creation.
    /// </summary>
    private void InitializeSlots()
    {
        int totalSlots = columns * rows;
        _model = new InventoryModel(totalSlots);
        _model.OnInventoryUpdated.AddListener(RefreshUI);

        // Delegate UI creation to View
        if (_view)
        {
            _view.InitializeGrid(totalSlots, this);
            // Need to repopulate local lists for now to keep legacy logic working if any
            // Actually, best to rely on View's slots or expose them if needed
            // But specific logic like 'GetSelectedSlotIndex' relies on 'uiSlotObjects'
            // So we might need to expose them from View or reimplement 'GetSelectedSlotIndex' using View
        }

        // Clear local legacy lists as they are now managed by View or we need to sync them
        // For THIS step, to avoid breaking everything, we will let View create items, 
        // AND we might need to fetch them back or refactor 'GetSelectedSlotIndex'

        // Re-implement legacy lists fill by querying View? 
        // Or better: Logic Refactor for GetSelectedSlotIndex.
        uiSlotObjects.Clear();
        uiSlotScripts.Clear();
        slotImages.Clear();

        // Iterate children of gridContainer to rebuild local cache for backward compatibility
        foreach (Transform child in gridContainer)
        {
            uiSlotObjects.Add(child.gameObject);
            if (child.TryGetComponent(out InventorySlotUI uiScript)) uiSlotScripts.Add(uiScript);
            if (child.TryGetComponent(out Image img)) slotImages.Add(img);
        }
    }

    /// <summary>
    /// Helper method to find the index of the currently selected UI slot.
    /// </summary>
    /// <returns>The index of the selected slot, or -1 if no slot is selected.</returns>
    private int GetSelectedSlotIndex()
    {
        GameObject selectedObj = EventSystem.current.currentSelectedGameObject;
        if (selectedObj)
        {
            if (selectedObj.TryGetComponent(out InventorySlotUI _))
                return uiSlotObjects.IndexOf(selectedObj);
        }
        return -1;
    }

    /// <summary>
    /// Input callback for opening the inventory.
    /// </summary>
    /// <param name="context">Input context.</param>
    private void OnOpenInput(InputAction.CallbackContext context)
    {
        if (playerController != null && playerController.IsInputLocked) return;
        if (!isTransitioning && !isInventoryOpen) StartCoroutine(ToggleRoutine(true));
    }

    /// <summary>
    /// Coroutine to handle smooth transitions when opening/closing the inventory.
    /// Handles camera blending, cursor state, and input map switching.
    /// </summary>
    /// <param name="open">True to open, False to close.</param>
    private IEnumerator ToggleRoutine(bool open)
    {
        isTransitioning = true;
        isInventoryOpen = open;

        if (open)
        {
            if (inventoryCam) inventoryCam.Priority = 20;

            if (_playerInput)
            {
                _playerInput.actions.FindActionMap("Player")?.Disable();
                _playerInput.actions.FindActionMap("UIInventory")?.Enable();
                _playerInput.actions.FindActionMap("UI")?.Enable();
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (_starterInputs) _starterInputs.cursorLocked = false;

            yield return new WaitForSeconds(cameraBlendTime);

            inventoryPanel.SetActive(true);
            RefreshUI();

            yield return null;
            if (uiSlotObjects.Count > 0)
            {
                EventSystem.current.SetSelectedGameObject(null);
                if (savedSelectionIndex < 0 || savedSelectionIndex >= uiSlotObjects.Count)
                    savedSelectionIndex = 0;
                EventSystem.current.SetSelectedGameObject(uiSlotObjects[savedSelectionIndex]);
            }
        }
        else
        {
            int currentSlot = GetSelectedSlotIndex();
            if (currentSlot != -1) savedSelectionIndex = currentSlot;
            else if (lastSelectedSlot != -1) savedSelectionIndex = lastSelectedSlot;

            CloseContextMenu(false);
            inventoryPanel.SetActive(false);

            if (inventoryCam) inventoryCam.Priority = 0;

            if (_playerInput)
            {
                _playerInput.actions.FindActionMap("UIInventory")?.Disable();
                _playerInput.actions.FindActionMap("Player")?.Enable();
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (_starterInputs) _starterInputs.cursorLocked = true;
            yield return new WaitForSeconds(cameraBlendTime);
        }
        isTransitioning = false;
    }

    /// <summary>
    /// Refreshes the UI to match the current internal data state.
    /// Updates all slot visuals (icons, quantities, selection).
    /// </summary>
    public void RefreshUI()
    {
        if (_view != null && _model != null)
        {
            _view.RefreshDisplay(_model.Slots);
        }

        // Maintain local legacy logic if necessary (e.g. SetSlotColor resets)
        // View.RefreshDisplay calls UpdateSlot which sets icons/text.
        // SetSlotColor(i, Color.white) was happening in loop.
        for (int i = 0; i < uiSlotScripts.Count; i++)
        {
            // We can keep this loop for specific tweaks not yet in View, 
            // or move them to View. 
            // View.RefreshDisplay iterates all slots.
            // We should trust View.
            uiSlotScripts[i].Setup(i, this); // Re-assert setup just in case
        }
    }

    /// <summary>
    /// Handles input for the quick access hotbar (D-pad or Number keys).
    /// </summary>
    private void HandleHotbarInput()
    {
        if (Gamepad.current != null)
        {
            Vector2 dpad = Gamepad.current.dpad.ReadValue();
            if (dpad.sqrMagnitude < 0.1f) dpadPressed = false;
            else if (!dpadPressed)
            {
                if (dpad.x > 0.5f) { CycleFavorite(1); dpadPressed = true; }
                else if (dpad.x < -0.5f) { CycleFavorite(-1); dpadPressed = true; }
                else if (dpad.y < -0.5f) { UnequipItem(); currentEquippedFavIndex = -1; dpadPressed = true; }
            }
        }
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) EquipFavorite(0);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) EquipFavorite(1);
            if (Keyboard.current.digit3Key.wasPressedThisFrame) EquipFavorite(2);
            if (Keyboard.current.digit4Key.wasPressedThisFrame) EquipFavorite(3);
        }
    }

    /// <summary>
    /// Cycles through favorite slots in a specific direction (Next/Previous).
    /// </summary>
    /// <param name="direction">1 for next, -1 for previous.</param>
    private void CycleFavorite(int direction)
    {
        currentEquippedFavIndex += direction;
        if (currentEquippedFavIndex > 3) currentEquippedFavIndex = 0;
        if (currentEquippedFavIndex < 0) currentEquippedFavIndex = 3;
        EquipFavorite(currentEquippedFavIndex);
    }

    /// <summary>
    /// Equips the item in the specified favorite slot index (0-3).
    /// </summary>
    /// <param name="favIndex">Index of the favorite slot.</param>
    private void EquipFavorite(int favIndex)
    {
        currentEquippedFavIndex = favIndex;
        int invIndex = favoriteSlots[favIndex];

        if (playerTransform.TryGetComponent(out StarterAssets.ThirdPersonController controller))
        {
            controller.TriggerActionAnimation("ShowItem");
        }

        if (invIndex != -1 && invIndex < inventorySlots.Count && !inventorySlots[invIndex].IsEmpty)
        {
            EquipItem(invIndex);
        }
        else
        {
            UnequipItem();
        }
    }

    public void OnFavoriteItem(int invIndex)
    {
        if (invIndex < 0 || invIndex >= inventorySlots.Count) return;
        if (inventorySlots[invIndex].itemData.itemType != ItemData.ItemType.Tool)
        {
            DebugLogger.Log("Only Tools can be favorited!");
            return;
        }

        int currentFav = GetFavoriteIndex(invIndex);
        if (currentFav != -1) favoriteSlots[currentFav] = -1;

        int nextFav = currentFav + 1;
        if (nextFav > 3)
        {
            DebugLogger.Log("Removed from Favorites");
        }
        else
        {
            favoriteSlots[nextFav] = invIndex;
            DebugLogger.Log($"Assigned to Favorite Slot {nextFav + 1}");
        }
    }

    public int GetFavoriteIndex(int invIndex)
    {
        for (int i = 0; i < favoriteSlots.Length; i++) { if (favoriteSlots[i] == invIndex) return i; }
        return -1;
    }

    /// <summary>
    /// Equips an item from the specified inventory slot
    /// </summary>
    public void EquipItem(int index)
    {
        // 1. Validation
        if (rightHandTransform == null || leftHandTransform == null)
        {
            DebugLogger.LogError("HANDS NOT ASSIGNED! Assign Right and Left Hand Transforms from InventoryManager Inspector.");
            return;
        }

        InventorySlotData slot = inventorySlots[index];
        if (slot.IsEmpty) return;

        // 2. Clear previously equipped item
        UnequipItem();
        currentEquippedSlot = index;

        // 3. Which hand?
        Transform targetHand = (slot.itemData.handType == ItemData.EquipHand.Left) ? leftHandTransform : rightHandTransform;

        // 4. Animation Layer and Bool settings
        if (playerController)
        {
            Animator anim = playerController.GetAnimator();
            if (anim)
            {
                // A. Enable Bool parameter
                if (!string.IsNullOrEmpty(slot.itemData.holdAnimBool))
                {
                    anim.SetBool(slot.itemData.holdAnimBool, true);
                    currentActiveAnimParam = slot.itemData.holdAnimBool;
                }

                // B. Adjust Layer Weights
                int rightIndex = anim.GetLayerIndex(rightArmLayerName);
                int leftIndex = anim.GetLayerIndex(leftArmLayerName);

                if (slot.itemData.handType == ItemData.EquipHand.Left)
                {
                    // Left Layer On, Right Off
                    if (leftIndex != -1) anim.SetLayerWeight(leftIndex, 1f);
                    if (rightIndex != -1) anim.SetLayerWeight(rightIndex, 0f);
                }
                else
                {
                    // Right Layer On, Left Off
                    if (rightIndex != -1) anim.SetLayerWeight(rightIndex, 1f);
                    if (leftIndex != -1) anim.SetLayerWeight(leftIndex, 0f);
                }
            }
        }

        // 5. Object instantiation
        GameObject prefabToSpawn = slot.itemData.equippedPrefab;
        if (prefabToSpawn == null) prefabToSpawn = slot.itemData.prefab;

        if (prefabToSpawn)
        {
            currentEquippedObject = Instantiate(prefabToSpawn, targetHand);
            currentEquippedObject.transform.localPosition = slot.itemData.gripPosition;
            currentEquippedObject.transform.localRotation = Quaternion.Euler(slot.itemData.gripRotation);

            if (currentEquippedObject.TryGetComponent(out Rigidbody rb)) Destroy(rb);
            if (currentEquippedObject.TryGetComponent(out Collider col)) Destroy(col);
        }
    }

    /// <summary>
    /// Unequips the currently equipped item
    /// </summary>
    /// <summary>
    /// Unequips the currently equipped item, destroying its visual representation and resetting animations.
    /// </summary>
    private void UnequipItem()
    {
        // 1. Stop Animation
        if (!string.IsNullOrEmpty(currentActiveAnimParam) && playerController)
        {
            Animator anim = playerController.GetAnimator();
            if (anim)
            {
                anim.SetBool(currentActiveAnimParam, false);

                // Optional: You can also reset layer weights when unequipping
                // But generally just setting the bool to false (if no Exit Time) is sufficient
            }
            currentActiveAnimParam = "";
        }

        // 2. Destroy Object
        if (currentEquippedObject)
        {
            Destroy(currentEquippedObject);
            currentEquippedObject = null;
        }
        currentEquippedSlot = -1;
    }

    /// <summary>
    /// Used by slot UI context menu to trigger item usage.
    /// Currently only supports equipping tools.
    /// </summary>
    /// <param name="index">Index of the item to use.</param>
    public void OnUseItem(int index)
    {
        if (inventorySlots[index].itemData.itemType == ItemData.ItemType.Tool)
        {
            EquipItem(index);
        }
        else
        {
            DebugLogger.Log("Used Item " + index);
        }
    }

    /// <summary>
    /// Splits a stackable item into two separate stacks.
    /// Takes one item from the source stack and places it in the first empty slot.
    /// </summary>
    /// <param name="index">Index of the stack to split.</param>
    public void OnSplitItem(int index)
    {
        if (index < 0 || index >= inventorySlots.Count) return;
        InventorySlotData slot = inventorySlots[index];
        if (slot.IsEmpty || slot.quantity < 2) return;
        int emptyIndex = -1;
        for (int i = 0; i < inventorySlots.Count; i++) { if (inventorySlots[i].IsEmpty) { emptyIndex = i; break; } }
        if (emptyIndex != -1) { slot.quantity--; inventorySlots[emptyIndex].itemData = slot.itemData; inventorySlots[emptyIndex].quantity = 1; RefreshUI(); }
    }

    /// <summary>
    /// Drops an item from the inventory into the world.
    /// Instantiates the item prefab and removes it from the inventory data.
    /// </summary>
    /// <param name="index">Index of the item to drop.</param>
    public void OnDropItem(int index)
    {
        if (index < 0 || index >= inventorySlots.Count) return;
        InventorySlotData slot = inventorySlots[index];
        if (slot.IsEmpty) return;

        if (currentEquippedObject && index == currentEquippedSlot)
        {
            UnequipItem();
        }

        int favIndex = GetFavoriteIndex(index);
        if (favIndex != -1) favoriteSlots[favIndex] = -1;

        if (slot.itemData.prefab && playerTransform)
        {
            // RAYCAST SAFETY CHECK
            Vector3 spawnOrigin = playerTransform.position + Vector3.up;
            Vector3 dropDirection = playerTransform.forward;
            float dropDistance = 1.5f;

            if (Physics.Raycast(spawnOrigin, dropDirection, out RaycastHit hit, dropDistance))
            {
                dropDistance = CalculateDropDistance(hit.distance);
            }

            Vector3 dropPos = playerTransform.position + (dropDirection * dropDistance) + new Vector3(0, 0.25f, 0);

            GameObject droppedObj = Instantiate(slot.itemData.prefab, dropPos, Quaternion.identity);

            if (droppedObj.TryGetComponent(out WorldItem worldItem))
            {
                worldItem.itemData = slot.itemData;
                worldItem.quantity = slot.quantity;
            }
        }
        inventorySlots[index] = new InventorySlotData(); RefreshUI();
    }

    public static float CalculateDropDistance(float hitDistance)
    {
        float dropDistance = hitDistance - 0.2f;
        if (dropDistance < 0.2f) dropDistance = 0.2f;

        // Security Fix: Prevent items from being dropped through walls
        if (dropDistance > hitDistance)
        {
            dropDistance = Mathf.Max(0f, hitDistance - 0.05f);
        }

        return dropDistance;
    }

    /// <summary>
    /// Adds an item to the inventory, handling stacking and slot finding.
    /// </summary>
    /// <param name="item">The item data to add.</param>
    /// <param name="amount">Quantity to add.</param>
    /// <returns>True if the item was successfully added, False if inventory is full.</returns>
    public bool AddItem(ItemData item, int amount = 1)
    {
        if (_model != null)
        {
            return _model.AddItem(item, amount);
        }
        return false;
    }

    /// <summary>
    /// Swaps the items in two specified inventory slots.
    /// Updates equipment state if one of the swapped items was equipped.
    /// </summary>
    /// <param name="indexA">First slot index.</param>
    /// <param name="indexB">Second slot index.</param>
    public void SwapItems(int indexA, int indexB)
    {
        if (_model != null)
        {
            _model.SwapItems(indexA, indexB);

            // Handle equipment update locally for now as Model doesn't know about equipment
            if (currentEquippedSlot == indexA) currentEquippedSlot = indexB;
            else if (currentEquippedSlot == indexB) currentEquippedSlot = indexA;
        }
    }

    /// <summary>
    /// Processes input for opening context menus or initiating gamepad drag-and-drop.
    /// </summary>
    private void HandleInputLogic()
    {
        if (_contextAction == null) return;
        if (_contextAction.WasPressedThisFrame()) { isHoldingButton = true; holdTimer = 0f; isDragging = false; }
        if (isHoldingButton)
        {
            holdTimer += Time.deltaTime;
            if (holdTimer > DRAG_THRESHOLD && !isDragging) StartGamepadDrag();
            if (isDragging && gamepadMovingIcon)
            {
                GameObject selectedObj = EventSystem.current.currentSelectedGameObject;
                if (selectedObj)
                {
                    gamepadMovingIcon.transform.position = selectedObj.transform.position;
                    if (selectedObj.TryGetComponent(out InventorySlotUI _))
                    {
                        int targetIndex = uiSlotObjects.IndexOf(selectedObj);
                        if (targetIndex != -1)
                        {
                            string itemName = "";
                            if (inventorySlots[targetIndex].itemData != null) itemName = inventorySlots[targetIndex].itemData.itemName;
                            ShowTooltip(itemName, selectedObj.transform.position);
                        }
                    }
                }
            }
        }
        if (_contextAction.WasReleasedThisFrame())
        {
            isHoldingButton = false;
            if (isDragging) FinishGamepadDrag();
            else
            {
                int index = GetSelectedSlotIndex();
                if (index != -1 && !inventorySlots[index].IsEmpty) { Vector2 menuPos = (Vector2)uiSlotObjects[index].transform.position + contextMenuOffset; OpenContextMenu(index, menuPos); }
            }
            isDragging = false; holdTimer = 0f;
        }
    }

    /// <summary>
    /// Input callback for closing the inventory or active context menus.
    /// </summary>
    /// <param name="context">Input context.</param>
    private void OnCloseInput(InputAction.CallbackContext context)
    {
        if (contextMenuScript != null && contextMenuScript.gameObject.activeSelf) { CloseContextMenu(true); return; }
        if (!isTransitioning && isInventoryOpen) StartCoroutine(ToggleRoutine(false));
    }

    /// <summary>
    /// Opens the context menu for a specific inventory slot.
    /// Disables grid interaction while menu is open.
    /// </summary>
    /// <param name="index">Slot index.</param>
    /// <param name="pos">Screen position to open menu.</param>
    public void OpenContextMenu(int index, Vector2 pos)
    {
        if (!inventorySlots[index].IsEmpty)
        {
            lastSelectedSlot = index;
            if (gridCanvasGroup) { gridCanvasGroup.interactable = false; gridCanvasGroup.blocksRaycasts = false; }
            contextMenuScript.OpenMenu(inventorySlots[index].itemData, index, pos, inventorySlots[index].quantity);
            ShowTooltip(inventorySlots[index].itemData.itemName, uiSlotObjects[index].transform.position);
        }
    }

    /// <summary>
    /// Closes the currently active context menu and optionally restores selection to the grid.
    /// </summary>
    /// <param name="restoreSelection">True to select the previously selected slot.</param>
    public void CloseContextMenu(bool restoreSelection = true)
    {
        if (!contextMenuScript || !contextMenuScript.gameObject.activeSelf) return;

        contextMenuScript.Close();
        tooltipPanel.SetActive(false);
        if (gridCanvasGroup) { gridCanvasGroup.interactable = true; gridCanvasGroup.blocksRaycasts = true; }
        if (restoreSelection && lastSelectedSlot != -1 && lastSelectedSlot < uiSlotObjects.Count) StartCoroutine(RestoreSelectionRoutine(uiSlotObjects[lastSelectedSlot]));
        else lastSelectedSlot = -1;
    }

    /// <summary>
    /// Coroutine to safe-guard selection restoration, waiting one frame.
    /// </summary>
    private IEnumerator RestoreSelectionRoutine(GameObject objToSelect) { yield return null; EventSystem.current.SetSelectedGameObject(objToSelect); lastSelectedSlot = -1; }

    /// <summary>
    /// Initiates a drag operation when using gamepad inputs.
    /// </summary>
    private void StartGamepadDrag()
    {
        int currentIndex = GetSelectedSlotIndex();
        if (currentIndex != -1 && !inventorySlots[currentIndex].IsEmpty)
        {
            CloseContextMenu(false); isDragging = true; gamepadSourceIndex = currentIndex;
            if (uiSlotObjects[currentIndex].TryGetComponent(out InventorySlotUI slotUI))
            {
                if (slotUI.iconImage)
                {
                    gamepadMovingIcon = slotUI.iconImage;
                    gamepadOriginalParent = gamepadMovingIcon.transform.parent;
                    if (slotUI.qtyText) { gamepadMovingText = slotUI.qtyText; gamepadOriginalTextParent = gamepadMovingText.transform.parent; gamepadMovingText.transform.SetParent(gamepadMovingIcon.transform); }
                    gamepadMovingIcon.transform.SetParent(inventoryPanel.transform);
                }
            }
        }
    }

    /// <summary>
    /// Completes the gamepad drag operation, swapping or moving items.
    /// </summary>
    private void FinishGamepadDrag()
    {
        if (gamepadSourceIndex != -1)
        {
            int targetIndex = GetSelectedSlotIndex();
            if (gamepadMovingIcon && gamepadOriginalParent)
            {
                if (gamepadMovingText && gamepadOriginalTextParent) { gamepadMovingText.transform.SetParent(gamepadOriginalTextParent); gamepadMovingText = null; }
                gamepadMovingIcon.transform.SetParent(gamepadOriginalParent); gamepadMovingIcon.rectTransform.anchoredPosition = Vector2.zero; gamepadMovingIcon = null;
            }
            if (targetIndex != -1 && targetIndex != gamepadSourceIndex)
            {
                UpdateFavoritesOnSwap(gamepadSourceIndex, targetIndex);
                InventorySlotData sourceSlot = inventorySlots[gamepadSourceIndex];
                InventorySlotData targetSlot = inventorySlots[targetIndex];
                if (!targetSlot.IsEmpty && sourceSlot.itemData == targetSlot.itemData && sourceSlot.itemData.isStackable)
                {
                    int spaceRemaining = targetSlot.itemData.maxStack - targetSlot.quantity;
                    if (spaceRemaining > 0)
                    {
                        int amountToMove = Mathf.Min(sourceSlot.quantity, spaceRemaining);
                        targetSlot.quantity += amountToMove; sourceSlot.quantity -= amountToMove;
                        if (sourceSlot.quantity <= 0) inventorySlots[gamepadSourceIndex] = new InventorySlotData();
                    }
                    else SwapItems(gamepadSourceIndex, targetIndex);
                }
                else SwapItems(gamepadSourceIndex, targetIndex);
                if (!inventorySlots[targetIndex].IsEmpty) ShowTooltip(inventorySlots[targetIndex].itemData.itemName, uiSlotObjects[targetIndex].transform.position);
                else HideTooltip();
                RefreshUI();
            }
            else RefreshUI();
            gamepadSourceIndex = -1;
        }
    }

    /// <summary>
    /// Updates the favorite slots mapping when items are swapped in the inventory.
    /// </summary>
    /// <param name="indexA">First slot index.</param>
    /// <param name="indexB">Second slot index.</param>
    private void UpdateFavoritesOnSwap(int indexA, int indexB)
    {
        for (int i = 0; i < 4; i++) { if (favoriteSlots[i] == indexA) favoriteSlots[i] = indexB; else if (favoriteSlots[i] == indexB) favoriteSlots[i] = indexA; }
    }

    /// <summary>
    /// Displays a tooltip with the specified name at the given position.
    /// </summary>
    /// <param name="name">Name of the item to display.</param>
    /// <param name="position">Screen position for the tooltip.</param>
    public void ShowTooltip(string name, Vector2 position)
    {
        if (string.IsNullOrEmpty(name)) { HideTooltip(); return; }

        if (_view)
        {
            _view.ShowTooltip(name, position);
        }
    }

    /// <summary>
    /// Hides the currently active tooltip.
    /// </summary>
    public void HideTooltip()
    {
        if (contextMenuPanel.activeSelf) return;
        if (_view) _view.HideTooltip();
    }

    /// <summary>
    /// Sets the color of a specific inventory slot icon.
    /// </summary>
    /// <param name="index">Slot index.</param>
    /// <param name="color">Color to set.</param>
    private void SetSlotColor(int index, Color color)
    {
        if (index >= 0 && index < slotImages.Count && slotImages[index])
        {
            slotImages[index].color = color;
        }
    }

    /// <summary>
    /// Retrieves the ItemData of the currently equipped item.
    /// </summary>
    /// <returns>The equipped ItemData, or null if nothing is equipped.</returns>
    public ItemData GetCurrentEquippedItem()
    {
        // Check if a slot is actually selected and within bounds
        if (currentEquippedSlot != -1 && currentEquippedSlot < inventorySlots.Count)
        {
            return inventorySlots[currentEquippedSlot].itemData;
        }
        return null;
    }
}