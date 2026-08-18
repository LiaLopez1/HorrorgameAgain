using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerInteractionState
{
    Free,
    Focused
}

public class PlayerInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;

    [Header("Input")]
    [SerializeField] private InputActionReference interactAction;

    [Header("Interaction")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask raycastMask;

    private Interactable currentInteractable;
    private int interactableLayer;

    public PlayerInteractionState State { get; private set; } = PlayerInteractionState.Free;

    private void OnEnable()
    {
        interactAction.action.Enable();
    }


    private void OnDisable()
    {
        interactAction.action.Disable();
    }

    // Update is called once per frame
    private void Update()
    {
        if (State == PlayerInteractionState.Focused)
        //Si estamos enfocando un objeto, no seguimos detectando nuevos objetos ni podemos movernos
        { 
            if (interactAction.action.WasPressedThisFrame())
            {
                ExitFocusMode();
            }
            
            return;
        }

        DetectInteractable();

        if (currentInteractable != null && interactAction.action.WasPressedThisFrame())
        {
            //Debug.Log("Interactuando con: " + currentInteractable);
            currentInteractable.Interact();

            if(currentInteractable is Focusable focusable)
            {
                EnterFocusMode(focusable.ViewPoint);
            }
        }
    }


    private void DetectInteractable()
    { 
        currentInteractable = null;


        Ray ray = playerCamera.ViewportPointToRay( new Vector3(0.5f, 0.5f, 0f));


        if (Physics.Raycast(ray,out RaycastHit hit, interactionDistance, raycastMask, QueryTriggerInteraction.Ignore))
        {
            currentInteractable = hit.collider.GetComponentInParent<Interactable>();
            //Unity permite buscar componentes en el objeto impactado o en cualquiera de sus padres mediante GetComponentInParent<T>(), 
            // que es justo lo que nos interesa para objetos compuestos como una puerta.
            if (currentInteractable != null)
            {
                Debug.Log("Detectando objeto interactuable: " + hit.collider.name);
            }
        }

        Debug.DrawRay(ray.origin, ray.direction * interactionDistance, currentInteractable != null? Color.green: Color.red);
    }

    private Transform currentViewPoint;
    public Transform CurrentViewPoint => currentViewPoint;

    public void EnterFocusMode(Transform viewPoint)
    {
        State = PlayerInteractionState.Focused;
        currentViewPoint = viewPoint;
    }

    public void ExitFocusMode()
    {
        State = PlayerInteractionState.Free; 
    }

    
}
