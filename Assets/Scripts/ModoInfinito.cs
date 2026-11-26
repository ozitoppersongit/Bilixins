using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-20000)]
public class ModoInfinito : MonoBehaviour
{
    enum Estado { Preparando, Jogando, Recompensa, Encerrado }
    const string ChaveRecorde = "Infinito.Recorde";
    public static ModoInfinito Instance { get; private set; }
    [SerializeField] MapaGerador mapa;
    [SerializeField] SpawnsInfinito spawns;
    [SerializeField] MenuPausa pausa;
    [SerializeField] GameObject[] somenteHistoria;
    [SerializeField] GameObject[] barreiras;
    [SerializeField] GameObject telaResultado;
    [SerializeField] TMP_FontAsset fonte;
    [SerializeField] Sprite sombraHud;
    [SerializeField] RectTransform minimapaHud;
    [SerializeField] Sprite iconeColeta;
    [SerializeField] Sprite iconeCombate;
    Estado estado;
    bool mapaPronto;
    int score;
    int recorde;
    int concluidas;
    int coletados;
    int quantidade;
    int ultimaRegiao = -1;
    int ultimoTipo = -1;
    float restante;
    float duracao;
    ObjetivoInfinito tipo;
    BattleScript batalha;
    bool preparandoCombate;
    PlayerMovement movimento;
    SpawnsInfinito.Rodada rodada;
    Canvas hud;
    RectTransform painelPontos;
    RectTransform painelMissao;
    RectTransform painelAviso;
    Image iconeMissao;
    readonly Vector3[] cantosHud = new Vector3[4];
    TMP_Text textoScore;
    TMP_Text textoRecorde;
    TMP_Text textoObjetivo;
    TMP_Text textoRegiao;
    TMP_Text textoProgresso;
    TMP_Text textoTempo;
    TMP_Text textoAviso;
    readonly HashSet<GatilhoDeEncontro> derrotados = new HashSet<GatilhoDeEncontro>();

    public bool Encerrado => estado == Estado.Encerrado;
    public bool PodeAbrirPausa => estado == Estado.Jogando && !CarregamentoPartida.EmAndamento;
    public bool PodeAnimarEfeitos => (estado == Estado.Jogando || estado == Estado.Recompensa) &&
        batalha == null && !preparandoCombate && (pausa == null || !pausa.EstaPausado) &&
        (TransicaoBatalha.instancia == null || !TransicaoBatalha.instancia.EmTransicao);
    public bool PodeBatalhar => PodeExplorar;
    public bool PodeExplorar => estado == Estado.Jogando && batalha == null && !preparandoCombate &&
        Time.timeScale > 0f && (pausa == null || !pausa.EstaPausado) &&
        (TransicaoBatalha.instancia == null || !TransicaoBatalha.instancia.EmTransicao);
    public bool EmBusca => tipo == ObjetivoInfinito.Busca;
    public bool PossuiRodada => rodada != null && !Encerrado;
    public Vector3 CentroObjetivo => rodada != null ? rodada.centro : Vector3.zero;

    public IEnumerable<Transform> LixosVisiveis()
    {
        if (rodada == null || tipo != ObjetivoInfinito.Coleta) yield break;
        foreach (GameObject item in rodada.itens)
            if (item != null && item.activeInHierarchy) yield return item.transform;
    }

    public IEnumerable<Transform> InimigosVisiveis()
    {
        if (rodada == null) yield break;
        foreach (GatilhoDeEncontro inimigo in rodada.inimigos)
            if (inimigo != null && inimigo.isActiveAndEnabled &&
                inimigo.Resultado.Resultado != ResultadoBatalha.Vitoria) yield return inimigo.transform;
    }

    void Awake()
    {
        if (!ModoDeJogo.Infinito) { enabled = false; return; }
        Instance = this;
        foreach (GameObject objeto in somenteHistoria)
            if (objeto != null) objeto.SetActive(false);
        foreach (GameObject objeto in barreiras)
            if (objeto != null) objeto.SetActive(false);
        mapa.aoTerminarDeGerar.AddListener(MapaGerado);
        CriarHud();
        recorde = PlayerPrefs.GetInt(ChaveRecorde, 0);
        AtualizarHud();
        textoAviso.text = "Preparando missão...";
    }

    void MapaGerado() => mapaPronto = true;

