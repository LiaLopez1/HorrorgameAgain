using UnityEngine;

// Este script no detecta nada por su cuenta: solo LEE el UIState que ya calcula
// PlayerInteraction cada frame, y prende/apaga los elementos visuales correspondientes.
// Así evitamos tener dos raycasts (uno para lógica, otro para UI) y mantenemos
// PlayerInteraction como la única fuente de verdad del estado de interacción.
public class InteractionUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInteraction playerInteraction;

    [Header("UI Elements")]
    [SerializeField] private GameObject detectionIcon;      // ícono simple, aparece en rango de detección (lejos)
    [SerializeField] private GameObject interactionPrompt;  // texto/ícono con la tecla, aparece en rango de interacción (cerca)

    private InteractionUIState lastState = InteractionUIState.None;

    [Header("Camera")]
    [SerializeField] private Camera playerCamera; // arrastrar la misma cámara que usa PlayerInteraction


    private void Update()
    {
        InteractionUIState currentState = playerInteraction.UIState;

        // Solo tocamos los GameObjects si el estado cambió respecto al frame anterior,
        // para no llamar SetActive todos los frames sin necesidad.
        if (currentState != lastState)
        {
            ApplyState(currentState);
            lastState = currentState;
        }

        // Mientras haya algo que mostrar, actualizamos su posición en pantalla cada frame
        if (currentState != InteractionUIState.None && playerInteraction.CurrentUITarget != null)
        {
            UpdateIconPosition(currentState);
        }

        
    }

    private void UpdateIconPosition(InteractionUIState state)
    {
        Vector3 screenPos = playerCamera.WorldToScreenPoint(playerInteraction.CurrentUITarget.position);

        RectTransform target = state == InteractionUIState.Interactable ? interactionPrompt.GetComponent<RectTransform>() 
        : detectionIcon.GetComponent<RectTransform>();
        
        target.position = screenPos;
    }

    private void ApplyState(InteractionUIState state)
    {
        detectionIcon.SetActive(state == InteractionUIState.Detected);
        interactionPrompt.SetActive(state == InteractionUIState.Interactable);
    }
}