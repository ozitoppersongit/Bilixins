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
    public Image hpBarraJogador;

    [Header("UI Inimigo")]
    public TextMeshProUGUI nomeInimigo;
    public TextMeshProUGUI hpTextoInimigo;
    public Image hpBarraInimigo;

    [Header("Ícones dos tipos")]
    public RawImage iconeTipoJogador;
    public RawImage iconeTipoInimigo;

    public Texture2D iconeOrganico;
    public Texture2D iconeMetal;
    public Texture2D iconeVidro;
    public Texture2D iconePlastico;
    public Texture2D iconePapel;

    private int hpJogador;
    private int hpInimigo;

    public int maxHpJogador;
    public int maxHpInimigo;

    [Header("Ataque")]
    public float power = 40f;
    private bool batalhaTerminou = false;
    private bool turnoJogador = false;

    [Header("Dano Crítico e Miss")]
    [Range(0f, 1f)]
    public float chanceMiss = 0.15f;

    [Range(0f, 1f)]
    public float chanceCritico = 0.10f;

    public float multiplicadorCritico = 1.5f;


    private GatilhoDeEncontro gatilhoEncontro;

    private bool batalhaPausada = false;

    [Header("Tutorial")]
    public bool modoTutorial = false;
    private bool tutorialConcluido = false;
    public Typewriter typewriterTutorial;
    public Transform personagemNome;
    public Image destaqueLutar;
    public Image destaqueItens;
    public Image destaqueFugir;
    public Image destaqueStatus;
    public Image destaqueTipo;

    [Header("Menu de Ataques")]
    public GameObject painelAtaques;
    public Button botaoAtaque1;
    public Button botaoAtaque2;
    public TextMeshProUGUI textoAtaque1;
    public TextMeshProUGUI textoAtaque2;

    [Header("Menu de Itens")]
    public Button botaoItens;
    public GameObject painelItens;
    public Transform conteudoItens;
    public GameObject prefabBotaoItem;

    [Header("Cooldown do Ataque Forte")]
    public TextMeshProUGUI textoCooldownAtaque2;

    private int cooldownAtaque2 = 0;
    private bool pularReducaoCooldown = false;

    void AtualizarCooldownAtaque2()
    {
        if (cooldownAtaque2 > 0)
        {
            textoCooldownAtaque2.text =
                cooldownAtaque2 == 1
                ? "1 turno"
                : cooldownAtaque2 + " turnos";
        }
        else
        {
            textoCooldownAtaque2.text = "";
        }

        botaoAtaque2.interactable = cooldownAtaque2 <= 0;
    }

    void ReiniciarBatalhaTutorial()
    {
        // Restaura a batalha
        batalhaTerminou = false;
        batalhaPausada = false;
        turnoJogador = false;

        // Restaura a vida dos Bilixins
        hpJogador = maxHpJogador;
        hpInimigo = maxHpInimigo;

        // Reseta o cooldown do ataque forte
        cooldownAtaque2 = 0;
        pularReducaoCooldown = false;

        // Restaura a aparência dos sprites
        Color corJogador = spriteJogador.color;
        corJogador.a = 1f;
        spriteJogador.color = corJogador;

        Color corInimigo = spriteInimigo.color;
        corInimigo.a = 1f;
        spriteInimigo.color = corInimigo;

        // Atualiza a interface
        AtualizarUI();
        AtualizarCooldownAtaque2();

        // Fecha os painéis
        painelAtaques.SetActive(false);
        painelItens.SetActive(false);

        // Bloqueia os botões durante a preparação
        BloquearBotoes(true);

        // O tutorial explicativo já foi concluído
        tutorialConcluido = true;

        typewriter.Escrever("Vamos tentar novamente!");

        // Começa uma nova batalha
        StartCoroutine(IniciarBatalha());
    }

    private IEnumerator ExecutarTutorial()
{
    botaoLutar.interactable = false;
    botaoFugir.interactable = false;

    destaqueLutar.gameObject.SetActive(false);
    destaqueItens.gameObject.SetActive(false);
    destaqueFugir.gameObject.SetActive(false);

    personagemNome.gameObject.SetActive(true);

    yield return typewriterTutorial.EscreverTutorial(
        "\nEnfim Galego, eu vou te ensinar a batalhar contra qualquer Bilixim."
    );

    destaqueLutar.gameObject.SetActive(true);

    yield return typewriterTutorial.EscreverTutorial(
        "\nPara atacar seu adversário, toque no botão LUTAR.\nVocê pode escolher entre 2 ataques: um mediano e um mais forte."
    );

    yield return typewriterTutorial.EscreverTutorial(
        "\nPorém, utilizando o ataque forte, você só poderá usar novamente após 2 turnos."
    );

    destaqueLutar.gameObject.SetActive(false);
    destaqueItens.gameObject.SetActive(true);

    yield return typewriterTutorial.EscreverTutorial(
        "\nVocê também pode usar itens durante a batalha.\nPara isso, toque em ITENS para utilizar um item que esteja na sua mochila."
    );

    destaqueItens.gameObject.SetActive(false);
    destaqueFugir.gameObject.SetActive(true);
    
    yield return typewriterTutorial.EscreverTutorial(
        "\nO botão FUGIR encerra a batalha e te leva de volta ao mapa."
    );

    destaqueFugir.gameObject.SetActive(false);
    destaqueStatus.gameObject.SetActive(true);

    yield return typewriterTutorial.EscreverTutorial(
        "\nAqui você pode ver o status dos Bilixins que estão lutando, como o nome e a vida de cada um deles."
    );

    destaqueStatus.gameObject.SetActive(false);
    destaqueTipo.gameObject.SetActive(true);

    yield return typewriterTutorial.EscreverTutorial(
        "\nE por último, esses ícones representam o tipo de cada Bilixim. Como você pode ver, eu sou de tipo plástico e o do inimigo sendo tipo metal."
    );

    yield return typewriterTutorial.EscreverTutorial(
        "\nAlém disso, certos tipos de Bilixins contra outros tipos possuem vantagens e desvantagens."
    );

    destaqueTipo.gameObject.SetActive(false);

    yield return typewriterTutorial.EscreverTutorial(
        "\nPronto! Agora você já sabe o básico. Boa batalha!"
    );

    personagemNome.gameObject.SetActive(false);

    tutorialConcluido = true;

    IniciarTurnoJogador();
}
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

    maxHpJogador = bilixinJogador.hp;
    maxHpInimigo = bilixinInimigo.hp;

    hpJogador = maxHpJogador;
    hpInimigo = maxHpInimigo;

    AtualizarUI();

    painelAtaques.SetActive(false);
    painelItens.SetActive(false);
    botaoLutar.interactable = false;
    botaoFugir.interactable = false;

    Time.timeScale = 0f;

    tutorialConcluido = !modoTutorial;

    if (modoTutorial)
    {
        StartCoroutine(ExecutarTutorial());
    }
    else
    {
        typewriter.Escrever("Um bilixin selvagem apareceu!");
        StartCoroutine(IniciarBatalha());
    }
}
    void Start()
    {
        painelAtaques.SetActive(false);
        painelItens.SetActive(false);
        botaoLutar.interactable = false;
        botaoItens.interactable = false;
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

    hpBarraJogador.fillAmount = Mathf.Clamp01((float)hpJogador / maxHpJogador);

    // =========================
    // INIMIGO
    // =========================

    nomeInimigo.text =
        bilixinInimigo.name + "  Lv." + bilixinInimigo.nivel;

    hpTextoInimigo.text =
        hpInimigo.ToString("F0") + " / " +
        maxHpInimigo.ToString("F0");

    hpBarraInimigo.fillAmount = Mathf.Clamp01((float)hpInimigo / maxHpInimigo);
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

bool AtaqueErrou()
{
    return Random.value < chanceMiss;
}

bool AtaqueCritico()
{
    return Random.value < chanceCritico;
}

    IEnumerator IniciarBatalha()
{
    yield return EsperarBatalha(2f);

    ScreenCapture.CaptureScreenshot(
                "Screenshot.png"
            );

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

    painelAtaques.SetActive(false);
    painelItens.SetActive(false);

    BloquearBotoes(true);

    // 75% de chance de escolher o primeiro ataque.
    // 25% de chance de escolher o segundo.
    AtaqueBilixin ataqueEscolhido =
        Random.value < 0.75f
        ? bilixinInimigo.ataque1
        : bilixinInimigo.ataque2;

    typewriter.Escrever(
        bilixinInimigo.nome +
        " usou " +
        ataqueEscolhido.nome +
        "!"
    );

    yield return EsperarBatalha(1.5f);

// Verifica se o inimigo errou
if (AtaqueErrou())
{
    typewriter.Escrever(
        bilixinInimigo.nome + " errou o ataque!"
    );

    yield return EsperarBatalha(1.5f);

    IniciarTurnoJogador();
    yield break;
}

// Calcula o dano normal
int dano = Mathf.RoundToInt(
    CalcularDano(
        bilixinInimigo,
        bilixinJogador,
        ataqueEscolhido.poder
    )
);

// Verifica crítico
bool critico = AtaqueCritico();

if (critico)
{
    dano = Mathf.RoundToInt(
        dano * multiplicadorCritico
    );
}

    float multiplicador = MultiplicadorTipo(
        bilixinInimigo.tipo,
        bilixinJogador.tipo
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

    hpJogador = Mathf.Max(0, hpJogador - dano);

    AtualizarUI();

    StartCoroutine(EfeitoDano(spriteJogador));

    if (critico)
{
    typewriter.Escrever(
        "ACERTO CRÍTICO! " +
        bilixinInimigo.nome +
        " causou " +
        dano +
        " de dano!"
    );
}
else
{
    typewriter.Escrever(
        bilixinInimigo.nome +
        " causou " +
        dano +
        " de dano!"
    );
}

    yield return EsperarBatalha(1.5f);

    if (hpJogador <= 0)
    {
        batalhaTerminou = true;

        yield return StartCoroutine(EfeitoDesmaio(spriteJogador));

        StartCoroutine(FinalizarDerrota());
        yield break;
    }

    IniciarTurnoJogador();
}


void IniciarTurnoJogador()
{
    if (batalhaTerminou) return;
    if (modoTutorial && !tutorialConcluido) return;

    // Atualiza o cooldown ao começar um novo turno.
    if (cooldownAtaque2 > 0)
    {
        if (pularReducaoCooldown)
        {
            pularReducaoCooldown = false;
        }
        else
        {
            cooldownAtaque2--;
        }
    }

    AtualizarCooldownAtaque2();

    turnoJogador = true;

    painelAtaques.SetActive(false);
    painelItens.SetActive(false);

    BloquearBotoes(false);

    typewriter.Escrever("O que " + bilixinJogador.nome + " fará?");
}

void MostrarAtaques()
{
    if (!turnoJogador || batalhaTerminou) return;

    painelAtaques.SetActive(true);
    painelItens.SetActive(false);

    botaoAtaque1.interactable = true;
    AtualizarCooldownAtaque2();

    textoAtaque1.text = bilixinJogador.ataque1.nome;
    textoAtaque2.text = bilixinJogador.ataque2.nome;

    typewriter.Escrever("Escolha o tipo de ataque.");
}

public void EscolherAtaque(int numero)
{
    if (!turnoJogador || batalhaTerminou)
        return;

    // Impede o uso do ataque forte durante o cooldown.
    if (numero == 2 && cooldownAtaque2 > 0)
        return;

    AtaqueBilixin ataqueEscolhido;

    if (numero == 1)
        ataqueEscolhido = bilixinJogador.ataque1;
    else
        ataqueEscolhido = bilixinJogador.ataque2;

    if (numero == 2)
    {
        cooldownAtaque2 = 2;
        pularReducaoCooldown = true;
    }

    turnoJogador = false;

    painelAtaques.SetActive(false);

    botaoAtaque1.interactable = false;
    botaoAtaque2.interactable = false;

    StartCoroutine(
        ExecutarAtaqueJogador(ataqueEscolhido)
    );
}


IEnumerator ExecutarAtaqueJogador(AtaqueBilixin ataque)
{
    BloquearBotoes(true);

    typewriter.Escrever(
        bilixinJogador.nome + " usou " + ataque.nome + "!"
    );

    yield return EsperarBatalha(1.5f);

// Verifica se o ataque errou
if (AtaqueErrou())
{
    typewriter.Escrever(
        bilixinJogador.nome + " errou o ataque!"
    );

    yield return EsperarBatalha(1.5f);

    StartCoroutine(TurnoInimigo());
    yield break;
}

// Calcula o dano normal
int dano = Mathf.RoundToInt(
    CalcularDano(
        bilixinJogador,
        bilixinInimigo,
        ataque.poder
    )
);

// Verifica crítico
bool critico = AtaqueCritico();

if (critico)
{
    dano = Mathf.RoundToInt(
        dano * multiplicadorCritico
    );
}

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

    hpInimigo = Mathf.Max(0, hpInimigo - dano);

    AtualizarUI();

    StartCoroutine(EfeitoDano(spriteInimigo));

    if (critico)
{
    typewriter.Escrever(
        "ACERTO CRÍTICO! " +
        bilixinJogador.nome +
        " causou " +
        dano +
        " de dano!"
    );
}
else
{
    typewriter.Escrever(
        bilixinJogador.nome +
        " causou " +
        dano +
        " de dano!"
    );
}

    yield return EsperarBatalha(1.5f);

    if (hpInimigo <= 0)
    {
        batalhaTerminou = true;

        yield return StartCoroutine(EfeitoDesmaio(spriteInimigo));

        StartCoroutine(FinalizarVitoria());
        yield break;
    }

    StartCoroutine(TurnoInimigo());
}


public void MostrarItens()
{
    if (!turnoJogador || batalhaTerminou) return;

    painelItens.SetActive(true);
    painelAtaques.SetActive(false);

    typewriter.Escrever("Qual item usar?");

    // Limpa os botões anteriores.
    foreach (Transform filho in conteudoItens)
    {
        Destroy(filho.gameObject);
    }

    InventoryManager inventario = InventoryManager.instancia;

    if (inventario == null)
    {
        typewriter.Escrever("Inventário não encontrado!");
        return;
    }

    bool encontrouItem = false;

    foreach (ItemStack stack in inventario.ObterItens())
    {
        if (stack.item == null || stack.quantidade <= 0)
            continue;

        encontrouItem = true;

        ItemData itemAtual = stack.item;
        int quantidadeAtual = stack.quantidade;

        GameObject objeto = Instantiate(
            prefabBotaoItem,
            conteudoItens
        );

        ItemBotaoUI botaoUI = objeto.GetComponent<ItemBotaoUI>();

        botaoUI.Configurar(
            itemAtual,
            quantidadeAtual,
            () => UsarItem(itemAtual)
        );
    }

    if (!encontrouItem)
    {
        typewriter.Escrever("Você não possui itens!");
        painelItens.SetActive(false);

        botaoLutar.interactable = true;
        botaoItens.interactable = true;
        botaoFugir.interactable = true;
    }
}


public void UsarItem(ItemData item)
{
    if (!turnoJogador || batalhaTerminou || item == null)
        return;

    if (item.efeito == null)
    {
        typewriter.Escrever("Esse item não possui um efeito!");
        return;
    }

    // Tenta aplicar o efeito antes de consumir.
    bool sucesso = item.efeito.Aplicar(this);

    if (!sucesso)
    {
        typewriter.Escrever("Não foi possível usar esse item!");
        return;
    }

    // Consome somente se for consumível.
    if (item.consumivel)
    {
        bool removido = InventoryManager.instancia.RemoverItem(item, 1);

        if (!removido)
        {
            typewriter.Escrever("Você não possui esse item!");
            return;
        }
    }

    turnoJogador = false;

    painelItens.SetActive(false);

    typewriter.Escrever(
        bilixinJogador.nome + " utilizou " + item.nome + "!"
    );

    AtualizarUI();

    StartCoroutine(PassarTurnoAposItem());
}

IEnumerator PassarTurnoAposItem()
{
    yield return EsperarBatalha(1.5f);

    if (batalhaTerminou)
        yield break;

    StartCoroutine(TurnoInimigo());
}


public void VoltarMenuPrincipal()
{
    if (!turnoJogador || batalhaTerminou) return;

    painelAtaques.SetActive(false);
    painelItens.SetActive(false);

    botaoLutar.interactable = true;
    botaoItens.interactable = true;
    botaoFugir.interactable = true;

    typewriter.Escrever("O que " + bilixinJogador.nome + " fará?");
}

    // Essa função será chamada pelo botão LUTAR
    public void Jogar()
    {
        MostrarAtaques();
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
    
    if (modoTutorial)
        {
            typewriter.Escrever("Você não pode escapar agora.");
            return;
        }

    batalhaTerminou = true;

    painelAtaques.SetActive(false);
    painelItens.SetActive(false);
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

    BloquearBotoes(true);

    StartCoroutine(FinalizarDerrota());
}

private IEnumerator FinalizarDerrota()
{
    typewriter.Escrever("Você perdeu a batalha!");

    yield return EsperarBatalha(1.5f);

    // Se estiver no tutorial, reinicia a batalha.
    if (modoTutorial)
    {
        ReiniciarBatalhaTutorial();
        yield break;
    }

    // Comportamento normal fora do tutorial
    yield return StartCoroutine(
        TransicaoBatalha.instancia.Abrir()
    );

    Time.timeScale = 1f;

    if (cameraScript != null)
        cameraScript.enabled = true;

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

public bool RecuperarHpJogador(int quantidade)
{
    if (hpJogador >= maxHpJogador || quantidade <= 0)
        return false;

    hpJogador = Mathf.Min(
        hpJogador + quantidade,
        maxHpJogador
    );

    AtualizarUI();

    return true;
}

void BloquearBotoes(bool bloquear)
{
    botaoLutar.interactable = !bloquear;
    botaoItens.interactable = !bloquear;
    botaoFugir.interactable = !bloquear;
}
}