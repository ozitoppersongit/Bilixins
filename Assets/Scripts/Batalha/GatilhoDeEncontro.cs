using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using Unity.Cinemachine;

public class GatilhoDeEncontro : MonoBehaviour
{
    public UnityEvent aoEncontrarPlayer;

    public BilixinsData bilixinInimigo;

    [Header("Batalha")]
    public GameObject battlePrefab;
    private GerenciadorBatalha gB;
    private Collider gatilho;
    private bool esperandoSair = false;
    private bool batalhaIniciada = false;

    void Awake()
    {
        gatilho = GetComponent<Collider>();

        gB = Object.FindAnyObjectByType<GerenciadorBatalha>();
    }

    void OnTriggerEnter(Collider other)
{
    if (!other.CompareTag("Player"))
        return;

    if (esperandoSair)
        return;

    if (batalhaIniciada)
        return;

    batalhaIniciada = true;

    Debug.Log($"GatilhoDeEncontro: player encontrou '{gameObject.name}'.");

    if (gatilho != null)
        gatilho.enabled = false;

    StartCoroutine(IniciarBatalhaComTransicao());
}

void OnTriggerExit(Collider other)
{
    if (!other.CompareTag("Player"))
        return;

    if (esperandoSair)
    {
        esperandoSair = false;
        batalhaIniciada = false;

        if (gatilho != null)
            gatilho.enabled = true;

        Debug.Log("Player saiu do gatilho. Encontro reativado.");
    }
}

    private IEnumerator IniciarBatalhaComTransicao()
{
    // A bolinha cresce e cobre a tela
    yield return StartCoroutine(
        TransicaoBatalha.instancia.Abrir()
    );

    // Instancia a batalha
    GameObject batalha = Instantiate(battlePrefab);

    BattleScript battleScript =
        batalha.GetComponent<BattleScript>();

    battleScript.IniciarBatalha(
        bilixinInimigo,
        this
    );

    // A bolinha diminui revelando a batalha
    yield return StartCoroutine(
        TransicaoBatalha.instancia.Fechar()
    );
}

    public void Vencer()
    {
        gB.BilixinDerrotado();
        Destroy(gameObject);
    }

    private IEnumerator FinalizarVitoria()
{
    // Cobre a batalha
    yield return StartCoroutine(
        TransicaoBatalha.instancia.Abrir()
    );

    // Registra o Bilixin derrotado
    if (GerenciadorBatalha.instancia != null)
        GerenciadorBatalha.instancia.BilixinDerrotado();

    // Remove o inimigo do mapa
    Destroy(gameObject);

    // Fecha a transição
    TransicaoBatalha.instancia.FecharDepoisDaVitoria();
}

    public void Reativar()
{
    esperandoSair = true;

    if (gatilho != null)
        gatilho.enabled = true;
}
}