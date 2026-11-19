using System;

public sealed class ProgressoBilixin
{
    public int Nivel { get; private set; }
    public int Xp { get; private set; }
    public int XpProximoNivel => checked(100 + 50 * (Nivel - 1));

    public ProgressoBilixin(int nivelInicial)
    {
        Nivel = Math.Max(1, nivelInicial);
    }

    public void ReceberXp(int quantidade)
    {
        if (quantidade <= 0) return;
        Xp = checked(Xp + quantidade);
        while (Xp >= XpProximoNivel)
        {
            Xp -= XpProximoNivel;
            Nivel++;
        }
    }
}