    IEnumerator Start()
    {
        while (!mapaPronto || mapa.PlayerInstanciado == null) yield return null;
        movimento = mapa.PlayerInstanciado.GetComponent<PlayerMovement>();
        movimento.enabled = false;
        Time.timeScale = 0f;
        GerenciadorBatalha.instancia?.EncerrarPartida();
        // A geração também pode ter criado encontros vindos dos pixels do mapa.
        foreach (GatilhoDeEncontro encontro in FindObjectsByType<GatilhoDeEncontro>(FindObjectsSortMode.None))
        {
            Animator animador = encontro.GetComponentInParent<Animator>();
            GameObject objeto = animador != null ? animador.gameObject : encontro.gameObject;
            objeto.SetActive(false);
        }
        yield return null;
        if (!spawns.ConstruirNavegacao(mapa.PlayerInstanciado))
        {
            Encerrar("Não foi possível preparar a navegação");
            yield break;
        }
        yield return PrepararRodada();
    }

    IEnumerator PrepararRodada()
    {
        estado = Estado.Preparando;
        movimento.enabled = false;
        Time.timeScale = 0f;
        textoAviso.text = "Preparando missão...";
        SpawnsInfinito.Limpar(rodada);
        rodada = null;
        derrotados.Clear();
        if (!spawns.TemModelosValidos(concluidas))
        {
            Debug.LogError("Infinito: configure ao menos um inimigo com prefab, dados e peso positivo.", spawns);
            Encerrar("Nenhum inimigo configurado para o sorteio");
            yield break;
        }
        yield return null;
        int primeiroTipo = Random.Range(0, 3);
        int primeiraRegiao = Random.Range(0, Mathf.Max(1, spawns.QuantidadeRegioes));
        // Primeiro tenta combinações diferentes da rodada anterior.
        for (int repeticao = 0; repeticao < 2 && rodada == null; repeticao++)
            for (int t = 0; t < 3 && rodada == null; t++)
            {
                int candidato = (primeiroTipo + t) % 3;
                if (repeticao == 0 && candidato == ultimoTipo) continue;
                tipo = (ObjetivoInfinito)candidato;
                quantidade = InfinitoRegras.Quantidade(tipo, concluidas);
                for (int r = 0; r < spawns.QuantidadeRegioes; r++)
                {
                    int regiao = (primeiraRegiao + r) % spawns.QuantidadeRegioes;
                    if (repeticao == 0 && regiao == ultimaRegiao) continue;
                    rodada = spawns.Criar(tipo, quantidade, InfinitoRegras.Inimigos(concluidas),
                        InfinitoRegras.NivelInimigo(concluidas), regiao, movimento.transform, concluidas);
                    if (rodada != null) { ultimaRegiao = regiao; break; }
                    yield return null;
                }
            }
        if (rodada == null)
        {
            Debug.LogError("Infinito: nenhum conjunto de spawns acessíveis pôde ser preparado.");
            Encerrar("Não foi possível preparar a missão");
            yield break;
        }
        ultimoTipo = (int)tipo;
        coletados = 0;
        duracao = InfinitoRegras.Duracao(tipo, quantidade, rodada.distancia, movimento.VelocidadeNormal);
        restante = duracao;
        AtualizarHud();
        CarregamentoPartida.PreparacaoConcluida();
        yield return CarregamentoPartida.AguardarConclusao();
        for (int numero = 3; numero > 0; numero--)
        {
            textoAviso.text = numero.ToString();
            yield return new WaitForSecondsRealtime(1f);
        }
        textoAviso.text = "Já!";
        estado = Estado.Jogando;
        Time.timeScale = 1f;
        movimento.enabled = true;
        yield return new WaitForSecondsRealtime(0.5f);
        // Uma coleta imediata pode ter iniciado o aviso da próxima etapa.
        if (estado == Estado.Jogando && textoAviso.text == "Já!") textoAviso.text = "";
    }

    void Update()
    {
        if (estado == Estado.Jogando && pausa != null &&
            Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (pausa.EstaPausado) pausa.Retomar();
            else pausa.Pausar();
        }
        if (!PodeExplorar) return;
        // A última vitória só conclui a missão quando a transição terminar.
        if (coletados >= quantidade)
        {
            StartCoroutine(ConcluirRodada());
            return;
        }
        restante = Mathf.Max(0f, restante - Time.deltaTime);
        if (restante <= 0f) { Encerrar("Tempo esgotado!"); return; }
        if (tipo != ObjetivoInfinito.Combate)
            foreach (GameObject item in rodada.itens)
            {
                if (item == null || !item.activeSelf) continue;
                Vector3 distancia = item.transform.position - movimento.transform.position;
                if (Mathf.Abs(distancia.y) > 2.5f) continue;
                distancia.y = 0;
                if (distancia.sqrMagnitude > 2.25f || !spawns.PodeColetar(item)) continue;
                spawns.CriarRajada(item, rodada);
                item.SetActive(false);
                coletados++;
                AudioJogo.Instance?.TocarColeta();
            }
        AtualizarHud();
        if (coletados >= quantidade) StartCoroutine(ConcluirRodada());
    }

