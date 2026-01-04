using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    public class StarterAssetsInputs : MonoBehaviour
    {
        [Header("Character Input Values")]
        public Vector2 move;
        public Vector2 look;
        public bool jump;
        public bool sprint;
        public bool interact;
        public bool pickup;
        public bool cameraDrag;
        public bool hotbar;

        // --- YENİ EKLENENLER: MEZAR TAŞI KAZIMA ---
        [Header("Stone Carving Input Values")]
        public bool carve;          // Kazıma tuşuna basılıyor mu?
        public Vector2 moveBrush;   // Gamepad stick hareketi
        public bool exitCarving;    // Çıkış tuşu
        public bool undo;
        // -------------------------------------------

        [Header("Movement Settings")]
        public bool analogMovement;

        [Header("Mouse Cursor Settings")]
        public bool cursorLocked = true;
        public bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM
        public void OnMove(InputValue value) { MoveInput(value.Get<Vector2>()); }
        public void OnLook(InputValue value) { if (cursorInputForLook) LookInput(value.Get<Vector2>()); }
        public void OnJump(InputValue value) { JumpInput(value.isPressed); }
        public void OnSprint(InputValue value) { SprintInput(value.isPressed); }
        public void OnInteract(InputValue value) { InteractInput(value.isPressed); }
        public void OnPickup(InputValue value) { pickup = value.isPressed; }
        public void OnCameraDrag(InputValue value) { CameraDragInput(value.Get<float>() > 0.5f); }
        public void OnHotbar(InputValue value) { hotbar = value.isPressed; }

        // --- YENİ INPUT METOTLARI ---
        // Input System otomatik olarak "On" + "Action İsmi" arar.

        public void OnCarve(InputValue value)
        {
            carve = value.isPressed;
        }

        public void OnMoveBrush(InputValue value)
        {
            moveBrush = value.Get<Vector2>();
        }

        public void OnExit(InputValue value) // Action ismini "Exit" koyduysan
        {
            exitCarving = value.isPressed;
        }

        public void OnUndo(InputValue value)
        {
            undo = value.isPressed;
        }
        // -----------------------------
#endif

        public void MoveInput(Vector2 newMoveDirection) { move = newMoveDirection; }
        public void LookInput(Vector2 newLookDirection) { look = newLookDirection; }
        public void JumpInput(bool newJumpState) { jump = newJumpState; }
        public void SprintInput(bool newSprintState) { sprint = newSprintState; }
        public void UndoInput(bool newUndoState) { undo = newUndoState; }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!cursorLocked) return;
            SetCursorState(cursorLocked);
        }

        private void SetCursorState(bool newState)
        {
            Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
        }

        public void InteractInput(bool newInteractState) { interact = newInteractState; }
        public void PickupInput(bool newPickupState) { pickup = newPickupState; }
        public void CameraDragInput(bool newDragState) { cameraDrag = newDragState; }
    }
}