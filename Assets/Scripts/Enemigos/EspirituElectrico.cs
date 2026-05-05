using UnityEngine;

public class EspirituElectrico : EnemyAI
{
    [Header("Chispazo")]
    public float dañoChispazo = 15f;

    private JugadorVida jugadorVidaRef;

    protected override void Start()
    {
        tipo = TipoEnemigo.Electrico;
        base.Start();
        jugadorVidaRef = GameObject.FindWithTag("Player")?.GetComponent<JugadorVida>();
    }

    protected override bool EsEfectivo(TipoRecurso recurso)
        => recurso == TipoRecurso.Arena;

    // Agua contra eléctrico: chispazo que electrocuta al jugador
    protected override void ReaccionInefectiva()
    {
        Debug.Log($"[{gameObject.name}] ¡Chispazo! Agua contra eléctrico — jugador electrocutado");
        jugadorVidaRef?.Electrocutar();
    }
}