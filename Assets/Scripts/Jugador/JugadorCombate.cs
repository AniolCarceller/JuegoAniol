using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gestiona el combate del jugador: disparo de agua/arena,
/// cambio de recurso e impulso vertical de agua.
/// Requiere GestorRecursos y PlayerMovement en el mismo GameObject.
/// </summary>
[RequireComponent(typeof(GestorRecursos))]
[RequireComponent(typeof(PlayerMovement))]
public class JugadorCombate : MonoBehaviour
{
    // ══════════════════════════════════════════
    // CONFIGURACIÓN
    // ══════════════════════════════════════════

    [Header("Disparo")]
    public float rangoDisparo      = 15f;
    public float dañoPorSegundo    = 20f;   // Daño continuo mientras disparas
    public float consumoPorSegundo = 15f;   // Recurso consumido por segundo al disparar
    public LayerMask capaObjetivos;         // Capas que puede golpear (Enemy, Foco…)

    [Header("Impulso de agua")]
    [Tooltip("Ángulo mínimo mirando hacia abajo para activar impulso (0=recto, 90=suelo)")]
    public float anguloImpulso = 50f;       // Si la cámara apunta más abajo de esto → impulso

    [Header("Feedback visual (opcional)")]
    public ParticleSystem particulasDisparo;   // Arrastra el sistema de partículas de la manguera

    // ══════════════════════════════════════════
    // ESTADO
    // ══════════════════════════════════════════

    [Header("Debug")]
    [SerializeField] private TipoRecurso recursoActual = TipoRecurso.Agua;
    [SerializeField] private bool disparando = false;

    // ══════════════════════════════════════════
    // REFERENCIAS
    // ══════════════════════════════════════════

    private GestorRecursos gestorRecursos;
    private PlayerMovement playerMovement;
    private Transform camara;

    // ══════════════════════════════════════════
    // INICIALIZACIÓN
    // ══════════════════════════════════════════

    void Start()
    {
        gestorRecursos = GetComponent<GestorRecursos>();
        playerMovement = GetComponent<PlayerMovement>();
        camara         = Camera.main.transform;
    }

    // ══════════════════════════════════════════
    // INPUT CALLBACKS (Input System)
    // ══════════════════════════════════════════

    /// <summary>Disparar — mantener pulsado para chorro continuo.</summary>
    public void OnShoot(InputValue value)
    {
        disparando = value.isPressed;

        if (disparando)
            IniciarDisparo();
        else
            DetenerDisparo();
    }

    /// <summary>Cambiar entre Agua y Arena.</summary>
    public void OnSwitchResource(InputValue value)
    {
        if (!value.isPressed) return;

        recursoActual = (recursoActual == TipoRecurso.Agua)
            ? TipoRecurso.Arena
            : TipoRecurso.Agua;

        Debug.Log($"Recurso activo: {recursoActual}");
    }

    /// <summary>Impulso de agua manual (botón dedicado).</summary>
    public void OnImpulso(InputValue value)
    {
        if (!value.isPressed) return;
        IntentarImpulsoAgua();
    }

    // ══════════════════════════════════════════
    // UPDATE — chorro continuo
    // ══════════════════════════════════════════

    void Update()
    {
        if (!disparando) return;

        float cantidad = consumoPorSegundo * Time.deltaTime;

        // Verificar que hay recurso disponible
        if (!gestorRecursos.Consumir(recursoActual, cantidad))
        {
            // Sin recurso → cortar disparo
            DetenerDisparo();
            disparando = false;
            return;
        }

        // Comprobar si la cámara apunta hacia abajo → impulso automático
        if (recursoActual == TipoRecurso.Agua && MirandoHaciaAbajo())
        {
            IntentarImpulsoAgua();
            return; // El agua se usa en el impulso, no en el raycast
        }

        // Raycast de disparo
        Ray rayo = new Ray(camara.position, camara.forward);
        if (Physics.Raycast(rayo, out RaycastHit golpe, rangoDisparo, capaObjetivos))
        {
            AplicarDaño(golpe, dañoPorSegundo * Time.deltaTime);
        }

        // Línea de debug en escena
        Debug.DrawRay(camara.position, camara.forward * rangoDisparo,
                      recursoActual == TipoRecurso.Agua ? Color.blue : Color.yellow);
    }

    // ══════════════════════════════════════════
    // LÓGICA DE DISPARO
    // ══════════════════════════════════════════

    void IniciarDisparo()
    {
        if (particulasDisparo != null && !particulasDisparo.isPlaying)
            particulasDisparo.Play();
    }

    void DetenerDisparo()
    {
        if (particulasDisparo != null && particulasDisparo.isPlaying)
            particulasDisparo.Stop();
    }

    void AplicarDaño(RaycastHit golpe, float daño)
    {
        // ¿Es un enemigo?
        EnemyAI enemigo = golpe.collider.GetComponent<EnemyAI>();
        if (enemigo != null)
        {
            enemigo.RecibirDaño(daño, recursoActual);
            return;
        }

        // ¿Es un foco de incendio?
        FocoIncendio foco = golpe.collider.GetComponent<FocoIncendio>();
        if (foco != null)
        {
            foco.RecibirDaño(daño, recursoActual);
        }
    }

    // ══════════════════════════════════════════
    // IMPULSO DE AGUA
    // ══════════════════════════════════════════

    void IntentarImpulsoAgua()
    {
        if (recursoActual != TipoRecurso.Agua) return;

        // PlayerMovement consume el agua y aplica la fuerza
        playerMovement.ActivarImpulsoAgua();
    }

    bool MirandoHaciaAbajo()
    {
        // camara.forward.y va de -1 (suelo) a 1 (cielo)
        // Si el ángulo es menor a -anguloImpulso grados → mirando abajo
        float angulo = Vector3.Angle(Vector3.down, camara.forward);
        return angulo < anguloImpulso;
    }

    // ══════════════════════════════════════════
    // DEBUG EN PANTALLA
    // ══════════════════════════════════════════

    void OnGUI()
    {
        string icono   = recursoActual == TipoRecurso.Agua ? "💧" : "🟡";
        string recurso = recursoActual == TipoRecurso.Agua ? "Agua" : "Arena";
        GUI.Label(new Rect(20, 120, 250, 25), $"{icono} Disparando: {recurso}  [{(disparando ? "ON" : "off")}]");
    }
}
