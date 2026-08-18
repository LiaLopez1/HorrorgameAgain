using UnityEngine;

public class PruebaInteraccion : MonoBehaviour , Interactable, Focusable
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField] private Transform viewPoint;

    public Transform ViewPoint => viewPoint;
    
    public void Interact()
        {
            Debug.Log("Cilindro interactuado!");
        }
    }
