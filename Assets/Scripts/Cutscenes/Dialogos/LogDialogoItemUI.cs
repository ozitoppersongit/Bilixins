using TMPro;
using UnityEngine;

public class LogDialogoItemUI : MonoBehaviour
{
    public TextMeshProUGUI texto;

    public void Configurar(string nome, string fala)
    {
        texto.text = $"- {nome}\n{fala}";
    }
}