    IEnumerator ConcluirRodada()
    {
        estado = Estado.Recompensa;
        movimento.enabled = false;
        Time.timeScale = 0f;
        int pontosBase = InfinitoRegras.PontosBase(tipo, quantidade);
        int bonus = InfinitoRegras.Bonus(pontosBase, restante, duracao);
        score += pontosBase + bonus;
        concluidas++;
        AtualizarHud();
        textoAviso.text = $"Missão concluída! +{pontosBase + bonus} pontos\n{pontosBase} da missão + {bonus} de bônus de tempo";
        yield return new WaitForSecondsRealtime(2f);
        yield return PrepararRodada();
    }

    public void IniciarEncontro()
    {
        preparandoCombate = true;
        if (hud != null) hud.enabled = false;
    }

    public void IniciarBatalha(BattleScript atual)
    {
        batalha = atual;
        preparandoCombate = false;
        if (hud != null) hud.enabled = false;
    }
    public void FecharBatalha(BattleScript atual)
    {
        if (batalha == atual) batalha = null;
    }

    public void RegistrarVitoria(GatilhoDeEncontro encontro)
    {
        if (estado != Estado.Jogando || rodada == null || !rodada.inimigos.Contains(encontro) ||
            !derrotados.Add(encontro)) return;
        if (tipo == ObjetivoInfinito.Combate) coletados++;
    }

    public void PerderBatalha(BattleScript atual)
    {
        if (Encerrado) return;
        estado = Estado.Encerrado;
        StartCoroutine(MostrarResultado("Seu Bilixim foi derrotado!", atual));
    }

    void Encerrar(string motivo)
    {
        if (Encerrado) return;
        CarregamentoPartida.Cancelar();
        estado = Estado.Encerrado;
        StartCoroutine(MostrarResultado(motivo, null));
    }

    IEnumerator MostrarResultado(string motivo, BattleScript atual)
    {
        Time.timeScale = 0f;
        if (movimento != null) movimento.enabled = false;
        CameraSeguirPlayer cameraExploracao = FindAnyObjectByType<CameraSeguirPlayer>();
        if (cameraExploracao != null) cameraExploracao.enabled = false;
        if (score > recorde)
        {
            recorde = score;
            PlayerPrefs.SetInt(ChaveRecorde, recorde);
            PlayerPrefs.Save();
        }
        TransicaoBatalha transicao = TransicaoBatalha.instancia;
        if (transicao != null) yield return transicao.Abrir();
        if (atual != null) { atual.gameObject.SetActive(false); Destroy(atual.gameObject); }
        SpawnsInfinito.Limpar(rodada);
        rodada = null;
        hud.gameObject.SetActive(false);
        GameObject tela = Instantiate(telaResultado);
        tela.GetComponent<Canvas>().sortingOrder = 150;
        tela.GetComponent<TelaResultadoInfinito>().Mostrar(motivo, score, recorde);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (transicao != null) yield return transicao.Fechar();
    }

