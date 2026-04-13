using UnityEngine;

public class TorreHP : MonoBehaviour
{
    public float vidaMaxima = 100f;
    public float vidaActual;

    void Start()
    {
        vidaActual = vidaMaxima;
    }

    public void RecibirDaño(float cantidad)
    {
        vidaActual -= cantidad;
        vidaActual = Mathf.Clamp(vidaActual, 0, vidaMaxima);
        Debug.Log($"Torre HP: {vidaActual}/{vidaMaxima}");
    }

    public void Curar(float cantidad)
    {
        vidaActual += cantidad;
        vidaActual = Mathf.Clamp(vidaActual, 0, vidaMaxima);
        Debug.Log($"Torre curada. HP: {vidaActual}/{vidaMaxima}");
    }

    public bool EstaBajaDeLifeDeMedio()
    {
        return vidaActual <= vidaMaxima * 0.5f;
    }
}