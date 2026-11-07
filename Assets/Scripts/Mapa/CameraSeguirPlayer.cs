
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

// Orbita ao redor do player.
// PC: mouse (scroll = zoom, ALT solta o cursor).
// Mobile: arrastar somente o lado direito da tela.
public class CameraSeguirPlayer : MonoBehaviour
{
    [SerializeField] MapaGerador mapaGerador;
    [SerializeField] float distancia = 7f;

    [Header("Zoom (scroll do mouse)")]
    [SerializeField] float distanciaMinima = 1f;
    [SerializeField] float distanciaMaxima = 12f;
    [SerializeField] float passoZoom = 1.2f;       // metros por "clique" do scroll
    [SerializeField] float suavizacaoZoom = 10f;   // maior = zoom mais rápido

    [SerializeField] float sensibilidade = 0.2f;
    [SerializeField] float anguloMinimo = -25f;
    [SerializeField] float anguloMaximo = 80f;
    [SerializeField] float anguloInicial = 35f;
    [SerializeField] float raioColisao = 0.3f;

    [Tooltip("Ponto que a câmera mira acima do centro do player.")]
    [SerializeField] Vector3 pivo = Vector3.zero;

    [Header("Efeito de velocidade (dash/corrida)")]
    [SerializeField] float fovExtra = 6f;            // graus a mais de FOV
    [SerializeField] float distanciaExtra = 0.5f;    // metros a mais de distância
    [SerializeField] float suavizacaoEfeito = 6f;    // maior = entra/sai mais rápido

    [Header("Colisão da câmera")]
    [Tooltip("Velocidade com que a câmera volta ao normal depois de liberar da parede. Ao encostar ela aproxima na hora.")]
    [SerializeField] float suavizacaoSaidaColisao = 5f;

    [Header("Esconder o player com a câmera muito perto")]
    [SerializeField] float distanciaOcultar = 1.6f;      // some sempre que a câmera chegar tão perto
    [SerializeField] float distanciaMostrar = 1.9f;
    [Tooltip("Se a colisão encostar a câmera no chão/parede (puxa pelo menos isso), o corpo some até esta distância.")]
    [SerializeField] float distanciaOcultarColisao = 3.2f;
    [Tooltip("Câmera mais baixa que o ponto que ela mira por pelo menos isto = olhando de baixo para cima.")]
    [SerializeField] float distanciaDeBaixo = 0.6f;
    [Tooltip("Olhando de baixo, o corpo some até esta distância.")]
    [SerializeField] float distanciaOcultarDeBaixo = 5.5f;
    [Tooltip("Segundos para o corpo sumir/aparecer aos poucos.")]
    [SerializeField] float duracaoFade = 0.3f;

    // Opções do jogador (salvas em PlayerPrefs; o menu de opções chama os setters)
    const string ChaveSensibilidade = "CameraSensibilidade";
    const string ChaveInverterY = "CameraInverterY";
    static float sensibilidadeOpcao = 1f;
    static bool inverterYOpcao = false;
    static bool opcoesCarregadas = false;

    public static float SensibilidadeOpcao
    {
        get { CarregarOpcoes(); return sensibilidadeOpcao; }
        set { CarregarOpcoes(); sensibilidadeOpcao = Mathf.Clamp(value, 0.25f, 3f); PlayerPrefs.SetFloat(ChaveSensibilidade, sensibilidadeOpcao); }
    }

    public static bool InverterYOpcao
    {
        get { CarregarOpcoes(); return inverterYOpcao; }
        set { CarregarOpcoes(); inverterYOpcao = value; PlayerPrefs.SetInt(ChaveInverterY, value ? 1 : 0); }
    }

    static void CarregarOpcoes()
    {
        if (opcoesCarregadas) return;
        opcoesCarregadas = true;
        sensibilidadeOpcao = Mathf.Clamp(PlayerPrefs.GetFloat(ChaveSensibilidade, 1f), 0.25f, 3f);
        inverterYOpcao = PlayerPrefs.GetInt(ChaveInverterY, 0) == 1;
    }

