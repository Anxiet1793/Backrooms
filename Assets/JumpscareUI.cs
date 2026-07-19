using System.Collections;
using UnityEngine;

public class JumpscareUI : MonoBehaviour
{
    public static JumpscareUI Instance;

    [Header("Referencia UI")]
    public GameObject jumpscareImage;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip sonidoJumpscare;
    public bool detenerAudioAlTerminar = false;

    [Header("Configuración")]
    public float duracionJumpscare = 1.5f;

    private bool mostrandoJumpscare = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (jumpscareImage != null)
        {
            jumpscareImage.SetActive(false);
        }
    }

    public void MostrarJumpscare()
    {
        if (!mostrandoJumpscare)
        {
            StartCoroutine(MostrarJumpscareCoroutine());
        }
    }

    private IEnumerator MostrarJumpscareCoroutine()
    {
        mostrandoJumpscare = true;

        if (jumpscareImage != null)
        {
            jumpscareImage.SetActive(true);
        }

        if (audioSource != null && sonidoJumpscare != null)
        {
            audioSource.PlayOneShot(sonidoJumpscare);
        }

        yield return new WaitForSeconds(duracionJumpscare);

        if (jumpscareImage != null)
        {
            jumpscareImage.SetActive(false);
        }

        if (detenerAudioAlTerminar && audioSource != null)
        {
            audioSource.Stop();
        }

        mostrandoJumpscare = false;
    }
}