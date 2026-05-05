using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(GestorRecursos))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Cámara")]
    [Tooltip("Arrastra aquí la Main Camera (la que tiene CinemachineBrain).")]
    public Transform camaraTransform;

    [Header("Velocidad")]
    public float velocidadAndar      = 9f;    // +3 — respuesta inmediata al caminar
    public float velocidadCorrer     = 16f;   // +4 — sprint se nota de verdad
    public float tasaCambioVelocidad = 18f;   // aceleración muy rápida, sin inercia molesta

    [Header("Rotación del personaje")]
    [Range(0f, 0.3f)]
    public float suavizadoRotacion = 0.06f;   // más ágil al girar

    [Header("Salto y Gravedad")]
    public float alturasSalto = 5f;    // salto alto tipo Mario
    public float gravedad     = -35f;  // caída rápida y contundente
    public float timeoutSalto = 0.50f;
    public float timeoutCaida = 0.15f;

    [Header("Suelo")]
    public bool      enSuelo     = true;
    public float     offsetSuelo = -0.14f;
    public float     radioSuelo  = 0.28f;
    public LayerMask capasSuelo;

    [Header("Impulso agua")]
    public float fuerzaImpulso = 28f;   // propulsión potente tipo FLUDD
    public float consumoAgua   = 30f;

    private CharacterController controller;
    private GestorRecursos       recursos;
    private PlayerInput          playerInput;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    private float _speed;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private const float _terminalVelocity = 53f;

    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

    private bool impulsoActivo    = false;
    private bool disparandoActivo = false;

    void Awake()
    {
        controller  = GetComponent<CharacterController>();
        recursos    = GetComponent<GestorRecursos>();
        playerInput = GetComponent<PlayerInput>();
    }

    void Start()
    {
        if (camaraTransform == null)
        {
            if (Camera.main != null)
                camaraTransform = Camera.main.transform;
            else
            {
                Debug.LogError("[PlayerMovement] Asigna la Main Camera al campo 'Camara Transform'.");
                enabled = false;
                return;
            }
        }

        moveAction   = playerInput.actions.FindAction("Move",   true);
        jumpAction   = playerInput.actions.FindAction("Jump",   true);
        sprintAction = playerInput.actions.FindAction("Sprint", false);

        moveAction.Enable();
        jumpAction.Enable();
        if (sprintAction != null) sprintAction.Enable();

        _jumpTimeoutDelta = timeoutSalto;
        _fallTimeoutDelta = timeoutCaida;
    }

    void Update()
    {
        GroundedCheck();
        JumpAndGravity();
        Move();
    }

    void GroundedCheck()
    {
        Vector3 sphere = new Vector3(
            transform.position.x,
            transform.position.y - offsetSuelo,
            transform.position.z);

        enSuelo = controller.isGrounded ||
                  Physics.CheckSphere(sphere, radioSuelo, capasSuelo, QueryTriggerInteraction.Ignore);
    }

    void Move()
    {
        if (camaraTransform == null) return;

        Vector2 input  = moveAction.ReadValue<Vector2>();
        bool    sprint = sprintAction != null && sprintAction.IsPressed();

        float targetSpeed    = (input == Vector2.zero) ? 0f : (sprint ? velocidadCorrer : velocidadAndar);
        float currentH       = new Vector3(controller.velocity.x, 0f, controller.velocity.z).magnitude;
        float inputMagnitude = Mathf.Clamp01(input.magnitude);

        if (Mathf.Abs(currentH - targetSpeed) > 0.1f)
        {
            _speed = Mathf.Lerp(currentH, targetSpeed * inputMagnitude, Time.deltaTime * tasaCambioVelocidad);
            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        }
        else
        {
            _speed = targetSpeed;
        }

        Vector3 camF = camaraTransform.forward; camF.y = 0f; camF.Normalize();
        Vector3 camR = camaraTransform.right;   camR.y = 0f; camR.Normalize();
        Vector3 moveDir = camF * input.y + camR * input.x;

        if (disparandoActivo && input == Vector2.zero)
        {
            float yawCam = camaraTransform.eulerAngles.y;
            float rot = Mathf.SmoothDampAngle(
                transform.eulerAngles.y, yawCam, ref _rotationVelocity, suavizadoRotacion);
            transform.rotation = Quaternion.Euler(0f, rot, 0f);
        }
        else if (moveDir.sqrMagnitude > 0.0001f)
        {
            moveDir.Normalize();
            float targetRot = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
            float rot = Mathf.SmoothDampAngle(
                transform.eulerAngles.y, targetRot, ref _rotationVelocity, suavizadoRotacion);
            transform.rotation = Quaternion.Euler(0f, rot, 0f);
        }

        if (moveDir.sqrMagnitude < 0.0001f) moveDir = Vector3.zero;

        controller.Move(
            moveDir    * (_speed            * Time.deltaTime) +
            Vector3.up * (_verticalVelocity * Time.deltaTime)
        );

        disparandoActivo = false;
    }

    void JumpAndGravity()
    {
        if (enSuelo)
        {
            _fallTimeoutDelta = timeoutCaida;

            if (_verticalVelocity < 0f)
                _verticalVelocity = -2f;

            if (jumpAction.WasPressedThisFrame() && _jumpTimeoutDelta <= 0f)
                _verticalVelocity = Mathf.Sqrt(alturasSalto * -2f * gravedad);

            if (_jumpTimeoutDelta >= 0f)
                _jumpTimeoutDelta -= Time.deltaTime;
        }
        else
        {
            _jumpTimeoutDelta = timeoutSalto;
            if (_fallTimeoutDelta >= 0f)
                _fallTimeoutDelta -= Time.deltaTime;
        }

        if (impulsoActivo && recursos.Consumir(TipoRecurso.Agua, consumoAgua * Time.deltaTime))
            _verticalVelocity = fuerzaImpulso;

        if (_verticalVelocity < _terminalVelocity)
            _verticalVelocity += gravedad * Time.deltaTime;

        impulsoActivo = false;
    }

    public void SetImpulsoMantenido(bool activo) => impulsoActivo    = activo;
    public void SetDisparando(bool activo)        => disparandoActivo = activo;

    void OnDrawGizmosSelected()
    {
        // Esfera de detección de suelo
        Gizmos.color = enSuelo ? new Color(0f, 1f, 0f, 0.35f) : new Color(1f, 0f, 0f, 0.35f);
        Gizmos.DrawSphere(
            new Vector3(transform.position.x, transform.position.y - offsetSuelo, transform.position.z),
            radioSuelo);

        // Flecha hacia donde mira el jugador
        Vector3 origen = transform.position + Vector3.up * 1f;
        Vector3 dir    = transform.forward;
        float   largo  = 2f;

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(origen, origen + dir * largo);

        // Punta de la flecha
        Vector3 punta     = origen + dir * largo;
        Vector3 derechaAla = Quaternion.Euler(0, 30, 0) * (-dir) * 0.4f;
        Vector3 izqAla    = Quaternion.Euler(0, -30, 0) * (-dir) * 0.4f;
        Gizmos.DrawLine(punta, punta + derechaAla);
        Gizmos.DrawLine(punta, punta + izqAla);
    }
}