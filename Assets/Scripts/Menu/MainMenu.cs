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
    [SerializeField] GameObject VictoryScreen;
    [SerializeField] GameObject CreditsScreen;
    [SerializeField] GameObject telaCarregamento;

    static bool abrirVitoria;

    public static void Carregar(bool vitoriaHistoria = false)
    {
        abrirVitoria = vitoriaHistoria;
        Cutscenes.Instance?.EncerrarPartida();
        Missoes.Instance?.EncerrarPartida();
        GerenciadorBatalha.instancia?.EncerrarPartida();
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }

    void Awake()
    {
        CarregamentoPartida.ConfigurarPrefab(telaCarregamento);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        bool mostrarVitoria = abrirVitoria;
        abrirVitoria = false;
        SelectModeScreen.SetActive(false);
        CreditsScreen.SetActive(false);
        MainScreen.SetActive(!mostrarVitoria);
        VictoryScreen.SetActive(mostrarVitoria);
        if (mostrarVitoria) FocarBotao(VictoryScreen);
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

    public void Credits()
    {
        MainScreen.SetActive(false);
        CreditsScreen.SetActive(true);
        FocarBotao(CreditsScreen);
    }

    public void GoBack()
    {
        CreditsScreen.SetActive(false);
        VictoryScreen.SetActive(false);
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
