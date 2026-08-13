using UnityEngine;
using UnityEngine.InputSystem;

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
        DetectInteractable();

        if (currentInteractable != null && interactAction.action.WasPressedThisFrame())
        {
            Debug.Log("Interactuando con: " + currentInteractable);
            currentInteractable.Interact();
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


        Debug.DrawRay(ray.origin, ray.direction * interactionDistance, currentInteractable != null? Color.green: Color.red
        );
    }

    
}