    void CriarHud()
    {
        var objeto = new GameObject("HUD Infinito", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        hud = objeto.GetComponent<Canvas>();
        hud.renderMode = RenderMode.ScreenSpaceOverlay;
        hud.sortingOrder = 110;
        var scaler = objeto.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800, 450);
        scaler.matchWidthOrHeight = 0.5f;

        painelPontos = CriarColuna(hud.transform, "Pontuação", 0, 3f);
        painelPontos.pivot = new Vector2(1f, 1f);
        painelPontos.sizeDelta = new Vector2(160f, 0f);
        textoScore = CriarLinhaHud(painelPontos, "Pontos", 18f, TextAlignmentOptions.Right);
        textoRecorde = CriarLinhaHud(painelPontos, "Recorde", 11f, TextAlignmentOptions.Right);

        painelMissao = CriarColuna(hud.transform, "Missão infinita", 12, 5f);
        painelMissao.pivot = Vector2.up;
        painelMissao.sizeDelta = new Vector2(280f, 0f);
        CriarSombra(painelMissao);

        var cabecalho = new GameObject("Objetivo e ícone", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        cabecalho.transform.SetParent(painelMissao, false);
        var linha = cabecalho.GetComponent<HorizontalLayoutGroup>();
        linha.spacing = 6f;
        linha.childAlignment = TextAnchor.UpperLeft;
        linha.childControlWidth = linha.childControlHeight = true;
        linha.childForceExpandWidth = linha.childForceExpandHeight = false;

        var icone = new GameObject("Ícone", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        icone.transform.SetParent(cabecalho.transform, false);
        iconeMissao = icone.GetComponent<Image>();
        iconeMissao.preserveAspect = true;
        iconeMissao.raycastTarget = false;
        var medidaIcone = icone.GetComponent<LayoutElement>();
        medidaIcone.minWidth = medidaIcone.preferredWidth = 28f;
        medidaIcone.minHeight = medidaIcone.preferredHeight = 28f;
        var descricao = CriarColuna(cabecalho.transform, "Descrição", 0, 5f, false);
        descricao.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        textoObjetivo = CriarLinhaHud(descricao, "Objetivo", 14f, TextAlignmentOptions.Left);
        textoRegiao = CriarLinhaHud(descricao, "Região", 11f, TextAlignmentOptions.Left);

        var rodape = new GameObject("Progresso e tempo", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        rodape.transform.SetParent(painelMissao, false);
        var layoutRodape = rodape.GetComponent<HorizontalLayoutGroup>();
        layoutRodape.spacing = 8f;
        layoutRodape.childControlWidth = layoutRodape.childControlHeight = true;
        layoutRodape.childForceExpandWidth = true;
        layoutRodape.childForceExpandHeight = false;
        textoProgresso = CriarLinhaHud(rodape.transform, "Progresso", 14f, TextAlignmentOptions.Left);
        textoTempo = CriarLinhaHud(rodape.transform, "Tempo", 14f, TextAlignmentOptions.Right);

        var aviso = new GameObject("Aviso central", typeof(RectTransform));
        painelAviso = aviso.GetComponent<RectTransform>();
        painelAviso.SetParent(hud.transform, false);
        painelAviso.anchorMin = painelAviso.anchorMax = Vector2.one * 0.5f;
        CriarSombra(painelAviso);
        textoAviso = CriarTexto("Mensagem", Vector2.zero, Vector2.zero, 22f);
        textoAviso.transform.SetParent(painelAviso, false);
        textoAviso.rectTransform.anchorMin = Vector2.zero;
        textoAviso.rectTransform.anchorMax = Vector2.one;
        textoAviso.rectTransform.offsetMin = Vector2.one * 12f;
        textoAviso.rectTransform.offsetMax = Vector2.one * -12f;
    }

    RectTransform CriarColuna(Transform pai, string nome, int margem, float espaco, bool ajustarAltura = true)
    {
        var objeto = new GameObject(nome, typeof(RectTransform), typeof(VerticalLayoutGroup));
        var area = objeto.GetComponent<RectTransform>();
        area.SetParent(pai, false);
        area.anchorMin = area.anchorMax = Vector2.one * 0.5f;
        var layout = objeto.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(margem, margem, margem, margem);
        layout.spacing = espaco;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        if (ajustarAltura)
            objeto.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return area;
    }

    void CriarSombra(RectTransform pai)
    {
        var objeto = new GameObject("Sombra", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        var imagem = objeto.GetComponent<Image>();
        imagem.rectTransform.SetParent(pai, false);
        imagem.rectTransform.anchorMin = Vector2.zero;
        imagem.rectTransform.anchorMax = Vector2.one;
        imagem.rectTransform.offsetMin = imagem.rectTransform.offsetMax = Vector2.zero;
        objeto.GetComponent<LayoutElement>().ignoreLayout = true;
        imagem.sprite = sombraHud;
        imagem.type = Image.Type.Sliced;
        imagem.pixelsPerUnitMultiplier = 24f;
        imagem.color = sombraHud != null ? Color.white : new Color(0f, 0f, 0f, 0.65f);
        imagem.raycastTarget = false;
    }

    TMP_Text CriarLinhaHud(Transform pai, string nome, float tamanho, TextAlignmentOptions alinhamento)
    {
        TMP_Text texto = CriarTexto(nome, Vector2.zero, Vector2.zero, tamanho);
        texto.transform.SetParent(pai, false);
        texto.alignment = alinhamento;
        return texto;
    }

    void LateUpdate()
    {
        if (hud == null) return;
        bool transicao = TransicaoBatalha.instancia != null && TransicaoBatalha.instancia.EmTransicao;
        hud.enabled = !Encerrado && !preparandoCombate && batalha == null && !transicao;
        if (!hud.enabled) return;

        RectTransform raiz = (RectTransform)hud.transform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(raiz, Screen.safeArea.min, null, out Vector2 minimo);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(raiz, Screen.safeArea.max, null, out Vector2 maximo);
        Rect botao = pausa != null && pausa.botaoPausa != null
            ? LimitesNoHud((RectTransform)pausa.botaoPausa.transform)
            : Rect.MinMaxRect(maximo.x - 65f, maximo.y - 65f, maximo.x - 8f, maximo.y - 8f);
        painelPontos.anchoredPosition = new Vector2(botao.xMin - 8f, Mathf.Min(botao.yMax, maximo.y - 8f));

        Rect mapa = minimapaHud != null ? LimitesNoHud(minimapaHud)
            : Rect.MinMaxRect(minimo.x + 8f, maximo.y - 130f, minimo.x + 130f, maximo.y - 8f);
        float esquerda = Mathf.Max(mapa.xMax + 16f, minimo.x + 8f);
        float direita = Mathf.Min(painelPontos.anchoredPosition.x - painelPontos.rect.width - 8f, maximo.x - 8f);
        float largura = Mathf.Min(280f, Mathf.Max(140f, direita - esquerda));
        painelMissao.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, largura);
        painelMissao.anchoredPosition = new Vector2(Mathf.Min(esquerda, direita - largura),
            Mathf.Min(mapa.yMax, maximo.y - 8f));

        bool temAviso = !string.IsNullOrEmpty(textoAviso.text);
        painelAviso.gameObject.SetActive(temAviso);
        if (temAviso)
        {
            float larguraMaxima = Mathf.Min(600f, maximo.x - minimo.x - 24f);
            Vector2 preferido = textoAviso.GetPreferredValues(textoAviso.text, larguraMaxima - 24f, Mathf.Infinity);
            painelAviso.sizeDelta = new Vector2(Mathf.Min(larguraMaxima, preferido.x + 24f), preferido.y + 24f);
        }
    }

    Rect LimitesNoHud(RectTransform origem)
    {
        origem.GetWorldCorners(cantosHud);
        Canvas canvasOrigem = origem.GetComponentInParent<Canvas>();
        Camera cameraOrigem = canvasOrigem != null && canvasOrigem.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvasOrigem.worldCamera : null;
        Vector2 minimo = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 maximo = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        foreach (Vector3 canto in cantosHud)
        {
            Vector2 tela = RectTransformUtility.WorldToScreenPoint(cameraOrigem, canto);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)hud.transform, tela, null, out Vector2 local);
            minimo = Vector2.Min(minimo, local);
            maximo = Vector2.Max(maximo, local);
        }
        return Rect.MinMaxRect(minimo.x, minimo.y, maximo.x, maximo.y);
    }

    TMP_Text CriarTexto(string nome, Vector2 ancora, Vector2 tamanho, float tamanhoFonte)
    {
        var objeto = new GameObject(nome, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(Outline));
        objeto.transform.SetParent(hud.transform, false);
        TMP_Text texto = objeto.GetComponent<TMP_Text>();
        texto.font = fonte;
        texto.fontSize = tamanhoFonte;
        texto.alignment = TextAlignmentOptions.Center;
        texto.raycastTarget = false;
        texto.rectTransform.anchorMin = texto.rectTransform.anchorMax = ancora;
        texto.rectTransform.sizeDelta = tamanho;
        objeto.GetComponent<Outline>().effectDistance = Vector2.one;
        return texto;
    }

    void AtualizarHud()
    {
        textoScore.text = $"Pontos: {score}";
        textoRecorde.text = $"Recorde: {recorde}";
        string tarefa = tipo == ObjetivoInfinito.Coleta ? "Colete lixo" :
            tipo == ObjetivoInfinito.Combate ? "Derrote os Bilixins" : "Encontre pacotes recicláveis";
        iconeMissao.sprite = tipo == ObjetivoInfinito.Combate ? iconeCombate : iconeColeta;
        iconeMissao.enabled = iconeMissao.sprite != null;
        textoObjetivo.text = rodada != null ? tarefa : "";
        textoRegiao.text = rodada != null ? rodada.regiao : "";
        textoProgresso.text = rodada != null ? $"{coletados} / {quantidade}" : "";
        int segundos = Mathf.CeilToInt(restante);
        textoTempo.text = rodada != null ? $"{segundos / 60:00}:{segundos % 60:00}" : "";
        textoTempo.color = restante <= 10 ? new Color(1f, 0.3f, 0.2f) : Color.white;
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        if (mapa != null) mapa.aoTerminarDeGerar.RemoveListener(MapaGerado);
        SpawnsInfinito.Limpar(rodada);
        if (hud != null) Destroy(hud.gameObject);
        Instance = null;
    }
}
