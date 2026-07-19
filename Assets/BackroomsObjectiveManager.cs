using UnityEngine;

public class BackroomsObjectiveManager : MonoBehaviour
{
    public int totalLevers = 3;
    public int activatedLevers = 0;
    public GameObject exitObject;

    public bool ExitUnlocked => activatedLevers >= totalLevers;

    public void Setup(int total, GameObject exit)
    {
        totalLevers = total;
        activatedLevers = 0;
        exitObject = exit;

        if (exitObject != null)
            exitObject.SetActive(false);

        Debug.Log("Encuentra " + totalLevers + " palancas para revelar la salida.");
    }

    public void LeverActivated(BackroomsLever lever)
    {
        activatedLevers++;

        Debug.Log("Palanca activada: " + activatedLevers + "/" + totalLevers);

        if (ExitUnlocked)
        {
            if (exitObject != null)
                exitObject.SetActive(true);

            Debug.Log("Las 3 palancas fueron activadas. La salida apareció.");
        }
    }

    public void Escape()
    {
        if (!ExitUnlocked) return;

        Debug.Log("ESCAPASTE DE LOS BACKROOMS.");
    }
}