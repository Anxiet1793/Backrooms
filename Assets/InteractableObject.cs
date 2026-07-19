using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    [Header("Datos del objeto")]
    public string nombreObjeto = "Objeto raro";

    [Header("Comportamiento")]
    public bool desaparecerAlInteractuar = true;
    public bool activarJumpscare = true;

    public void Interactuar()
    {
        if (activarJumpscare && JumpscareUI.Instance != null)
        {
            JumpscareUI.Instance.MostrarJumpscare();
        }

        if (desaparecerAlInteractuar)
        {
            gameObject.SetActive(false);
        }
    }
}