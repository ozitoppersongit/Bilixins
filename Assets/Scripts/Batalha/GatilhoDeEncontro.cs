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
    private bool batalhaIniciada = false;

    [Header("Tutorial")]
    public bool tutorialBatalha = false;

    void Awake()
    {
        gatilho = GetComponent<Collider>();

        gB = Object.FindAnyObjectByType<GerenciadorBatalha>();
    }
    

void OnTriggerEnter(Collider other)
{
    if (!other.CompareTag("Player"))
        return;

    if (batalhaIniciada)
        return;

    batalhaIniciada = true;

    Debug.Log($"GatilhoDeEncontro: player encontrou '{gameObject.name}'.");

    if (gatilho != null)
        gatilho.enabled = false;

    StartCoroutine(IniciarBatalhaComTransicao(other.transform));
}

    private IEnumerator IniciarBatalhaComTransicao(Transform player = null)
{
    /// oziel, aqui a animação antes da batalha: se o inimigo tem um script IAntesDaBatalha (ex: Ferralha), ele toca o ataque
    /// e a batalha só abre depois do tempo que ele devolver. Sem o script, abre na hora como antes.
    float espera = 0f;
    foreach (IAntesDaBatalha a in GetComponentsInChildren<IAntesDaBatalha>())
        espera = Mathf.Max(espera, a.IniciarAntesDaBatalha(player));
    if (espera > 0f)
        yield return new WaitForSeconds(espera);

    // A bolinha cresce e cobre a tela
    yield return StartCoroutine(
        TransicaoBatalha.instancia.Abrir()
    );

    // Instancia a batalha
    GameObject batalha = Instantiate(battlePrefab);

    BattleScript battleScript =
        batalha.GetComponent<BattleScript>();

    battleScript.modoTutorial = tutorialBatalha;

    battleScript.IniciarBatalha(
        bilixinInimigo,
        this
    );

    // A bolinha diminui revelando a batalha
    yield return StartCoroutine(
        TransicaoBatalha.instancia.Fechar()
    );
}

    void IniciarBatalha()
    {
        if (battlePrefab == null)
        {
            Debug.LogError("BattlePrefab não foi configurado no GatilhoDeEncontro!");
            return;
        }

        if (bilixinInimigo == null)
        {
            Debug.LogError("Nenhum BilixinsData foi atribuído a este inimigo!");
            return;
        }

        Transform battleParent = GameObject.Find("Batalha").transform;

        GameObject batalha = Instantiate(
            battlePrefab,
            battleParent
        );

        BattleScript battleScript =
            batalha.GetComponent<BattleScript>();

        if (battleScript == null)
        {
            Debug.LogError(
                "BattlePrefab não possui um BattleScript!"
            );
            return;
        }

        battleScript.IniciarBatalha(
            bilixinInimigo,
            this
        );
        
    }

    public void Vencer()
    {
        /// oziel, aqui a derrota animada: se o inimigo tem um script IDerrotaAnimada (ex: Ferralha), ele toca a animação
        /// e o objeto só é destruído depois do tempo que ele devolver. Sem o script, some na hora como antes.
        float espera = 0f;
        foreach (IDerrotaAnimada d in GetComponentsInChildren<IDerrotaAnimada>())
            espera = Mathf.Max(espera, d.IniciarDerrota());
        Destroy(gameObject, espera);
    }

    public void Reativar()
{
    batalhaIniciada = false;

    foreach (IAntesDaBatalha a in GetComponentsInChildren<IAntesDaBatalha>())
        a.VoltarAoNormal();

    if (gatilho != null)
        gatilho.enabled = true;
}
}