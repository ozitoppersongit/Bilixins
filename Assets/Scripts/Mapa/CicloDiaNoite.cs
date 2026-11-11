using UnityEngine;
using UnityEngine.Rendering;

// Ciclo de dia e noite (tempo real, em loop). "hora" vai de 0 a 1: 0 = meia-noite, 0.25 = amanhecer, 0.5 = meio-dia, 0.75 = pôr do sol.
// Uma única luz direcional faz de sol (de dia) e de lua (de noite). O mesmo relógio controla:
//  - o céu (material Bilixins/SkyboxDiaNoite: mistura dia/noite, sol, lua e brilho do horizonte),
//  - luz ambiente (Trilight), neblina leve ao amanhecer,
//  - janelas e lâmpadas acesas à noite (emissão dos materiais da lista).
// No editor dá para ver qualquer horário arrastando "hora". As cutscenes podem usar DefinirHora, Pausar e as predefinições.
[ExecuteAlways]
public class CicloDiaNoite : MonoBehaviour
{
    [Header("Tempo")]
    [Tooltip("Quanto dura um dia inteiro, em segundos: 1200 = 10 minutos de dia + 10 de noite.")]
    [SerializeField] float duracaoDoDia = 1200f;
    [Tooltip("0 = meia-noite, 0.25 = amanhecer, 0.5 = meio-dia, 0.75 = pôr do sol.")]
    [SerializeField, Range(0f, 1f)] float hora = 0.35f;
    [SerializeField] bool correndo = true;

    [Header("Sol e lua (uma luz só)")]
    [SerializeField] Light luz;
    [Tooltip("Para onde aponta o leste, em graus (0 = +Z).")]
    [SerializeField, Range(0f, 360f)] float direcaoDoLeste = 60f;
    [Tooltip("Quanto o caminho do sol se inclina para o norte (graus). 0 = passa pelo zênite.")]
    [SerializeField, Range(0f, 50f)] float inclinacaoDoCaminho = 25f;
    [SerializeField] Gradient corDaLuz;
    [SerializeField] AnimationCurve intensidadeDaLuz;
    [Tooltip("Força da sombra ao longo do dia (mais fraca ao amanhecer, no pôr do sol e à noite).")]
    [SerializeField] AnimationCurve forcaDaSombra;

    [Header("Céu")]
    [Tooltip("Material com o shader Bilixins/SkyboxDiaNoite.")]
    [SerializeField] Material skybox;
    [Tooltip("0 = textura de dia, 1 = textura de noite.")]
    [SerializeField] AnimationCurve noite;
    [Tooltip("Cor (e força no alfa) do brilho no horizonte, voltado para o sol: laranja no nascer e no pôr.")]
    [SerializeField] Gradient brilhoDoHorizonte;
    [Tooltip("Quantos graus o céu gira num dia (as nuvens e as estrelas andam devagar).")]
    [SerializeField] float giroDoCeuNoDia = 40f;

    [Header("Luz ambiente (Trilight)")]
    [SerializeField] Gradient ambienteCeu;
    [SerializeField] Gradient ambienteEquador;
    [SerializeField] Gradient ambienteChao;

    [Header("Neblina leve (amanhecer)")]
    [SerializeField] Gradient corDaNeblina;
    [SerializeField] AnimationCurve densidadeDaNeblina;

    [Header("Janelas e lâmpadas acesas")]
    [Tooltip("Janelas acesas à noite.")]
    [SerializeField] Material[] janelas;
    [Tooltip("Janelas com luz fraca (variação). As que não estão em nenhuma lista ficam apagadas.")]
    [SerializeField] Material[] janelasFracas;
    [SerializeField, Range(0f, 1f)] float forcaDasFracas = 0.35f;
    [SerializeField, ColorUsage(false, true)] Color corDasJanelas = new Color(1f, 0.78f, 0.42f) * 1.4f;
    [SerializeField] Material[] lampadas;
    [SerializeField, ColorUsage(false, true)] Color corDasLampadas = new Color(1f, 0.93f, 0.7f) * 3f;
    [Tooltip("0 = apagadas, 1 = acesas.")]
    [SerializeField] AnimationCurve acesas;

