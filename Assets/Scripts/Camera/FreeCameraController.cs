using UnityEngine;
using UnityEngine.InputSystem;

namespace StationeersBuildPlanner.Camera
{
    /// <summary>
    /// Free-look camera controller similar to Stationeers jetpack movement.
    /// Uses the new Input System for proper Unity 6 compatibility.
    ///
    /// Controls:
    /// - WASD: Horizontal/forward movement
    /// - Q/E or Space: Vertical movement
    /// - Right Mouse: Hold to look around
    /// - Scroll: Adjust movement speed
    /// - Shift: Fast movement
    /// - Alt: Slow movement
    /// </summary>
    public class FreeCameraController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float fastMultiplier = 3f;
        [SerializeField] private float slowMultiplier = 0.25f;

        [Header("Look")]
        [SerializeField] private float lookSensitivity = 0.1f;
        [SerializeField] private float maxVerticalAngle = 89f;

        [Header("Speed Adjustment")]
        [SerializeField] private float minSpeed = 1f;
        [SerializeField] private float maxSpeed = 100f;
        [SerializeField] private float scrollSensitivity = 0.5f;

        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;

        // Input action references
        private InputAction moveAction;
        private InputAction verticalMoveAction;
        private InputAction lookAction;
        private InputAction lookEnableAction;
        private InputAction speedModifierAction;
        private InputAction speedScrollAction;

        // State
        private float rotationX;
        private float rotationY;
        private bool isLooking;
        private Vector2 moveInput;
        private float verticalInput;
        private Vector2 lookInput;
        private float speedModifier;

        private void Awake()
        {
            // If no input actions assigned, try to load from Resources
            if (inputActions == null)
            {
                inputActions = Resources.Load<InputActionAsset>("BuildPlannerInput");
            }

            if (inputActions == null)
            {
                Debug.LogError("[FreeCameraController] No InputActionAsset assigned and couldn't load from Resources!");
                enabled = false;
                return;
            }

            SetupInputActions();
        }

        private void SetupInputActions()
        {
            var cameraMap = inputActions.FindActionMap("Camera");
            if (cameraMap == null)
            {
                Debug.LogError("[FreeCameraController] Could not find 'Camera' action map!");
                enabled = false;
                return;
            }

            moveAction = cameraMap.FindAction("Move");
            verticalMoveAction = cameraMap.FindAction("VerticalMove");
            lookAction = cameraMap.FindAction("Look");
            lookEnableAction = cameraMap.FindAction("LookEnable");
            speedModifierAction = cameraMap.FindAction("SpeedModifier");
            speedScrollAction = cameraMap.FindAction("SpeedScroll");

            // Subscribe to look enable button
            if (lookEnableAction != null)
            {
                lookEnableAction.started += OnLookEnableStarted;
                lookEnableAction.canceled += OnLookEnableCanceled;
            }

            // Subscribe to scroll for speed adjustment
            if (speedScrollAction != null)
            {
                speedScrollAction.performed += OnSpeedScroll;
            }
        }

        private void OnEnable()
        {
            inputActions?.Enable();

            // Initialize rotation from current transform
            Vector3 euler = transform.eulerAngles;
            rotationX = euler.y;
            rotationY = euler.x;

            // Normalize rotationY to -180 to 180 range
            if (rotationY > 180f) rotationY -= 360f;
        }

        private void OnDisable()
        {
            inputActions?.Disable();
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (lookEnableAction != null)
            {
                lookEnableAction.started -= OnLookEnableStarted;
                lookEnableAction.canceled -= OnLookEnableCanceled;
            }

            if (speedScrollAction != null)
            {
                speedScrollAction.performed -= OnSpeedScroll;
            }
        }

        private void Update()
        {
            ReadInputValues();
            HandleLook();
            HandleMovement();
        }

        private void ReadInputValues()
        {
            if (moveAction != null)
                moveInput = moveAction.ReadValue<Vector2>();

            if (verticalMoveAction != null)
                verticalInput = verticalMoveAction.ReadValue<float>();

            if (lookAction != null && isLooking)
                lookInput = lookAction.ReadValue<Vector2>();
            else
                lookInput = Vector2.zero;

            if (speedModifierAction != null)
                speedModifier = speedModifierAction.ReadValue<float>();
        }

        private void HandleLook()
        {
            if (!isLooking) return;

            rotationX += lookInput.x * lookSensitivity;
            rotationY -= lookInput.y * lookSensitivity;
            rotationY = Mathf.Clamp(rotationY, -maxVerticalAngle, maxVerticalAngle);

            transform.rotation = Quaternion.Euler(rotationY, rotationX, 0f);
        }

        private void HandleMovement()
        {
            // Calculate speed multiplier from modifier input
            // speedModifier: -1 = slow (Alt), 0 = normal, +1 = fast (Shift)
            float speedMult = 1f;
            if (speedModifier > 0.5f)
                speedMult = fastMultiplier;
            else if (speedModifier < -0.5f)
                speedMult = slowMultiplier;

            // Calculate movement vector
            Vector3 move = transform.right * moveInput.x +
                          transform.forward * moveInput.y +
                          Vector3.up * verticalInput;

            transform.position += move * moveSpeed * speedMult * Time.deltaTime;
        }

        private void OnLookEnableStarted(InputAction.CallbackContext context)
        {
            isLooking = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnLookEnableCanceled(InputAction.CallbackContext context)
        {
            isLooking = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnSpeedScroll(InputAction.CallbackContext context)
        {
            float scrollValue = context.ReadValue<float>();
            moveSpeed = Mathf.Clamp(moveSpeed + scrollValue * scrollSensitivity, minSpeed, maxSpeed);
        }
    }
}
