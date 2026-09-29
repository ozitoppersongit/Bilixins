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

    public float CalcularDano(
        BilixinsData atacante,
        BilixinsData defensor,
        float power)
    {
        return ((((2f * atacante.nivel / 5f) + 2f) * power *
            ((float)atacante.ataque / defensor.defesa)) / 50f) + 2f;
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