using UnityEngine;
using System.Collections.Generic;

// Este script no detecta nada por su cuenta: solo LEE el UIState que ya calcula
// PlayerInteraction cada frame, y prende/apaga los elementos visuales correspondientes.
// Así evitamos tener dos raycasts (uno para lógica, otro para UI) y mantenemos
// PlayerInteraction como la única fuente de verdad del estado de interacción.
public class InteractionUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInteraction playerInteraction;

    [Header("Pooling de íconos de detección")]
    [SerializeField] private GameObject detectionIconPrefab; // el PREFAB del ícono, no un objeto de la escena
    [SerializeField] private Transform iconsParent; // dónde van a vivir las copias (puede ser el mismo Canvas)


//
    private Dictionary<Transform, RectTransform> activeIcons = new Dictionary<Transform, RectTransform>(); // qué ícono le corresponde a cada objeto
    private List<RectTransform> inactivePool = new List<RectTransform>(); // íconos creados pero sin usar ahora, listos para reciclar
    private HashSet<Transform> stillVisible = new HashSet<Transform>(); // reutilizado cada frame, para no generar basura creando uno nuevo cada vez

    [Header("UI Elements")]
    [SerializeField] private GameObject interactionPrompt;  // texto/ícono con la tecla, aparece en rango de interacción (cerca)

    private InteractionUIState lastState = InteractionUIState.None;

    [Header("Camera")]
    [SerializeField] private Camera playerCamera; // arrastrar la misma cámara que usa PlayerInteraction

    [Header("Oclusión")]
    [SerializeField] private LayerMask obstructionMask;


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
            Vector3 screenPos = playerCamera.WorldToScreenPoint(playerInteraction.CurrentUITarget.position);
            interactionPrompt.GetComponent<RectTransform>().position = screenPos;
        }

        UpdateDetectionIcons();
    }


    private void UpdateDetectionIcons()
    {
        List<Transform> nearby = playerInteraction.NearbyInteractables;
        Transform aimed = playerInteraction.UIState == InteractionUIState.Interactable ? playerInteraction.CurrentUITarget : null;

        stillVisible.Clear();

        foreach (Transform t in nearby)
        {
            if (t == aimed) continue;

            Vector3 screenPos = playerCamera.WorldToScreenPoint(t.position);
            if (screenPos.z <= 0f) continue;
            if (!HasLineOfSight(t)) continue;

            RectTransform icon = GetIconFor(t);
            icon.position = screenPos;
            stillVisible.Add(t);
        }

        // Cualquier objeto que tenía ícono el frame pasado pero ya no califica ahora, se apaga y vuelve al pool
        List<Transform> toRemove = new List<Transform>();
        foreach (var kvp in activeIcons)
        {
            if (!stillVisible.Contains(kvp.Key))
            {
                kvp.Value.gameObject.SetActive(false);
                inactivePool.Add(kvp.Value);
                toRemove.Add(kvp.Key);
            }
        }
        foreach (Transform t in toRemove)
        {
            activeIcons.Remove(t);
        }
    }
    
    private RectTransform GetIconFor(Transform t)
    {
        if (activeIcons.TryGetValue(t, out RectTransform existing))
        {
            return existing; // este objeto ya tenía un ícono propio, lo reusamos — nunca cambia de instancia
        }

        RectTransform icon;
        if (inactivePool.Count > 0)
        {
            icon = inactivePool[inactivePool.Count - 1];
            inactivePool.RemoveAt(inactivePool.Count - 1);
        }
        else
        {
            GameObject newIcon = Instantiate(detectionIconPrefab, iconsParent);
            icon = newIcon.GetComponent<RectTransform>();
        }

        icon.gameObject.SetActive(true);
        activeIcons[t] = icon;
        return icon;
    }

    private void ApplyState(InteractionUIState state)
    {
        interactionPrompt.SetActive(state == InteractionUIState.Interactable);
    }

    private bool HasLineOfSight(Transform target)
    {
        Vector3 origin = playerCamera.transform.position;
        Vector3 toTarget = target.position - origin;
        float distance = toTarget.magnitude;

        if (Physics.Raycast(origin, toTarget.normalized, out RaycastHit hit, distance, obstructionMask, QueryTriggerInteraction.Ignore))
        {
            // Si lo primero que el rayo golpea no es el propio objeto (ni un hijo suyo), algo lo está tapando
            return hit.transform == target || hit.transform.IsChildOf(target);
        }

        return true; // el rayo no golpeó nada en el camino: vista despejada
    }
}