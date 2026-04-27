using UnityEngine;
using UnityEngine.InputSystem;

public class TerceraPersonaCamara : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform objetivo;

    [Header("Configuración")]
    public float distancia    = 4f;
    public float altura       = 2f;
    public float sensibilidad = 3f;
    public float suavizado    = 5f;

    [Header("Límites verticales")]
    public float limiteAbajo  = -20f;
    public float limiteArriba = 60f;

    private float rotacionX = 0f;
    private float rotacionY = 0f;

    /// <summary>Ángulo horizontal de la cámara. PlayerMovement lo usa para moverse relativo a la cámara.</summary>
    public float YawAngle => rotacionY;

    void Start()
    {
        rotacionY = transform.eulerAngles.y;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    void LateUpdate()
    {
        if (objetivo == null) return;

        // Leer ratón con el nuevo Input System
        Vector2 raton = Mouse.current.delta.ReadValue();

        rotacionX -= raton.y * sensibilidad * Time.deltaTime * 10f;
        rotacionY += raton.x * sensibilidad * Time.deltaTime * 10f;

        rotacionX = Mathf.Clamp(rotacionX, limiteAbajo, limiteArriba);

        Quaternion rotacion        = Quaternion.Euler(rotacionX, rotacionY, 0f);
        Vector3    offset          = rotacion * new Vector3(0f, 0f, -distancia);
        Vector3    posicionObjetivo = objetivo.position + Vector3.up * altura + offset;

        transform.position = Vector3.Lerp(transform.position, posicionObjetivo, suavizado * Time.deltaTime);
        transform.LookAt(objetivo.position + Vector3.up * altura);
    }
}