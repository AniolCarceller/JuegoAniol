using System.Collections.Generic;
using UnityEngine;

public enum TipoMundo { Arena, Fuego, Mixto }

public class GestorMundo : MonoBehaviour
{
    public static GestorMundo Instance { get; private set; }

    [Header("Mundos")]
    public MundoData mundoArena;
    public MundoData mundoFuego;
    public MundoData mundoMixto;

    [Header("Acceso al mundo mixto")]
    [Tooltip("Transform del portal/puerta al mundo mixto")]
    public GameObject accesoMixto;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // El mundo mixto empieza bloqueado
        if (accesoMixto != null) accesoMixto.SetActive(false);
    }

    public void NotificarFocoApagado(FocoIncendio foco)
    {
        // Intentar encontrar a qué mundo pertenece este foco
        if (mundoArena.ContieneFoco(foco))  { mundoArena.MarcarFocoApagado(foco);  ComprobarMundo(mundoArena,  TipoMundo.Arena); }
        if (mundoFuego.ContieneFoco(foco))  { mundoFuego.MarcarFocoApagado(foco);  ComprobarMundo(mundoFuego,  TipoMundo.Fuego); }
        if (mundoMixto.ContieneFoco(foco))  { mundoMixto.MarcarFocoApagado(foco);  ComprobarMundo(mundoMixto,  TipoMundo.Mixto); }
    }

    void ComprobarMundo(MundoData mundo, TipoMundo tipo)
    {
        if (!mundo.EstaCompleto()) return;

        Debug.Log($"[GestorMundo] Mundo {tipo} completado!");
        mundo.completado = true;

        // Si Arena y Fuego están completos → desbloquear Mixto
        if (mundoArena.completado && mundoFuego.completado && !mundoMixto.desbloqueado)
        {
            mundoMixto.desbloqueado = true;
            if (accesoMixto != null) accesoMixto.SetActive(true);
            Debug.Log("[GestorMundo] ¡Mundo Mixto desbloqueado!");
        }
    }
}

[System.Serializable]
public class MundoData
{
    public string nombre;
    public List<FocoIncendio> focos = new List<FocoIncendio>();
    public bool completado    = false;
    public bool desbloqueado  = false;

    private HashSet<FocoIncendio> focosApagados = new HashSet<FocoIncendio>();

    public bool ContieneFoco(FocoIncendio f) => focos.Contains(f);

    public void MarcarFocoApagado(FocoIncendio f) => focosApagados.Add(f);

    public bool EstaCompleto()
    {
        if (focos.Count == 0) return false;
        return focosApagados.Count >= focos.Count;
    }
}