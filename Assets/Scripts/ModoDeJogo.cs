using UnityEngine;

public static class ModoDeJogo
{
    public static bool Infinito { get; private set; }

    public static void Jogar(bool infinito)
    {
        if (CarregamentoPartida.EmAndamento) return;
        Infinito = infinito;
        Time.timeScale = 1f;
        CarregamentoPartida.Carregar();
    }
}
