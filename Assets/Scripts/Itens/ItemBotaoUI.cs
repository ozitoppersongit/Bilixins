
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemBotaoUI : MonoBehaviour
{
    public Image icone;
    public TextMeshProUGUI nome;
    public TextMeshProUGUI quantidade;
    public Button botao;

    public void Configurar(
        ItemData item,
        int qtd,
        Action aoClicar)
    {
        icone.sprite = item.icone;
        nome.text = item.nome;
        quantidade.text = "x" + qtd;

        botao.onClick.RemoveAllListeners();
        botao.onClick.AddListener(() => aoClicar?.Invoke());
    }
}
