using UnityEngine;

public class ControleInterfaceExploracao : MonoBehaviour
{
    [Header("Referências")]
    public MapaGerador mapaGerador;
    public Cutscenes cutscenes;
    public DialogoManager dialogoManager;

    [Header("Interface")]
    public GameObject minimapa;
    public GameObject analogico;

    private bool? estadoAnterior;

    void Start()
    {
        if (mapaGerador == null)
            mapaGerador = FindAnyObjectByType<MapaGerador>();

        if (cutscenes == null)
            cutscenes = FindAnyObjectByType<Cutscenes>();

        if (dialogoManager == null)
            dialogoManager = FindAnyObjectByType<DialogoManager>();

        AtualizarInterface();
    }

    void Update()
    {
        AtualizarInterface();
    }

    void AtualizarInterface()
    {
        // Verifica se o jogador já foi gerado
        bool jogadorExiste =
            mapaGerador != null &&
            mapaGerador.PlayerInstanciado != null;

        // Verifica se existe cutscene
        bool emCutscene =
            cutscenes != null &&
            cutscenes.EstaTocando;

        // Verifica se existe diálogo
        bool emDialogo =
            dialogoManager != null &&
            dialogoManager.TemDialogoAberto;

        // Só mostra quando o jogador estiver livre
        bool mostrar =
            jogadorExiste &&
            !emCutscene &&
            !emDialogo;

        // Evita chamar SetActive desnecessariamente
        if (estadoAnterior.HasValue &&
            estadoAnterior.Value == mostrar)
            return;

        estadoAnterior = mostrar;

        if (minimapa != null)
            minimapa.SetActive(mostrar);

        if (analogico != null)
            analogico.SetActive(mostrar);
    }
}