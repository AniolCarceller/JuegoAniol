using UnityEngine;
using UnityEngine.AI;

public enum EstadoEnemigo { Idle, Patrol, Wander, ChasePlayer, Attack, BeingHit, Death }
public enum TipoEnemigo { Fuego, Electrico }

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Tipo")]
    public TipoEnemigo tipo = TipoEnemigo.Fuego;
    public bool esMayor = false;

    [Header("Stats")]
    public float vidaMaxima = 50f;
    public float vidaActual;
    public float dañoAtaque = 10f;
    public float velocidadNormal = 3.5f;
    public float velocidadPersecucion = 5.5f;

    [Header("Detección")]
    public float rangoDeteccion = 8f;
    public float rangoAtaque = 1.5f;
    public float tiempoEntreAtaques = 1.5f;

    [Header("Wander")]
    public float radioWander = 6f;

    [Header("Estado (debug)")]
    [SerializeField] protected EstadoEnemigo estado = EstadoEnemigo.Idle;

    protected NavMeshAgent agente;
    protected Transform jugador;
    protected JugadorVida jugadorVida;

    private float timerAtaque = 0f;
    private float timerIdle = 0f;
    private Vector3 puntoWander;
    private bool fueGolpeado = false;
    private bool elementoEfectivo = false;

    protected virtual void Start()
    {
        agente = GetComponent<NavMeshAgent>();
        vidaActual = esMayor ? vidaMaxima * 2f : vidaMaxima;

        GameObject obj = GameObject.FindWithTag("Player");
        if (obj != null)
        {
            jugador = obj.transform;
            jugadorVida = obj.GetComponent<JugadorVida>();
        }

        agente.speed = velocidadNormal;
        CambiarEstado(EstadoEnemigo.Wander);
    }

    protected virtual void Update()
    {
        if (estado == EstadoEnemigo.Death) return;

        timerAtaque -= Time.deltaTime;

        // ═══════════════════════════════
        // ¿Lo he golpeado (al jugador)?
        // ═══════════════════════════════
        if (fueGolpeado)
        {
            fueGolpeado = false;

            if (elementoEfectivo)
            {
                // Recibir daño — ya aplicado en RecibirDaño()
                CambiarEstado(EstadoEnemigo.BeingHit);
                Invoke(nameof(VolverDeGolpe), 0.2f);
            }
            else
            {
                // No recibir daño → reacción inefectiva
                ReaccionInefectiva();
            }
            return;
        }

        // ═══════════════════════════════
        // NO fue golpeado → lógica normal
        // ═══════════════════════════════
        switch (estado)
        {
            case EstadoEnemigo.Wander:   EstadoWander();   break;
            case EstadoEnemigo.ChasePlayer: EstadoChase(); break;
            case EstadoEnemigo.Attack:   EstadoAtaque();   break;
            case EstadoEnemigo.Idle:     EstadoIdle();     break;
        }

        // Comprobar visión desde cualquier estado excepto Attack y Death
        if (estado != EstadoEnemigo.Attack && estado != EstadoEnemigo.Death)
            ComprobarVision();
    }

    // ═══════════════════════════════
    // ESTADOS
    // ═══════════════════════════════

    void EstadoIdle()
    {
        timerIdle -= Time.deltaTime;
        if (timerIdle <= 0f)
            CambiarEstado(EstadoEnemigo.Wander);
    }

    void EstadoWander()
    {
        if (agente.remainingDistance < 0.5f)
        {
            // Pausa breve antes de ir al siguiente punto
            timerIdle = Random.Range(1f, 2.5f);
            CambiarEstado(EstadoEnemigo.Idle);
        }
    }

    void EstadoChase()
    {
        if (jugador == null) return;

        agente.SetDestination(jugador.position);
        float distancia = Vector3.Distance(transform.position, jugador.position);

        // En rango → atacar
        if (distancia <= rangoAtaque)
        {
            CambiarEstado(EstadoEnemigo.Attack);
            return;
        }

        // Jugador se alejó demasiado → volver a wander
        if (distancia > rangoDeteccion * 1.5f)
            CambiarEstado(EstadoEnemigo.Wander);
    }

    void EstadoAtaque()
    {
        if (jugador == null) return;

        agente.SetDestination(transform.position); // No moverse
        transform.LookAt(jugador);

        float distancia = Vector3.Distance(transform.position, jugador.position);

        // Jugador se alejó → chase
        if (distancia > rangoAtaque * 1.2f)
        {
            CambiarEstado(EstadoEnemigo.ChasePlayer);
            return;
        }

        // Atacar si el cooldown lo permite
        if (timerAtaque <= 0f)
        {
            AtacarJugador();
            timerAtaque = tiempoEntreAtaques;
        }
    }

    // ═══════════════════════════════
    // VISIÓN
    // ═══════════════════════════════

    void ComprobarVision()
    {
        if (jugador == null) return;

        float distancia = Vector3.Distance(transform.position, jugador.position);
        if (distancia > rangoDeteccion) return;

        // Raycast para comprobar que no hay obstáculos
        Vector3 dir = (jugador.position - transform.position).normalized;
        if (Physics.Raycast(transform.position + Vector3.up, dir, out RaycastHit hit, rangoDeteccion))
        {
            if (hit.transform == jugador)
                CambiarEstado(EstadoEnemigo.ChasePlayer);
        }
    }

    // ═══════════════════════════════
    // COMBATE
    // ═══════════════════════════════

    protected virtual void AtacarJugador()
    {
        float daño = esMayor ? dañoAtaque * 1.5f : dañoAtaque;
        jugadorVida?.RecibirDaño(daño);
        Debug.Log($"{gameObject.name} ataca por {daño}");
    }

    // Llamado desde JugadorCombate cuando dispara a este enemigo
    public void RecibirDaño(float cantidad, TipoRecurso recurso)
    {
        if (estado == EstadoEnemigo.Death) return;

        elementoEfectivo = EsEfectivo(recurso);
        fueGolpeado = true;

        if (elementoEfectivo)
        {
            vidaActual -= cantidad;
            Debug.Log($"{gameObject.name} recibe {cantidad} daño. Vida: {vidaActual:F0}");
            if (vidaActual <= 0f) Morir();
        }
    }

    protected virtual bool EsEfectivo(TipoRecurso recurso) => false;

    protected virtual void ReaccionInefectiva()
    {
        Debug.Log($"Elemento incorrecto contra {gameObject.name}");
    }

    // ═══════════════════════════════
    // UTILIDADES
    // ═══════════════════════════════

    protected virtual void Morir()
    {
        CambiarEstado(EstadoEnemigo.Death);
        agente.enabled = false;
        Debug.Log($"{gameObject.name} muerto");
        Destroy(gameObject, 1f);
    }

    void VolverDeGolpe()
    {
        if (estado != EstadoEnemigo.Death)
            CambiarEstado(EstadoEnemigo.ChasePlayer);
    }

    protected void CambiarEstado(EstadoEnemigo nuevo)
    {
        estado = nuevo;

        switch (nuevo)
        {
            case EstadoEnemigo.Wander:
                agente.speed = velocidadNormal;
                puntoWander = ObtenerPuntoAleatorio();
                agente.SetDestination(puntoWander);
                break;
            case EstadoEnemigo.ChasePlayer:
                agente.speed = velocidadPersecucion;
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangoAtaque);
    }
}