using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float speed = 5f;
    [SerializeField] float gravidade = -20f;

    private JoystickMovimento joystick;
    private CharacterController controller;
    private float velocidadeY;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Start()
    {
        EncontrarJoystick();
    }

    void EncontrarJoystick()
    {
        GameObject objetoJoystick = GameObject.Find("Joystick");

        if (objetoJoystick != null)
        {
            joystick = objetoJoystick.GetComponent<JoystickMovimento>();

            Debug.Log("Joystick encontrado!");
        }
        else
        {
            Debug.LogWarning("Joystick não encontrado na cena!");
        }
    }

    void Update()
    {
        float h = 0f;
        float v = 0f;

        // JOYSTICK
        if (joystick != null)
        {
            h = joystick.Direcao.x;
            v = joystick.Direcao.y;
        }

        // TECLADO
        Keyboard teclado = Keyboard.current;

        if (teclado != null)
        {
            if (teclado.aKey.isPressed || teclado.leftArrowKey.isPressed)
                h -= 1f;

            if (teclado.dKey.isPressed || teclado.rightArrowKey.isPressed)
                h += 1f;

            if (teclado.sKey.isPressed || teclado.downArrowKey.isPressed)
                v -= 1f;

            if (teclado.wKey.isPressed || teclado.upArrowKey.isPressed)
                v += 1f;
        }

        Vector3 dir = DirecaoDaCamera(h, v);

        float dt = Mathf.Min(Time.deltaTime, 0.05f);

        velocidadeY = controller.isGrounded
            ? -1f
            : velocidadeY + gravidade * dt;

        Vector3 movimento =
            dir * speed +
            Vector3.up * velocidadeY;

        controller.Move(movimento * dt);
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