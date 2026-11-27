using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using Unity.Cinemachine;

public class GatilhoDeEncontro : MonoBehaviour
{
    public UnityEvent aoEncontrarPlayer;
    public UnityEvent aoVencer = new UnityEvent();
    bool vitoriaProcessada;

    public BilixinsData bilixinInimigo;

    [Header("Batalha")]
    public GameObject battlePrefab;
    [SerializeField] Transform pontoDeRetorno;
    [SerializeField] bool encerraHistoria;
    public bool EncerraHistoria => encerraHistoria && !ModoDeJogo.Infinito;
    private GerenciadorBatalha gB;
    private Collider gatilho;
    private bool batalhaIniciada = false;

    private Ferralha ferralha;

    [Header("Tutorial")]
    public bool tutorialBatalha = false;
    public bool EncontroInfinito { get; set; }
    Transform jogadorInfinito;
    Quaternion rotacaoInicialInfinito;
    Quaternion? rotacaoDeOlhar;
    const float DistanciaParaOlhar = 5f;
    const float VelocidadeGiro = 420f;

    public void ConfigurarInfinito(BilixinsData dados, GameObject tela, Collider volume, Transform jogador)
    {
        enabled = true;
        bilixinInimigo = dados;
        battlePrefab = tela;
        gatilho = volume;
        tutorialBatalha = false;
        EncontroInfinito = true;
        jogadorInfinito = jogador;
        rotacaoInicialInfinito = transform.localRotation;
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
    if (EncontroInfinito && ferralha == null && DirecaoJogador(other.transform, out Vector3 direcao))
        transform.rotation = Quaternion.LookRotation(direcao);
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
        if (vitoriaProcessada || (Resultado.Resultado != ResultadoBatalha.EmAndamento && Resultado.Resultado != ResultadoBatalha.Vitoria)) return;
        vitoriaProcessada = true;
        Resultado.Concluir(ResultadoBatalha.Vitoria);
        if (EncontroInfinito) ModoInfinito.Instance?.RegistrarVitoria(this);
        if (!ModoDeJogo.Infinito) aoVencer?.Invoke();
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
    // Triggers também recebem OnTriggerEnter em MonoBehaviours desativados.
    // A liberação da proteção, porém, precisa de Update em execução.
    enabled = true;

    foreach (IAntesDaBatalha a in GetComponentsInChildren<IAntesDaBatalha>())
        a.VoltarAoNormal();

    if (gatilho != null)
        gatilho.enabled = true;
}

    public void ProtegerAposRetorno()
    {
        enabled = true;
        aguardandoSaidaAposDerrota = true;
        protegidoAte = Time.unscaledTime + 3f;
        if (ferralha != null) ferralha.ProtegerRetorno();
    }

    public void ReativarAposFuga()
    {
        rotacaoDeOlhar = null;
        if (EncontroInfinito) ProtegerAposRetorno();
        Reativar();
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
    void Update()
    {
        if (!aguardandoSaidaAposDerrota) return;
        var controller = jogadorDoEncontro != null ? jogadorDoEncontro.GetComponent<CharacterController>() : null;
        if (Time.unscaledTime >= protegidoAte && SaiuDoVolume(controller))
        {
            aguardandoSaidaAposDerrota = false;
            if (ferralha != null) ferralha.JogadorSaiuDaVisao();
        }
    }

    bool SaiuDoVolume(CharacterController controlador)
    {
        if (gatilho == null || controlador == null) return true;
        Bounds area = gatilho.bounds;
        if (EncontroInfinito) area.Expand(1f);
        return !area.Intersects(controlador.bounds);
    }

    bool DirecaoJogador(Transform jogador, out Vector3 direcao)
    {
        direcao = jogador.position - transform.position;
        float altura = Mathf.Abs(direcao.y);
        direcao.y = 0f;
        return altura < 2.5f && direcao.sqrMagnitude > 0.0001f &&
            direcao.sqrMagnitude <= DistanciaParaOlhar * DistanciaParaOlhar;
    }

    void LateUpdate()
    {
        if (!EncontroInfinito || batalhaIniciada || jogadorInfinito == null ||
            ModoInfinito.Instance == null || !ModoInfinito.Instance.PodeExplorar) return;
        Quaternion destino = ferralha != null ? transform.localRotation : rotacaoInicialInfinito;
        bool perto = DirecaoJogador(jogadorInfinito, out Vector3 direcao);
        if (perto)
        {
            Quaternion olhar = Quaternion.LookRotation(direcao);
            destino = transform.parent != null ? Quaternion.Inverse(transform.parent.rotation) * olhar : olhar;
        }
        else if (!rotacaoDeOlhar.HasValue && ferralha != null) return;

        Quaternion atual = Quaternion.RotateTowards(rotacaoDeOlhar ?? transform.localRotation, destino, VelocidadeGiro * Time.deltaTime);
        transform.localRotation = atual;
        rotacaoDeOlhar = perto || Quaternion.Angle(atual, destino) > 0.1f ? atual : null;
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
