using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 2.5f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference crouchAction;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float animationDampTime = 0.15f;


    private CharacterController characterController;

    private float verticalVelocity;
    private bool isCrouching;


    // Guardamos el ID del parámetro para no buscar
    // constantemente el string "Speed".
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static  readonly int crouchHash = Animator.StringToHash("IsCrouching");


    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }


    private void OnEnable()
    {
        moveAction.action.Enable();
        crouchAction.action.Enable();
    }


    private void OnDisable()
    {
        moveAction.action.Disable();
        crouchAction.action.Disable();
    }


    private void Update()
    {
        HandleCrouch();
        HandleMovement();
    }


    private void HandleMovement()
    {
        // Leemos WASD desde el nuevo Input System.
        Vector2 input = moveAction.action.ReadValue<Vector2>();


        // Convertimos el Vector2 del input en una dirección tridimensional.
        Vector3 moveDirection = transform.right * input.x + transform.forward * input.y;


        // Evitamos que moverse en diagonal sea más rápido.
        moveDirection = Vector3.ClampMagnitude( moveDirection, 1f );


        // Si estamos tocando el suelo mantenemos
        // una pequeña fuerza hacia abajo.
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }


        // CharacterController no aplica gravedad solo.
        verticalVelocity += gravity * Time.deltaTime;
        Vector3 velocity = moveDirection * walkSpeed;
        velocity.y = verticalVelocity;
        characterController.Move( velocity * Time.deltaTime);
        UpdateAnimation(moveDirection);
    }

    private void HandleCrouch()
    {
        if(crouchAction.action.WasPressedThisFrame())
        {
            //togle
            isCrouching = !isCrouching;
            animator.SetBool(crouchHash, isCrouching);
        }
    }


    private void UpdateAnimation(Vector3 moveDirection)
    {
        float speed = moveDirection.magnitude;


        animator.SetFloat(  SpeedHash, speed, animationDampTime,Time.deltaTime);
    }
}