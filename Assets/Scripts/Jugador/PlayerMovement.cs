using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    // ══════════════════════════════════════════
    // MOVIMIENTO
    // ══════════════════════════════════════════
    [Header("Velocidad")]
    public float velocidadAndar   = 5f;
    public float velocidadCorrer  = 9f;
    public float aceleracion      = 15f;   // Qué tan rápido llega a la velocidad objetivo
    public float deceleracion     = 20f;   // Qué tan rápido frena al soltar el stick

    // ══════════════════════════════════════════
    // SALTO
    // ══════════════════════════════════════════
    [Header("Salto")]
    public float alturaMaximaSalto  = 2.5f;  // Altura si mantienes pulsado
    public float alturaMinimaSalto  = 0.8f;  // Altura si sueltas enseguida
    public float gravedad           = -25f;
    public float gravedad_Caida     = -40f;  // Caída más rápida para feel de peso
    [Tooltip("Segundos de gracia para saltar al caer de un borde")]
    public float tiempoCoyote       = 0.12f;
    [Tooltip("Segundos de buffer para el botón de salto")]
    public float bufferSalto        = 0.12f;

    // ══════════════════════════════════════════
    // IMPULSO DE AGUA (Mario Sunshine style)
    // ══════════════════════════════════════════
    [Header("Impulso de agua")]
    public float fuerzaImpulsoAgua  = 14f;
    public float aguaPorImpulso     = 10f;   // Agua que consume cada impulso
    [HideInInspector] public bool impulsoActivo = false; // Lo activa JugadorCombate

    // ══════════════════════════════════════════
    // PRIVADOS
    // ══════════════════════════════════════════
    private CharacterController controller;
    private Transform camara;
    private GestorRecursos gestorRecursos;
    private TerceraPersonaCamara camaraScript;

    // Velocidad horizontal real (acumulada suavemente)
    private Vector3 velocidadHorizontal = Vector3.zero;
    private float   velocidadVertical   = 0f;

    // Coyote time
    private float timerCoyote = 0f;
    private bool  enSueloFrame = false;

    // Buffer de salto
    private float timerBufferSalto = 0f;

    // Salto variable
    private bool  saltandoArriba = false;   // Sigue subiendo y el botón está pulsado

    // Input crudo
    private Vector2 inputMovimiento;
    private bool    botonSalto       = false;
    private bool    botonSaltoSuelto = false;  // Flanco de bajada
    private bool    corriendo        = false;

    // ══════════════════════════════════════════
    // INICIALIZACIÓN
    // ══════════════════════════════════════════
    void Start()
    {
        controller       = GetComponent<CharacterController>();
        camara           = Camera.main.transform;
        camaraScript     = Camera.main.GetComponent<TerceraPersonaCamara>();
        gestorRecursos   = GetComponent<GestorRecursos>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    // ── Input System callbacks ─────────────────
    public void OnMove(InputValue value)  => inputMovimiento = value.Get<Vector2>();
    public void OnJump(InputValue value)
    {
        bool pulsado = value.isPressed;
        if (pulsado)
        {
            botonSalto = true;
            timerBufferSalto = bufferSalto; // Guardar intención de salto
        }
        else
        {
            botonSalto       = false;
            botonSaltoSuelto = true;        // Señalar que soltó (para cortar el salto)
        }
    }
    public void OnSprint(InputValue value) => corriendo = value.isPressed;

    // ══════════════════════════════════════════
    // UPDATE
    // ══════════════════════════════════════════
    void Update()
    {
        bool enSuelo = controller.isGrounded;

        // ── Coyote time ───────────────────────
        if (enSuelo)
        {
            timerCoyote  = tiempoCoyote;
            saltandoArriba = false;

            if (velocidadVertical < -2f)
                velocidadVertical = -2f;   // Pequeña fuerza hacia abajo para mantener contacto
        }
        else
        {
            timerCoyote -= Time.deltaTime;
        }

        // ── Buffer de salto ───────────────────
        timerBufferSalto -= Time.deltaTime;

        // ── ¿Puede saltar? ────────────────────
        bool puedeUsarCoyote = timerCoyote > 0f && !enSuelo; // Cayendo pero dentro del margen
        bool tieneBufferSalto = timerBufferSalto > 0f;

        if (tieneBufferSalto && (enSuelo || puedeUsarCoyote))
        {
            Saltar(alturaMaximaSalto);
            timerBufferSalto = 0f;
            timerCoyote      = 0f;
        }

        // ── Cortar salto al soltar el botón ───
        if (botonSaltoSuelto)
        {
            botonSaltoSuelto = false;
            if (saltandoArriba && velocidadVertical > 0f)
            {
                // Cortar hasta la velocidad mínima equivalente
                float velocidadMin = Mathf.Sqrt(2f * Mathf.Abs(gravedad) * alturaMinimaSalto);
                velocidadVertical  = Mathf.Min(velocidadVertical, velocidadMin);
            }
        }

        // ── Impulso de agua ───────────────────
        if (impulsoActivo)
        {
            impulsoActivo = false;
            if (gestorRecursos != null && gestorRecursos.Consumir(TipoRecurso.Agua, aguaPorImpulso))
            {
                velocidadVertical = fuerzaImpulsoAgua;
                saltandoArriba    = true;
                Debug.Log("¡Impulso de agua!");
            }
        }

        // ── Gravedad diferenciada ─────────────
        if (!enSuelo)
        {
            float g = (velocidadVertical > 0f) ? gravedad : gravedad_Caida;
            velocidadVertical += g * Time.deltaTime;

            // Si está subiendo y soltó el botón, saltandoArriba se gestiona arriba
            if (velocidadVertical < 0f)
                saltandoArriba = false;
        }

        // ── Movimiento horizontal ─────────────
        // Leemos el YawAngle directamente desde TerceraPersonaCamara
        // porque LookAt sobreescribe eulerAngles y no podemos fiarnos de él
        float yaw = camaraScript != null ? camaraScript.YawAngle : camara.eulerAngles.y;
        Quaternion camYaw     = Quaternion.Euler(0f, yaw, 0f);
        Vector3 camaraForward = camYaw * Vector3.forward;
        Vector3 camaraRight   = camYaw * Vector3.right;

        Vector3 direccion = camaraForward * inputMovimiento.y
                          + camaraRight   * inputMovimiento.x;

        // Línea verde en Scene que muestra hacia dónde va el jugador
        Debug.DrawRay(transform.position + Vector3.up, direccion * 2f, Color.green);

        float velObjetivo = corriendo ? velocidadCorrer : velocidadAndar;
        Vector3 objetivoHorizontal = direccion.normalized * (direccion.magnitude > 0.1f ? velObjetivo : 0f);

        // Escoger tasa: acelerar si hay input, frenar si no
        float tasa = (direccion.magnitude > 0.1f) ? aceleracion : deceleracion;
        velocidadHorizontal = Vector3.MoveTowards(velocidadHorizontal, objetivoHorizontal, tasa * Time.deltaTime);

        // ── Rotación suave hacia donde se mueve ─
        if (velocidadHorizontal.magnitude > 0.1f)
        {
            Quaternion rotObjetivo = Quaternion.LookRotation(velocidadHorizontal.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotObjetivo, 15f * Time.deltaTime);
        }

        // ── Aplicar movimiento total ──────────
        Vector3 movimientoTotal = velocidadHorizontal + Vector3.up * velocidadVertical;
        controller.Move(movimientoTotal * Time.deltaTime);

        enSueloFrame = enSuelo;
    }

    // ══════════════════════════════════════════
    // SALTAR
    // ══════════════════════════════════════════
    void Saltar(float altura)
    {
        // v = sqrt(2 * |g| * h)
        velocidadVertical = Mathf.Sqrt(2f * Mathf.Abs(gravedad) * altura);
        saltandoArriba    = true;
    }

    // ══════════════════════════════════════════
    // API PÚBLICA (para JugadorCombate)
    // ══════════════════════════════════════════

    /// <summary>Activa un impulso vertical consumiendo agua. Llamar desde JugadorCombate.</summary>
    public void ActivarImpulsoAgua() => impulsoActivo = true;

    /// <summary>Devuelve si el jugador está en el suelo.</summary>
    public bool EstaEnSuelo() => controller.isGrounded;

    /// <summary>Velocidad horizontal actual (útil para animaciones).</summary>
    public float VelocidadHorizontal() => velocidadHorizontal.magnitude;
}