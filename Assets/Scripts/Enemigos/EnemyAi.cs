using UnityEngine;
using UnityEngine.AI;

public enum EstadoEnemigo { Idle, Wander, ChasePlayer, Attack, DefenderFoco, BeingHit, Death }
public enum TipoEnemigo   { Fuego, Electrico }

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Tipo")]
    public TipoEnemigo tipo    = TipoEnemigo.Fuego;
    public bool        esMayor = false;

    [Header("Stats")]
    public float vidaMaxima           = 50f;
    public float vidaActual;
    public float dañoAtaque           = 10f;
    public float velocidadNormal      = 10f;
    public float velocidadPersecucion = 16f;

    [Header("Detección")]
    public float rangoDeteccion     = 25f;
    public float rangoAtaque        = 1.8f;
    public float tiempoEntreAtaques = 1.2f;

    [Header("Raycast de visión")]
    public float alturaRaycast = 1f;   // ← CORREGIDO: variable que faltaba

    [Header("Wander")]
    public float radioWander = 15f;

    [Header("Estado (debug)")]
    [SerializeField] protected EstadoEnemigo estado = EstadoEnemigo.Idle;
    public EstadoEnemigo EstadoActual => estado;

    [HideInInspector] public FocoIncendio focoOrigen;
    [HideInInspector] public bool         esDefensor = false;

    protected NavMeshAgent agente;
    protected Transform    jugador;
    protected JugadorVida  jugadorVida;

    private float timerAtaque       = 0f;
    private float timerIdle         = 0f;
    private float timerContraataque = 0f;
    private bool  fueGolpeado       = false;
    private bool  elementoEfectivo  = false;

    protected virtual void Start()
    {
        agente     = GetComponent<NavMeshAgent>();
        vidaActual = esMayor ? vidaMaxima * 2f : vidaMaxima;

        GameObject obj = GameObject.FindWithTag("Player");
        if (obj != null)
        {
            jugador     = obj.transform;
            jugadorVida = obj.GetComponent<JugadorVida>();
        }

        agente.speed        = velocidadNormal;
        agente.acceleration = 20f;
        agente.angularSpeed = 300f;
        CambiarEstado(EstadoEnemigo.Wander);
    }

    protected virtual void Update()
    {
        if (estado == EstadoEnemigo.Death) return;

        timerAtaque       -= Time.deltaTime;
        timerContraataque -= Time.deltaTime;

        // PRIORIDAD 1 — golpe recibido
        if (fueGolpeado)
        {
            fueGolpeado = false;
            if (elementoEfectivo)
            {
                CambiarEstado(EstadoEnemigo.BeingHit);
                Invoke(nameof(VolverDeGolpe), 0.2f);
            }
            else if (timerContraataque <= 0f)
            {
                ReaccionInefectiva();
                timerContraataque = 2f;
            }
            return;
        }

        if (estado == EstadoEnemigo.BeingHit) return;

        // PRIORIDAD 2 — visión del jugador
        bool veAlJugador = ComprobarVision();

        if (veAlJugador)
        {
            float dist = Vector3.Distance(transform.position, jugador.position);

            if (dist <= rangoAtaque)
            {
                if (estado != EstadoEnemigo.Attack) CambiarEstado(EstadoEnemigo.Attack);
                EstadoAtaque();
                return;
            }
            else
            {
                if (estado != EstadoEnemigo.ChasePlayer) CambiarEstado(EstadoEnemigo.ChasePlayer);
                EstadoChase();
                return;
            }
        }

        if (estado == EstadoEnemigo.ChasePlayer || estado == EstadoEnemigo.Attack)
        {
            IrAWanderODefender();
            return;
        }

        // PRIORIDAD 3 — defender foco
        if (esDefensor && focoOrigen != null && !focoOrigen.EstaApagado)
        {
            if (estado != EstadoEnemigo.DefenderFoco) CambiarEstado(EstadoEnemigo.DefenderFoco);
            EstadoDefender();
            return;
        }

        // PRIORIDAD 4 — wander / idle
        switch (estado)
        {
            case EstadoEnemigo.Wander:       EstadoWander();       break;
            case EstadoEnemigo.Idle:         EstadoIdle();         break;
            case EstadoEnemigo.DefenderFoco: IrAWanderODefender(); break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ESTADOS
    // ─────────────────────────────────────────────────────────────────────────

    void EstadoIdle()
    {
        timerIdle -= Time.deltaTime;
        if (timerIdle <= 0f) CambiarEstado(EstadoEnemigo.Wander);
    }

    void EstadoWander()
    {
        if (!agente.isActiveAndEnabled || !agente.isOnNavMesh) return;
        if (agente.remainingDistance < 0.5f)
        {
            timerIdle = Random.Range(0.5f, 1.5f);
            CambiarEstado(EstadoEnemigo.Idle);
        }
    }

    void EstadoChase()
    {
        if (jugador == null) return;
        if (agente.isActiveAndEnabled && agente.isOnNavMesh)
            agente.SetDestination(jugador.position);
    }

    void EstadoAtaque()
    {
        if (jugador == null) return;
        if (agente.isActiveAndEnabled && agente.isOnNavMesh)
            agente.SetDestination(transform.position);

        transform.LookAt(new Vector3(jugador.position.x, transform.position.y, jugador.position.z));

        if (timerAtaque <= 0f)
        {
            AtacarJugador();
            timerAtaque = tiempoEntreAtaques;
        }
    }

    void EstadoDefender()
    {
        if (focoOrigen == null) { IrAWanderODefender(); return; }
        if (agente.isActiveAndEnabled && agente.isOnNavMesh)
            agente.SetDestination(focoOrigen.transform.position);
        if (agente.remainingDistance < 1.5f)
        {
            timerIdle = 1.5f;
            CambiarEstado(EstadoEnemigo.Idle);
        }
    }

    void IrAWanderODefender()
    {
        if (esDefensor && focoOrigen != null && !focoOrigen.EstaApagado)
            CambiarEstado(EstadoEnemigo.DefenderFoco);
        else
            CambiarEstado(EstadoEnemigo.Wander);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // VISIÓN
    // ─────────────────────────────────────────────────────────────────────────

    bool ComprobarVision()
    {
        if (jugador == null) return false;
        float distancia = Vector3.Distance(transform.position, jugador.position);
        if (distancia > rangoDeteccion) return false;

        Vector3 origen = transform.position + Vector3.up * alturaRaycast;
        Vector3 dest   = jugador.position   + Vector3.up * alturaRaycast;
        Vector3 dir    = (dest - origen).normalized;

        int mask = ~LayerMask.GetMask("Enemies");

        if (Physics.Raycast(origen, dir, out RaycastHit hit, rangoDeteccion, mask))
            return hit.transform == jugador || hit.transform.IsChildOf(jugador);

        return true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // COMBATE
    // ─────────────────────────────────────────────────────────────────────────

    protected virtual void AtacarJugador()
    {
        float daño = esMayor ? dañoAtaque * 1.5f : dañoAtaque;
        jugadorVida?.RecibirDaño(daño);

        Debug.Log($"{gameObject.name} ataca por {daño}");
    }

    public void RecibirDaño(float cantidad, TipoRecurso recurso)
    {
        if (estado == EstadoEnemigo.Death) return;
        elementoEfectivo = EsEfectivo(recurso);
        fueGolpeado      = true;
        if (elementoEfectivo)
        {
            vidaActual -= cantidad;
            Debug.Log($"{gameObject.name} recibe {cantidad:F1} daño. Vida: {vidaActual:F0}");
            if (vidaActual <= 0f) Morir();
        }
    }

    protected virtual bool EsEfectivo(TipoRecurso recurso) => false;
    protected virtual void ReaccionInefectiva() => Debug.Log($"[{gameObject.name}] Elemento incorrecto");

    protected virtual void Morir()
    {
        CambiarEstado(EstadoEnemigo.Death);
        agente.enabled = false;
        focoOrigen?.NotificarEnemigoMuerto(this);
        Debug.Log($"{gameObject.name} muerto");
        Destroy(gameObject, 1f);
    }

    void VolverDeGolpe()
    {
        if (estado != EstadoEnemigo.Death)
            CambiarEstado(EstadoEnemigo.ChasePlayer);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CAMBIO DE ESTADO
    // ─────────────────────────────────────────────────────────────────────────

    protected void CambiarEstado(EstadoEnemigo nuevo)
    {
        estado = nuevo;
        if (!agente.isActiveAndEnabled || !agente.isOnNavMesh) return;

        switch (nuevo)
        {
            case EstadoEnemigo.Wander:
                agente.speed = velocidadNormal;
                agente.SetDestination(ObtenerPuntoAleatorio());
                break;
            case EstadoEnemigo.ChasePlayer:
                agente.speed = velocidadPersecucion;
                break;
            case EstadoEnemigo.DefenderFoco:
                agente.speed = velocidadNormal;
                break;
            case EstadoEnemigo.Idle:
            case EstadoEnemigo.Attack:
                agente.SetDestination(transform.position);
                break;
        }
    }

    Vector3 ObtenerPuntoAleatorio()
    {
        Vector3 punto = transform.position + Random.insideUnitSphere * radioWander;
        punto.y = transform.position.y;
        if (NavMesh.SamplePosition(punto, out NavMeshHit hit, radioWander, NavMesh.AllAreas))
            return hit.position;
        return transform.position;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GIZMOS
    // ─────────────────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangoAtaque);

        Vector3 origen = transform.position + Vector3.up * 1f;
        Vector3 dir    = transform.forward;
        float   largo  = 2f;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origen, origen + dir * largo);

        Vector3 punta      = origen + dir * largo;
        Vector3 derechaAla = Quaternion.Euler(0,  30, 0) * (-dir) * 0.4f;
        Vector3 izqAla     = Quaternion.Euler(0, -30, 0) * (-dir) * 0.4f;
        Gizmos.DrawLine(punta, punta + derechaAla);
        Gizmos.DrawLine(punta, punta + izqAla);

        if (Application.isPlaying && jugador != null)
        {
            Gizmos.color = (estado == EstadoEnemigo.ChasePlayer || estado == EstadoEnemigo.Attack)
                ? Color.red
                : new Color(1f, 0.5f, 0f);
            Gizmos.DrawLine(transform.position + Vector3.up * 0.8f, jugador.position + Vector3.up * 0.8f);
        }
    }
}