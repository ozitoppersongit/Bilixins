using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// Gerenciador central de cutscenes do jogo. Cada cutscene é uma lista de passos feita no Inspector;
// qualquer script chama Cutscenes.Instance.Tocar("nome"). Enquanto uma cutscene toca, o controle do
// player e a câmera de seguir ficam desligados (sem editar nenhum dos dois), e voltam no fim.
// Também cuida das câmeras fixas por zona (tutorial): player dentro da zona = câmera parada no ponto.
[DefaultExecutionOrder(-100)] // roda antes da CameraSeguirPlayer, pra desligá-la antes que ela mova a câmera
public class Cutscenes : MonoBehaviour
{
    public static Cutscenes Instance { get; private set; }
    public static bool EmCutscene => Instance != null && Instance.tocando;

    public enum Tipo
    {
        Camera,      // leva a câmera até um ponto (alvo) em "tempo" segundos
        AndarPlayer, // o player anda até o alvo (só no plano XZ) na "velocidade", virando pra frente
        Esperar,     // espera "tempo" segundos
        Evento,      // dispara "aoExecutar" (ligar um objeto, desligar outro, tocar som...)
        Dialogo,     // pede um diálogo (id em "fala") e espera a UI avisar o fim com DialogoTerminado(); sem UI ouvindo, espera "tempo" segundos
        Batalha      // o player anda até o inimigo mais perto do marcador (alvo) e a cutscene espera a batalha acabar; fuga ou derrota repete até vencer
    }

    [Serializable]
    public class Passo
    {
        public Tipo tipo;
        [Tooltip("Camera: ponto da câmera. AndarPlayer: destino.")] public Transform alvo;
        [Tooltip("Camera: duração da transição (0 = corta). Esperar: segundos.")] public float tempo = 1f;
        [Tooltip("AndarPlayer: metros por segundo.")] public float velocidade = 3f;
        [Tooltip("Camera/AndarPlayer: se ligado, o passo seguinte começa junto, sem esperar este acabar.")] public bool simultaneo;
        [Tooltip("Dialogo: id da fala (a UI usa pra escolher o texto).")] public string fala;
        public UnityEvent aoExecutar;
    }

    [Serializable]
    public class Cutscene
    {
        public string nome;
        public List<Passo> passos = new List<Passo>();
        public UnityEvent aoIniciar;
        public UnityEvent aoTerminar;
    }

    [Serializable]
    public class ZonaDeCamera
    {
        public string nome;
        [Tooltip("Volume da zona (um Collider, pode ser Trigger; nenhum script nele).")] public Collider zona;
        public Transform ponto;
    }

    [Header("Ligações")]
    [SerializeField] MapaGerador mapaGerador;
    [SerializeField] CameraSeguirPlayer cameraSeguir;

    [Header("Cutscenes")]
    [SerializeField] List<Cutscene> cutscenes = new List<Cutscene>();
    [Tooltip("Nome da cutscene que toca sozinha quando o mapa termina de gerar (vazio = nenhuma).")]
    [SerializeField] string cutsceneAoGerarMapa;

    [Header("Câmera fixa por zona")]
    [SerializeField] bool cameraFixaAtiva = true;
    [SerializeField] List<ZonaDeCamera> zonasDeCamera = new List<ZonaDeCamera>();
    [SerializeField] float suavizacao = 4f;

    public event Action<string> aoTerminarCutscene;
    // A UI assina isto: recebe o id da fala e chama DialogoTerminado() quando o jogador termina de ler.
    public event Action<string> aoPedirDialogo;

    bool dialogoAberto;
    public void DialogoTerminado() { dialogoAberto = false; }

    bool tocando;
    ZonaDeCamera zonaAtual;
    Behaviour seguirCinemachine;

