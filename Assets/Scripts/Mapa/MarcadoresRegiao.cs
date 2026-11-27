using System;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(500)]
public class MarcadoresRegiao : MonoBehaviour
{
    [Serializable]
    public class Regiao
    {
        public string nome;
        public Transform centro;
        [Min(0)] public float altura = 12f;
        public Color cor = Color.white;
    }

    [Header("Visibilidade por modo")]
    [SerializeField] bool mostrarNaHistoria = true;
    [SerializeField] bool mostrarNoInfinito = true;
    [Header("Regiões")]
    [SerializeField] Regiao[] regioes = Array.Empty<Regiao>();
    [SerializeField] MapaGerador mapa;
    [SerializeField] TMP_FontAsset fonte;
    [Header("Aparência")]
    [SerializeField, Min(1)] float tamanhoBolinha = 10f;
    [SerializeField, Min(1)] float tamanhoFonte = 13f;
    [SerializeField, Min(0)] float margemTela = 6f;

    sealed class Marcador
    {
        public Regiao dados;
        public RectTransform raiz;
        public Image bolinha;
        public TMP_Text nome;
    }

    readonly List<Marcador> marcadores = new List<Marcador>();
    readonly Vector3[] cantos = new Vector3[4];
    Canvas canvas;
    RectTransform area;
    Texture2D textura;
    Sprite circulo;
    GameObject jogador;
    PlayerMovement movimento;

    void Awake()
    {
        var go = new GameObject("Nomes das regiões", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        go.transform.SetParent(transform, false);
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -1; // Atrás do Canvas do minimapa e das missões (ordem 0).
        area = go.GetComponent<RectTransform>();
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800, 450);
        scaler.matchWidthOrHeight = 0.5f;

        textura = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        textura.name = "Círculo dos marcadores de região";
        var pixels = new Color[32 * 32];
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
                pixels[y * 32 + x] = new Color(1, 1, 1, Mathf.Clamp01(15.5f - Vector2.Distance(new Vector2(x, y), Vector2.one * 15.5f)));
        textura.SetPixels(pixels);
        textura.Apply();
        circulo = Sprite.Create(textura, new Rect(0, 0, 32, 32), Vector2.one * 0.5f);

        foreach (Regiao regiao in regioes)
        {
            if (regiao == null || regiao.centro == null) continue;
            var raiz = new GameObject(regiao.nome, typeof(RectTransform)).GetComponent<RectTransform>();
            raiz.SetParent(area, false);
            raiz.anchorMin = raiz.anchorMax = Vector2.one * 0.5f;
            var imagem = new GameObject("Bolinha", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            imagem.transform.SetParent(raiz, false);
            imagem.sprite = circulo;
            imagem.raycastTarget = false;
            var contorno = imagem.gameObject.AddComponent<Outline>();
            contorno.effectColor = Color.black;
            contorno.effectDistance = Vector2.one;
            var texto = new GameObject("Nome", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
            texto.transform.SetParent(raiz, false);
            texto.font = fonte;
            texto.alignment = TextAlignmentOptions.Center;
            texto.raycastTarget = false;
            texto.textWrappingMode = TextWrappingModes.NoWrap;
            texto.color = Color.white;
            texto.outlineWidth = 0.25f;
            texto.outlineColor = Color.black;
            marcadores.Add(new Marcador { dados = regiao, raiz = raiz, bolinha = imagem, nome = texto });
        }
        canvas.enabled = false;
    }

    void OnEnable() => CinemachineCore.CameraUpdatedEvent.AddListener(CameraAtualizada);
    void OnDisable()
    {
        CinemachineCore.CameraUpdatedEvent.RemoveListener(CameraAtualizada);
        if (canvas != null) canvas.enabled = false;
    }

    void CameraAtualizada(CinemachineBrain brain)
    {
        if (brain.OutputCamera == Camera.main) LateUpdate();
    }

    bool PodeMostrar()
    {
        if (!(ModoDeJogo.Infinito ? mostrarNoInfinito : mostrarNaHistoria) ||
            CarregamentoPartida.EmAndamento || Cutscenes.EmCutscene || Time.timeScale <= 0f ||
            (DialogoManager.Instance != null && DialogoManager.Instance.TemDialogoAberto) ||
            (TransicaoBatalha.instancia != null && (TransicaoBatalha.instancia.EmTransicao || TransicaoBatalha.instancia.RetornandoDaDerrota))) return false;
        if (ModoDeJogo.Infinito && (ModoInfinito.Instance == null || !ModoInfinito.Instance.PodeExplorar)) return false;
        GameObject atual = mapa != null ? mapa.PlayerInstanciado : null;
        if (atual != jogador)
        {
            jogador = atual;
            movimento = jogador != null ? jogador.GetComponent<PlayerMovement>() : null;
        }
        return movimento != null && movimento.isActiveAndEnabled;
    }

    void LateUpdate()
    {
        if (canvas == null) return;
        Camera camera = Camera.main;
        canvas.enabled = camera != null && PodeMostrar();
        if (!canvas.enabled) return;
        Rect visivel = camera.pixelRect;
        Rect safe = Screen.safeArea;
        visivel = Rect.MinMaxRect(Mathf.Max(visivel.xMin, safe.xMin), Mathf.Max(visivel.yMin, safe.yMin),
            Mathf.Min(visivel.xMax, safe.xMax), Mathf.Min(visivel.yMax, safe.yMax));
        foreach (Marcador marcador in marcadores)
        {
            Regiao regiao = marcador.dados;
            if (regiao.centro == null) { marcador.raiz.gameObject.SetActive(false); continue; }
            Vector3 tela = camera.WorldToScreenPoint(regiao.centro.position + Vector3.up * regiao.altura);
            bool mostrar = tela.z > 0 && visivel.Contains(new Vector2(tela.x, tela.y));
            marcador.raiz.gameObject.SetActive(mostrar);
            if (!mostrar) continue;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, tela, null, out Vector2 local);
            marcador.raiz.anchoredPosition = local;
            marcador.bolinha.color = regiao.cor;
            marcador.bolinha.rectTransform.sizeDelta = Vector2.one * tamanhoBolinha;
            marcador.nome.text = regiao.nome;
            marcador.nome.fontSize = tamanhoFonte;
            Vector2 texto = marcador.nome.GetPreferredValues(regiao.nome);
            marcador.nome.rectTransform.sizeDelta = new Vector2(texto.x + 4, tamanhoFonte * 1.6f);
            marcador.nome.rectTransform.anchoredPosition = Vector2.up * (tamanhoBolinha * 0.5f + tamanhoFonte * 0.8f + 3f);
            // Oculta o conjunto antes de cortar o nome. Não prende os marcadores à borda.
            mostrar = InteiroNaTela(marcador.nome.rectTransform, visivel) && InteiroNaTela(marcador.bolinha.rectTransform, visivel);
            marcador.raiz.gameObject.SetActive(mostrar);
        }
    }

    bool InteiroNaTela(RectTransform elemento, Rect limites)
    {
        elemento.GetWorldCorners(cantos);
        float margem = margemTela * canvas.scaleFactor;
        foreach (Vector3 canto in cantos)
            if (canto.x < limites.xMin + margem || canto.x > limites.xMax - margem ||
                canto.y < limites.yMin + margem || canto.y > limites.yMax - margem) return false;
        return true;
    }

    void OnDestroy()
    {
        if (canvas != null) Destroy(canvas.gameObject);
        if (circulo != null) Destroy(circulo);
        if (textura != null) Destroy(textura);
    }
}
