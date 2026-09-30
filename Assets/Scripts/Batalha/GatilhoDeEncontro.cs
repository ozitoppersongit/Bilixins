using UnityEngine;
using UnityEngine.Events;

// O MapaGerador gruda isso em cada inimigo do mapa, com o trigger já do tamanho certo.
// Oziel, a batalha é na mesma cena — só ouve o aoEncontrarPlayer aqui embaixo (ou no
// Inspector mesmo) e faz a tua parte. Só dispara uma vez, pra não ficar chamando de
// novo se o player ficar parado em cima. Quando resolver a luta: Vencer() some com o
// bicho, Reativar() destrava de novo se o player perder ou correr.
public class GatilhoDeEncontro : MonoBehaviour
{
    public UnityEvent aoEncontrarPlayer;

    Collider gatilho;

    void Awake()
    {
        gatilho = GetComponent<Collider>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log($"GatilhoDeEncontro: player encontrou '{gameObject.name}'.");
        if (gatilho != null) gatilho.enabled = false;
        aoEncontrarPlayer?.Invoke();
    }

    public void Vencer()
    {
        Destroy(gameObject);
    }

    public void Reativar()
    {
        if (gatilho != null) gatilho.enabled = true;
    }
}
