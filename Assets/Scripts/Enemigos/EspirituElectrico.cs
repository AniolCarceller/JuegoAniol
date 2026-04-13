using UnityEngine;

public class EspirituElectrico : EnemyAI
{
    private JugadorVida jugadorVidaRef;

    protected override void Start()
    {
        tipo = TipoEnemigo.Electrico;
        base.Start();
        jugadorVidaRef = GameObject.FindWithTag("Player")?.GetComponent<JugadorVida>();
    }

    protected override bool EsEfectivo(TipoRecurso recurso)
        => recurso == TipoRecurso.Arena;

    protected override void ReaccionInefectiva()
    {
        Debug.Log("Agua contra eléctrico — jugador electrocutado");
        jugadorVidaRef?.Electrocutar();
    }
}