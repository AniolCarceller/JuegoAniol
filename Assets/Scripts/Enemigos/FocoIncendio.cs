using System.Collections.Generic;
using UnityEngine;

public enum TipoFoco { Fuego, Electrico }

public class FocoIncendio : MonoBehaviour
{
    [Header("Tipo")]
    public TipoFoco tipo = TipoFoco.Fuego;

    [Header("Vida")]
    public float vidaMaxima = 100f;
    public float vidaActual;

    [Header("Spawn de enemigos")]
    public GameObject prefabEnemigo;
    public GameObject prefabEnemigoMayor;   // opcional — spawn cuando foco < 50% vida
    public int        maxEnemigosVivos  = 7;
    public float      rangoDeteccionJugador = 20f; // distancia para considerar "jugador cerca"

    [Header("Oleada final al apagarse")]
    public int   enemigosOleadaFinal = 3;

    [Header("Daño por contacto")]
    public float dañoPorSegundo = 5f;

    // Estado público para que los enemigos lo consulten
    public bool EstaApagado => apagado;

    private float vidaMaximaCache;
    private bool  apagado      = false;
    private float timerSpawn   = 0f;
    private bool  cooldownActivo = false;

    // Lista de enemigos vivos generados por este foco
    private List<EnemyAI> enemigosVivos = new List<EnemyAI>();
    private EnemyAI       defensorActual = null;

    private Transform jugador;

    // ═══════════════════════════════════════════════════
    // INIT
    // ═══════════════════════════════════════════════════

    void Start()
    {
        vidaActual      = vidaMaxima;
        vidaMaximaCache = vidaMaxima;

        GameObject obj = GameObject.FindWithTag("Player");
        if (obj != null) jugador = obj.transform;

        timerSpawn = 1f; // pequeño delay inicial antes del primer spawn
    }

    // ═══════════════════════════════════════════════════
    // UPDATE — árbol de spawn
    // ═══════════════════════════════════════════════════

    void Update()
    {
        if (apagado) return;

        // Limpiar referencias nulas (enemigos que murieron)
        enemigosVivos.RemoveAll(e => e == null);

        // Cooldown activo → esperar
        if (cooldownActivo) return;

        // ¿El jugador está cerca?
        if (jugador == null) return;
        float distJugador = Vector3.Distance(transform.position, jugador.position);
        if (distJugador > rangoDeteccionJugador) return;

        // ¿Hay 7 o menos enemigos vivos de este foco?
        if (enemigosVivos.Count >= maxEnemigosVivos) return;

        // Determinar cooldown según vida del foco
        float cooldown = (vidaActual <= vidaMaximaCache * 0.5f) ? 5f : 10f;

        SpawnEnemigo();
        ActualizarDefensor();

        IniciarCooldown(cooldown);
    }

    // ═══════════════════════════════════════════════════
    // RECIBIR DAÑO
    // ═══════════════════════════════════════════════════

    public void RecibirDaño(float cantidad, TipoRecurso recurso)
    {
        if (apagado) return;

        bool esEfectivo = (tipo == TipoFoco.Fuego     && recurso == TipoRecurso.Agua)
                       || (tipo == TipoFoco.Electrico && recurso == TipoRecurso.Arena);

        if (!esEfectivo)
        {
            Debug.Log($"[{gameObject.name}] Recurso incorrecto");
            return;
        }

        vidaActual -= cantidad;
        Debug.Log($"[{gameObject.name}] Foco recibe daño. Vida: {vidaActual:F0}");

        if (vidaActual <= 0f)
            Apagarse();
    }

    // ═══════════════════════════════════════════════════
    // SPAWN
    // ═══════════════════════════════════════════════════

