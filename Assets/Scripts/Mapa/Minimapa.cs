using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;

// A projeção usa a mesma câmera e o mesmo retângulo que mostram o cenário.
[DefaultExecutionOrder(100)]
public class Minimapa : MonoBehaviour
{
    [SerializeField] MapaGerador mapaGerador;
    [SerializeField] Cutscenes cutscenes;
    [SerializeField] Missoes missoes;
    [SerializeField] RectTransform areaCircular;
    [SerializeField] RawImage imagemMapa;
    [SerializeField] Sprite iconeLixo;
    [SerializeField] TMP_FontAsset fonte;
    [SerializeField, Min(1f)] float tamanhoBolinha = 8f;
    [SerializeField, Min(1f)] float tamanhoLixo = 12f;

    readonly List<Image> marcadores = new List<Image>();
    readonly HashSet<Transform> itensExibidos = new HashSet<Transform>();
    readonly TMP_Text[] pontosCardeais = new TMP_Text[4];
    readonly Vector3[] direcoes = { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };
    Ferralha[] ferralhas = System.Array.Empty<Ferralha>();
    GameObject jogador;
    Camera cameraMapa;
    PlayerVisual visualJogador;
    Image setaJogador;
    Sprite seta;
    Texture2D texturaSeta;
    RectTransform camada;
    RectTransform bussola;
    Sprite bolinha;
    Texture2D texturaBolinha;
    float proximaBusca;
    int usados;

    void Awake()
    {
        var objeto = new GameObject("Marcadores do minimapa", typeof(RectTransform));
        camada = objeto.GetComponent<RectTransform>();
        camada.SetParent(areaCircular, false);
        camada.anchorMin = Vector2.zero;
        camada.anchorMax = Vector2.one;
        camada.offsetMin = camada.offsetMax = Vector2.zero;

        var objetoBussola = new GameObject("Rosa dos ventos", typeof(RectTransform));
        bussola = objetoBussola.GetComponent<RectTransform>();
        bussola.SetParent(areaCircular.parent, false);
        bussola.SetSiblingIndex(areaCircular.GetSiblingIndex() + 1);

        texturaBolinha = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        var pixels = new Color[32 * 32];
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float distancia = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
                pixels[y * 32 + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(15.5f - distancia));
            }
        texturaBolinha.SetPixels(pixels);
        texturaBolinha.Apply();
        bolinha = Sprite.Create(texturaBolinha, new Rect(0, 0, 32, 32), Vector2.one * 0.5f);

