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

    protected override void ReaccionInefectiva()
    {
        Debug.Log("Arena contra fuego — sin efecto, genera humo");
    }
}