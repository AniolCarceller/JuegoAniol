using UnityEngine;

public class ZonaRecarga : MonoBehaviour
{
    [Header("Tipo de recurso que recarga esta zona")]
    public TipoRecurso tipoRecurso = TipoRecurso.Agua;

    [Header("Recarga")]
    [Tooltip("Unidades por segundo que se recargan mientras el jugador esté dentro.")]
    public float velocidadRecarga = 30f;

    [Tooltip("Si está activado, recarga instantáneamente al entrar en vez de gradual.")]
    public bool recargaInstantanea = false;

    private GestorRecursos gestorRecursos;

    void OnTriggerEnter(Collider otro)
    {
        GestorRecursos gr = otro.GetComponent<GestorRecursos>();
        if (gr == null) return;

        if (recargaInstantanea)
        {
            gr.RecargarTodo();
            Debug.Log($"[ZonaRecarga] Recarga instantánea de {tipoRecurso}");
        }
        else
        {
            // Guardamos referencia para la recarga gradual en Stay
            gestorRecursos = gr;
        }
    }

    void OnTriggerStay(Collider otro)
    {
        if (recargaInstantanea) return;

        GestorRecursos gr = otro.GetComponent<GestorRecursos>();
        if (gr == null) return;

        gr.Recargar(tipoRecurso, velocidadRecarga * Time.deltaTime);
    }

    void OnTriggerExit(Collider otro)
    {
        if (otro.GetComponent<GestorRecursos>() != null)
            gestorRecursos = null;
    }

    // Gizmo para ver la zona en el editor
    void OnDrawGizmos()
    {
        Gizmos.color = tipoRecurso == TipoRecurso.Agua
            ? new Color(0.1f, 0.5f, 1f, 0.25f)
            : new Color(0.9f, 0.7f, 0.1f, 0.25f);

        // Intenta usar el tamaño del collider para el gizmo
        SphereCollider sc = GetComponent<SphereCollider>();
        BoxCollider    bc = GetComponent<BoxCollider>();

        if (sc != null)
            Gizmos.DrawSphere(transform.position, sc.radius * transform.lossyScale.x);
        else if (bc != null)
            Gizmos.DrawCube(transform.position, Vector3.Scale(bc.size, transform.lossyScale));
        else
            Gizmos.DrawSphere(transform.position, 2f);

        // Borde
        Gizmos.color = tipoRecurso == TipoRecurso.Agua
            ? new Color(0.1f, 0.5f, 1f, 0.8f)
            : new Color(0.9f, 0.7f, 0.1f, 0.8f);

        if (sc != null)
            Gizmos.DrawWireSphere(transform.position, sc.radius * transform.lossyScale.x);
        else if (bc != null)
            Gizmos.DrawWireCube(transform.position, Vector3.Scale(bc.size, transform.lossyScale));
    }
}
