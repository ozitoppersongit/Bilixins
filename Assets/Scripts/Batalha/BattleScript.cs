using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Cinemachine;

public class BattleScript : MonoBehaviour
{
    public BilixinsData bilixinJogador;
    public BilixinsData bilixinInimigo;

    public Image spriteJogador;
    public Image spriteInimigo;

    [Header("UI")]
    public Button botaoLutar;
    public Button botaoFugir;
    public Typewriter typewriter;

    [Header("Câmera")]
    public CinemachineCamera camera;
    private CameraSeguirPlayer cameraScript;

    [Header("UI Jogador")]
    public TextMeshProUGUI nomeJogador;
    public TextMeshProUGUI hpTextoJogador;
    public Slider hpBarraJogador;

    [Header("UI Inimigo")]
    public TextMeshProUGUI nomeInimigo;
    public TextMeshProUGUI hpTextoInimigo;
    public Slider hpBarraInimigo;

    [Header("Fill das barras")]
    public Image fillBarraJogador;
    public Image fillBarraInimigo;

    [Header("Ícones dos tipos")]
    public RawImage fundoiconeJogador;
    public RawImage fundoiconeInimigo;
    public RawImage iconeTipoJogador;
    public RawImage iconeTipoInimigo;

    public Texture2D iconeOrganico;
    public Texture2D iconeMetal;
    public Texture2D iconeVidro;
    public Texture2D iconePlastico;
    public Texture2D iconePapel;

    private Color32 vermelhoPlastico = new Color32(255, 31, 42, 255);

    private int hpJogador;
    private int hpInimigo;

    public int maxHpJogador;
    public int maxHpInimigo;

    [Header("Ataque")]
    public float power = 40f;

    private bool batalhaTerminou = false;
    private bool turnoJogador = false;


    private GatilhoDeEncontro gatilhoEncontro;

    private bool batalhaPausada = false;

    public void PausarBatalha()
    {
        batalhaPausada = true;
    }

    public void RetomarBatalha()
    {
        batalhaPausada = false;
    }

    private IEnumerator EsperarBatalha(float segundos)
{
    float tempo = 0f;

    while (tempo < segundos)
    {
        if (!batalhaPausada)
        {
            tempo += Time.unscaledDeltaTime;
        }

        yield return null;
    }
}

public void IniciarBatalha(
    BilixinsData inimigo,
    GatilhoDeEncontro gatilho)
{
    camera = Object.FindAnyObjectByType<CinemachineCamera>();
    cameraScript = camera.GetComponent<CameraSeguirPlayer>();
    cameraScript.enabled = false;


    bilixinInimigo = inimigo;
    gatilhoEncontro = gatilho;

    spriteJogador.sprite = bilixinJogador.spr;
    spriteInimigo.sprite = bilixinInimigo.spr;

    iconeTipoJogador.texture = PegarIconeTipo(bilixinJogador.tipo);
    iconeTipoInimigo.texture = PegarIconeTipo(bilixinInimigo.tipo);

    fundoiconeJogador.color = PegarCorTipo(bilixinJogador.tipo);
    fundoiconeInimigo.color = PegarCorTipo(bilixinInimigo.tipo);

    maxHpJogador = bilixinJogador.hp;
    maxHpInimigo = bilixinInimigo.hp;

    hpJogador = maxHpJogador;
    hpInimigo = maxHpInimigo;


    AtualizarUI();

    botaoLutar.interactable = false;
    botaoFugir.interactable = false;

    Time.timeScale = 0f;
    
    typewriter.Escrever(
        "Um bilixin selvagem apareceu!"
    );

    StartCoroutine(IniciarBatalha());
}
    void Start()
    {
        botaoLutar.interactable = false;
        botaoFugir.interactable = false;
    }

