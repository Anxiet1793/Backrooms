using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 4f;
    public float velocidadCorrer = 7f;

    [Header("Mouse")]
    [Tooltip("Prueba valores entre 1 y 4.")]
    public float sensibilidadMouse = 2.2f;

    [Tooltip("Valores bajos dan suavizado sin demasiado retraso.")]
    [Range(0f, 0.1f)]
    public float suavizadoMouse = 0.025f;

    public Transform camaraJugador;

    [Header("Gravedad")]
    public float gravedad = -9.81f;

    private CharacterController controller;

    private Vector3 velocidadVertical;

    private float rotacionVertical;
    private float rotacionHorizontal;

    private Vector2 movimientoMouseSuavizado;
    private Vector2 velocidadSuavizadoMouse;

    private void Start()
    {
        controller = GetComponent<CharacterController>();

        // Busca automáticamente una cámara hija si no fue asignada.
        if (camaraJugador == null)
        {
            Camera camaraEncontrada = GetComponentInChildren<Camera>();

            if (camaraEncontrada != null)
            {
                camaraJugador = camaraEncontrada.transform;
            }
            else
            {
                Debug.LogError(
                    "No se encontró una cámara dentro del jugador."
                );
            }
        }

        rotacionHorizontal = transform.eulerAngles.y;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // Primero rotamos y luego nos movemos en la nueva dirección.
        GirarCamara();
        MoverJugador();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void MoverJugador()
    {
        float movimientoX = Input.GetAxisRaw("Horizontal");
        float movimientoZ = Input.GetAxisRaw("Vertical");

        Vector3 movimiento =
            transform.right * movimientoX +
            transform.forward * movimientoZ;

        // Evita que diagonalmente camine más rápido.
        movimiento = Vector3.ClampMagnitude(movimiento, 1f);

        float velocidadActual =
            Input.GetKey(KeyCode.LeftShift)
                ? velocidadCorrer
                : velocidad;

        controller.Move(
            movimiento * velocidadActual * Time.deltaTime
        );

        if (controller.isGrounded && velocidadVertical.y < 0f)
        {
            velocidadVertical.y = -2f;
        }

        velocidadVertical.y += gravedad * Time.deltaTime;

        controller.Move(
            velocidadVertical * Time.deltaTime
        );
    }

    private void GirarCamara()
    {
        if (camaraJugador == null)
            return;

        // Mouse X/Y ya representan el desplazamiento del mouse.
        // No multiplicamos por Time.deltaTime.
        Vector2 entradaMouse = new Vector2(
            Input.GetAxisRaw("Mouse X"),
            Input.GetAxisRaw("Mouse Y")
        );

        entradaMouse *= sensibilidadMouse;

        if (suavizadoMouse > 0f)
        {
            movimientoMouseSuavizado = Vector2.SmoothDamp(
                movimientoMouseSuavizado,
                entradaMouse,
                ref velocidadSuavizadoMouse,
                suavizadoMouse
            );
        }
        else
        {
            movimientoMouseSuavizado = entradaMouse;
        }

        rotacionHorizontal += movimientoMouseSuavizado.x;
        rotacionVertical -= movimientoMouseSuavizado.y;

        rotacionVertical = Mathf.Clamp(
            rotacionVertical,
            -80f,
            80f
        );

        // Rotación horizontal del cuerpo completo.
        transform.rotation = Quaternion.Euler(
            0f,
            rotacionHorizontal,
            0f
        );

        // Rotación vertical exclusiva de la cámara.
        camaraJugador.localRotation = Quaternion.Euler(
            rotacionVertical,
            0f,
            0f
        );
    }
}