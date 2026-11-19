using UnityEngine;
using System.Collections.Generic;

public class GerenciadorBatalha : MonoBehaviour
{
    public static GerenciadorBatalha instancia;
    readonly Dictionary<BilixinsData, ProgressoBilixin> progressos = new Dictionary<BilixinsData, ProgressoBilixin>();

    public ProgressoBilixin ObterProgresso(BilixinsData bilixin)
    {
        if (!progressos.TryGetValue(bilixin, out ProgressoBilixin progresso))
        {
            progresso = new ProgressoBilixin(bilixin.nivel);
            progressos.Add(bilixin, progresso);
        }
        return progresso;
    }

    public void EncerrarPartida()
    {
        progressos.Clear();
    }

    private void Awake()
    {
        if (instancia == null)
        {
            instancia = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }
}