    Texture2D PegarIconeTipo(TipoLixo tipo)
{
    switch (tipo)
    {
        case TipoLixo.Organico:
            return iconeOrganico;

        case TipoLixo.Metal:
            return iconeMetal;

        case TipoLixo.Vidro:
            return iconeVidro;

        case TipoLixo.Plastico:
            return iconePlastico;

        case TipoLixo.Papel:
            return iconePapel;
    }

    return null;
}

Color PegarCorTipo(TipoLixo tipo)
{
    switch (tipo)
    {
        case TipoLixo.Organico:
            return Color.black;

        case TipoLixo.Metal:
            return Color.yellow;

        case TipoLixo.Vidro:
            return Color.green;

        case TipoLixo.Plastico:
            return vermelhoPlastico;

        case TipoLixo.Papel:
            return Color.blue;
    }

    return Color.white;
}


    void AtualizarUI()
{
    // =========================
    // JOGADOR
    // =========================

    nomeJogador.text =
        bilixinJogador.name + "  Lv." + bilixinJogador.nivel;

    hpTextoJogador.text =
        hpJogador.ToString("F0") + " / " +
        maxHpJogador.ToString("F0");

    hpBarraJogador.maxValue = maxHpJogador;
    hpBarraJogador.value = hpJogador;


    // =========================
    // INIMIGO
    // =========================

    nomeInimigo.text =
        bilixinInimigo.name + "  Lv." + bilixinInimigo.nivel;

    hpTextoInimigo.text =
        hpInimigo.ToString("F0") + " / " +
        maxHpInimigo.ToString("F0");

    hpBarraInimigo.maxValue = maxHpInimigo;
    hpBarraInimigo.value = hpInimigo;

    // Esconde a barra quando o HP chegar a 0
    fillBarraJogador.gameObject.SetActive(hpJogador > 0);
    fillBarraInimigo.gameObject.SetActive(hpInimigo > 0);
}

float MultiplicadorTipo(TipoLixo atacante, TipoLixo defensor)
{
    // ORGÂNICO tem vantagem contra TODOS
    if (atacante == TipoLixo.Organico)
    {
        return 1.25f;
    }

    // Ninguém tem vantagem contra ORGÂNICO
    if (defensor == TipoLixo.Organico)
    {
        return 1f;
    }

    // METAL → VIDRO
    if (atacante == TipoLixo.Metal &&
        defensor == TipoLixo.Vidro)
    {
        return 1.25f;
    }

    // VIDRO → PLÁSTICO
    if (atacante == TipoLixo.Vidro &&
        defensor == TipoLixo.Plastico)
    {
        return 1.25f;
    }

    // PLÁSTICO → PAPEL
    if (atacante == TipoLixo.Plastico &&
        defensor == TipoLixo.Papel)
    {
        return 1.25f;
    }

    // PAPEL → METAL
    if (atacante == TipoLixo.Papel &&
        defensor == TipoLixo.Metal)
    {
        return 1.25f;
    }

    // Sem vantagem
    return 1f;
}
    public float CalcularDano(
    BilixinsData atacante,
    BilixinsData defensor,
    float power)
{
    float danoBase =
        (((2f * atacante.nivel / 5f) + 2f)
        * power
        * atacante.ataque
        / defensor.defesa)
        / 50f
        + 2f;

    float multiplicador =
        MultiplicadorTipo(atacante.tipo, defensor.tipo);

    float danoFinal = danoBase * multiplicador;

    Debug.Log(
        $"ATAQUE: {atacante.name} | " +
        $"ATK: {atacante.ataque} | " +
        $"DEF: {defensor.defesa} | " +
        $"Nível: {atacante.nivel} | " +
        $"Dano Base: {danoBase:F2} | " +
        $"Multiplicador: {multiplicador} | " +
        $"Dano Final: {danoFinal:F2}"
    );

    return danoFinal;
}

