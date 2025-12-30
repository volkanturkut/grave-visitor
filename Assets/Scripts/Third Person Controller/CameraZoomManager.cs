using UnityEngine;
using Cinemachine;
using StarterAssets;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(StarterAssetsInputs))]
[RequireComponent(typeof(PlayerInput))]
public class CameraZoomManager : MonoBehaviour
{
    [Header("Cameras")]
    public CinemachineVirtualCamera standardCamera;
    public CinemachineVirtualCamera zoomedCamera;

    [Header("Timing")]
    public float cameraBlendTime = 0.5f;
    public float shyAnimationDelay = 1.5f;

    [Header("Settings - Zoom Toggle")]
    public float sensitivity = 10.0f;
    public float dragThreshold = 0.5f;
    public float stickThreshold = 0.7f;

    [Header("Settings - Rotation Fix")]
    public float zoomPitchOffset = 25.0f;

    [Header("Settings - Peek / Lean")]
    public float leanSpeed = 5.0f;
    public float maxLeanAngle = 12.5f;
    public float maxLeanOffset = 0.35f;
    public float leanSensitivity = 2.0f;

    private StarterAssetsInputs _input;
    private PlayerInput _playerInput;
    private CinemachineFramingTransposer _zoomedFramer;
    private ThirdPersonController _thirdPersonController;

    private float _dragAccumulatorY = 0f;
    private float _leanAccumulatorX = 0f;
    private bool _wasDragging = false;
    private bool _isZoomed = false;

    private float _currentLeanRot;
    private float _currentLeanOffset;
    private float _defaultOffsetX;

    private Coroutine _zoomCoroutine;

    private void Start()
    {
        _input = GetComponent<StarterAssetsInputs>();
        _playerInput = GetComponent<PlayerInput>();
        _thirdPersonController = GetComponent<ThirdPersonController>();

        if (zoomedCamera != null)
        {
            _zoomedFramer = zoomedCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
            if (_zoomedFramer != null)
            {
                _defaultOffsetX = _zoomedFramer.m_TrackedObjectOffset.x;
                _currentLeanOffset = _defaultOffsetX;
            }
        }

        // Force initial state
        SetZoom(false, true);
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

        if (_input.cameraDrag)
        {
            if (!_wasDragging)
            {
                _dragAccumulatorY = 0f;
                _leanAccumulatorX = 0f;
                _wasDragging = true;
            }

            float dt = Time.deltaTime;
            _dragAccumulatorY += _input.look.y * sensitivity * dt;

            if (_input.move.sqrMagnitude < 0.01f && _isZoomed)
            {
                _leanAccumulatorX += _input.look.x * leanSensitivity * dt;
                _leanAccumulatorX = Mathf.Clamp(_leanAccumulatorX, -1.0f, 1.0f);
            }
            else
            {
                _leanAccumulatorX = Mathf.MoveTowards(_leanAccumulatorX, 0f, dt * 5.0f);
            }

            if (Mathf.Abs(_dragAccumulatorY) > dragThreshold)
            {
                _leanAccumulatorX = 0f;
                EvaluateZoomDrag();
                _dragAccumulatorY = 0f;
            }
        }
        else
        {
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
        float coneBias = 1.2f;

        bool isVerticalCone = absY > (absX * coneBias);

        if (isVerticalCone)
        {
            if (y > stickThreshold) SetZoom(true);
            else if (y < -stickThreshold) SetZoom(false);
        }

        if (_isZoomed)
        {
            bool isHorizontalCone = absX > (absY * coneBias);
            if (absX > 0.2f && isHorizontalCone)
            {
                _leanAccumulatorX = Mathf.Clamp(x, -1.0f, 1.0f);
            }
            else
            {
                _leanAccumulatorX = 0f;
            }
        }
    }

    private void ApplyLeanAndOffset()
    {
        if (!_isZoomed || _zoomedFramer == null) return;
        if (_input.move != Vector2.zero) _leanAccumulatorX = 0f;

        float targetLeanY = -_leanAccumulatorX * maxLeanAngle;
        float targetOffsetX = _defaultOffsetX + (_leanAccumulatorX * maxLeanOffset);

        _currentLeanRot = Mathf.Lerp(_currentLeanRot, targetLeanY, Time.deltaTime * leanSpeed);
        _currentLeanOffset = Mathf.Lerp(_currentLeanOffset, targetOffsetX, Time.deltaTime * leanSpeed);

        Vector3 currentEuler = zoomedCamera.transform.localEulerAngles;
        zoomedCamera.transform.localRotation = Quaternion.Euler(zoomPitchOffset, _currentLeanRot, currentEuler.z);
        _zoomedFramer.m_TrackedObjectOffset.x = _currentLeanOffset;
    }

    // --- UPDATED LOGIC HERE ---
    private void SetZoom(bool isZoomed, bool force = false)
    {
        // Guard Clause: Only stop redundant "True" calls. 
        // We ALWAYS allow "False" calls to pass through to fix the stuck animation bug.
        if (!force && _isZoomed == isZoomed && isZoomed == true) return;

        _isZoomed = isZoomed;

        // 1. Always stop any pending "Shy" timer
        if (_zoomCoroutine != null) StopCoroutine(_zoomCoroutine);

        if (isZoomed)
        {
            // Zoom IN: Start the timer
            zoomedCamera.Priority = 20;
            standardCamera.Priority = 10;
            _zoomCoroutine = StartCoroutine(ZoomTransitionRoutine());
        }
        else
        {
            // Zoom OUT: Force Cancel IMMEDIATELY (No Coroutine)
            Debug.Log("Zoom Out: Cancelling Shy Animation"); // Check your Console for this!
            if (_thirdPersonController) _thirdPersonController.SetZoomAnimation(false);

            zoomedCamera.Priority = 10;
            standardCamera.Priority = 20;

            _leanAccumulatorX = 0f;
            _currentLeanRot = 0f;
            _currentLeanOffset = _defaultOffsetX;
        }
    }

    private IEnumerator ZoomTransitionRoutine()
    {
        // Wait for Blend + Extra Delay
        yield return new WaitForSeconds(cameraBlendTime + shyAnimationDelay);

        // Only trigger shy if we are STILL zoomed and NOT moving
        if (_isZoomed && _thirdPersonController && _input.move.sqrMagnitude < 0.01f)
        {
            _thirdPersonController.SetZoomAnimation(true);
        }
    }
}