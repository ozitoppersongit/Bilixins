using System;

public enum ObjetivoInfinito { Coleta, Combate, Busca }

public static class InfinitoRegras
{
    public static int Quantidade(ObjetivoInfinito tipo, int concluidas)
    {
        switch (tipo)
        {
            case ObjetivoInfinito.Coleta: return Math.Min(12, 4 + concluidas / 3);
            case ObjetivoInfinito.Combate: return Math.Min(4, 1 + concluidas / 5);
            default: return Math.Min(3, 1 + concluidas / 8);
        }
    }

    public static int NivelInimigo(int concluidas) => Math.Min(10, 1 + concluidas / 3);
    public static int Inimigos(int concluidas) => Math.Min(6, 2 + concluidas / 4);

    public static int PontosBase(ObjetivoInfinito tipo, int quantidade)
    {
        int valor = tipo == ObjetivoInfinito.Coleta ? 50 : tipo == ObjetivoInfinito.Combate ? 150 : 200;
        return valor * quantidade;
    }

    public static float Duracao(ObjetivoInfinito tipo, int quantidade, float distancia, float velocidade)
    {
        int custo = tipo == ObjetivoInfinito.Coleta ? 12 : tipo == ObjetivoInfinito.Combate ? 20 : 35;
        return 30 + quantidade * custo + (float)Math.Ceiling(distancia / Math.Max(0.1f, velocidade));
    }

    public static int Bonus(int pontosBase, float restante, float inicial)
    {
        double fracao = inicial > 0 ? Math.Max(0, Math.Min(1, restante / inicial)) : 0;
        return (int)Math.Floor(pontosBase * fracao);
    }
}