    void Awake()
    {
        Instance = this;
        if (cameraSeguir != null) seguirCinemachine = cameraSeguir.GetComponent<Unity.Cinemachine.CinemachineFollow>();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void OnEnable()
    {
        if (mapaGerador != null) mapaGerador.aoTerminarDeGerar.AddListener(AoGerarMapa);
    }

    void OnDisable()
    {
        if (mapaGerador != null) mapaGerador.aoTerminarDeGerar.RemoveListener(AoGerarMapa);
    }

    void AoGerarMapa()
    {
        if (!string.IsNullOrEmpty(cutsceneAoGerarMapa)) Tocar(cutsceneAoGerarMapa);
    }

    public void AtivarCameraFixa(bool ativa)
    {
        cameraFixaAtiva = ativa;
        if (!ativa) SairDaZona();
    }

    public void Tocar(string nome)
    {
        Cutscene c = cutscenes.Find(x => x.nome == nome);
        if (c == null) { Debug.LogWarning($"Cutscenes: não existe cutscene chamada '{nome}'."); return; }
        if (tocando) { Debug.LogWarning($"Cutscenes: '{nome}' ignorada, já tem uma cutscene tocando."); return; }
        StartCoroutine(Executar(c));
    }

    IEnumerator Executar(Cutscene c)
    {
        tocando = true;
        yield return null; // deixa o player e a câmera terminarem de se montar

        Transform player = mapaGerador != null && mapaGerador.PlayerInstanciado != null ? mapaGerador.PlayerInstanciado.transform : null;
        Travar(true);
        c.aoIniciar?.Invoke();

        foreach (Passo p in c.passos)
        {
            Coroutine rodando = null;
            switch (p.tipo)
            {
                case Tipo.Camera: rodando = StartCoroutine(MoverCamera(p)); break;
                case Tipo.AndarPlayer: rodando = StartCoroutine(AndarPlayer(player, p)); break;
                case Tipo.Esperar: rodando = StartCoroutine(Esperar(p.tempo)); break;
                case Tipo.Evento: p.aoExecutar?.Invoke(); break;
                case Tipo.Dialogo: rodando = StartCoroutine(Dialogo(p)); break;
                case Tipo.Batalha: rodando = StartCoroutine(Batalha(player, p)); break;
            }
            if (rodando != null && !p.simultaneo) yield return rodando;
        }

        Travar(false);
        tocando = false;       // antes do aoTerminar, pra quem ele chamar (ex: AtivarCameraFixa) já ver a cutscene como acabada
        c.aoTerminar?.Invoke();
        aoTerminarCutscene?.Invoke(c.nome);
    }

    // Liga/desliga o que controla player e câmera. Durante a cutscene a câmera é movida direto pelo passo.
    void Travar(bool travar)
    {
        GameObject p = mapaGerador != null ? mapaGerador.PlayerInstanciado : null;
        if (p != null)
        {
            PlayerMovement mov = p.GetComponent<PlayerMovement>();
            if (mov != null) mov.enabled = !travar;
        }
        if (cameraSeguir != null) cameraSeguir.enabled = !travar && zonaAtual == null;
        if (seguirCinemachine != null) seguirCinemachine.enabled = !travar && zonaAtual == null;
    }

    IEnumerator Esperar(float segundos)
    {
        yield return new WaitForSeconds(segundos);
    }

    IEnumerator MoverCamera(Passo p)
    {
        if (p.alvo == null || cameraSeguir == null) yield break;
        Transform cam = cameraSeguir.transform;
        Vector3 pos0 = cam.position;
        Quaternion rot0 = cam.rotation;

        for (float t = 0f; t < p.tempo; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / p.tempo);
            cam.SetPositionAndRotation(Vector3.Lerp(pos0, p.alvo.position, k), Quaternion.Slerp(rot0, p.alvo.rotation, k));
            yield return null;
        }
        cam.SetPositionAndRotation(p.alvo.position, p.alvo.rotation);
    }

    IEnumerator AndarPlayer(Transform player, Passo p)
    {
        if (p.alvo == null) yield break;
        yield return AndarAte(player, p.alvo.position, p.velocidade, null);
    }

    // Anda no plano XZ até o destino; "parar" (opcional) encerra antes se ficar verdadeiro.
    IEnumerator AndarAte(Transform player, Vector3 destino, float velocidade, Func<bool> parar)
    {
        CharacterController cc = player != null ? player.GetComponent<CharacterController>() : null;
        if (cc == null) yield break;

        float velocidadeY = 0f;
        float limite = Time.time + 60f; // segurança: nunca prende o jogo se o destino for inalcançável
        while (Time.time < limite)
        {
            if (parar != null && parar()) break;
            Vector3 falta = destino - player.position;
            falta.y = 0f;
            if (falta.magnitude < 0.15f) break;

            Vector3 dir = falta.normalized;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            velocidadeY = cc.isGrounded ? -1f : velocidadeY - 20f * dt;
            cc.Move((dir * velocidade + Vector3.up * velocidadeY) * dt);
            player.rotation = Quaternion.Slerp(player.rotation, Quaternion.LookRotation(dir), 10f * dt);
            yield return null;
        }
    }

    IEnumerator Dialogo(Passo p)
    {
        if (aoPedirDialogo == null)
        {
            yield return new WaitForSeconds(p.tempo); // ninguém da UI ouvindo ainda: só espera
            yield break;
        }
        dialogoAberto = true;
        aoPedirDialogo.Invoke(p.fala);
        while (dialogoAberto) yield return null;
    }

