using UnityEngine;

public class BackroomsExit : MonoBehaviour
{
    public BackroomsObjectiveManager manager;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;

        if (manager != null && manager.ExitUnlocked)
        {
            manager.Escape();
        }
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player") ||
               other.GetComponent<CharacterController>() != null ||
               other.GetComponentInParent<CharacterController>() != null;
    }
}