using UnityEngine;
using Cinemachine;
using StarterAssets;
using UnityEngine.InputSystem;

[RequireComponent(typeof(StarterAssetsInputs))]
[RequireComponent(typeof(PlayerInput))]
public class CameraZoomManager : MonoBehaviour
{
    [Header("Cameras")]
    public CinemachineVirtualCamera standardCamera;
    public CinemachineVirtualCamera zoomedCamera;

    [Header("Settings - Zoom Toggle")]
    public float sensitivity = 10.0f;
    public float dragThreshold = 0.5f;
    public float stickThreshold = 0.7f;

    [Header("Settings - Rotation Fix")]
    [Tooltip("Set this to 25 to force the camera to look down.")]
    public float zoomPitchOffset = 25.0f;

    [Header("Settings - Peek / Lean")]
    public float leanSpeed = 5.0f;
    public float maxLeanAngle = 12.5f;
    public float maxLeanOffset = 0.35f;
    public float leanSensitivity = 2.0f;

    // References
    private StarterAssetsInputs _input;
    private PlayerInput _playerInput;
    private CinemachineFramingTransposer _zoomedFramer;

    // State
    private float _dragAccumulatorY = 0f;
    private float _leanAccumulatorX = 0f;
    private bool _wasDragging = false;
    private bool _isZoomed = false;

    // Smooth Damping Variables
    private float _currentLeanRot;
    private float _currentLeanOffset;
    private float _defaultOffsetX;

    private void Start()
    {
        _input = GetComponent<StarterAssetsInputs>();
        _playerInput = GetComponent<PlayerInput>();

        if (zoomedCamera != null)
        {
            _zoomedFramer = zoomedCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
            if (_zoomedFramer != null)
            {
                _defaultOffsetX = _zoomedFramer.m_TrackedObjectOffset.x;
                _currentLeanOffset = _defaultOffsetX;
            }
        }
        SetZoom(false);
    }

    private void Update()
    {
        HandleMouseGesture();
        HandleGamepadInput();
    }

    private void LateUpdate()
    {
        ApplyLeanAndOffset();
    }

    private void HandleMouseGesture()
    {
        if (_playerInput.currentControlScheme != "KeyboardMouse") return;

        // 1. If Right Click is HELD
        if (_input.cameraDrag)
        {
            if (!_wasDragging)
            {
                _dragAccumulatorY = 0f;
                _leanAccumulatorX = 0f;
                _wasDragging = true;
            }

            float dt = Time.deltaTime;

            // --- ZOOM LOGIC (Always Allow) ---
            _dragAccumulatorY += _input.look.y * sensitivity * dt;

            // --- LEAN LOGIC (Only Allow if STANDING STILL) ---
            if (_input.move == Vector2.zero && _isZoomed)
            {
                _leanAccumulatorX += _input.look.x * leanSensitivity * dt;
                _leanAccumulatorX = Mathf.Clamp(_leanAccumulatorX, -1.0f, 1.0f);
            }
            else
            {
                _leanAccumulatorX = Mathf.MoveTowards(_leanAccumulatorX, 0f, dt * 5.0f);
            }

            // Conflict Check
            if (Mathf.Abs(_dragAccumulatorY) > dragThreshold)
            {
                _leanAccumulatorX = 0f;
                EvaluateZoomDrag();
                _dragAccumulatorY = 0f;
            }
        }
        else
        {
            // 2. Button Released
            _leanAccumulatorX = 0f;

            if (_wasDragging)
            {
                _wasDragging = false;
                _dragAccumulatorY = 0f;
            }
        }
    }

    private void EvaluateZoomDrag()
    {
        if (_dragAccumulatorY > dragThreshold) SetZoom(true);
        else if (_dragAccumulatorY < -dragThreshold) SetZoom(false);
    }

    private void HandleGamepadInput()
    {
        if (_playerInput.currentControlScheme != "Gamepad") return;

        float x = _input.look.x;
        float y = _input.look.y;
        float absX = Mathf.Abs(x);
        float absY = Mathf.Abs(y);

        // Define a "strictness" factor. 
        // 1.2 means the main direction must be 20% stronger than the other.
        float coneBias = 1.2f;

        // 1. ZOOM LOGIC (Vertical Cone)
        // Only zoom if Y is strong AND Y is significantly larger than X
        bool isVerticalCone = absY > (absX * coneBias);

        if (isVerticalCone)
        {
            if (y > stickThreshold) SetZoom(true);        // Strict UP
            else if (y < -stickThreshold) SetZoom(false); // Strict DOWN
        }

        // 2. LEAN LOGIC (Horizontal Cone)
        if (_isZoomed)
        {
            // Only lean if X is strong AND X is significantly larger than Y
            bool isHorizontalCone = absX > (absY * coneBias);
            bool outsideDeadzone = absX > 0.2f;

            if (outsideDeadzone && isHorizontalCone)
            {
                _leanAccumulatorX = x;
                _leanAccumulatorX = Mathf.Clamp(_leanAccumulatorX, -1.0f, 1.0f);
            }
            else
            {
                // Reset if diagonal, vertical, or released
                _leanAccumulatorX = 0f;
            }
        }
    }

    private void ApplyLeanAndOffset()
    {
        if (!_isZoomed || _zoomedFramer == null) return;

        // CRITICAL: If moving (WASD), force lean to 0
        if (_input.move != Vector2.zero)
        {
            _leanAccumulatorX = 0f;
        }

        // Calculate Targets
        float targetLeanY = -_leanAccumulatorX * maxLeanAngle;
        float targetOffsetX = _defaultOffsetX + (_leanAccumulatorX * maxLeanOffset);

        _currentLeanRot = Mathf.Lerp(_currentLeanRot, targetLeanY, Time.deltaTime * leanSpeed);
        _currentLeanOffset = Mathf.Lerp(_currentLeanOffset, targetOffsetX, Time.deltaTime * leanSpeed);

        // Apply Rotation (Fixed 25 + Lean)
        Vector3 currentEuler = zoomedCamera.transform.localEulerAngles;
        zoomedCamera.transform.localRotation = Quaternion.Euler(zoomPitchOffset, _currentLeanRot, currentEuler.z);

        // Apply Offset
        _zoomedFramer.m_TrackedObjectOffset.x = _currentLeanOffset;
    }

    private void SetZoom(bool isZoomed)
    {
        _isZoomed = isZoomed;

        if (isZoomed)
        {
            zoomedCamera.Priority = 20;
            standardCamera.Priority = 10;
        }
        else
        {
            zoomedCamera.Priority = 10;
            standardCamera.Priority = 20;

            // Reset accumulators
            _leanAccumulatorX = 0f;
            _currentLeanRot = 0f;
            _currentLeanOffset = _defaultOffsetX;

            // We do NOT reset rotation/offset here to prevent glitches
        }
    }
}