    // O player anda até o inimigo (GatilhoDeEncontro) mais perto do marcador "alvo"; a batalha (sistema à parte, com timeScale 0) começa quando ele encosta.
    // Vitória destrói o gatilho e termina o passo. Fuga ou derrota reativam o gatilho e o player tenta de novo.
    IEnumerator Batalha(Transform player, Passo p)
    {
        // o MapaGerador recria os inimigos a cada Play, então o alvo é um marcador: vale o gatilho mais próximo dele (até 3 m)
        GatilhoDeEncontro gatilho = null;
        float melhor = 3f;
        if (p.alvo != null)
            foreach (GatilhoDeEncontro g in FindObjectsByType<GatilhoDeEncontro>(FindObjectsSortMode.None))
            {
                float d = Vector3.Distance(g.transform.position, p.alvo.position);
                if (d < melhor) { melhor = d; gatilho = g; }
            }
        Collider col = gatilho != null ? gatilho.GetComponent<Collider>() : null;
        if (player == null || gatilho == null || col == null)
        {
            Debug.LogWarning("Cutscenes: passo Batalha não achou um GatilhoDeEncontro com Collider a menos de 3 m do alvo.");
            yield break;
        }

        Vector3 origem = player.position;
        while (gatilho != null)
        {
            // anda até a batalha disparar (o gatilho se desliga no OnTriggerEnter)
            yield return AndarAte(player, p.alvo.position, p.velocidade, () => gatilho == null || !col.enabled);
            if (gatilho == null) break;

            // espera a tela de batalha aparecer e depois acabar
            float espera = 0f;
            while (gatilho != null && FindFirstObjectByType<BattleScript>() == null && espera < 3f)
            {
                espera += Time.unscaledDeltaTime;
                yield return null;
            }
            while (FindFirstObjectByType<BattleScript>() != null) yield return null;
            if (gatilho == null) break; // venceu

            // fugiu ou perdeu: reativa o gatilho e se afasta pra poder encostar de novo
            gatilho.Reativar();
            yield return AndarAte(player, origem, p.velocidade, null);
        }
    }

    // A batalha e o menu de pausa usam botões, e a CameraSeguirPlayer trava o cursor no PC: com ele travado não dá pra clicar.
    // Enquanto um dos dois existir o cursor fica solto, e volta ao estado de antes no fim.
    bool batalhaAberta;
    float proximaChecagem;
    CursorLockMode travaAntes;
    bool visivelAntes;
    MenuPausa menuPausa;

    bool Pausado()
    {
        if (menuPausa == null) menuPausa = FindAnyObjectByType<MenuPausa>();
        return menuPausa != null && menuPausa.bg != null && menuPausa.bg.gameObject.activeSelf;
    }

    void Update()
    {
        // ESC abre e fecha o menu de pausa durante uma cutscene (o botão de pausa na tela só dá pra usar com toque)
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (Pausado()) menuPausa.Retomar();
            else if (tocando && menuPausa != null) menuPausa.Pausar();
        }

        if (Time.unscaledTime < proximaChecagem) return;
        proximaChecagem = Time.unscaledTime + 0.2f;

        bool existe = FindAnyObjectByType<BattleScript>() != null || Pausado();
        if (existe && !batalhaAberta)
        {
            batalhaAberta = true;
            travaAntes = Cursor.lockState;
            visivelAntes = Cursor.visible;
        }
        if (existe)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (batalhaAberta)
        {
            batalhaAberta = false;
            Cursor.lockState = travaAntes;
            Cursor.visible = visivelAntes;
        }
    }

    void LateUpdate()
    {
        // a tela de batalha religa a CameraSeguirPlayer sozinha ao terminar; durante a cutscene ela fica desligada
        if (tocando && cameraSeguir != null)
        {
            cameraSeguir.enabled = false;
            if (seguirCinemachine != null) seguirCinemachine.enabled = false;
            return;
        }

        if (tocando || !cameraFixaAtiva || cameraSeguir == null || mapaGerador == null || mapaGerador.PlayerInstanciado == null) return;

        Vector3 pos = mapaGerador.PlayerInstanciado.transform.position;
        ZonaDeCamera nova = null;
        foreach (ZonaDeCamera z in zonasDeCamera)
            if (z.zona != null && z.ponto != null && z.zona.bounds.Contains(pos)) { nova = z; break; }

        if (nova != zonaAtual)
        {
            zonaAtual = nova;
            // fora de qualquer zona a câmera volta a seguir o player; dentro, fica parada no ponto
            cameraSeguir.enabled = zonaAtual == null;
            if (seguirCinemachine != null) seguirCinemachine.enabled = zonaAtual == null;
        }

        if (zonaAtual != null)
        {
            float k = 1f - Mathf.Exp(-suavizacao * Time.deltaTime);
            Transform cam = cameraSeguir.transform;
            cam.SetPositionAndRotation(Vector3.Lerp(cam.position, zonaAtual.ponto.position, k), Quaternion.Slerp(cam.rotation, zonaAtual.ponto.rotation, k));
        }
    }

    void SairDaZona()
    {
        zonaAtual = null;
        if (tocando || cameraSeguir == null) return;
        cameraSeguir.enabled = true;
        if (seguirCinemachine != null) seguirCinemachine.enabled = true;
    }
}