    void SpawnEnemigo()
    {
        // Si el foco está por debajo del 50% y hay prefab mayor, usarlo
        GameObject prefab = (prefabEnemigoMayor != null && vidaActual <= vidaMaximaCache * 0.5f)
            ? prefabEnemigoMayor
            : prefabEnemigo;

        if (prefab == null) return;

        Vector3 pos = transform.position + Random.insideUnitSphere * 1.5f;
        pos.y = transform.position.y;

        GameObject go   = Instantiate(prefab, pos, Quaternion.identity);
        EnemyAI enemigo = go.GetComponent<EnemyAI>();

        if (enemigo != null)
        {
            enemigo.focoOrigen = this;
            enemigosVivos.Add(enemigo);
        }
    }

    void SpawnOleadaFinal()
    {
        for (int i = 0; i < enemigosOleadaFinal; i++)
        {
            if (prefabEnemigo == null) break;

            Vector3 pos = transform.position + Random.insideUnitSphere * 2f;
            pos.y = transform.position.y;

            GameObject go   = Instantiate(prefabEnemigo, pos, Quaternion.identity);
            EnemyAI enemigo = go.GetComponent<EnemyAI>();
            // La oleada final ya no tiene foco activo, así que no asignamos focoOrigen
            // para que no intenten defenderse de uno que ya no existe
        }
    }

    // ═══════════════════════════════════════════════════
    // DEFENSOR — solo el más cercano defiende el foco
    // ═══════════════════════════════════════════════════

    void ActualizarDefensor()
    {
        if (enemigosVivos.Count == 0) { defensorActual = null; return; }

        EnemyAI masCercano   = null;
        float   distMinima   = float.MaxValue;

        foreach (EnemyAI e in enemigosVivos)
        {
            if (e == null) continue;
            float d = Vector3.Distance(e.transform.position, transform.position);
            if (d < distMinima) { distMinima = d; masCercano = e; }
        }

        // Quitar rol defensor al anterior
        if (defensorActual != null && defensorActual != masCercano)
            defensorActual.esDefensor = false;

        defensorActual = masCercano;
        if (defensorActual != null)
            defensorActual.esDefensor = true;
    }

    // ═══════════════════════════════════════════════════
    // NOTIFICACIÓN DE MUERTE DE ENEMIGO
    // ═══════════════════════════════════════════════════

    public void NotificarEnemigoMuerto(EnemyAI enemigo)
    {
        enemigosVivos.Remove(enemigo);

        // Si era el defensor, recalcular
        if (enemigo == defensorActual)
        {
            defensorActual = null;
            ActualizarDefensor();
        }
    }

    // ═══════════════════════════════════════════════════
    // COOLDOWN
    // ═══════════════════════════════════════════════════

    void IniciarCooldown(float segundos)
    {
        cooldownActivo = true;
        Invoke(nameof(TerminarCooldown), segundos);
    }

    void TerminarCooldown()
    {
        cooldownActivo = false;
    }

    // ═══════════════════════════════════════════════════
    // APAGARSE
    // ═══════════════════════════════════════════════════

    void Apagarse()
    {
        apagado = true;

        // Quitar rol defensor a todos los enemigos
        foreach (EnemyAI e in enemigosVivos)
            if (e != null) { e.esDefensor = false; e.focoOrigen = null; }

        // Oleada final — los últimos espíritus salen al apagarse el foco
        Invoke(nameof(SpawnOleadaFinal), 1f);

        Debug.Log($"[{gameObject.name}] Apagado — oleada final en 1s");

        // TODO: desactivar partículas de fuego, activar VFX de extinción
        // Notificar al gestor de mundo que este foco ya está apagado
        GestorMundo.Instance?.NotificarFocoApagado(this);

        Invoke(nameof(DesactivarObjeto), 2f);
    }

    void DesactivarObjeto() => gameObject.SetActive(false);

    // ═══════════════════════════════════════════════════
    // DAÑO POR CONTACTO
    // ═══════════════════════════════════════════════════

    void OnTriggerStay(Collider otro)
    {
        if (apagado) return;
        otro.GetComponent<JugadorVida>()?.RecibirDaño(dañoPorSegundo * Time.deltaTime);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, rangoDeteccionJugador);
    }
}