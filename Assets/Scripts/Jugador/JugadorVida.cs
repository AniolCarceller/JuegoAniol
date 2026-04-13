using UnityEngine;

public enum EstadoJugador { Normal, Dañado, Electrocutado, Muerto }

public class JugadorVida : MonoBehaviour
{
    [Header("Vida")]
    public float vidaMaxima = 100f;
    public float vidaActual = 100f;

    [Header("Electrocución")]
    public float duracionElectrocucion = 2f;

    public EstadoJugador estado = EstadoJugador.Normal;

    private float timerElectrocucion = 0f;

    void Update()
    {
        if (estado == EstadoJugador.Electrocutado)
        {
            timerElectrocucion -= Time.deltaTime;
            if (timerElectrocucion <= 0f)
            {
                estado = EstadoJugador.Normal;
                Debug.Log("Ya no estás electrocutado");
            }
        }
    }

    public void RecibirDaño(float cantidad)
    {
        if (estado == EstadoJugador.Muerto) return;

        vidaActual = Mathf.Max(0, vidaActual - cantidad);
        Debug.Log($"Jugador recibe {cantidad} daño. Vida: {vidaActual}");

        if (vidaActual <= 0)
            Morir();
        else
            estado = EstadoJugador.Dañado;

        Invoke(nameof(Volver), 0.3f);
    }

    public void Electrocutar()
    {
        if (estado == EstadoJugador.Muerto) return;

        estado = EstadoJugador.Electrocutado;
        timerElectrocucion = duracionElectrocucion;
        RecibirDaño(20f);
        Debug.Log("¡Electrocutado!");
    }

    void Morir()
    {
        estado = EstadoJugador.Muerto;
        Debug.Log("Jugador muerto");
    }

    void Volver()
    {
        if (estado != EstadoJugador.Muerto && estado != EstadoJugador.Electrocutado)
            estado = EstadoJugador.Normal;
    }

    void OnGUI()
    {
        GUI.Label(new Rect(20, 70, 200, 25), $"Vida: {vidaActual:F0} / {vidaMaxima}");
        GUI.Label(new Rect(20, 95, 200, 25), $"Estado: {estado}");
    }
}