    IEnumerator IniciarBatalha()
{
    yield return EsperarBatalha(2f);

    if (Random.value < 0.5f)
    {
        // Jogador começa
        IniciarTurnoJogador();
    }
    else
    {
        // Inimigo começa
        StartCoroutine(TurnoInimigo());
    }
}

IEnumerator TurnoInimigo()
{
    if (batalhaTerminou)
        yield break;

    turnoJogador = false;

    botaoLutar.interactable = false;
    botaoFugir.interactable = false;

    typewriter.Escrever(
        bilixinInimigo.name + " está atacando!"
    );

    yield return EsperarBatalha(1.5f);

    int danoInimigo = Mathf.RoundToInt(
        CalcularDano(
            bilixinInimigo,
            bilixinJogador,
            power
        )
    );

    hpJogador -= danoInimigo;
    hpJogador = Mathf.Max(hpJogador, 0);

    AtualizarUI();

    StartCoroutine(EfeitoDano(spriteJogador));

    typewriter.Escrever(
        bilixinInimigo.name +
        " causou " +
        danoInimigo +
        " de dano!"
    );

    yield return EsperarBatalha(1.5f);

    if (hpJogador <= 0)
    {
        batalhaTerminou = true;

        botaoLutar.interactable = false;
        botaoFugir.interactable = false;

        yield return StartCoroutine(EfeitoDesmaio(spriteJogador));

        StartCoroutine(FinalizarDerrota());

        yield break;
    }

    IniciarTurnoJogador();
}

    void IniciarTurnoJogador()
    {
        if (batalhaTerminou)
            return;

        turnoJogador = true;

        botaoLutar.interactable = true;
        botaoFugir.interactable = true;

        typewriter.Escrever(
            "O que " + bilixinJogador.name + " fará?"
        );
    }

    // Essa função será chamada pelo botão LUTAR
    public void Jogar()
    {
        if (!turnoJogador || batalhaTerminou)
            return;

        botaoLutar.interactable = false;
        botaoFugir.interactable = false;
        turnoJogador = false;

        StartCoroutine(TurnoDeBatalha());
    }

    IEnumerator TurnoDeBatalha()
    {
        // =========================
        // JOGADOR
        // =========================

        typewriter.Escrever(
            bilixinJogador.name + " atacou!"
        );

        yield return EsperarBatalha(1.5f);

        int danoJogador = Mathf.RoundToInt(CalcularDano(
            bilixinJogador,
            bilixinInimigo,
            power
        ));

        float multiplicador = MultiplicadorTipo(
        bilixinJogador.tipo,
        bilixinInimigo.tipo
    );

    if (multiplicador > 1f)
    {
        typewriter.Escrever("É super efetivo!");

        yield return EsperarBatalha(1.5f);
    }
    else if (multiplicador < 1f)
    {
        typewriter.Escrever("Não foi muito efetivo...");

        yield return EsperarBatalha(1.5f);
    }

        hpInimigo -= danoJogador;
        hpInimigo = Mathf.Max(hpInimigo, 0);

        AtualizarUI();

        StartCoroutine(EfeitoDano(spriteInimigo));

        typewriter.Escrever(
            bilixinJogador.name +
            " causou " +
            danoJogador.ToString("F0") +
            " de dano!"
        );

        yield return EsperarBatalha(1.5f);

        // VERIFICAÇÃO DA MORTE
        if (hpInimigo <= 0)
        {
            batalhaTerminou = true;

            botaoLutar.interactable = false;
            botaoFugir.interactable = false;

            yield return StartCoroutine(EfeitoDesmaio(spriteInimigo));

            StartCoroutine(FinalizarVitoria());

            yield break;
        }

        // =========================
        // TURNO DO INIMIGO
        // =========================

        typewriter.Escrever(
            bilixinInimigo.name + " está atacando!"
        );

        // Botão continua desabilitado
        botaoLutar.interactable = false;

        yield return EsperarBatalha(1.5f);

        int danoInimigo = Mathf.RoundToInt(CalcularDano(
            bilixinInimigo,
            bilixinJogador,
            power
        ));

        hpJogador -= danoInimigo;
        hpJogador = Mathf.Max(hpJogador, 0);

        AtualizarUI();

        StartCoroutine(EfeitoDano(spriteJogador));

        typewriter.Escrever(
            bilixinInimigo.name +
            " causou " +
            danoInimigo.ToString("F0") +
            " de dano!"
        );

        yield return EsperarBatalha(1.5f);

        if (hpJogador <= 0)
        {
            batalhaTerminou = true;

            botaoLutar.interactable = false;
            botaoFugir.interactable = false;

            yield return StartCoroutine(EfeitoDesmaio(spriteJogador));

            StartCoroutine(FinalizarDerrota());

            yield break;
        }

        // =========================
        // NOVO TURNO DO JOGADOR
        // =========================

        IniciarTurnoJogador();
    }

