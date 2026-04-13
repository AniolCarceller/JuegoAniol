using UnityEngine;

public class TerceraPersonaCamara : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform objetivo;          // Arrastra el jugador aquí

    [Header("Configuración")]
    public float distancia = 4f;
    public float altura = 2f;
    public float sensibilidad = 3f;
    public float suavizado = 5f;

    [Header("Límites verticales")]
    public float limiteAbajo = -20f;
    public float limiteArriba = 60f;

    private float rotacionX = 0f;
    private float rotacionY = 0f;

    void Start()
    {
        rotacionY = transform.eulerAngles.y;
    }

    void LateUpdate()  // LateUpdate para que vaya después del movimiento del jugador
    {
        if (objetivo == null) return;

        // Leer ratón
        rotacionX -= Input.GetAxis("Mouse Y") * sensibilidad;
        rotacionY += Input.GetAxis("Mouse X") * sensibilidad;

        // Limitar vertical
        rotacionX = Mathf.Clamp(rotacionX, limiteAbajo, limiteArriba);

        // Calcular posición detrás del jugador
        Quaternion rotacion = Quaternion.Euler(rotacionX, rotacionY, 0f);
        Vector3 offset = rotacion * new Vector3(0f, 0f, -distancia);
        Vector3 posicionObjetivo = objetivo.position + Vector3.up * altura + offset;

        // Mover la cámara suavemente
        transform.position = Vector3.Lerp(transform.position, posicionObjetivo, suavizado * Time.deltaTime);
        transform.LookAt(objetivo.position + Vector3.up * altura);
    }
}