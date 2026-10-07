using UnityEngine;
using UnityEngine.EventSystems;

public class JoystickMovimento : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [Header("Referências")]
    [SerializeField] private RectTransform fundo;
    [SerializeField] private RectTransform bolinha;

    [Header("Configuração")]
    [SerializeField] private float raio = 100f;

    private Vector2 direcao;

    public Vector2 Direcao => direcao;

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 posicao;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            fundo,
            eventData.position,
            eventData.pressEventCamera,
            out posicao
        );

        posicao = Vector2.ClampMagnitude(posicao, raio);

        bolinha.anchoredPosition = posicao;

        direcao = posicao / raio;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        direcao = Vector2.zero;
        bolinha.anchoredPosition = Vector2.zero;
    }
}