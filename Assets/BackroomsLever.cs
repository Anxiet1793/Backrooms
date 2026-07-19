using UnityEngine;

public class BackroomsLever : MonoBehaviour
{
    public BackroomsObjectiveManager manager;
    public Transform handle;

    private bool activated = false;
    private Renderer[] renderers;

    public bool Activated => activated;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();

        if (handle == null)
        {
            Transform foundHandle = transform.Find("Lever_Handle");
            if (foundHandle != null)
                handle = foundHandle;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (activated) return;

        if (IsPlayer(other))
        {
            Activate();
        }
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player") ||
               other.GetComponent<CharacterController>() != null ||
               other.GetComponentInParent<CharacterController>() != null;
    }

    private void Activate()
    {
        activated = true;

        if (handle != null)
        {
            handle.localRotation = Quaternion.Euler(-55f, 0f, 0f);
        }

        foreach (Renderer r in renderers)
        {
            r.material.color = Color.green;
        }

        if (manager != null)
        {
            manager.LeverActivated(this);
        }
    }
}