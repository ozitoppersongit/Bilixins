using System;

public static class RegrasRecompensa
{
    public static int Sortear(float chancePercentual, int quantidade, double sorteioChance, double sorteioItem)
    {
        if (quantidade <= 0 || float.IsNaN(chancePercentual) || chancePercentual <= 0 ||
            double.IsNaN(sorteioChance) || double.IsNaN(sorteioItem)) return -1;
        double chance = Math.Min(100, chancePercentual) / 100.0;
        // 100% sempre premia, inclusive quando Random.value retorna 1.
        if (chance < 1 && sorteioChance >= chance) return -1;
        return Math.Min(quantidade - 1, (int)(Math.Max(0, Math.Min(1, sorteioItem)) * quantidade));
    }
}
