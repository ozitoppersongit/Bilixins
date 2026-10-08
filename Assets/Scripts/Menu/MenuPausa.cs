using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MenuPausa : MonoBehaviour
{
    [Header("Celular")]
    public RectTransform celular;
    public Transform bg;

    [Header("Posições")]
    public Vector2 posicaoFechado;
    public Vector2 posicaoAberto;

    [Header("Velocidade")]
    public float velocidade = 1000f;

    [Header("Botões")]
    public Button botaoPausa;
    public Button botaoRetomar;

    [Header("Menu de Opções")]
    public GameObject opcoesPrefab;
    public GameObject sairTela;

    [Header("Câmera")]
    public CinemachineCamera camera;
    private CameraSeguirPlayer cameraScript;

    private bool pausado = false;
    private bool animando = false;
    private bool abrindo = false;
    private BattleScript batalha;

    void Start()
    {
        cameraScript = camera.GetComponent<CameraSeguirPlayer>();

        celular.anchoredPosition = posicaoFechado;
    }

    void Update()
    {
        if (!animando)
            return;

        float passo = velocidade * Time.unscaledDeltaTime;

        Vector2 destino;

        if (abrindo)
            destino = posicaoAberto;
        else
            destino = posicaoFechado;

        celular.anchoredPosition = Vector2.MoveTowards(
            celular.anchoredPosition,
            destino,
            passo
        );

        if (celular.anchoredPosition == destino)
        {
            animando = false;

            if (abrindo)
            {
                // Terminou de abrir
                botaoRetomar.gameObject.SetActive(true);
            }
            else
            {
                // Terminou de fechar
                pausado = false;
                Time.timeScale = 1f;
                cameraScript.enabled = true;
            }
        }
    }

    public void AbrirOpcoes()
{
    if (opcoesPrefab == null)
    {
        Debug.LogError("O prefab de opções não foi definido!");
        return;
    }

    EventSystem.current.SetSelectedGameObject(null);
    Instantiate(opcoesPrefab);
}

    public void Pausar()
{
    if (pausado || animando)
        return;

    pausado = true;
    animando = true;
    abrindo = true;

    bg.gameObject.SetActive(true);
    botaoPausa.interactable = false;

    batalha = Object.FindAnyObjectByType<BattleScript>();

    if (batalha != null)
    {
        batalha.PausarBatalha();
    }

    Time.timeScale = 0f;
    cameraScript.enabled = false;
}

    public void Retomar()
{
    if (!pausado || animando)
        return;

    animando = true;
    abrindo = false;

    bg.gameObject.SetActive(false);
    botaoPausa.interactable = true;

    if (batalha != null)
    {
        batalha.RetomarBatalha();
    }
}

    public void Sair()
    {
        EventSystem.current.SetSelectedGameObject(null);
        sairTela.SetActive(true);
    }

    public void ConfirmarSaida()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void RecusarSaida()
    {
        sairTela.SetActive(false);
    }
}