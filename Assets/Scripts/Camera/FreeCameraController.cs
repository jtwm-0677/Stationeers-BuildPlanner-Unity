using UnityEngine;

namespace StationeersBuildPlanner.Camera
{
    /// <summary>
    /// Free-look camera controller similar to Stationeers jetpack movement.
    /// WASD + QE for movement, mouse for look, scroll for speed.
    /// </summary>
    public class FreeCameraController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float fastMultiplier = 3f;
        [SerializeField] private float slowMultiplier = 0.25f;

        [Header("Look")]
        [SerializeField] private float lookSensitivity = 2f;
        [SerializeField] private float maxVerticalAngle = 89f;

        [Header("Zoom")]
        [SerializeField] private float minSpeed = 1f;
        [SerializeField] private float maxSpeed = 100f;
        [SerializeField] private float scrollSensitivity = 5f;

        private float rotationX = 0f;
        private float rotationY = 0f;
        private bool isLooking = false;

        private void Start()
        {
            // Initialize rotation from current transform
            Vector3 euler = transform.eulerAngles;
            rotationX = euler.y;
            rotationY = euler.x;
        }

        private void Update()
        {
            HandleLookInput();
            HandleMovementInput();
            HandleSpeedAdjustment();
        }

        private void HandleLookInput()
        {
            // Right mouse button to look
            if (Input.GetMouseButtonDown(1))
            {
                isLooking = true;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (Input.GetMouseButtonUp(1))
            {
                isLooking = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (isLooking)
            {
                rotationX += Input.GetAxis("Mouse X") * lookSensitivity;
                rotationY -= Input.GetAxis("Mouse Y") * lookSensitivity;
                rotationY = Mathf.Clamp(rotationY, -maxVerticalAngle, maxVerticalAngle);

                transform.rotation = Quaternion.Euler(rotationY, rotationX, 0f);
            }
        }

        private void HandleMovementInput()
        {
            // Get input
            float horizontal = Input.GetAxis("Horizontal"); // A/D
            float vertical = Input.GetAxis("Vertical");     // W/S
            float upDown = 0f;

            if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Space))
                upDown = 1f;
            else if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftControl))
                upDown = -1f;

            // Calculate speed multiplier
            float speedMult = 1f;
            if (Input.GetKey(KeyCode.LeftShift))
                speedMult = fastMultiplier;
            else if (Input.GetKey(KeyCode.LeftAlt))
                speedMult = slowMultiplier;

            // Calculate movement
            Vector3 move = transform.right * horizontal +
                          transform.forward * vertical +
                          Vector3.up * upDown;

            transform.position += move * moveSpeed * speedMult * Time.deltaTime;
        }

        private void HandleSpeedAdjustment()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                moveSpeed = Mathf.Clamp(moveSpeed + scroll * scrollSensitivity, minSpeed, maxSpeed);
            }
        }
    }
}
