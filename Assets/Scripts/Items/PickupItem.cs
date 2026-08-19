using UnityEngine;

public class PickupItem : MonoBehaviour, Interactable
{
    [SerializeField] private string itemName = "Llave";

    public void Interact()
    {
        Debug.Log("Se guardó: " + itemName);

        // TODO: acá tu compañero va a enganchar el inventario real,
        // por ejemplo algo como InventoryManager.Instance.AddItem(itemName);

        gameObject.SetActive(false); // el objeto "desaparece" del mundo al recogerlo
    }
}