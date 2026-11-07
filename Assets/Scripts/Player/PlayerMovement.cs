
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimentação")]
    [SerializeField] float speed = 5f;
    [SerializeField] float gravidade = -20f;

    [Header("Dash / Corrida (Shift)")]
    [SerializeField] int dashCargasMax = 5;          // quantos dashs seguidos
    [SerializeField] float dashRecarga = 1.5f;       // segundos para voltar 1 carga
    [SerializeField] float dashDuracao = 0.25f;      // tempo do impulso
    [SerializeField] float dashDistancia = 6f;       // metros percorridos no impulso
    [SerializeField] float corridaMultiplicador = 1.5f; // velocidade da corrida depois do dash

    CharacterController controller;

    [Header("Controles Mobile")]
    JoystickMovimento joystick;

    float velocidadeY;

    int dashCargas;
    float dashTimerRecarga;
    float dashTempoRestante;
    Vector3 dashDirecao;
    Vector3 ultimaDirecao = Vector3.forward;
    bool correndo;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        joystick = FindAnyObjectByType<JoystickMovimento>();
        dashCargas = dashCargasMax;
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

            if (Keyboard.current.leftShiftKey.wasPressedThisFrame)
                IniciarDash();
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
        if (direcao.sqrMagnitude > 0.01f) ultimaDirecao = direcao;

        // Recarga das cargas de dash (1 a cada dashRecarga segundos)
        if (dashCargas < dashCargasMax)
        {
            dashTimerRecarga += Time.deltaTime;
            if (dashTimerRecarga >= dashRecarga)
            {
                dashTimerRecarga = 0f;
                dashCargas++;
                Debug.Log($"[Dash] +1 carga recarregada ({dashCargas}/{dashCargasMax})");
            }
        }

        // Gravidade
        if (controller.isGrounded && velocidadeY < 0)
        {
            velocidadeY = -2f;
        }

        velocidadeY += gravidade * Time.deltaTime;

        Vector3 movimento;

        if (dashTempoRestante > 0f)
        {
            // Impulso do dash
            movimento = dashDirecao * (dashDistancia / dashDuracao);
            dashTempoRestante -= Time.deltaTime;

            // No fim do dash, se o jogador ainda está andando, continua correndo
            if (dashTempoRestante <= 0f)
            {
                correndo = direcao.sqrMagnitude > 0.01f;
                Debug.Log(correndo ? "[Corrida] dash terminou, continua correndo" : "[Dash] terminou, sem input: passo normal");
            }
        }
        else
        {
            // Parou de andar: a corrida acaba e volta ao passo normal
            if (direcao.sqrMagnitude < 0.01f)
            {
                if (correndo) Debug.Log("[Corrida] terminou (parou de andar), voltou ao passo normal");
                correndo = false;
            }

            movimento = direcao * (correndo ? speed * corridaMultiplicador : speed);
        }

        movimento.y = velocidadeY;

        controller.Move(movimento * Time.deltaTime);
    }

    // A câmera usa isto para o efeito de velocidade (FOV/distância)
    public bool EmMovimentoRapido => dashTempoRestante > 0f || correndo;

    /// oziel, aqui é o dash/corrida
    public void IniciarDash()
    {
        if (dashCargas <= 0)
        {
            Debug.Log($"[Dash] sem cargas (0/{dashCargasMax}), próxima em {dashRecarga - dashTimerRecarga:0.0}s");
            return;
        }

        dashCargas--;
        if (dashCargas == dashCargasMax - 1) dashTimerRecarga = 0f;

        dashDirecao = ultimaDirecao;
        dashTempoRestante = dashDuracao;

        Debug.Log($"[Dash] dash! cargas restantes: {dashCargas}/{dashCargasMax}");
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
