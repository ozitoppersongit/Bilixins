using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class BotaoDash : MonoBehaviour
{
    [Header("Referências")]
    public MapaGerador mapaGerador;

    private PlayerMovement playerMovement;
    private TMP_Text rotulo;
    private Color corNormal;
    private Button botao;

    void Start()
    {
        if (mapaGerador == null)
            mapaGerador = FindAnyObjectByType<MapaGerador>();
        rotulo = GetComponentInChildren<TMP_Text>(true);
        if (rotulo != null)
            corNormal = rotulo.color;
        botao = GetComponent<Button>();
        botao.onClick.AddListener(AlternarCorrida);
    }

    void Update()
    {
        GameObject player = mapaGerador != null ? mapaGerador.PlayerInstanciado : null;
        if (playerMovement == null || playerMovement.gameObject != player)
            playerMovement = player != null ? player.GetComponent<PlayerMovement>() : null;

        if (rotulo != null)
            rotulo.color = playerMovement != null && playerMovement.CorridaLigada
                ? new Color(0.55f, 1f, 0.55f)
                : corNormal;
    }

    void AlternarCorrida()
    {
        if (playerMovement != null)
            playerMovement.AlternarCorrida();
    }

    void OnDestroy()
    {
        if (botao != null)
            botao.onClick.RemoveListener(AlternarCorrida);
    }
}
