using UnityEngine;

public class EspirituFuego : EnemyAI
{
    protected override void Start()
    {
        tipo = TipoEnemigo.Fuego;
        base.Start();
    }

    protected override bool EsEfectivo(TipoRecurso recurso)
        => recurso == TipoRecurso.Agua;

    // Arena contra fuego: sin efecto, solo humo (aquí puedes lanzar partículas)
    protected override void ReaccionInefectiva()
    {
        Debug.Log($"[{gameObject.name}] Arena contra fuego — sin efecto");
        // TODO: activar partículas de humo
    }
}