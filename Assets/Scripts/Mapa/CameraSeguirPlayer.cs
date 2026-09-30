using UnityEngine;
using UnityEngine.InputSystem;

// Orbita ao redor do player, arrastando o dedo (celular) ou o mouse (Editor).
// Liga no player assim que ele nasce, já que antes disso ele não existe.
public class CameraSeguirPlayer : MonoBehaviour
{
    [SerializeField] MapaGerador mapaGerador;
    [SerializeField] float distancia = 10f;
    [SerializeField] float sensibilidade = 0.2f;
    [SerializeField] float anguloMinimo = -9f;
    [SerializeField] float anguloMaximo = 60f;
    [SerializeField] float anguloInicial = 35f;

    Transform player;
    float rotX;
    float rotY;

    void Awake()
    {
        rotY = anguloInicial;

        // No PC o mouse gira a câmera direto, sem precisar segurar botão (igual FPS).
        // No celular não existe "mouse passando por cima", então ali continua sendo arrasto com o dedo.
        if (Touchscreen.current == null)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void OnEnable()
    {
        mapaGerador.aoTerminarDeGerar.AddListener(PegarPlayer);
    }

    void OnDisable()
    {
        mapaGerador.aoTerminarDeGerar.RemoveListener(PegarPlayer);
    }

    void PegarPlayer()
    {
        if (mapaGerador.PlayerInstanciado != null)
            player = mapaGerador.PlayerInstanciado.transform;
    }

    void LateUpdate()
    {
        if (player == null) return;

        Vector2 delta = LerArrasto() * sensibilidade;
        rotX += delta.x;
        rotY -= delta.y;
        rotY = Mathf.Clamp(rotY, anguloMinimo, anguloMaximo);

        Quaternion rotacao = Quaternion.Euler(rotY, rotX, 0);
        transform.position = player.position - (rotacao * Vector3.forward * distancia);
        transform.rotation = rotacao;
    }

    Vector2 LerArrasto()
    {
        if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
        {
            var toque = Touchscreen.current.primaryTouch;
            return toque.press.isPressed ? toque.delta.ReadValue() : Vector2.zero;
        }

        if (Mouse.current != null)
            return Mouse.current.delta.ReadValue();

        return Vector2.zero;
    }
}
