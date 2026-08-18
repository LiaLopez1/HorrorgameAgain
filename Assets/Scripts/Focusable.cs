using UnityEngine;

public interface Focusable //
{
    Transform ViewPoint { get;}
}

//a diferencia del método Interact(), acá defino una propiedad de solo lectura (get sin set).
// Esto obliga a cualquier clase que implemente IFocusable a exponer un Transform,
//  pero no permite que otro script lo modifique desde afuera.