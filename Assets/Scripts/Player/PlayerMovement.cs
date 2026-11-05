
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimentação")]
    [SerializeField] float speed = 5f;
    [SerializeField] float gravidade = -20f;

    CharacterController controller;

    [Header("Controles Mobile")]
    JoystickMovimento joystick;

    float velocidadeY;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        joystick = FindAnyObjectByType<JoystickMovimento>();
    }

    void Update()
    {
        // Leitura do teclado
        Vector2 inputTeclado = Vector2.zero;

        if (Keyboard.current != null)
        {
            inputTeclado.x =
                (Keyboard.current.dKey.isPressed ? 1 : 0) -
                (Keyboard.current.aKey.isPressed ? 1 : 0);

            inputTeclado.y =
                (Keyboard.current.wKey.isPressed ? 1 : 0) -
                (Keyboard.current.sKey.isPressed ? 1 : 0);
        }

        // Leitura do analógico
        Vector2 input = inputTeclado;

        if (joystick != null && joystick.Direcao != Vector2.zero)
        {
            input = joystick.Direcao;
        }

        // Evita velocidade maior na diagonal
        input = Vector2.ClampMagnitude(input, 1f);

        // Movimento relativo à câmera
        Vector3 direcao = DirecaoDaCamera(input.x, input.y);

        // Gravidade
        if (controller.isGrounded && velocidadeY < 0)
        {
            velocidadeY = -2f;
        }

        velocidadeY += gravidade * Time.deltaTime;

        Vector3 movimento = direcao * speed;
        movimento.y = velocidadeY;

        controller.Move(movimento * Time.deltaTime);
    }

    Vector3 DirecaoDaCamera(float h, float v)
    {
        Camera cam = Camera.main;

        if (cam == null)
            return new Vector3(h, 0f, v).normalized;

        Vector3 frente = cam.transform.forward;
        Vector3 direita = cam.transform.right;

        frente.y = 0f;
        direita.y = 0f;

        frente.Normalize();
        direita.Normalize();

        return (direita * h + frente * v).normalized;
    }
}
