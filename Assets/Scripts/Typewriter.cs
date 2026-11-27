using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Typewriter : MonoBehaviour
{
    public TMP_Text texto;
    public float velocidade = 0.05f;

    [Header("Tutorial")]
    public Button botaoContinuar;

    private bool aguardandoContinuar;
    public bool EstaEscrevendo { get; private set; }

    public void Escrever(string mensagem)
    {
        StopAllCoroutines();
        StartCoroutine(Digitar(mensagem));
    }

    IEnumerator Digitar(string mensagem)
    {
        EstaEscrevendo = true;
        texto.maxVisibleCharacters = 0;
        texto.text = mensagem;
        texto.ForceMeshUpdate();
        int total = texto.textInfo.characterCount;

        for (int visiveis = 1; visiveis <= total; visiveis++)
        {
            texto.maxVisibleCharacters = visiveis;
            yield return new WaitForSecondsRealtime(velocidade);
        }
        texto.maxVisibleCharacters = int.MaxValue;
        EstaEscrevendo = false;
    }

    public IEnumerator EscreverTutorial(string mensagem)
    {
        aguardandoContinuar = false;

        botaoContinuar.gameObject.SetActive(false);

        texto.text = "";

        foreach (char letra in mensagem)
        {
            texto.text += letra;
            yield return new WaitForSecondsRealtime(velocidade);
        }

        botaoContinuar.gameObject.SetActive(true);

        aguardandoContinuar = true;

        while (aguardandoContinuar)
        {
            yield return null;
        }

        botaoContinuar.gameObject.SetActive(false);
    }

    public void ContinuarTutorial()
    {
        aguardandoContinuar = false;
    }
}
