using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(GestorRecursos))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerInput))]
public class JugadorCombate : MonoBehaviour
{
    [Header("Disparo")]
    public float rangoDisparo      = 15f;
    public float dañoPorSegundo    = 20f;
    public float consumoPorSegundo = 15f;

    [Header("Punto de salida del chorro")]
    [Tooltip("Asigna aquí un empty child del personaje (p.ej. la boquilla del FLUDD). Si no asignas nada usa el centro del jugador.")]
    public Transform puntoSalida;

    [Header("Visual del chorro")]
    public float anchoChorroInicio = 0.12f;
    public float anchoChorroFin    = 0.04f;
    public int   segmentosChorro   = 12;
    public float amplitudOnda      = 0.06f;

    [Header("Impacto")]
    public float radioAnilloImpacto = 0.35f;

    [Header("Partículas")]
    public ParticleSystem particulasDisparo;
    public ParticleSystem particulasImpacto;

    [Header("Debug")]
    [SerializeField] private TipoRecurso recursoActual = TipoRecurso.Agua;
    [SerializeField] private bool disparando           = false;
    [SerializeField] private bool impulsoMantenido     = false;
    [SerializeField] private string ultimoGolpe        = "ninguno";

    private GestorRecursos gestorRecursos;
    private PlayerMovement playerMovement;
    private PlayerInput    playerInput;

    private InputAction shootAction;
    private InputAction impulseAction;
    private InputAction switchResourceAction;

    private LineRenderer lineaChorro;
    private LineRenderer anilloImpacto;

    private bool    hayImpacto    = false;
    private Vector3 puntoImpacto  = Vector3.zero;
    private Vector3 normalImpacto = Vector3.up;

    void Awake()
    {
        gestorRecursos = GetComponent<GestorRecursos>();
        playerMovement = GetComponent<PlayerMovement>();
        playerInput    = GetComponent<PlayerInput>();
    }

    void Start()
    {
        if (Camera.main == null)
        {
            Debug.LogError("[JugadorCombate] No hay cámara con tag MainCamera.");
            enabled = false;
            return;
        }

        if (puntoSalida == null)
            puntoSalida = transform;

        shootAction          = playerInput.actions.FindAction("Shoot",          true);
        impulseAction        = playerInput.actions.FindAction("Impulso",        true);
        switchResourceAction = playerInput.actions.FindAction("SwitchResource", true);

        InicializarLineaChorro();
        InicializarAnilloImpacto();
    }

    void InicializarLineaChorro()
    {
        lineaChorro = gameObject.AddComponent<LineRenderer>();
        lineaChorro.positionCount     = segmentosChorro;
        lineaChorro.startWidth        = anchoChorroInicio;
        lineaChorro.endWidth          = anchoChorroFin;
        lineaChorro.useWorldSpace     = true;
        lineaChorro.numCapVertices    = 4;
        lineaChorro.numCornerVertices = 2;
        lineaChorro.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineaChorro.receiveShadows    = false;

        Shader shader = Shader.Find("Sprites/Default")
                     ?? Shader.Find("Unlit/Color")
                     ?? Shader.Find("Hidden/Internal-Colored");
        if (shader != null) lineaChorro.material = new Material(shader);

        lineaChorro.enabled = false;
    }

    void InicializarAnilloImpacto()
    {
        GameObject goAnillo = new GameObject("AnilloImpacto");
        goAnillo.transform.SetParent(transform);

        anilloImpacto = goAnillo.AddComponent<LineRenderer>();
        anilloImpacto.positionCount     = 17;
        anilloImpacto.startWidth        = 0.04f;
        anilloImpacto.endWidth          = 0.04f;
        anilloImpacto.useWorldSpace     = true;
        anilloImpacto.loop              = false;
        anilloImpacto.numCapVertices    = 4;
        anilloImpacto.numCornerVertices = 2;
        anilloImpacto.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        anilloImpacto.receiveShadows    = false;

        Shader shader = Shader.Find("Sprites/Default")
                     ?? Shader.Find("Unlit/Color")
                     ?? Shader.Find("Hidden/Internal-Colored");
        if (shader != null) anilloImpacto.material = new Material(shader);

        anilloImpacto.enabled = false;
    }

    void Update()
    {
        bool shootHeld   = shootAction   != null && shootAction.IsPressed();
        impulsoMantenido = impulseAction != null && impulseAction.IsPressed();

        if (switchResourceAction != null && switchResourceAction.WasPressedThisFrame())
        {
            recursoActual = (recursoActual == TipoRecurso.Agua) ? TipoRecurso.Arena : TipoRecurso.Agua;
            Debug.Log($"Recurso: {recursoActual}");
        }

        if (playerMovement != null)
        {
            playerMovement.SetDisparando(shootHeld);
            playerMovement.SetImpulsoMantenido(impulsoMantenido && recursoActual == TipoRecurso.Agua);
        }

        if (!shootHeld)
        {
            PararDisparo();
            return;
        }

        if (!gestorRecursos.Consumir(recursoActual, consumoPorSegundo * Time.deltaTime))
        {
            PararDisparo();
            return;
        }

        disparando = true;
        if (particulasDisparo != null && !particulasDisparo.isPlaying)
            particulasDisparo.Play();

        // El chorro sale desde puntoSalida hacia donde mira el personaje (transform.forward).
        // PlayerMovement ya giró al personaje hacia la cámara si estaba quieto,
        // así que transform.forward siempre coincide con la intención del jugador.
        Vector3 origen    = puntoSalida.position;
        Vector3 direccion = transform.forward;

        bool golpeado = Physics.Raycast(origen, direccion, out RaycastHit golpe, rangoDisparo);

        Vector3 fin   = golpeado ? golpe.point : origen + direccion * rangoDisparo;
        ultimoGolpe   = golpeado ? golpe.collider.gameObject.name : "—";
        hayImpacto    = true;
        puntoImpacto  = fin;
        normalImpacto = golpeado ? golpe.normal : -direccion;

        DibujarChorro(origen, fin);
        DibujarAnilloImpacto();

        if (particulasImpacto != null)
        {
            particulasImpacto.transform.position = fin;
            if (golpeado) { if (!particulasImpacto.isPlaying) particulasImpacto.Play(); }
            else          { if (particulasImpacto.isPlaying)  particulasImpacto.Stop(); }
        }

        if (golpeado)
            AplicarDaño(golpe, dañoPorSegundo * Time.deltaTime);
    }

