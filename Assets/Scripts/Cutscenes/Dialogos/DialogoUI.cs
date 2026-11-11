using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogoUI : MonoBehaviour
{
    [Header("Painel de Diálogo")]
    public TextMeshProUGUI textoDialogo;
    public TextMeshProUGUI nomePersonagem;

    [Header("Portraits")]
    public Image portraitEsquerdo;
    public Image portraitDireito;

    [Header("Botões")]
    public Button botaoProximo;
    public Button botaoLog;

    [Header("Painel de Log")]
    public GameObject painelLog;
    public Transform conteudoLog;
    public GameObject prefabLogItem;

    [Header("Efeito Typewriter")]
    public float velocidadeTexto = 0.03f;

    private Coroutine coroutineTypewriter;
    private bool textoTerminouDeEscrever;

    private DialogoData dialogoAtual;
    private int indiceFala;

    private readonly List<DialogoLinha> historico =
        new List<DialogoLinha>();

    public void Iniciar(DialogoData dialogo)
    {
        dialogoAtual = dialogo;
        indiceFala = 0;

        painelLog.SetActive(false);

        historico.Clear();

        botaoProximo.onClick.RemoveAllListeners();
        botaoLog.onClick.RemoveAllListeners();

        botaoProximo.onClick.AddListener(ProximaFala);
        botaoLog.onClick.AddListener(AbrirLog);

        MostrarFala();
    }

    void MostrarFala()
    {
        if (dialogoAtual == null ||
            indiceFala >= dialogoAtual.falas.Count)
        {
            EncerrarDialogo();
            return;
        }

        DialogoLinha fala = dialogoAtual.falas[indiceFala];

        nomePersonagem.text = fala.nomePersonagem;
        textoDialogo.text = fala.texto;

        portraitEsquerdo.sprite = fala.portraitEsquerdo;
        portraitDireito.sprite = fala.portraitDireito;

        portraitEsquerdo.color =
            fala.personagemFalando == LadoPersonagem.Esquerdo
            ? new Color(1f, 1f, 1f, 1f)
            : new Color(1f, 1f, 1f, 0.5f);

        portraitDireito.color =
            fala.personagemFalando == LadoPersonagem.Direito
            ? new Color(1f, 1f, 1f, 1f)
            : new Color(1f, 1f, 1f, 0.5f);

        historico.Add(fala);

        // Esconde o botão enquanto o texto está sendo escrito
        botaoProximo.gameObject.SetActive(false);

        textoTerminouDeEscrever = false;

        if (coroutineTypewriter != null)
        {
            StopCoroutine(coroutineTypewriter);
        }

        coroutineTypewriter = StartCoroutine(Typewriter());
    }

    IEnumerator Typewriter()
    {
        textoDialogo.maxVisibleCharacters = 0;

        textoDialogo.ForceMeshUpdate();

        int totalCaracteres = textoDialogo.textInfo.characterCount;

        for (int i = 0; i <= totalCaracteres; i++)
        {
            textoDialogo.maxVisibleCharacters = i;

            yield return new WaitForSecondsRealtime(velocidadeTexto);
        }

        textoTerminouDeEscrever = true;

        // Mostra o botão somente quando terminar
        botaoProximo.gameObject.SetActive(true);

        coroutineTypewriter = null;
    }
    public void ProximaFala()
    {
        if (dialogoAtual == null)
            return;

        if (painelLog != null && painelLog.activeSelf)
            return;

        // Impede avançar antes de terminar a escrita
        if (!textoTerminouDeEscrever)
            return;

        indiceFala++;

        if (indiceFala >= dialogoAtual.falas.Count)
        {
            EncerrarDialogo();
            return;
        }

        MostrarFala();
    }

    void AbrirLog()
    {
        painelLog.SetActive(true);

        // Limpa os registros visuais anteriores.
        foreach (Transform filho in conteudoLog)
        {
            Destroy(filho.gameObject);
        }

        // Reconstrói o histórico.
        foreach (DialogoLinha fala in historico)
        {
            GameObject item = Instantiate(
                prefabLogItem,
                conteudoLog
            );

            LogDialogoItemUI itemUI =
                item.GetComponent<LogDialogoItemUI>();

            itemUI.Configurar(
                fala.nomePersonagem,
                fala.texto
            );
        }
    }

    public void FecharLog()
    {
        painelLog.SetActive(false);
    }

    void EncerrarDialogo()
    {
        if (Cutscenes.Instance != null)
        {
            Cutscenes.Instance.DialogoTerminado();
        }

        Destroy(gameObject);
    }
}
