using UnityEngine;

public class DialogoManager : MonoBehaviour
{
    public static DialogoManager Instance { get; private set; }

    [Header("UI")]
    public GameObject prefabDialogo;
    public Transform canvas;

    [Header("Diálogos")]
    public DialogoData[] dialogos;

    private DialogoUI dialogoAberto;

    

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

        void OnEnable()
    {
        if (Cutscenes.Instance != null)
        {
            Cutscenes.Instance.aoPedirDialogo += AbrirDialogo;
        }
    }

    void OnDisable()
    {
        if (Cutscenes.Instance != null)
        {
            Cutscenes.Instance.aoPedirDialogo -= AbrirDialogo;
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void AbrirDialogo(string id)
    {
        if (dialogoAberto != null)
        {
            Debug.LogWarning("Já existe um diálogo aberto.");
            return;
        }

        DialogoData dados = null;

        foreach (DialogoData d in dialogos)
        {
            if (d != null && d.id == id)
            {
                dados = d;
                break;
            }
        }

        if (dados == null)
        {
            Debug.LogError(
                "DialogoManager: não encontrou o diálogo com ID " + id
            );

            Cutscenes.Instance.DialogoTerminado();
            return;
        }

        GameObject objeto = Instantiate(
            prefabDialogo,
            canvas
        );

        dialogoAberto = objeto.GetComponent<DialogoUI>();

        if (dialogoAberto == null)
        {
            Debug.LogError(
                "O prefab de diálogo não possui DialogoUI."
            );

            Destroy(objeto);
            Cutscenes.Instance.DialogoTerminado();
            return;
        }

        dialogoAberto.Iniciar(dados);
    }
}
