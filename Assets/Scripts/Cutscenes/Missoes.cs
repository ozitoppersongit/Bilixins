using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Missoes : MonoBehaviour
{
    public static Missoes Instance { get; private set; }
    public Transform missionPanel;
    [SerializeField] RectTransform minimapa;
    [SerializeField] Sprite iconePadrao;
    [SerializeField] TMP_Text tituloModelo;
    [SerializeField] TMP_Text objetivoModelo;
    [SerializeField] TMP_Text contadorModelo;

    [Serializable]
    public class Missao
    {
        public string nome;
        public TipoMissao tipo;
        [TextArea] public string titulo;
        [TextArea] public string objetivo = "Colete o lixo";
        [TextArea] public string objetivoEntrega = "Entregue os itens";
        public Sprite icone;
        public bool ocultarIcone;
        [Tooltip("Objeto cujos filhos são os itens a coletar.")] public Transform itens;
        [Tooltip("Quantos coletar (0 = todos os filhos).")] public int quantidade;
        [Tooltip("Onde entregar. Vazio = a missão fecha assim que coletar tudo.")] public Transform entrega;
        public float raioColeta = 1.2f;
        public float raioEntrega = 2.5f;
        public ItemData itemRecompensa;
        [Min(1)] public int quantidadeRecompensa = 1;
        public UnityEvent aoIniciar;
        public UnityEvent aoConcluir;
    }

    public enum TipoMissao { Coleta, Combate, Aviso }

    sealed class MissaoAtiva
    {
        public Missao dados;
        public int coletados;
        public int total;
        public bool entregando;
        public GameObject linha;
        public TMP_Text objetivo;
        public TMP_Text contador;
    }

    [SerializeField] MapaGerador mapaGerador;
    [SerializeField] List<Missao> missoes = new List<Missao>();

    /// oziel, aqui é o efeito de coleta (opcional): prefab com ParticleSystem de uma rajada que se destrói sozinho. Vazio = nada.
    [SerializeField] GameObject efeitoColeta;

    // missão, coletados, total
    public event Action<string, int, int> aoMudarProgresso;
    public event Action<string> aoMissaoConcluida;

    readonly List<MissaoAtiva> ativas = new List<MissaoAtiva>();
    readonly HashSet<string> concluidas = new HashSet<string>();
    readonly HashSet<string> vitoriasPendentes = new HashSet<string>();

    public IEnumerable<Transform> LixosColetaveis()
    {
        foreach (MissaoAtiva ativa in ativas)
        {
            if (ativa.dados.tipo != TipoMissao.Coleta || ativa.entregando || ativa.coletados >= ativa.total || ativa.dados.itens == null) continue;
            foreach (Transform item in ativa.dados.itens)
                if (item.gameObject.activeInHierarchy) yield return item;
        }
    }
    RectTransform lista;
    GameObject avisoRecompensa;
    readonly Queue<(string mensagem, float largura)> avisosRecompensa = new();

    void Awake()
    {
        Instance = this;
        CriarLista();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (lista != null) Destroy(lista.gameObject);
        if (avisoRecompensa != null) Destroy(avisoRecompensa);
    }

    public void EncerrarPartida()
    {
        StopAllCoroutines();
        avisosRecompensa.Clear();
        if (avisoRecompensa != null)
        {
            avisoRecompensa.SetActive(false);
            Destroy(avisoRecompensa);
        }
        enabled = false;
        foreach (MissaoAtiva ativa in ativas)
        {
            if (ativa.linha != null) Destroy(ativa.linha);
        }
        ativas.Clear();
        concluidas.Clear();
        vitoriasPendentes.Clear();
        aoMudarProgresso = null;
        aoMissaoConcluida = null;
        if (lista != null)
        {
            lista.gameObject.SetActive(false);
            Destroy(lista.gameObject);
            lista = null;
        }
        if (Instance == this) Instance = null;
    }

    public void Iniciar(string nome)
    {
        if (ModoDeJogo.Infinito) return;
        Missao m = missoes.Find(x => x.nome == nome);
        if (m == null) { Debug.LogWarning($"Missoes: não existe missão chamada '{nome}'."); return; }
        if (ativas.Exists(ativa => ativa.dados == m)) return;
        if ((m.tipo != TipoMissao.Coleta || m.itemRecompensa != null) && concluidas.Contains(nome)) return;

        var nova = new MissaoAtiva
        {
            dados = m,
            total = m.quantidade > 0 ? m.quantidade : (m.itens != null ? m.itens.childCount : 0)
        };
        ativas.Add(nova);
        CriarLinha(nova);
        m.aoIniciar?.Invoke();
        aoMudarProgresso?.Invoke(m.nome, 0, nova.total);
        // Registra vitórias dos encontros mesmo se o jogador visitar os chefes fora de ordem.
        if (m.tipo == TipoMissao.Combate && vitoriasPendentes.Remove(nome)) Concluir(nova);
    }

    public void ConcluirMissao(string nome)
    {
        if (ModoDeJogo.Infinito) return;
        if (concluidas.Contains(nome) || !missoes.Exists(m => m.nome == nome && m.tipo == TipoMissao.Combate)) return;
        MissaoAtiva ativa = ativas.Find(x => x.dados.nome == nome);
        if (ativa == null) { vitoriasPendentes.Add(nome); return; }
        Concluir(ativa);
    }

    void Update()
    {
        if (ativas.Count == 0 || mapaGerador == null || mapaGerador.PlayerInstanciado == null) return;
        Vector3 jogador = mapaGerador.PlayerInstanciado.transform.position;

        // Começos disparados por callbacks entram no próximo frame.
        for (int i = 0, restantes = ativas.Count; restantes > 0; restantes--)
        {
            MissaoAtiva ativa = ativas[i];
            AtualizarMissao(ativa, jogador);
            if (i < ativas.Count && ativas[i] == ativa) i++;
        }
    }

    void LateUpdate()
    {
        if (lista == null) return;
        GameObject jogador = mapaGerador != null ? mapaGerador.PlayerInstanciado : null;
        PlayerMovement movimento = jogador != null ? jogador.GetComponent<PlayerMovement>() : null;
        bool mostrar = ativas.Count > 0 && movimento != null && movimento.isActiveAndEnabled &&
            Time.timeScale > 0f && !Cutscenes.EmCutscene &&
            (DialogoManager.Instance == null || !DialogoManager.Instance.TemDialogoAberto) &&
            (TransicaoBatalha.instancia == null || (!TransicaoBatalha.instancia.RetornandoDaDerrota && !TransicaoBatalha.instancia.EmTransicao));
        if (lista.gameObject.activeSelf != mostrar) lista.gameObject.SetActive(mostrar);
    }

    void AtualizarMissao(MissaoAtiva ativa, Vector3 jogador)
    {
        Missao m = ativa.dados;
        if (m.tipo != TipoMissao.Coleta) return;
        if (ativa.entregando)
        {
            if (m.entrega == null)
            {
                Concluir(ativa);
                return;
            }
            Vector3 distancia = m.entrega.position - jogador;
            distancia.y = 0f;
            if (distancia.magnitude <= m.raioEntrega) Concluir(ativa);
            return;
        }

        if (ativa.coletados >= ativa.total)
        {
            TerminouDeColetar(ativa);
            return;
        }
        if (m.itens == null) return;
        foreach (Transform item in m.itens)
        {
            if (!item.gameObject.activeSelf) continue;
            Vector3 distancia = item.position - jogador;
            if (Mathf.Abs(distancia.y) > 2.5f) continue;
            distancia.y = 0f;
            if (distancia.magnitude > m.raioColeta) continue;

            item.gameObject.SetActive(false);
            if (efeitoColeta != null) Instantiate(efeitoColeta, item.position + Vector3.up * 1.9f, Quaternion.identity);
            ativa.coletados++;
            AudioJogo.Instance?.TocarColeta();
            if (ativa.contador != null) ativa.contador.text = $"{ativa.coletados} / {ativa.total}";
            aoMudarProgresso?.Invoke(m.nome, ativa.coletados, ativa.total);
            if (ativa.coletados >= ativa.total)
            {
                TerminouDeColetar(ativa);
                break;
            }
        }
    }

    void TerminouDeColetar(MissaoAtiva ativa)
    {
        if (ativa.dados.entrega == null)
        {
            Concluir(ativa);
            return;
        }
        ativa.entregando = true;
        if (ativa.objetivo != null) ativa.objetivo.text = ativa.dados.objetivoEntrega;
    }

    void Concluir(MissaoAtiva ativa)
    {
        Missao m = ativa.dados;
        if (!ativas.Remove(ativa)) return;
        concluidas.Add(m.nome);
        if (ativa.linha != null)
        {
            ativa.linha.SetActive(false);
            Destroy(ativa.linha);
        }
        if (lista != null && ativas.Count == 0) lista.gameObject.SetActive(false);
        InventoryManager inventario = InventoryManager.instancia;
        if (m.itemRecompensa != null && inventario != null &&
            inventario.TentarAdicionarItem(m.itemRecompensa, m.quantidadeRecompensa))
            EnfileirarAviso("Missão concluída!\n+2 Poções de Cura!", this);

        aoMissaoConcluida?.Invoke(m.nome);
        m.aoConcluir?.Invoke();
    }

    public static void MostrarAvisoColeta(string mensagem)
    {
        if (Instance != null && Instance.isActiveAndEnabled)
        {
            Instance.EnfileirarAviso(mensagem, Instance, largura: 460f);
            return;
        }

        // No Infinito, as missões ficam inativas, mas seus modelos de UI continuam disponíveis.
        var infinito = ModoInfinito.Instance;
        if (infinito == null || !infinito.isActiveAndEnabled) return;

        var modelo = FindAnyObjectByType<Missoes>(FindObjectsInactive.Include);
        if (modelo != null) modelo.EnfileirarAviso(mensagem, infinito, largura: 460f);
    }

    void EnfileirarAviso(string mensagem, MonoBehaviour executor, float largura = 360f)
    {
        avisosRecompensa.Enqueue((mensagem, largura));
        if (avisoRecompensa == null)
            executor.StartCoroutine(MostrarAvisosRecompensa());
    }

    IEnumerator MostrarAvisosRecompensa()
    {
        Canvas canvas = missionPanel.GetComponentInParent<Canvas>(true).rootCanvas;
        var painel = new GameObject("Recompensa da missão", typeof(RectTransform), typeof(Image));
        avisoRecompensa = painel;
        var fundo = painel.GetComponent<Image>();
        fundo.rectTransform.SetParent(canvas.transform, false);
        fundo.sprite = missionPanel.GetComponent<Image>()?.sprite;
        fundo.type = Image.Type.Sliced;
        fundo.color = fundo.sprite != null ? Color.white : Color.black;
        fundo.raycastTarget = false;

        TMP_Text texto = CriarTexto(objetivoModelo, painel.transform,
            string.Empty, 22f, TextAlignmentOptions.Center);
        texto.rectTransform.anchorMin = Vector2.zero;
        texto.rectTransform.anchorMax = Vector2.one;
        texto.rectTransform.offsetMin = new Vector2(12f, 8f);
        texto.rectTransform.offsetMax = new Vector2(-12f, -8f);
        texto.textWrappingMode = TextWrappingModes.Normal;

        while (avisosRecompensa.Count > 0)
        {
            var aviso = avisosRecompensa.Dequeue();
            fundo.rectTransform.sizeDelta = new Vector2(aviso.largura, 90f);
            texto.text = aviso.mensagem;
            yield return new WaitForSecondsRealtime(3f);
        }
        painel.SetActive(false);
        Destroy(painel);
        avisoRecompensa = null;
    }

    void CriarLista()
    {
        if (missionPanel is not RectTransform modelo || minimapa == null ||
            tituloModelo == null || objetivoModelo == null || contadorModelo == null) return;

        modelo.gameObject.SetActive(false);
        var painel = new GameObject("Missões ativas", typeof(RectTransform),
            typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        lista = painel.GetComponent<RectTransform>();
        // O minimapa tem uma máscara circular. O painel fica no mesmo Canvas, como irmão.
        lista.SetParent(minimapa.parent, false);
        lista.SetSiblingIndex(minimapa.GetSiblingIndex() + 1);
        lista.anchorMin = lista.anchorMax = minimapa.anchorMax;
        lista.pivot = Vector2.up;
        lista.sizeDelta = new Vector2(420f, 0f);
        lista.anchoredPosition = minimapa.anchoredPosition + minimapa.rect.max + new Vector2(16f, 0f);

        var objetoFundo = new GameObject("Sombra", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        var fundo = objetoFundo.GetComponent<Image>();
        fundo.rectTransform.SetParent(lista, false);
        fundo.rectTransform.anchorMin = Vector2.zero;
        fundo.rectTransform.anchorMax = Vector2.one;
        fundo.rectTransform.offsetMin = Vector2.zero;
        fundo.rectTransform.offsetMax = Vector2.zero;
        objetoFundo.GetComponent<LayoutElement>().ignoreLayout = true;
        fundo.sprite = modelo.GetComponent<Image>()?.sprite;
        fundo.type = Image.Type.Sliced;
        fundo.pixelsPerUnitMultiplier = 24f;
        fundo.raycastTarget = false;

        var layout = painel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(22, 22, 5, 12);
        layout.spacing = 5f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        painel.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CriarTexto(tituloModelo, lista, "Missões", 14f, TextAlignmentOptions.Left);
        painel.SetActive(false);
    }

    void CriarLinha(MissaoAtiva ativa)
    {
        if (lista == null) return;
        var linha = new GameObject("Missão", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        linha.transform.SetParent(lista, false);
        ativa.linha = linha;
        var colunas = linha.GetComponent<HorizontalLayoutGroup>();
        colunas.spacing = 8f;
        colunas.childAlignment = TextAnchor.MiddleLeft;
        colunas.childControlWidth = colunas.childControlHeight = true;
        colunas.childForceExpandWidth = colunas.childForceExpandHeight = false;

        var objetoIcone = new GameObject("Ícone", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        objetoIcone.transform.SetParent(linha.transform, false);
        var icone = objetoIcone.GetComponent<Image>();
        icone.sprite = ativa.dados.icone != null ? ativa.dados.icone : iconePadrao;
        icone.enabled = icone.sprite != null;
        icone.preserveAspect = true;
        icone.raycastTarget = false;
        var espacoIcone = objetoIcone.GetComponent<LayoutElement>();
        espacoIcone.minWidth = espacoIcone.preferredWidth = 24f;
        espacoIcone.minHeight = espacoIcone.preferredHeight = 24f;
        objetoIcone.SetActive(!ativa.dados.ocultarIcone && ativa.dados.tipo != TipoMissao.Aviso);

        var descricao = new GameObject("Descrição", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        descricao.transform.SetParent(linha.transform, false);
        var textos = descricao.GetComponent<VerticalLayoutGroup>();
        textos.spacing = 2f;
        textos.childControlWidth = textos.childControlHeight = true;
        textos.childForceExpandWidth = true;
        textos.childForceExpandHeight = false;
        var espacoDescricao = descricao.GetComponent<LayoutElement>();
        espacoDescricao.minWidth = espacoDescricao.preferredWidth = 0f;
        espacoDescricao.flexibleWidth = 1f;

        if (ativa.dados.tipo == TipoMissao.Aviso)
        {
            var aviso = CriarTexto(objetivoModelo, descricao.transform, ativa.dados.objetivo, 12f, TextAlignmentOptions.Left);
            aviso.textWrappingMode = TextWrappingModes.Normal;
            aviso.overflowMode = TextOverflowModes.Overflow;
            return;
        }
        if (!string.IsNullOrWhiteSpace(ativa.dados.titulo))
            CriarTexto(tituloModelo, descricao.transform, ativa.dados.titulo, 10f, TextAlignmentOptions.Left);
        ativa.objetivo = CriarTexto(objetivoModelo, descricao.transform, ativa.dados.objetivo, 12f, TextAlignmentOptions.Left);
        if (ativa.dados.tipo != TipoMissao.Coleta) return;
        ativa.contador = CriarTexto(contadorModelo, linha.transform, $"0 / {ativa.total}", 14f, TextAlignmentOptions.MidlineRight);
        var espacoContador = ativa.contador.gameObject.AddComponent<LayoutElement>();
        espacoContador.minWidth = espacoContador.preferredWidth = 42f;
    }

    static TMP_Text CriarTexto(TMP_Text modelo, Transform pai, string conteudo, float tamanho, TextAlignmentOptions alinhamento)
    {
        TMP_Text texto = Instantiate(modelo, pai, false);
        texto.rectTransform.localScale = Vector3.one;
        texto.text = conteudo;
        texto.raycastTarget = false;
        texto.alignment = alinhamento;
        texto.margin = Vector4.zero;
        texto.textWrappingMode = TextWrappingModes.NoWrap;
        texto.overflowMode = TextOverflowModes.Ellipsis;
        texto.enableAutoSizing = false;
        texto.fontSize = tamanho;
        texto.gameObject.SetActive(true);
        return texto;
    }
}
