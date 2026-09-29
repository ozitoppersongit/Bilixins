using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleScript : MonoBehaviour
{
    public BilixinsData bilixinJogador;
    public BilixinsData bilixinInimigo;

    public Image spriteJogador;
    public Image spriteInimigo;

    [Header("UI")]
    public Button botaoLutar;
    public Typewriter typewriter;

    [Header("UI Jogador")]
    public TextMeshProUGUI nomeJogador;
    public TextMeshProUGUI hpTextoJogador;
    public Slider hpBarraJogador;

[Header("UI Inimigo")]
    public TextMeshProUGUI nomeInimigo;
    public TextMeshProUGUI hpTextoInimigo;
    public Slider hpBarraInimigo;

    [Header("Ícones dos tipos")]
    public RawImage iconeTipoJogador;
    public RawImage iconeTipoInimigo;

    public Texture2D iconeOrganico;
    public Texture2D iconeMetal;
    public Texture2D iconeVidro;
    public Texture2D iconePlastico;
    public Texture2D iconePapel;

    [Header("HP")]
    public float hpJogador;
    public float hpInimigo;

    public float maxHpJogador;
    public float maxHpInimigo;

    [Header("Ataque")]
    public float power = 40f;

    private bool batalhaTerminou = false;
    private bool turnoJogador = false;

    void Start()
    {
        spriteJogador.sprite = bilixinJogador.spr;
        spriteInimigo.sprite = bilixinInimigo.spr;

        iconeTipoJogador.texture = PegarIconeTipo(bilixinJogador.tipo);
        iconeTipoInimigo.texture = PegarIconeTipo(bilixinInimigo.tipo);

        maxHpJogador = CalcularHP(bilixinJogador);
        maxHpInimigo = CalcularHP(bilixinInimigo);

        hpJogador = maxHpJogador;
        hpInimigo = maxHpInimigo;

        AtualizarUI();

        // Começa desabilitado
        botaoLutar.interactable = false;

        typewriter.Escrever("Um bilixin selvagem apareceu!");

        // Espera a mensagem terminar antes de começar
        StartCoroutine(IniciarBatalha());
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
}

    float CalcularHP(BilixinsData bilixin)
    {
        return (2f * bilixin.nivel) + 50f;
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

    return danoBase * multiplicador;
}

    IEnumerator IniciarBatalha()
    {
        yield return new WaitForSeconds(2f);

        // Começa o turno do jogador
        IniciarTurnoJogador();
    }

    void IniciarTurnoJogador()
    {
        if (batalhaTerminou)
            return;

        turnoJogador = true;

        botaoLutar.interactable = true;

        typewriter.Escrever(
            "O que " + bilixinJogador.name + " fará?"
        );
    }

    // Essa função será chamada pelo botão LUTAR
    public void Jogar()
    {
        // Segurança para impedir clicar fora do turno
        if (!turnoJogador || batalhaTerminou)
            return;

        // Desabilita imediatamente
        botaoLutar.interactable = false;

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

        yield return new WaitForSeconds(1.5f);

        float danoJogador = CalcularDano(
            bilixinJogador,
            bilixinInimigo,
            power
        );

        float multiplicador = MultiplicadorTipo(
        bilixinJogador.tipo,
        bilixinInimigo.tipo
    );

    if (multiplicador > 1f)
    {
        typewriter.Escrever("É super efetivo!");

        yield return new WaitForSeconds(1.5f);
    }
    else if (multiplicador < 1f)
    {
        typewriter.Escrever("Não foi muito efetivo...");

        yield return new WaitForSeconds(1.5f);
    }

        hpInimigo -= danoJogador;
        hpInimigo = Mathf.Max(hpInimigo, 0);

        AtualizarUI();

        typewriter.Escrever(
            bilixinJogador.name +
            " causou " +
            danoJogador.ToString("F0") +
            " de dano!"
        );

        yield return new WaitForSeconds(1.5f);

        // =========================
        // INIMIGO MORREU?
        // =========================

        if (hpInimigo <= 0)
        {
            typewriter.Escrever(
                bilixinInimigo.name + " foi derrotado!"
            );

            batalhaTerminou = true;
            botaoLutar.interactable = false;

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

        yield return new WaitForSeconds(1.5f);

        float danoInimigo = CalcularDano(
            bilixinInimigo,
            bilixinJogador,
            power
        );

        hpJogador -= danoInimigo;
        hpJogador = Mathf.Max(hpJogador, 0);

        AtualizarUI();

        typewriter.Escrever(
            bilixinInimigo.name +
            " causou " +
            danoInimigo.ToString("F0") +
            " de dano!"
        );

        yield return new WaitForSeconds(1.5f);

        // =========================
        // JOGADOR MORREU?
        // =========================

        if (hpJogador <= 0)
        {
            typewriter.Escrever(
                bilixinJogador.name + " foi derrotado!"
            );

            batalhaTerminou = true;
            botaoLutar.interactable = false;

            yield break;
        }

        // =========================
        // NOVO TURNO DO JOGADOR
        // =========================

        IniciarTurnoJogador();
    }
}