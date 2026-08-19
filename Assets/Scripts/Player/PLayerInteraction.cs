using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerInteractionState
{
    Free,
    Focused
}

public enum InteractionUIState
{
    None,
    Detected,
    Interactable
}



public class PlayerInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;

    [Header("Input")]
    [SerializeField] private InputActionReference interactAction;

    [Header("Interaction")]
    [SerializeField] private float detectionDistance = 8f; // distancia a la que aparece el ícono simple (lejos)
    [SerializeField] private float interactionDistance = 1.5f; // distancia a la que aparece el prompt con la tecla y ya se puede presionar E (cerca)
    [SerializeField] private LayerMask raycastMask;

    private Interactable currentInteractable;
    private int interactableLayer;

    public PlayerInteractionState State { get; private set; } = PlayerInteractionState.Free;

    // La UI (InteractionUIController) lee esto cada frame para decidir qué mostrar.
    public InteractionUIState UIState { get; private set; } = InteractionUIState.None;

    public Transform CurrentUITarget { get; private set; } // el objeto que el ícono/prompt debe seguir en pantalla

  

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
            UIState = InteractionUIState.None; // para ocultar la UI

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
        UIState = InteractionUIState.None;


        Ray ray = playerCamera.ViewportPointToRay( new Vector3(0.5f, 0.5f, 0f));


        if (Physics.Raycast(ray,out RaycastHit hit, interactionDistance, raycastMask, QueryTriggerInteraction.Ignore))
        {
            
            currentInteractable = hit.collider.GetComponentInParent<Interactable>();
            //Unity permite buscar componentes en el objeto impactado o en cualquiera de sus padres mediante GetComponentInParent<T>(), 
            // que es justo lo que nos interesa para objetos compuestos como una puerta.
            if (currentInteractable != null)
            {
                UIState = InteractionUIState.Interactable;
                CurrentUITarget = hit.transform;
                Debug.Log("Detectando objeto interactuable: " + hit.collider.name);
            }
        }

        if (UIState != InteractionUIState.Interactable)
        {
            Transform nearest = FindNearestInteractable();
            if (nearest != null)
            {
                UIState = InteractionUIState.Detected;
                CurrentUITarget = nearest; // <- agregar
            }
        }

    
        Debug.DrawRay(ray.origin, ray.direction * interactionDistance, currentInteractable != null? Color.green: Color.red);
    }


    private Transform FindNearestInteractable()
    {
        Collider[] hitsNearby = Physics.OverlapSphere(transform.position, detectionDistance, raycastMask, QueryTriggerInteraction.Ignore);

        Transform nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider col in hitsNearby)
        {
            if (col.GetComponentInParent<Interactable>() != null)
            {
                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist < nearestDistance)
                {
                    nearestDistance = dist;
                    nearest = col.transform;
                }
            }
        }

        return nearest; // null si no había ninguno
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
