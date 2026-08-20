using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private PlayerController playerController;

    [SerializeField] private Transform cameraTransform; // arrastrar el Main Camera real
    [SerializeField] private PlayerInteraction playerInteraction;

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
    [SerializeField] private float crouchPivotzOffset = 0F;
    [SerializeField] private float crouchTransitionSpeed = 8f;

    [Header("Focus")]   
    [SerializeField] private float focusTransitionSpeed = 6f;



    private float verticalRotation;
    private float bobTimer;

    private float standingPivotY;
    private float targetPivotY;
    private float currentPivotY;

    private float standingPivotZ;
    private float targetPivotZ;
    private float currentPivotZ;


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

        standingPivotY = cameraPivot.localPosition.y;
        currentPivotY = standingPivotY;

        standingPivotZ = cameraPivot.localPosition.z;
        currentPivotZ = standingPivotZ;


    }

    private void Update()
    {

        if (playerInteraction.State == PlayerInteractionState.Focused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            HandleFocusedCamera();
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;


        HandleReturnFromFocus();
        HandleLook();
        HandleCrouchHeight();
        HandleHeadBob();
    }

    private void HandleLook()
    {
        Vector2 mouseInput = lookAction.action.ReadValue<Vector2>();

        float horizontalRotation = mouseInput.x * mouseSensitivity;
        transform.Rotate(Vector3.up * horizontalRotation); //rota el objeto completo por eso esta en el player

        verticalRotation -= mouseInput.y * mouseSensitivity;
        verticalRotation = Mathf.Clamp(verticalRotation, minLookAngle, maxLookAngle);
        cameraPivot.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    private void HandleCrouchHeight()
    {
        targetPivotY = playerController.IsCrouching ? standingPivotY - crouchPivotOffset: standingPivotY;
        currentPivotY = Mathf.Lerp(currentPivotY, targetPivotY, Time.deltaTime * crouchTransitionSpeed);

        targetPivotZ = playerController.IsCrouching ? standingPivotZ + crouchPivotzOffset : standingPivotZ;
        currentPivotZ = Mathf.Lerp( currentPivotZ , targetPivotZ, Time.deltaTime * crouchTransitionSpeed);

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

        cameraPivot.localPosition = new Vector3(0f, currentPivotY + currentBobOffset, currentPivotZ);
    }


    private void HandleFocusedCamera()
    {
        Transform target = playerInteraction.CurrentViewPoint;

        cameraTransform.position = Vector3.Lerp(cameraTransform.position, target.position, Time.deltaTime * focusTransitionSpeed);
        cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, target.rotation, Time.deltaTime * focusTransitionSpeed);
    }

    private void HandleReturnFromFocus()
    {
        // Si la cámara quedó desplazada de su posición local normal (0,0,0) tras un enfoque, la devolvemos suavemente
        if (cameraTransform.localPosition != Vector3.zero || cameraTransform.localRotation != Quaternion.identity)
        {
            cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, Vector3.zero, Time.deltaTime * focusTransitionSpeed);
            cameraTransform.localRotation = Quaternion.Slerp(cameraTransform.localRotation, Quaternion.identity, Time.deltaTime * focusTransitionSpeed);
        }
    }
}