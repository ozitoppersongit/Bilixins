using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TelaResultadoInfinito : MonoBehaviour
{
    [SerializeField] TMP_Text titulo;
    [SerializeField] TMP_Text pontuacao;
    [SerializeField] Button tentarNovamente;
    [SerializeField] Button sair;

    public void Mostrar(string motivo, int score, int recorde)
    {
        titulo.text = motivo;
        pontuacao.text = $"Pontos:\n<size=30>{score}</size>\nRecorde:\n<size=30>{recorde}</size>";
        tentarNovamente.onClick.AddListener(Reiniciar);
        sair.onClick.AddListener(VoltarAoMenu);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(tentarNovamente.gameObject);
    }

    void Reiniciar() => ModoDeJogo.Jogar(true);

    void VoltarAoMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }
}