    Transform player;
    PlayerMovement playerMovimento;
    Renderer[] renderersPlayer;
    bool playerOculto;
    float ultimaTrocaVisibilidade;

    // Fade do corpo: opaco (original) -> transparente (cópia do material com alpha) -> só sombra
    float alphaPlayer = 1f;
    Material[][] materiaisOriginais;
    Material[][] materiaisFade;
    int estadoVisual = 0; // 0 = opaco, 1 = em fade, 2 = oculto (só sombra)

    CinemachineCamera cameraCinemachine;
    float fovBase = 60f;

    float rotX;
    float rotY;

    float distanciaAlvo;
    float distanciaAtual;
    float distanciaColisao;
    bool colisaoIniciada;
    float efeitoVelocidade;
    bool cursorLivre;

    [Header("Controle Mobile da Câmera")]
    private int dedoCamera = -1;
    private Vector2 deltaCameraMobile;

    void Awake()
    {
        rotY = anguloInicial;

        distanciaAlvo = Mathf.Clamp(distancia, distanciaMinima, distanciaMaxima);
        distanciaAtual = distanciaAlvo;

        cameraCinemachine = GetComponent<CinemachineCamera>();
        if (cameraCinemachine != null)
            fovBase = cameraCinemachine.Lens.FieldOfView;
        else if (Camera.main != null)
            fovBase = Camera.main.fieldOfView;

        TravarCursor();
    }

    void OnEnable()
    {
        if (mapaGerador != null)
            mapaGerador.aoTerminarDeGerar.AddListener(PegarPlayer);

        TravarCursor();
    }

    void OnDisable()
    {
        if (mapaGerador != null)
            mapaGerador.aoTerminarDeGerar.RemoveListener(PegarPlayer);

        // Pausado (Time.timeScale = 0): o cursor fica livre para usar o menu
        if (Time.timeScale == 0f) LiberarCursor();
        RestaurarPlayerAgora();
    }

    void TravarCursor()
    {
        if (Touchscreen.current != null) return;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        cursorLivre = false;
    }

    void LiberarCursor()
    {
        if (Touchscreen.current != null) return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        cursorLivre = true;
    }

    void PegarPlayer()
    {
        if (mapaGerador.PlayerInstanciado != null)
        {
            player = mapaGerador.PlayerInstanciado.transform;
            playerMovimento = player.GetComponent<PlayerMovement>();
            renderersPlayer = player.GetComponentsInChildren<Renderer>();
            playerOculto = false;
            PrepararMateriaisDeFade();
        }
    }

    // Cria, para cada material do player, uma cópia transparente (URP Lit) usada só durante o fade
    void PrepararMateriaisDeFade()
    {
        LimparMateriaisDeFade();

        materiaisOriginais = new Material[renderersPlayer.Length][];
        materiaisFade = new Material[renderersPlayer.Length][];

        for (int i = 0; i < renderersPlayer.Length; i++)
        {
            var originais = renderersPlayer[i].sharedMaterials;
            materiaisOriginais[i] = originais;
            materiaisFade[i] = new Material[originais.Length];

            for (int j = 0; j < originais.Length; j++)
            {
                if (originais[j] == null) continue;

                var copia = new Material(originais[j]);
                copia.name = originais[j].name + " (Fade)";
                copia.SetFloat("_Surface", 1f);   // Transparent
                copia.SetFloat("_Blend", 0f);     // Alpha
                copia.SetOverrideTag("RenderType", "Transparent");
                copia.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                copia.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                copia.SetInt("_ZWrite", 0);
                copia.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                copia.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                copia.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                materiaisFade[i][j] = copia;
            }
        }

        alphaPlayer = 1f;
        estadoVisual = 0;
    }

    void LimparMateriaisDeFade()
    {
        if (materiaisFade == null) return;

        foreach (var lista in materiaisFade)
            foreach (var m in lista)
                if (m != null) Destroy(m);

        materiaisFade = null;
    }

    void OnDestroy()
    {
        LimparMateriaisDeFade();
    }

