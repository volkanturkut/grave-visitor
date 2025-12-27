using Cinemachine;
using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
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
    public Transform handTransform;

    [Header("New UI Components")]
    public ContextMenu contextMenuScript;
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;

    // Internal Data
    private readonly List<InventorySlotData> inventorySlots = new();
    private readonly List<GameObject> uiSlotObjects = new();
    private readonly List<InventorySlotUI> uiSlotScripts = new();
    private readonly List<Image> slotImages = new();
    private readonly int[] favoriteSlots = { -1, -1, -1, -1 };

    private int currentEquippedFavIndex = -1;
    private int currentEquippedSlot = -1;
    private GameObject currentEquippedObject;
    private RectTransform tooltipRect;

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
    private StarterAssetsInputs _starterInputs;
    private InputAction _openAction;
    private InputAction _closeAction;
    private InputAction _contextAction;

    [System.Serializable]
    public class InventorySlotData
    {
        public ItemData itemData;
        public int quantity;
        public bool IsEmpty => itemData == null;
    }

    private void Start()
    {
        InitializeSystem();
        InitializeSlots();

        if (tooltipPanel)
            tooltipPanel.TryGetComponent(out tooltipRect);

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

    private void InitializeSystem()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            playerTransform = player.transform;
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

        if (gridContainer)
            gridContainer.TryGetComponent(out gridCanvasGroup);

        if (!gridCanvasGroup)
            Debug.LogError("InventoryManager: Please add a CanvasGroup component to your GridContainer object!");
    }

    private void InitializeSlots()
    {
        int totalSlots = columns * rows;
        for (int i = 0; i < totalSlots; i++) inventorySlots.Add(new InventorySlotData());

        foreach (Transform child in gridContainer) Destroy(child.gameObject);
        uiSlotObjects.Clear();
        slotImages.Clear();
        uiSlotScripts.Clear();

        for (int i = 0; i < totalSlots; i++)
        {
            GameObject newSlot = Instantiate(slotPrefab, gridContainer);

            if (newSlot.TryGetComponent(out InventorySlotUI uiScript))
            {
                uiScript.Setup(i, this);
                uiSlotScripts.Add(uiScript);
            }

            if (newSlot.TryGetComponent(out Image img))
            {
                slotImages.Add(img);
            }

            uiSlotObjects.Add(newSlot);
        }
    }

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

    private void OnOpenInput(InputAction.CallbackContext context) { if (!isTransitioning && !isInventoryOpen) StartCoroutine(ToggleRoutine(true)); }

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

            if (currentSlot != -1)
            {
                savedSelectionIndex = currentSlot;
            }
            else if (lastSelectedSlot != -1)
            {
                savedSelectionIndex = lastSelectedSlot;
            }

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

    public void RefreshUI()
    {
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (i < uiSlotScripts.Count)
            {
                var slotScript = uiSlotScripts[i];
                slotScript.Setup(i, this);
                slotScript.UpdateSlot(inventorySlots[i]);
                SetSlotColor(i, Color.white);

                if (slotScript.iconImage)
                {
                    if (slotScript.iconImage.transform.parent != slotScript.transform)
                    {
                        slotScript.iconImage.transform.SetParent(slotScript.transform);
                    }

                    slotScript.iconImage.rectTransform.anchoredPosition = Vector2.zero;
                    slotScript.iconImage.rectTransform.localScale = Vector3.one;
                }
            }
        }
    }

    private void HandleHotbarInput()
    {
        if (Gamepad.current != null)
        {
            Vector2 dpad = Gamepad.current.dpad.ReadValue();

            if (dpad.sqrMagnitude < 0.1f)
            {
                dpadPressed = false;
            }
            else if (!dpadPressed)
            {
                if (dpad.x > 0.5f) { CycleFavorite(1); dpadPressed = true; }
                else if (dpad.x < -0.5f) { CycleFavorite(-1); dpadPressed = true; }
                else if (dpad.y < -0.5f)
                {
                    UnequipItem();
                    currentEquippedFavIndex = -1;
                    dpadPressed = true;
                }
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

    private void CycleFavorite(int direction)
    {
        currentEquippedFavIndex += direction;
        if (currentEquippedFavIndex > 3) currentEquippedFavIndex = 0;
        if (currentEquippedFavIndex < 0) currentEquippedFavIndex = 3;
        EquipFavorite(currentEquippedFavIndex);
    }

    private void EquipFavorite(int favIndex)
    {
        currentEquippedFavIndex = favIndex;
        int invIndex = favoriteSlots[favIndex];
        if (invIndex != -1 && invIndex < inventorySlots.Count && !inventorySlots[invIndex].IsEmpty) EquipItem(invIndex);
        else { UnequipItem(); Debug.Log($"Favorite Slot {favIndex + 1} is empty."); }
    }

    public void OnFavoriteItem(int invIndex)
    {
        if (invIndex < 0 || invIndex >= inventorySlots.Count) return;
        if (inventorySlots[invIndex].itemData.itemType != ItemData.ItemType.Tool) { Debug.Log("Only Tools can be favorited!"); return; }

        int currentFav = GetFavoriteIndex(invIndex);
        if (currentFav != -1) favoriteSlots[currentFav] = -1;

        int nextFav = currentFav + 1;
        if (nextFav > 3) Debug.Log("Removed from Favorites");
        else { favoriteSlots[nextFav] = invIndex; Debug.Log($"Assigned to Favorite Slot {nextFav + 1}"); }
    }

    public int GetFavoriteIndex(int invIndex)
    {
        for (int i = 0; i < favoriteSlots.Length; i++) { if (favoriteSlots[i] == invIndex) return i; }
        return -1;
    }

    public void EquipItem(int index)
    {
        if (!handTransform) { Debug.LogError("Assign Hand Transform in InventoryManager Inspector!"); return; }
        InventorySlotData slot = inventorySlots[index];
        if (slot.IsEmpty) return;

        UnequipItem();
        currentEquippedSlot = index;

        // Choose correct prefab
        GameObject prefabToSpawn = slot.itemData.equippedPrefab;
        if (prefabToSpawn == null) prefabToSpawn = slot.itemData.prefab;

        if (prefabToSpawn)
        {
            currentEquippedObject = Instantiate(prefabToSpawn, handTransform);

            // FIX: Apply the offsets from ItemData
            currentEquippedObject.transform.localPosition = slot.itemData.gripPosition;
            currentEquippedObject.transform.localRotation = Quaternion.Euler(slot.itemData.gripRotation);

            if (currentEquippedObject.TryGetComponent(out Rigidbody rb)) Destroy(rb);
            if (currentEquippedObject.TryGetComponent(out Collider col)) Destroy(col);
        }
    }

    private void UnequipItem()
    {
        if (currentEquippedObject)
        {
            Destroy(currentEquippedObject);
            currentEquippedObject = null;
        }
        currentEquippedSlot = -1;
    }

    public void OnUseItem(int index) { if (inventorySlots[index].itemData.itemType == ItemData.ItemType.Tool) EquipItem(index); else Debug.Log("Used Item " + index); }

    public void OnSplitItem(int index)
    {
        if (index < 0 || index >= inventorySlots.Count) return;
        InventorySlotData slot = inventorySlots[index];
        if (slot.IsEmpty || slot.quantity < 2) return;
        int emptyIndex = -1;
        for (int i = 0; i < inventorySlots.Count; i++) { if (inventorySlots[i].IsEmpty) { emptyIndex = i; break; } }
        if (emptyIndex != -1) { slot.quantity--; inventorySlots[emptyIndex].itemData = slot.itemData; inventorySlots[emptyIndex].quantity = 1; RefreshUI(); }
    }

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

        // FIX: Always use the 'prefab' (Drop version) for the world
        if (slot.itemData.prefab && playerTransform)
        {
            Vector3 basePos = playerTransform.position + (playerTransform.forward * 1.5f) + new Vector3(0, 0.25f, 0);
            float randomX = Random.Range(-0.5f, 0.5f);
            float randomZ = Random.Range(-0.5f, 0.5f);
            Vector3 dropPos = basePos + new Vector3(randomX, 0, randomZ);

            // Explicitly use .prefab here (World Model)
            GameObject droppedObj = Instantiate(slot.itemData.prefab, dropPos, Quaternion.identity);

            if (droppedObj.TryGetComponent(out WorldItem worldItem))
            {
                worldItem.itemData = slot.itemData;
                worldItem.quantity = slot.quantity;
            }
        }
        inventorySlots[index] = new InventorySlotData(); RefreshUI();
    }

    public bool AddItem(ItemData item, int amount = 1)
    {
        if (item.isStackable)
        {
            foreach (var slot in inventorySlots)
            {
                if (!slot.IsEmpty && slot.itemData == item && slot.quantity < item.maxStack)
                {
                    int spaceRemaining = item.maxStack - slot.quantity;
                    int amountToAdd = Mathf.Min(spaceRemaining, amount);
                    slot.quantity += amountToAdd;
                    amount -= amountToAdd;
                    if (amount <= 0) { RefreshUI(); return true; }
                }
            }
        }
        while (amount > 0)
        {
            InventorySlotData emptySlot = null;
            foreach (var slot in inventorySlots) { if (slot.IsEmpty) { emptySlot = slot; break; } }
            if (emptySlot == null) { RefreshUI(); return false; }
            emptySlot.itemData = item;
            int amountToAdd = Mathf.Min(amount, item.maxStack);
            emptySlot.quantity = amountToAdd;
            amount -= amountToAdd;
        }
        RefreshUI();
        return true;
    }

    public void SwapItems(int indexA, int indexB)
    {
        (inventorySlots[indexA], inventorySlots[indexB]) = (inventorySlots[indexB], inventorySlots[indexA]);

        if (currentEquippedSlot == indexA) currentEquippedSlot = indexB;
        else if (currentEquippedSlot == indexB) currentEquippedSlot = indexA;

        RefreshUI();
    }

    // --- INPUT LOGIC ---

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
                            if (inventorySlots[targetIndex].itemData != null)
                            {
                                itemName = inventorySlots[targetIndex].itemData.itemName;
                            }
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

    private void OnCloseInput(InputAction.CallbackContext context)
    {
        if (contextMenuScript != null && contextMenuScript.gameObject.activeSelf) { CloseContextMenu(true); return; }
        if (!isTransitioning && isInventoryOpen) StartCoroutine(ToggleRoutine(false));
    }

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

    public void CloseContextMenu(bool restoreSelection = true)
    {
        if (!contextMenuScript || !contextMenuScript.gameObject.activeSelf) return;

        contextMenuScript.Close();
        tooltipPanel.SetActive(false);
        if (gridCanvasGroup) { gridCanvasGroup.interactable = true; gridCanvasGroup.blocksRaycasts = true; }
        if (restoreSelection && lastSelectedSlot != -1 && lastSelectedSlot < uiSlotObjects.Count) StartCoroutine(RestoreSelectionRoutine(uiSlotObjects[lastSelectedSlot]));
        else lastSelectedSlot = -1;
    }

    private IEnumerator RestoreSelectionRoutine(GameObject objToSelect) { yield return null; EventSystem.current.SetSelectedGameObject(objToSelect); lastSelectedSlot = -1; }

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

    private void UpdateFavoritesOnSwap(int indexA, int indexB)
    {
        for (int i = 0; i < 4; i++) { if (favoriteSlots[i] == indexA) favoriteSlots[i] = indexB; else if (favoriteSlots[i] == indexB) favoriteSlots[i] = indexA; }
    }

    public void ShowTooltip(string name, Vector2 position)
    {
        if (string.IsNullOrEmpty(name)) { HideTooltip(); return; }
        tooltipPanel.SetActive(true);
        tooltipText.text = name;
        tooltipPanel.transform.rotation = Quaternion.Euler(-180, 0, 0);

        if (tooltipRect) UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);

        tooltipPanel.transform.position = position;
        tooltipPanel.transform.Translate(tooltipOffset.x, tooltipOffset.y, 0, Space.Self);
    }

    public void HideTooltip() { if (contextMenuPanel.activeSelf) return; tooltipPanel.SetActive(false); }

    private void SetSlotColor(int index, Color color)
    {
        if (index >= 0 && index < slotImages.Count && slotImages[index])
        {
            slotImages[index].color = color;
        }
    }
}