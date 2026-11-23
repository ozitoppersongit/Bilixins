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
    [SerializeField] Transform pontoDeRetorno;
    private GerenciadorBatalha gB;
    private Collider gatilho;
    private bool batalhaIniciada = false;

    private Ferralha ferralha;

    [Header("Tutorial")]
    public bool tutorialBatalha = false;
    public bool EncontroInfinito { get; set; }

    public void ConfigurarInfinito(BilixinsData dados, GameObject tela, Collider volume)
    {
        bilixinInimigo = dados;
        battlePrefab = tela;
        gatilho = volume;
        tutorialBatalha = false;
        EncontroInfinito = true;
        DesvincularMissao();
    }

    public bool FugaBloqueadaPorCutscene { get; private set; }
    public Cutscenes CutsceneResponsavel { get; private set; }
    public bool EhFerralha => ferralha != null;
    Transform jogadorDoEncontro;
    bool aguardandoSaidaAposDerrota;
    float protegidoAte;
    public ResultadoEncontro Resultado { get; private set; } = new ResultadoEncontro();
    public Collider Volume => gatilho;
    public Transform PontoDeRetorno => pontoDeRetorno;

    public void MarcarBatalhaDeCutscene(Cutscenes responsavel)
    {
        FugaBloqueadaPorCutscene = true;
        CutsceneResponsavel = responsavel;
        Resultado = new ResultadoEncontro();
    }

    void Awake()
    {
        gatilho = GetComponent<Collider>();

        gB = Object.FindAnyObjectByType<GerenciadorBatalha>();

        ferralha = GetComponent<Ferralha>();

        if (ferralha == null)
            ferralha = GetComponentInParent<Ferralha>();
        if (ferralha == null)
            ferralha = GetComponentInChildren<Ferralha>(true);
    }
    

void OnTriggerEnter(Collider other)
{
    if (!other.CompareTag("Player"))
        return;
    if (ModoDeJogo.Infinito &&
        (!EncontroInfinito || ModoInfinito.Instance == null || !ModoInfinito.Instance.PodeBatalhar)) return;
    if (aguardandoSaidaAposDerrota || Time.unscaledTime < protegidoAte ||
        (TransicaoBatalha.instancia != null && TransicaoBatalha.instancia.RetornandoDaDerrota))
        return;

    // Ferralha ainda está ignorando o jogador?
    if (ferralha != null && !ferralha.PodePerseguir())
        return;

    if (batalhaIniciada)
        return;

    batalhaIniciada = true;
    if (EncontroInfinito) ModoInfinito.Instance?.IniciarEncontro();
    if (Resultado.Resultado != ResultadoBatalha.EmAndamento) Resultado = new ResultadoEncontro();
    jogadorDoEncontro = other.transform;

    Debug.Log($"GatilhoDeEncontro: player encontrou '{gameObject.name}'.");

    if (gatilho != null)
        gatilho.enabled = false;

    StartCoroutine(IniciarBatalhaComTransicao(other.transform));
}

private void OnTriggerExit(Collider other)
{
    if (!other.CompareTag("Player"))
        return;

    if (ferralha != null)
    {
        ferralha.JogadorSaiuDaVisao();
    }
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
        Resultado.Concluir(ResultadoBatalha.Vitoria);
        if (EncontroInfinito) ModoInfinito.Instance?.RegistrarVitoria(this);
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

    public void ProtegerAposRetorno()
    {
        aguardandoSaidaAposDerrota = true;
        protegidoAte = Time.unscaledTime + 3f;
        if (ferralha != null) ferralha.ProtegerRetorno();
    }

    public void DesvincularMissao()
    {
        CutsceneResponsavel = null;
        FugaBloqueadaPorCutscene = false;
    }

    public bool ReposicionarAposDerrota()
    {
        if (jogadorDoEncontro == null) return false;
        Reativar();
        var mapa = FindAnyObjectByType<MapaGerador>();
        if (mapa == null) return false;
        Pose retorno = RetornoSeguro.ObterPose(pontoDeRetorno, mapa.PoseInicialJogador);
        ReposicionarJogador(jogadorDoEncontro, retorno);
        ProtegerAposRetorno();
        return true;
    }
    void FixedUpdate()
    {
        if (!aguardandoSaidaAposDerrota) return;
        var controller = jogadorDoEncontro != null ? jogadorDoEncontro.GetComponent<CharacterController>() : null;
        if (Time.unscaledTime >= protegidoAte && (gatilho == null || controller == null || !gatilho.bounds.Intersects(controller.bounds)))
        {
            aguardandoSaidaAposDerrota = false;
            if (ferralha != null) ferralha.JogadorSaiuDaVisao();
        }
    }

    public static void ReposicionarJogador(Transform jogador, Pose pose)
    {
        CharacterController controller = jogador.GetComponent<CharacterController>();
        bool reativar = controller != null && controller.enabled;
        if (reativar) controller.enabled = false;
        jogador.SetPositionAndRotation(pose.position, pose.rotation);
        if (reativar) controller.enabled = true;
        Physics.SyncTransforms();
    }
}