        texturaSeta = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        var pixelsSeta = new Color[32 * 32];
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float distanciaCentro = Mathf.Abs(x - 15.5f);
                bool cabeca = y >= 14 && distanciaCentro <= (31f - y) * 0.85f;
                bool haste = y >= 2 && y < 14 && distanciaCentro <= 4f;
                pixelsSeta[y * 32 + x] = cabeca || haste ? Color.white : Color.clear;
            }
        texturaSeta.SetPixels(pixelsSeta);
        texturaSeta.Apply();
        seta = Sprite.Create(texturaSeta, new Rect(0, 0, 32, 32), Vector2.one * 0.5f);
        var objetoSeta = new GameObject("Direção do jogador", typeof(RectTransform), typeof(Image));
        objetoSeta.transform.SetParent(camada, false);
        setaJogador = objetoSeta.GetComponent<Image>();
        setaJogador.sprite = seta;
        setaJogador.color = Color.white;
        setaJogador.raycastTarget = false;
        setaJogador.rectTransform.sizeDelta = Vector2.one * 16f;
        setaJogador.rectTransform.anchoredPosition = Vector2.zero;
        var contorno = objetoSeta.AddComponent<Outline>();
        contorno.effectColor = Color.black;
        contorno.effectDistance = Vector2.one;

        string[] letras = { "N", "L", "S", "O" };
        for (int i = 0; i < letras.Length; i++)
        {
            var letra = new GameObject(letras[i], typeof(RectTransform), typeof(TextMeshProUGUI));
            letra.transform.SetParent(bussola, false);
            TMP_Text texto = letra.GetComponent<TMP_Text>();
            texto.font = fonte;
            texto.text = letras[i];
            texto.fontSize = 9f;
            texto.alignment = TextAlignmentOptions.Center;
            texto.raycastTarget = false;
            texto.rectTransform.sizeDelta = new Vector2(14f, 14f);
            pontosCardeais[i] = texto;
            var sombra = letra.AddComponent<Shadow>();
            sombra.effectDistance = new Vector2(1f, -1f);
        }
    }

    void OnEnable()
    {
        CinemachineCore.CameraUpdatedEvent.AddListener(AposAtualizarCamera);
        if (bussola != null) bussola.gameObject.SetActive(true);
    }

    void OnDisable()
    {
        CinemachineCore.CameraUpdatedEvent.RemoveListener(AposAtualizarCamera);
        if (bussola != null) bussola.gameObject.SetActive(false);
    }

    void AposAtualizarCamera(CinemachineBrain brain)
    {
        if (brain.OutputCamera == Camera.main) LateUpdate();
    }

    void LateUpdate()
    {
        GameObject atual = mapaGerador != null ? mapaGerador.PlayerInstanciado : null;
        if (atual != jogador)
        {
            jogador = atual;
            cameraMapa = null;
            visualJogador = jogador != null ? jogador.GetComponentInChildren<PlayerVisual>() : null;
            if (jogador != null)
                foreach (Camera camera in jogador.GetComponentsInChildren<Camera>())
                    if (camera.targetTexture == imagemMapa.texture) { cameraMapa = camera; break; }
        }
        if (cameraMapa == null || jogador == null)
        {
            camada.gameObject.SetActive(false);
            bussola.gameObject.SetActive(false);
            return;
        }
        camada.gameObject.SetActive(true);
        bussola.gameObject.SetActive(true);
        bussola.anchorMin = areaCircular.anchorMin;
        bussola.anchorMax = areaCircular.anchorMax;
        bussola.pivot = areaCircular.pivot;
        bussola.sizeDelta = areaCircular.sizeDelta;
        bussola.anchoredPosition3D = areaCircular.anchoredPosition3D;
        bussola.localRotation = areaCircular.localRotation;
        bussola.localScale = areaCircular.localScale;

        Camera principal = Camera.main;
        Vector3 frente = principal != null ? principal.transform.forward : cameraMapa.transform.up;
        float angulo = cameraMapa.transform.eulerAngles.y;
        if (frente.x * frente.x + frente.z * frente.z > 0.0001f)
            angulo = Mathf.Atan2(frente.x, frente.z) * Mathf.Rad2Deg;
        Vector3 posicao = cameraMapa.transform.position;
        posicao.x = jogador.transform.position.x;
        posicao.z = jogador.transform.position.z;
        cameraMapa.transform.SetPositionAndRotation(posicao, Quaternion.Euler(90f, angulo, 0f));

        Vector3 direcaoJogador = visualJogador != null ? visualJogador.DirecaoVisual : jogador.transform.forward;
        Vector3 direcaoNaTela = cameraMapa.transform.InverseTransformDirection(direcaoJogador);
        float rotacaoSeta = Mathf.Atan2(-direcaoNaTela.x, direcaoNaTela.y) * Mathf.Rad2Deg;
        setaJogador.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotacaoSeta);

        float raio = Mathf.Min(camada.rect.width, camada.rect.height) * 0.5f;
        for (int i = 0; i < pontosCardeais.Length; i++)
        {
            Vector3 direcao = cameraMapa.transform.InverseTransformDirection(direcoes[i]);
            Vector2 eixo = new Vector2(direcao.x, direcao.y).normalized;
            RectTransform letra = pontosCardeais[i].rectTransform;
            // Projeta metade do retângulo da letra para mantê-lo todo fora do círculo.
            float margemLetra = Mathf.Abs(eixo.x) * letra.rect.width * 0.5f +
                Mathf.Abs(eixo.y) * letra.rect.height * 0.5f;
            letra.anchoredPosition = eixo * (raio + margemLetra + 3f);
        }

        if (Time.unscaledTime >= proximaBusca)
        {
            ferralhas = FindObjectsByType<Ferralha>(FindObjectsSortMode.None);
            proximaBusca = Time.unscaledTime + 0.5f;
        }
        usados = 0;
        if (ModoInfinito.Instance != null)
        {
            foreach (Transform inimigo in ModoInfinito.Instance.InimigosVisiveis())
                Mostrar(inimigo.position, bolinha, Color.red, tamanhoBolinha, raio);
            foreach (Transform lixo in ModoInfinito.Instance.LixosVisiveis())
                Mostrar(lixo.position, iconeLixo, Color.white, tamanhoLixo, raio);
            if (ModoInfinito.Instance.PossuiRodada && ModoInfinito.Instance.EmBusca)
                Mostrar(ModoInfinito.Instance.CentroObjetivo, bolinha, new Color(1f, 1f, 0f, 0.35f), 26f, raio);
        }
        else
            foreach (Ferralha ferralha in ferralhas)
                if (ferralha != null && ferralha.isActiveAndEnabled && !ferralha.EstaDerrotada)
                    Mostrar(ferralha.transform.position, bolinha, Color.red, tamanhoBolinha, raio);
        if (!ModoDeJogo.Infinito && cutscenes != null)
            foreach (Collider gatilho in cutscenes.GatilhosDisponiveis())
                Mostrar(gatilho.bounds.center, bolinha, Color.yellow, tamanhoBolinha, raio);
        itensExibidos.Clear();
        if (!ModoDeJogo.Infinito && missoes != null)
            foreach (Transform item in missoes.LixosColetaveis())
                if (itensExibidos.Add(item))
                    Mostrar(item.position, iconeLixo, Color.white, tamanhoLixo, raio);
        for (int i = usados; i < marcadores.Count; i++) marcadores[i].gameObject.SetActive(false);
        setaJogador.transform.SetAsLastSibling();
    }

    void Mostrar(Vector3 mundo, Sprite sprite, Color cor, float tamanho, float raio)
    {
        if (sprite == null) return;
        if (usados == marcadores.Count)
        {
            var objeto = new GameObject("Objetivo", typeof(RectTransform), typeof(Image));
            objeto.transform.SetParent(camada, false);
            var imagem = objeto.GetComponent<Image>();
            imagem.raycastTarget = false;
            imagem.preserveAspect = true;
            marcadores.Add(imagem);
        }
        Image marcador = marcadores[usados++];
        Vector3 viewport = cameraMapa.WorldToViewportPoint(mundo);
        Rect uv = imagemMapa.uvRect;
        Vector2 normalizada = new Vector2((viewport.x - uv.x) / uv.width, (viewport.y - uv.y) / uv.height);
        Vector3 pontoLocal = new Vector3(
            imagemMapa.rectTransform.rect.xMin + normalizada.x * imagemMapa.rectTransform.rect.width,
            imagemMapa.rectTransform.rect.yMin + normalizada.y * imagemMapa.rectTransform.rect.height, 0f);
        Vector2 posicao = camada.InverseTransformPoint(imagemMapa.rectTransform.TransformPoint(pontoLocal));
        posicao = Vector2.ClampMagnitude(posicao, Mathf.Max(0f, raio - tamanho * 0.5f - 1f));
        marcador.rectTransform.anchoredPosition = posicao;
        marcador.rectTransform.sizeDelta = Vector2.one * tamanho;
        marcador.sprite = sprite;
        marcador.color = cor;
        marcador.gameObject.SetActive(true);
    }

    void OnDestroy()
    {
        if (camada != null) Destroy(camada.gameObject);
        if (bussola != null) Destroy(bussola.gameObject);
        if (bolinha != null) Destroy(bolinha);
        if (texturaBolinha != null) Destroy(texturaBolinha);
        if (seta != null) Destroy(seta);
        if (texturaSeta != null) Destroy(texturaSeta);
    }
}
