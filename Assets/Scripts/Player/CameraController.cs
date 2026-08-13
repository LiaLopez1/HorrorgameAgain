using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private PlayerController playerController;

    [Header("Input")]
    [SerializeField] private InputActionReference lookAction;

    [Header("Camera settings")]
    [SerializeField] private float mouseSensitivity = 1.0f;
    [SerializeField] private float minLookAngle = -80f;
    [SerializeField] private float maxLookAngle = 80f;

    [Header("Head Bob")]
    [SerializeField] private float bobFrequency = 1.5f;
    [SerializeField] private float bobAmplitude = 0.05f;
    [SerializeField] private float bobSmoothness = 10f;

    [Header("Crouch")]
    [SerializeField] private float crouchPivotOffset = 0.5f;
    [SerializeField] private float crouchTransitionSpeed = 8f;

    private float verticalRotation;
    private float bobTimer;
    private float standingPivotY;
    private float targetPivotY;
    private float currentPivotY;
    private float currentBobOffset;

    private void OnEnable()
    {
        lookAction.action.Enable();
    }

    private void OnDisable()
    {
        lookAction.action.Disable();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        standingPivotY = cameraPivot.localPosition.y;
        currentPivotY = standingPivotY;
    }

    private void Update()
    {
        HandleLook();
        HandleCrouchHeight();
        HandleHeadBob();
    }

    private void HandleLook()
    {
        Vector2 mouseInput = lookAction.action.ReadValue<Vector2>();

        float horizontalRotation = mouseInput.x * mouseSensitivity;
        transform.Rotate(Vector3.up * horizontalRotation);

        verticalRotation -= mouseInput.y * mouseSensitivity;
        verticalRotation = Mathf.Clamp(verticalRotation, minLookAngle, maxLookAngle);
        cameraPivot.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    private void HandleCrouchHeight()
    {
        targetPivotY = playerController.IsCrouching
            ? standingPivotY - crouchPivotOffset
            : standingPivotY;

        currentPivotY = Mathf.Lerp(currentPivotY, targetPivotY, Time.deltaTime * crouchTransitionSpeed);
    }

    private void HandleHeadBob()
    {
        Vector3 horizontalVelocity = new Vector3(characterController.velocity.x, 0f, characterController.velocity.z);
        float moveSpeed = horizontalVelocity.magnitude;

        float targetBob = 0f;

        if (moveSpeed > 0.1f && characterController.isGrounded)
        {
            bobTimer += Time.deltaTime * bobFrequency * moveSpeed;
            targetBob = Mathf.Sin(bobTimer) * bobAmplitude;
        }
        else
        {
            bobTimer = 0f;
        }

        currentBobOffset = Mathf.Lerp(currentBobOffset, targetBob, Time.deltaTime * bobSmoothness);

        cameraPivot.localPosition = new Vector3(0f, currentPivotY + currentBobOffset, 0f);
    }
}