    int dia = 1;
    public int Dia => dia;
    public float Hora => hora;
    public bool EhNoite => hora < 0.25f || hora > 0.75f;

    Material skyboxEmUso;
    Color[] emissaoOriginal;
    bool[] emissaoLigadaOriginal;
    Material[] todosAcesos;

    // ---------- chamadas para UnityEvents (cutscenes) ----------
    public void DefinirHora(float h)
    {
        if (float.IsNaN(h) || float.IsInfinity(h)) return;   // valor inválido: ignora e mantém a hora atual
        hora = Mathf.Repeat(h, 1f);
        Aplicar();
    }
    public void Pausar(bool pausar) { correndo = !pausar; }
    public void IrParaManha() { DefinirHora(0.30f); }
    public void IrParaMeioDia() { DefinirHora(0.50f); }
    public void IrParaPorDoSol() { DefinirHora(0.74f); }
    public void IrParaNoite() { DefinirHora(0.95f); }

    void Reset()
    {
        luz = FindAnyObjectByType<Light>();
        Padroes(true);
    }

    void OnEnable()
    {
        Padroes(false);
        GuardarEmissao();
        if (Application.isPlaying && skybox != null)
        {
            skyboxEmUso = new Material(skybox);   // no jogo não mexe no asset do material
            RenderSettings.skybox = skyboxEmUso;
        }
        Aplicar();
    }

    void OnDisable()
    {
        RestaurarEmissao();
        if (skyboxEmUso != null) { Destroy(skyboxEmUso); skyboxEmUso = null; }
    }

    void OnValidate()
    {
        if (isActiveAndEnabled) { Padroes(false); Aplicar(); }
    }

    void Update()
    {
        if (!Application.isPlaying) return;
        if (correndo)
        {
            float anterior = hora;
            hora = Mathf.Repeat(hora + Time.deltaTime / Mathf.Max(1f, duracaoDoDia), 1f);
            if (hora < anterior)   // passou da meia-noite: um dia inteiro (20 min) terminou
            {
                dia++;
                Debug.Log($"[Ciclo] Passaram-se {duracaoDoDia / 60f:0.#} minutos... um novo dia começa! Dia {dia}.");
            }
        }
        Aplicar();
    }

    // ---------- aplica o horário em tudo ----------
    void Aplicar()
    {
        if (corDaLuz == null) return;
        float a = (hora - 0.25f) * Mathf.PI * 2f;                                   // 0 = nascer do sol
        Vector3 leste = Quaternion.Euler(0f, direcaoDoLeste, 0f) * Vector3.forward;
        Vector3 lado = Vector3.Cross(Vector3.up, leste);
        float inc = inclinacaoDoCaminho * Mathf.Deg2Rad;
        Vector3 sol = (leste * Mathf.Cos(a) + Vector3.up * Mathf.Sin(a) * Mathf.Cos(inc) + lado * Mathf.Sin(a) * Mathf.Sin(inc)).normalized;
        Vector3 lua = -sol;
        Vector3 corpo = sol.y > -0.02f ? sol : lua;                                    // quem está no céu ilumina

        if (luz != null)
        {
            luz.transform.rotation = Quaternion.LookRotation(-corpo, Vector3.up);
            luz.color = corDaLuz.Evaluate(hora);
            luz.intensity = Mathf.Max(0f, intensidadeDaLuz.Evaluate(hora));
            luz.shadowStrength = Mathf.Clamp01(forcaDaSombra.Evaluate(hora));
        }

        Material sk = skyboxEmUso != null ? skyboxEmUso : skybox;
        if (sk != null)
        {
            if (!Application.isPlaying && RenderSettings.skybox != sk) RenderSettings.skybox = sk;
            sk.SetFloat("_Mix", Mathf.Clamp01(noite.Evaluate(hora)));
            sk.SetVector("_SunDir", sol);
            sk.SetVector("_MoonDir", lua);
            sk.SetFloat("_Rotacao", hora * giroDoCeuNoDia);
            sk.SetColor("_CorHorizonte", brilhoDoHorizonte.Evaluate(hora));
        }

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ambienteCeu.Evaluate(hora);
        RenderSettings.ambientEquatorColor = ambienteEquador.Evaluate(hora);
        RenderSettings.ambientGroundColor = ambienteChao.Evaluate(hora);

        float dens = Mathf.Max(0f, densidadeDaNeblina.Evaluate(hora));
        RenderSettings.fog = dens > 0.0004f;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = corDaNeblina.Evaluate(hora);
        RenderSettings.fogDensity = dens;

        float ac = Mathf.Clamp01(acesas.Evaluate(hora));
        Emitir(janelas, corDasJanelas * ac, ac);
        Emitir(janelasFracas, corDasJanelas * (ac * forcaDasFracas), ac * forcaDasFracas);
        Emitir(lampadas, corDasLampadas * ac, ac);
    }

