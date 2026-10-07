using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class StoryboardInicio : MonoBehaviour
{
    [Header("Imagens da tirinha")]
    [SerializeField] private Image[] imagens;

    [Header("Tempo")]
    [SerializeField] private float tempoPorImagem = 2f;
    [SerializeField] private float duracaoAparecer = 0.3f;
    [SerializeField] private float duracaoFadeFinal = 1.5f;

    [Header("Fundo preto")]
    [SerializeField] private Image fundoPreto;

    private void Start()
    {
        // Pausa a gameplay
        Time.timeScale = 0f;

        // Todas começam invisíveis
        foreach (Image imagem in imagens)
        {
            if (imagem == null)
                continue;

            Color cor = imagem.color;
            cor.a = 0f;
            imagem.color = cor;
        }

        StartCoroutine(TocarStoryboard());
    }

    IEnumerator TocarStoryboard()
    {
        // =========================
        // MOSTRA AS 7 IMAGENS
        // =========================

        for (int i = 0; i < imagens.Length; i++)
        {
            if (imagens[i] == null)
                continue;

            yield return StartCoroutine(
                AparecerImagem(imagens[i])
            );

            yield return Esperar(tempoPorImagem);
        }

        // =========================
        // CHEGOU NA 7ª IMAGEM
        // =========================

        // Esconde o fundo preto
        if (fundoPreto != null)
            fundoPreto.gameObject.SetActive(false);

        // Esconde as imagens 1 até 6
        for (int i = 0; i < imagens.Length - 1; i++)
        {
            if (imagens[i] != null)
                imagens[i].gameObject.SetActive(false);
        }

        // =========================
        // FADE DA 7ª
        // =========================

        yield return StartCoroutine(
            FadeUltimaImagem()
        );

        // Libera a gameplay
        Time.timeScale = 1f;

        // Desativa o Canvas inteiro
        gameObject.SetActive(false);
    }

    IEnumerator AparecerImagem(Image imagem)
    {
        float tempo = 0f;

        Color cor = imagem.color;
        cor.a = 0f;
        imagem.color = cor;

        while (tempo < duracaoAparecer)
        {
            tempo += Time.unscaledDeltaTime;

            float progresso =
                Mathf.Clamp01(tempo / duracaoAparecer);

            cor.a = Mathf.Lerp(0f, 1f, progresso);
            imagem.color = cor;

            yield return null;
        }

        cor.a = 1f;
        imagem.color = cor;
    }

    IEnumerator FadeUltimaImagem()
    {
        if (imagens.Length == 0)
            yield break;

        Image ultima = imagens[imagens.Length - 1];

        if (ultima == null)
            yield break;

        float tempo = 0f;

        Color cor = ultima.color;
        cor.a = 1f;
        ultima.color = cor;

        while (tempo < duracaoFadeFinal)
        {
            tempo += Time.unscaledDeltaTime;

            float progresso =
                Mathf.Clamp01(tempo / duracaoFadeFinal);

            // SOMENTE a opacidade diminui
            cor.a = Mathf.Lerp(1f, 0f, progresso);

            ultima.color = cor;

            yield return null;
        }

        // Garante que terminou invisível
        cor.a = 0f;
        ultima.color = cor;
    }

    IEnumerator Esperar(float segundos)
    {
        float tempo = 0f;

        while (tempo < segundos)
        {
            tempo += Time.unscaledDeltaTime;
            yield return null;
        }
    }
}