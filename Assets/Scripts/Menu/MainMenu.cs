using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] GameObject MainScreen;
    [SerializeField] GameObject OptionsScreen;
    [SerializeField] GameObject SelectModeScreen;
    [SerializeField] GameObject telaCarregamento;

    void Awake()
    {
        CarregamentoPartida.ConfigurarPrefab(telaCarregamento);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SelectModeScreen.SetActive(false);
        MainScreen.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartGame()
    {
        MainScreen.SetActive(false);
        SelectModeScreen.SetActive(true);
        FocarBotao(SelectModeScreen);
    }

    public void JogarHistoria() => ModoDeJogo.Jogar(false);
    public void JogarInfinito() => ModoDeJogo.Jogar(true);

    public void VoltarDaSelecao()
    {
        SelectModeScreen.SetActive(false);
        MainScreen.SetActive(true);
        FocarBotao(MainScreen);
        FocarBotao(MainScreen);
    }

    public void Options()
    {
        MainScreen.SetActive(false);
        OptionsScreen.SetActive(true);
    }

    public void GoBack()
    {
        OptionsScreen.SetActive(false);
        SelectModeScreen.SetActive(false);
        MainScreen.SetActive(true);
        FocarBotao(MainScreen);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    static void FocarBotao(GameObject painel)
    {
        Button botao = painel.GetComponentInChildren<Button>();
        if (botao != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(botao.gameObject);
    }
}