    void PararDisparo()
    {
        disparando            = false;
        hayImpacto            = false;
        lineaChorro.enabled   = false;
        anilloImpacto.enabled = false;
        if (particulasDisparo != null && particulasDisparo.isPlaying) particulasDisparo.Stop();
        if (particulasImpacto != null && particulasImpacto.isPlaying) particulasImpacto.Stop();
    }

    void DibujarChorro(Vector3 inicio, Vector3 fin)
    {
        lineaChorro.positionCount = segmentosChorro;
        lineaChorro.enabled       = true;

        Color colorBase = (recursoActual == TipoRecurso.Agua)
            ? new Color(0.15f, 0.65f, 1.0f, 0.90f)
            : new Color(0.95f, 0.80f, 0.20f, 0.90f);

        lineaChorro.startColor = colorBase;
        lineaChorro.endColor   = new Color(colorBase.r, colorBase.g, colorBase.b, 0.3f);

        float pulso = 1f + Mathf.Sin(Time.time * 25f) * 0.12f;
        lineaChorro.startWidth = anchoChorroInicio * pulso;
        lineaChorro.endWidth   = anchoChorroFin;

        Vector3 dir  = (fin - inicio).normalized;
        Vector3 perp = Vector3.Cross(dir, Vector3.up);
        if (perp.sqrMagnitude < 0.001f) perp = Vector3.Cross(dir, Vector3.right);
        perp.Normalize();

        for (int i = 0; i < segmentosChorro; i++)
        {
            float t    = (float)i / (segmentosChorro - 1);
            Vector3 p  = Vector3.Lerp(inicio, fin, t);
            float onda = Mathf.Sin(t * Mathf.PI * 4f + Time.time * 22f) * amplitudOnda * (1f - t * 0.6f);
            lineaChorro.SetPosition(i, p + perp * onda);
        }
    }

    void DibujarAnilloImpacto()
    {
        if (!hayImpacto) { anilloImpacto.enabled = false; return; }

        anilloImpacto.enabled = true;

        Color colorAnillo = (recursoActual == TipoRecurso.Agua)
            ? new Color(0.3f, 0.85f, 1.0f, 0.95f)
            : new Color(1.0f, 0.90f, 0.3f, 0.95f);

        anilloImpacto.startColor = colorAnillo;
        anilloImpacto.endColor   = new Color(colorAnillo.r, colorAnillo.g, colorAnillo.b, 0.5f);

        float   pulso      = radioAnilloImpacto + Mathf.Sin(Time.time * 18f) * 0.07f;
        Vector3 normal     = normalImpacto.normalized;
        Vector3 tangente   = Vector3.Cross(normal, Vector3.up);
        if (tangente.sqrMagnitude < 0.001f) tangente = Vector3.Cross(normal, Vector3.forward);
        tangente.Normalize();
        Vector3 bitangente = Vector3.Cross(normal, tangente).normalized;
        Vector3 centro     = puntoImpacto + normal * 0.01f;

        int puntos = anilloImpacto.positionCount;
        for (int i = 0; i < puntos; i++)
        {
            float angulo = (float)i / (puntos - 1) * Mathf.PI * 2f;
            anilloImpacto.SetPosition(i,
                centro
                + tangente   * Mathf.Cos(angulo) * pulso
                + bitangente * Mathf.Sin(angulo) * pulso);
        }
    }

    void AplicarDaño(RaycastHit golpe, float daño)
    {
        EnemyAI enemigo = golpe.collider.GetComponent<EnemyAI>();
        if (enemigo != null) { enemigo.RecibirDaño(daño, recursoActual); return; }

        FocoIncendio foco = golpe.collider.GetComponent<FocoIncendio>();
        if (foco != null) foco.RecibirDaño(daño, recursoActual);
    }

    void OnGUI()
    {
        string icono = recursoActual == TipoRecurso.Agua ? "💧" : "🟡";
        GUI.Label(new Rect(20, 120, 360, 25), $"{icono} {recursoActual} | Disparando: {(disparando ? "SÍ" : "no")} | Propulsión: {(impulsoMantenido ? "SÍ" : "no")}");
        GUI.Label(new Rect(20, 145, 360, 25), $"Apuntando a: {ultimoGolpe}");
    }
}