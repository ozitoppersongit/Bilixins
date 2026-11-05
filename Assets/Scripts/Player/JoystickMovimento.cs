using UnityEngine;
using UnityEngine.EventSystems;

public class JoystickMovimento : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Referências")]
    public RectTransform fundo;
    public RectTransform handle;

    [Header("Configuração")]
    public float raio = 100f;

    public Vector2 Direcao { get; private set; }

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

        Direcao = Vector2.ClampMagnitude(posicao / raio, 1f);

        handle.anchoredPosition = Direcao * raio;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Direcao = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
    }
}
