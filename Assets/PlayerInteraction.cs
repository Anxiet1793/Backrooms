using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Configuración")]
    public Camera camaraJugador;
    public float distanciaInteraccion = 3f;
    public KeyCode teclaInteraccion = KeyCode.E;

    private InteractableObject objetoActual;

    void Update()
    {
        DetectarObjetoInteractuable();

        if (objetoActual != null && Input.GetKeyDown(teclaInteraccion))
        {
            objetoActual.Interactuar();
            objetoActual = null;
        }
    }

    void DetectarObjetoInteractuable()
    {
        Ray rayo = new Ray(camaraJugador.transform.position, camaraJugador.transform.forward);

        if (Physics.Raycast(rayo, out RaycastHit hit, distanciaInteraccion))
        {
            InteractableObject objetoDetectado = hit.collider.GetComponent<InteractableObject>();

            if (objetoDetectado != null)
            {
                if (objetoActual != objetoDetectado)
                {
                    objetoActual = objetoDetectado;
                    Debug.Log("Presiona E para interactuar con: " + objetoActual.nombreObjeto);
                }

                return;
            }
        }

        objetoActual = null;
    }
}