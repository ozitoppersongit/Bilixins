using System;
using System.Collections.Generic;

// Independente do Unity para testar a distribuição e os limites do sorteio.
public static class SorteioPonderado
{
    public static int Sortear(IReadOnlyList<float> pesos, double fracao)
    {
        if (pesos == null || double.IsNaN(fracao)) return -1;
        double total = 0;
        int ultimo = -1;
        for (int i = 0; i < pesos.Count; i++)
            if (Valido(pesos[i])) { total += pesos[i]; ultimo = i; }
        if (ultimo < 0) return -1;
        double alvo = Math.Max(0, Math.Min(1, fracao)) * total;
        for (int i = 0; i < pesos.Count; i++)
        {
            if (!Valido(pesos[i])) continue;
            alvo -= pesos[i];
            if (alvo < 0) return i;
        }
        return ultimo; // Random.value também pode devolver exatamente 1.
    }

    public static bool Valido(float peso) => peso > 0 && !float.IsNaN(peso) && !float.IsInfinity(peso);
}
