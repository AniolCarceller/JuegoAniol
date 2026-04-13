using UnityEngine;

public enum TipoRecurso { Agua, Arena }

public class GestorRecursos : MonoBehaviour
{
    [Header("Agua")]
    public float aguaMaxima = 100f;
    public float aguaActual = 100f;

    [Header("Arena")]
    public float arenaMaxima = 100f;
    public float arenaActual = 100f;

    public bool Consumir(TipoRecurso tipo, float cantidad)
    {
        if (tipo == TipoRecurso.Agua)
        {
            if (aguaActual <= 0) return false;
            aguaActual = Mathf.Max(0, aguaActual - cantidad);
            return true;
        }
        else
        {
            if (arenaActual <= 0) return false;
            arenaActual = Mathf.Max(0, arenaActual - cantidad);
            return true;
        }
    }

    public void Recargar(TipoRecurso tipo, float cantidad)
    {
        if (tipo == TipoRecurso.Agua)
            aguaActual = Mathf.Min(aguaMaxima, aguaActual + cantidad);
        else
            arenaActual = Mathf.Min(arenaMaxima, arenaActual + cantidad);
    }

    public void RecargarTodo()
    {
        aguaActual = aguaMaxima;
        arenaActual = arenaMaxima;
    }

    // Debug visual en pantalla
    void OnGUI()
    {
        GUI.Label(new Rect(20, 20, 200, 25), $"💧 Agua: {aguaActual:F0} / {aguaMaxima}");
        GUI.Label(new Rect(20, 45, 200, 25), $"🟡 Arena: {arenaActual:F0} / {arenaMaxima}");
    }
}