    public void Fugir()
{
    if (batalhaTerminou)
        return;

    batalhaTerminou = true;

    botaoLutar.interactable = false;
    botaoFugir.interactable = false;

    StartCoroutine(FinalizarFuga());
}

IEnumerator FinalizarFuga()
{
    typewriter.Escrever("Você fugiu da batalha!");

    yield return EsperarBatalha(1.5f);

    // Cobre a tela
    yield return StartCoroutine(
        TransicaoBatalha.instancia.Abrir()
    );

    // Volta para o mapa
    Time.timeScale = 1f;

    if (cameraScript != null)
        cameraScript.enabled = true;

    // Reativa o inimigo
    if (gatilhoEncontro != null)
        gatilhoEncontro.Reativar();

    // Destrói a batalha
    Destroy(gameObject);

    // Pede para a transição revelar o mapa
    TransicaoBatalha.instancia.FecharDepoisDaFuga();
}
public void Vitoria()
{
    if (batalhaTerminou)
        return;

    batalhaTerminou = true;

    botaoLutar.interactable = false;
    botaoFugir.interactable = false;

    StartCoroutine(FinalizarVitoria());
}

private IEnumerator FinalizarVitoria()
{
    typewriter.Escrever("Você venceu a batalha!");

    yield return EsperarBatalha(1.5f);

    // Cobre a tela
    yield return StartCoroutine(
        TransicaoBatalha.instancia.Abrir()
    );

    Time.timeScale = 1f;

    if (cameraScript != null)
        cameraScript.enabled = true;

    // Avisa que o Bilixin foi derrotado
    if (gatilhoEncontro != null)
        gatilhoEncontro.Vencer();

    // Destrói a tela de batalha
    Destroy(gameObject);

    // Revela o mapa
    TransicaoBatalha.instancia.FecharDepoisDaVitoria();
}

public void Derrota()
{
    if (batalhaTerminou)
        return;

    batalhaTerminou = true;

    botaoLutar.interactable = false;
    botaoFugir.interactable = false;

    StartCoroutine(FinalizarDerrota());
}

private IEnumerator FinalizarDerrota()
{
    typewriter.Escrever("Você perdeu a batalha!");

    yield return EsperarBatalha(1.5f);

    // Cobre a tela
    yield return StartCoroutine(
        TransicaoBatalha.instancia.Abrir()
    );

    Time.timeScale = 1f;

    if (cameraScript != null)
        cameraScript.enabled = true;

    // Aqui você decide o que acontece depois da derrota.
    // Exemplo: voltar para uma cena de Game Over.

    Destroy(gameObject);

    TransicaoBatalha.instancia.FecharDepoisDaDerrota();
}

private IEnumerator EfeitoDano(Image imagem)
{
    RectTransform rect = imagem.rectTransform;
    Vector2 posicaoOriginal = rect.anchoredPosition;

    // Pisca vermelho
    imagem.color = Color.red;

    // Tremidinha
    float duracao = 0.15f;
    float intensidade = 8f;
    float tempo = 0f;

    while (tempo < duracao)
    {
        tempo += Time.unscaledDeltaTime;

        float x = Random.Range(-intensidade, intensidade);
        float y = Random.Range(-intensidade, intensidade);

        rect.anchoredPosition = posicaoOriginal + new Vector2(x, y);

        yield return null;
    }

    // Volta para a posição normal
    rect.anchoredPosition = posicaoOriginal;

    // Volta para a cor normal
    imagem.color = Color.white;
}
private IEnumerator EfeitoDesmaio(Image imagem)
{
    RectTransform rect = imagem.rectTransform;

    Vector2 posicaoOriginal = rect.anchoredPosition;

    Color corOriginal = imagem.color;

    float duracao = 0.5f;
    float tempo = 0f;

    while (tempo < duracao)
    {
        tempo += Time.unscaledDeltaTime;

        float progresso = tempo / duracao;

        // Desaparece aos poucos
        Color cor = imagem.color;
        cor.a = Mathf.Lerp(1f, 0f, progresso);
        imagem.color = cor;

        yield return null;
    }

    Color corFinal = imagem.color;
    corFinal.a = 0f;
    imagem.color = corFinal;
}
}