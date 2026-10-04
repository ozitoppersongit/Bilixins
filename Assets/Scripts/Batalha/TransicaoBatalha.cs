using System.Collections;
using UnityEngine;

public class TransicaoBatalha : MonoBehaviour
{
    public static TransicaoBatalha instancia;

    [Header("Bolinha")]
    public RectTransform bolinha;

    [Header("Configuração")]
    public float duracao = 0.5f;

    private void Awake()
    {
        instancia = this;

        bolinha.localScale = Vector3.zero;
    }

    public IEnumerator Abrir()
    {
        float tempo = 0f;

        while (tempo < duracao)
        {
            tempo += Time.unscaledDeltaTime;

            float progresso = tempo / duracao;

            bolinha.localScale = Vector3.Lerp(
                Vector3.zero,
                Vector3.one * 50f,
                progresso
            );

            yield return null;
        }

        bolinha.localScale = Vector3.one * 50f;
    }

    public IEnumerator Fechar()
    {
        float tempo = 0f;

        while (tempo < duracao)
        {
            tempo += Time.unscaledDeltaTime;

            float progresso = tempo / duracao;

            bolinha.localScale = Vector3.Lerp(
                Vector3.one * 50f,
                Vector3.zero,
                progresso
            );

            yield return null;
        }

        bolinha.localScale = Vector3.zero;
    }

public void FecharDepoisDaVitoria()
{
    StartCoroutine(FecharDepoisDaVitoriaCoroutine());
}

private IEnumerator FecharDepoisDaVitoriaCoroutine()
{
    yield return null;

    yield return StartCoroutine(Fechar());
}

public void FecharDepoisDaDerrota()
{
    StartCoroutine(FecharDepoisDaDerrotaCoroutine());
}

private IEnumerator FecharDepoisDaDerrotaCoroutine()
{
    yield return null;

    yield return StartCoroutine(Fechar());
}

    public void FecharDepoisDaFuga()
{
    StartCoroutine(FecharDepoisDaFugaCoroutine());
}

private IEnumerator FecharDepoisDaFugaCoroutine()
{
    // Espera a destruição da batalha acontecer
    yield return null;

    yield return StartCoroutine(Fechar());
}
}