using System;
using System.Collections.Generic;
using UnityEngine;

public enum LadoPersonagem
{
    Esquerdo,
    Direito
}

[Serializable]
public class DialogoLinha
{
    [Header("Fala")]
    public string nomePersonagem;
    [TextArea(3, 6)]
    public string texto;

    [Header("Portraits")]
    public Sprite portraitEsquerdo;
    public Sprite portraitDireito;

    [Header("Quem está falando")]
    public LadoPersonagem personagemFalando;
}

[CreateAssetMenu(fileName = "Novo Dialogo", menuName = "Bilixin/Dialogo")]
public class DialogoData : ScriptableObject
{
    [Header("Identificação")]
    public string id;

    [Header("Falas")]
    public List<DialogoLinha> falas = new List<DialogoLinha>();
}