    // Aproxima o alpha do alvo (0 = oculto, 1 = visível) e aplica no corpo
    void AtualizarFade()
    {
        if (renderersPlayer == null || materiaisFade == null) return;

        float alvoAlpha = playerOculto ? 0f : 1f;
        alphaPlayer = Mathf.MoveTowards(alphaPlayer, alvoAlpha, Time.unscaledDeltaTime / Mathf.Max(duracaoFade, 0.01f));

        AplicarAlpha();
    }

    void AplicarAlpha()
    {
        int novo = alphaPlayer >= 0.999f ? 0 : (alphaPlayer <= 0.001f ? 2 : 1);

        for (int i = 0; i < renderersPlayer.Length; i++)
        {
            var r = renderersPlayer[i];
            if (r == null) continue;

            if (novo != estadoVisual)
            {
                // Troca os materiais só quando muda de estado
                r.sharedMaterials = novo == 1 ? materiaisFade[i] : materiaisOriginais[i];
                r.shadowCastingMode = novo == 2 ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
            }

            if (novo == 1)
            {
                foreach (var m in materiaisFade[i])
                {
                    if (m == null) continue;
                    var cor = m.GetColor("_BaseColor");
                    cor.a = alphaPlayer;
                    m.SetColor("_BaseColor", cor);
                }
            }
        }

        estadoVisual = novo;
    }

    void LateUpdate()
    {
        if (player == null)
            return;

        Vector2 delta = Vector2.zero;

        // Segurar ALT solta o cursor (menus/UI) e congela o giro da câmera
        bool altSegurado = Keyboard.current != null &&
            (Keyboard.current.leftAltKey.isPressed || Keyboard.current.rightAltKey.isPressed);

        if (Touchscreen.current == null)
        {
            if (altSegurado && !cursorLivre) LiberarCursor();
            else if (!altSegurado && cursorLivre) TravarCursor();
        }

        // Mobile
        if (Touchscreen.current != null)
        {
            delta = LerArrastoMobile();
        }
        // PC
        else if (Mouse.current != null && !altSegurado)
        {
            delta = Mouse.current.delta.ReadValue();
        }

        delta *= sensibilidade * SensibilidadeOpcao;

        rotX += delta.x;
        rotY += InverterYOpcao ? delta.y : -delta.y;

        rotY = Mathf.Clamp(rotY, anguloMinimo, anguloMaximo);

        Quaternion rotacao = Quaternion.Euler(rotY, rotX, 0);

        // Zoom no scroll do mouse (aproxima/afasta com suavização)
        if (Mouse.current != null && !altSegurado)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;

            if (Mathf.Abs(scroll) > 0.01f)
            {
                distanciaAlvo = Mathf.Clamp(
                    distanciaAlvo - Mathf.Sign(scroll) * passoZoom,
                    distanciaMinima,
                    distanciaMaxima);
            }
        }

        distanciaAtual = Mathf.Lerp(
            distanciaAtual,
            distanciaAlvo,
            1f - Mathf.Exp(-suavizacaoZoom * Time.deltaTime));

        // Efeito do dash/corrida: um pouco mais de FOV e de distância
        bool rapido = playerMovimento != null && playerMovimento.EmMovimentoRapido;
        efeitoVelocidade = Mathf.Lerp(
            efeitoVelocidade,
            rapido ? 1f : 0f,
            1f - Mathf.Exp(-suavizacaoEfeito * Time.deltaTime));

        AplicarFov(fovBase + efeitoVelocidade * fovExtra);

        float distanciaDesejada = distanciaAtual + efeitoVelocidade * distanciaExtra;

        Vector3 alvo = player.position + pivo;
        Vector3 dir = -(rotacao * Vector3.forward);

        // Colisão: aproxima na hora ao encostar, volta devagar ao liberar
        float livre = DistanciaLivre(alvo, dir, distanciaDesejada);

        if (!colisaoIniciada)
        {
            distanciaColisao = livre;
            colisaoIniciada = true;
        }
        else if (livre < distanciaColisao)
        {
            distanciaColisao = livre;
        }
        else
        {
            distanciaColisao = Mathf.Lerp(
                distanciaColisao,
                livre,
                1f - Mathf.Exp(-suavizacaoSaidaColisao * Time.deltaTime));
        }

