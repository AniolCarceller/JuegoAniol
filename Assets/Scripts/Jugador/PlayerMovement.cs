using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 5f;
    public float gravedad = -9.81f;
    public float alturasSalto = 1.5f;

    private CharacterController controller;
    private Vector3 velocidadVertical;
    private Transform camara;

    // Input
    private Vector2 inputMovimiento;
    private bool saltando;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        camara = Camera.main.transform;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Estos métodos los llama automáticamente el Input System
    public void OnMove(InputValue value)
    {
        inputMovimiento = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        saltando = value.isPressed;
    }

    void Update()
    {
        // Movimiento relativo a la cámara
        Vector3 direccion = camara.forward * inputMovimiento.y + camara.right * inputMovimiento.x;
        direccion.y = 0f;

        if (direccion.magnitude > 0.1f)
        {
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, 10f * Time.deltaTime);
            controller.Move(direccion.normalized * velocidad * Time.deltaTime);
        }

        // Gravedad
        if (controller.isGrounded && velocidadVertical.y < 0)
            velocidadVertical.y = -2f;

        // Salto
        if (saltando && controller.isGrounded)
            velocidadVertical.y = Mathf.Sqrt(alturasSalto * -2f * gravedad);

        velocidadVertical.y += gravedad * Time.deltaTime;
        controller.Move(velocidadVertical * Time.deltaTime);
    }
}