    static void Emitir(Material[] ms, Color cor, float forca)
    {
        if (ms == null) return;
        foreach (Material m in ms)
        {
            if (m == null) continue;
            if (forca > 0.001f) m.EnableKeyword("_EMISSION"); else m.DisableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", cor);
        }
    }

    // guarda e restaura a emissão original dos materiais (para o asset não ficar "aceso" depois de sair do Play)
    void GuardarEmissao()
    {
        var lista = new System.Collections.Generic.List<Material>();
        if (janelas != null) lista.AddRange(janelas);
        if (janelasFracas != null) lista.AddRange(janelasFracas);
        if (lampadas != null) lista.AddRange(lampadas);
        lista.RemoveAll(m => m == null);
        todosAcesos = lista.ToArray();
        emissaoOriginal = new Color[todosAcesos.Length];
        emissaoLigadaOriginal = new bool[todosAcesos.Length];
        for (int i = 0; i < todosAcesos.Length; i++)
        {
            emissaoOriginal[i] = todosAcesos[i].HasProperty("_EmissionColor") ? todosAcesos[i].GetColor("_EmissionColor") : Color.black;
            emissaoLigadaOriginal[i] = todosAcesos[i].IsKeywordEnabled("_EMISSION");
        }
    }

    void RestaurarEmissao()
    {
        if (todosAcesos == null) return;
        for (int i = 0; i < todosAcesos.Length; i++)
        {
            if (todosAcesos[i] == null) continue;
            todosAcesos[i].SetColor("_EmissionColor", emissaoOriginal[i]);
            if (emissaoLigadaOriginal[i]) todosAcesos[i].EnableKeyword("_EMISSION"); else todosAcesos[i].DisableKeyword("_EMISSION");
        }
    }

    // ---------- valores padrão (só entram onde o campo está vazio) ----------
    static Gradient G(params (float t, string cor, float alfa)[] pontos)
    {
        var g = new Gradient();
        var cores = new GradientColorKey[pontos.Length];
        var alfas = new GradientAlphaKey[pontos.Length];
        for (int i = 0; i < pontos.Length; i++)
        {
            ColorUtility.TryParseHtmlString("#" + pontos[i].cor, out Color c);
            cores[i] = new GradientColorKey(c, pontos[i].t);
            alfas[i] = new GradientAlphaKey(pontos[i].alfa, pontos[i].t);
        }
        g.SetKeys(cores, alfas);
        return g;
    }

    static AnimationCurve C(params (float t, float v)[] pontos)
    {
        var k = new Keyframe[pontos.Length];
        for (int i = 0; i < pontos.Length; i++) k[i] = new Keyframe(pontos[i].t, pontos[i].v);
        var c = new AnimationCurve(k);
        for (int i = 0; i < c.length; i++) c.SmoothTangents(i, 0f);
        return c;
    }

