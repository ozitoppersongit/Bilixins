using UnityEngine;
using UnityEngine.EventSystems;

public class BotaoDash : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler
{
    [Header("Referências")]
    public MapaGerador mapaGerador;

    private PlayerMovement playerMovement;
    private bool segurando;

    void Start()
    {
        if (mapaGerador == null)
            mapaGerador = FindAnyObjectByType<MapaGerador>();
    }

    void Update()
    {
        if (playerMovement == null &&
            mapaGerador != null &&
            mapaGerador.PlayerInstanciado != null)
        {
            playerMovement =
                mapaGerador.PlayerInstanciado
                .GetComponent<PlayerMovement>();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (playerMovement == null ||
            !playerMovement.isActiveAndEnabled)
            return;

        segurando = true;

        // Mesmo comportamento de apertar Shift
        playerMovement.IniciarDash();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        segurando = false;
    }

    void OnDisable()
    {
        segurando = false;
    }
}