using System.Collections;
using TMPro;
using UnityEngine;

public class Typewriter : MonoBehaviour
{
    public TMP_Text texto;
    public float velocidade = 0.05f;

    public void Escrever(string mensagem)
    {
        StopAllCoroutines();
        StartCoroutine(Digitar(mensagem));
    }

    IEnumerator Digitar(string mensagem)
    {
        texto.text = "";

        foreach (char letra in mensagem)
        {
            texto.text += letra;
            yield return new WaitForSecondsRealtime(velocidade);
        }
    }

    void Start()
    {
        Escrever("Um bilixin selvagem apareceu!");
    }
}