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
    public GameObject prefabEnemigo;        // Arrastra EspirituFuego o Electrico
    public float intervaloSpawn = 5f;
    public int maxEnemigos = 3;

    [Header("Daño por contacto")]
    public float dañoPorSegundo = 5f;

    private float timerSpawn = 0f;
    private int enemigosActivos = 0;
    private bool apagado = false;

    void Start()
    {
        vidaActual = vidaMaxima;
    }

    void Update()
    {
        if (apagado) return;

        timerSpawn -= Time.deltaTime;
        if (timerSpawn <= 0f && enemigosActivos < maxEnemigos)
        {
            SpawnEnemigo();
            timerSpawn = intervaloSpawn;
        }
    }

    public void RecibirDaño(float cantidad, TipoRecurso recurso)
    {
        if (apagado) return;

        bool esEfectivo = (tipo == TipoFoco.Fuego && recurso == TipoRecurso.Agua)
                       || (tipo == TipoFoco.Electrico && recurso == TipoRecurso.Arena);

        if (!esEfectivo)
        {
            Debug.Log("Recurso incorrecto para este foco");
            return;
        }

        vidaActual -= cantidad;
        Debug.Log($"Foco recibe daño. Vida: {vidaActual:F0}");

        if (vidaActual <= 0f)
            Apagarse();
    }

    void SpawnEnemigo()
    {
        if (prefabEnemigo == null) return;

        Vector3 pos = transform.position + Random.insideUnitSphere * 1.5f;
        pos.y = transform.position.y;

        Instantiate(prefabEnemigo, pos, Quaternion.identity);
        enemigosActivos++;
    }

    public void NotificarEnemigoMuerto()
    {
        enemigosActivos = Mathf.Max(0, enemigosActivos - 1);
    }

    void Apagarse()
    {
        apagado = true;
        Debug.Log($"Foco {gameObject.name} apagado");
        // Aquí luego desactivas el efecto de partículas de fuego
        gameObject.SetActive(false);
    }

    void OnTriggerStay(Collider otro)
    {
        if (apagado) return;

        JugadorVida vida = otro.GetComponent<JugadorVida>();
        if (vida != null)
            vida.RecibirDaño(dañoPorSegundo * Time.deltaTime);
    }
}