    void Padroes(bool forcar)
    {
        if (forcar || corDaLuz == null || corDaLuz.colorKeys.Length == 0)
            corDaLuz = G((0f, "5C74C4", 1), (0.2f, "5C74C4", 1), (0.25f, "E8A6C4", 1), (0.32f, "F8E8E2", 1), (0.5f, "FFF4DC", 1), (0.68f, "FFE0B0", 1), (0.75f, "FF8A48", 1), (0.8f, "6078C0", 1), (1f, "5C74C4", 1));
        if (forcar || intensidadeDaLuz == null || intensidadeDaLuz.length == 0)
            intensidadeDaLuz = C((0f, 0.62f), (0.2f, 0.62f), (0.25f, 0.7f), (0.32f, 1.0f), (0.5f, 1.15f), (0.68f, 1.0f), (0.75f, 0.7f), (0.8f, 0.62f), (1f, 0.62f));
        if (forcar || forcaDaSombra == null || forcaDaSombra.length == 0)
            forcaDaSombra = C((0f, 0.55f), (0.2f, 0.55f), (0.27f, 0.8f), (0.35f, 1f), (0.65f, 1f), (0.73f, 0.8f), (0.8f, 0.55f), (1f, 0.55f));
        if (forcar || noite == null || noite.length == 0)
            noite = C((0f, 1f), (0.19f, 1f), (0.27f, 0f), (0.73f, 0f), (0.81f, 1f), (1f, 1f));
        if (forcar || brilhoDoHorizonte == null || brilhoDoHorizonte.alphaKeys.Length == 0)
            brilhoDoHorizonte = G((0f, "000000", 0), (0.2f, "FF8A3D", 0), (0.25f, "E08AB8", 0.9f), (0.31f, "F0B8C8", 0), (0.69f, "FFB070", 0), (0.75f, "FF6A3A", 0.9f), (0.81f, "FF6A3A", 0), (1f, "000000", 0));
        if (forcar || ambienteCeu == null || ambienteCeu.colorKeys.Length == 0)
            ambienteCeu = G((0f, "2E4284", 1), (0.2f, "2E4284", 1), (0.27f, "7E90C0", 1), (0.35f, "86AEE0", 1), (0.65f, "86AEE0", 1), (0.75f, "A08AA0", 1), (0.82f, "2E4284", 1), (1f, "2E4284", 1));
        if (forcar || ambienteEquador == null || ambienteEquador.colorKeys.Length == 0)
            ambienteEquador = G((0f, "36488A", 1), (0.2f, "36488A", 1), (0.27f, "909AB8", 1), (0.35f, "9AA4AE", 1), (0.65f, "9AA4AE", 1), (0.75f, "B08A80", 1), (0.82f, "36488A", 1), (1f, "36488A", 1));
        if (forcar || ambienteChao == null || ambienteChao.colorKeys.Length == 0)
            ambienteChao = G((0f, "1E2650", 1), (0.2f, "1E2650", 1), (0.27f, "5E524A", 1), (0.35f, "5C5850", 1), (0.65f, "5C5850", 1), (0.75f, "5E4C44", 1), (0.82f, "1E2650", 1), (1f, "1E2650", 1));
        if (forcar || corDaNeblina == null || corDaNeblina.colorKeys.Length == 0)
            corDaNeblina = G((0f, "2A3A6A", 1), (0.22f, "2A3A6A", 1), (0.28f, "D2DCF0", 1), (0.38f, "DCE8F2", 1), (1f, "DCE8F2", 1));
        if (forcar || densidadeDaNeblina == null || densidadeDaNeblina.length == 0)
            densidadeDaNeblina = C((0f, 0f), (0.22f, 0f), (0.27f, 0.011f), (0.33f, 0.007f), (0.42f, 0f), (1f, 0f));
        if (forcar || acesas == null || acesas.length == 0)
            acesas = C((0f, 1f), (0.2f, 1f), (0.27f, 0f), (0.7f, 0f), (0.78f, 1f), (1f, 1f));
    }
}