        transform.position = alvo + dir * distanciaColisao;
        transform.rotation = rotacao;

        AtualizarVisibilidadeDoPlayer(livre < distanciaDesejada - 0.5f);
        AtualizarFade();
    }

    void AplicarFov(float fov)
    {
        if (cameraCinemachine != null)
        {
            var lente = cameraCinemachine.Lens;
            lente.FieldOfView = fov;
            cameraCinemachine.Lens = lente;
        }
        else if (Camera.main != null)
        {
            Camera.main.fieldOfView = fov;
        }
    }

    // Com a câmera colada no player (ex.: rente ao chão olhando de baixo), esconde o corpo
    // para não aparecer o interior do modelo; a sombra continua.
    void AtualizarVisibilidadeDoPlayer(bool puxada)
    {
        if (renderersPlayer == null) return;

        float d = Vector3.Distance(transform.position, player.position);

        // "puxada" = a colisão está de fato encurtando a câmera agora (e não só o zoom/suavização demorando a chegar)

        // Câmera bem abaixo do ponto que ela mira (olhando o player de baixo para cima)
        bool deBaixo = transform.position.y < player.position.y + pivo.y - distanciaDeBaixo;

        bool deveOcultar = d < distanciaOcultar
            || (puxada && d < distanciaOcultarColisao)
            || (deBaixo && d < distanciaOcultarDeBaixo);

        bool podeMostrar = d > distanciaMostrar
            && !(puxada && d < distanciaOcultarColisao + 0.5f)
            && !(deBaixo && d < distanciaOcultarDeBaixo + 0.5f);

        // Espera um pouco entre trocas para o corpo não ficar piscando
        if (Time.unscaledTime - ultimaTrocaVisibilidade < 0.35f) return;

        if (!playerOculto && deveOcultar)
        {
            playerOculto = true;
            ultimaTrocaVisibilidade = Time.unscaledTime;
        }
        else if (playerOculto && podeMostrar)
        {
            playerOculto = false;
            ultimaTrocaVisibilidade = Time.unscaledTime;
        }
    }

    // Volta o corpo ao normal na hora (ex.: ao pausar)
    void RestaurarPlayerAgora()
    {
        playerOculto = false;
        alphaPlayer = 1f;

        if (renderersPlayer != null && materiaisFade != null)
            AplicarAlpha();
    }

    Vector2 LerArrastoMobile()
    {
        deltaCameraMobile = Vector2.zero;

        if (Touchscreen.current == null)
            return Vector2.zero;

        foreach (var toque in Touchscreen.current.touches)
        {
            int id = toque.touchId.ReadValue();

            Vector2 posicao = toque.position.ReadValue();

            var fase = toque.phase.ReadValue();

            // O dedo precisa começar no lado direito.
            if (fase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                if (posicao.x > Screen.width / 2f && dedoCamera == -1)
                {
                    dedoCamera = id;
                }
            }

            // Somente o dedo registrado controla a câmera.
            if (id == dedoCamera)
            {
                if (fase == UnityEngine.InputSystem.TouchPhase.Moved)
                {
                    deltaCameraMobile = toque.delta.ReadValue();
                }

                if (fase == UnityEngine.InputSystem.TouchPhase.Ended ||
                    fase == UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    dedoCamera = -1;
                }
            }
        }

        return deltaCameraMobile;
    }

    // Distância (a partir do jogador, na direção dir) até onde a câmera pode ficar sem entrar em paredes.
    float DistanciaLivre(Vector3 origemJogador, Vector3 dir, float distanciaTotal)
    {
        if (distanciaTotal < 0.01f)
            return distanciaTotal;

        float margem = 0.5f;

        Vector3 origem = origemJogador + dir * margem;

        float restante = distanciaTotal - margem;

        if (restante > 0f &&
            Physics.SphereCast(
                origem,
                raioColisao,
                dir,
                out RaycastHit hit,
                restante))
        {
            return margem + Mathf.Max(hit.distance - raioColisao, 0f);
        }

        return distanciaTotal;
    }
}
