
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimentação")]
    [SerializeField] float speed = 5f;
    public float VelocidadeNormal => speed;
    [SerializeField] float gravidade = -20f;

    [Header("Corrida (Shift)")]
    [SerializeField] float corridaMultiplicador = 1.5f;

    [Header("Animação")]
    [SerializeField] private Animator animator;
    [SerializeField] private float suavizacaoAnimacao = 0.1f;
    [SerializeField] private float velocidadeMinimaAnimacao = 0.05f;

    private Vector3 posicaoAnterior;
    private static readonly int VelocidadeAnim =
        Animator.StringToHash("Velocidade");

    CharacterController controller;

    [Header("Controles Mobile")]
    JoystickMovimento joystick;

    float velocidadeY;

    bool correndo;
    public bool CorridaLigada { get; private set; }

void AtualizarAnimacao()
{
    if (animator == null)
        return;

    Vector3 deslocamento = transform.position - posicaoAnterior;
    deslocamento.y = 0f;

    float velocidadeReal = Time.deltaTime > 0f
        ? deslocamento.magnitude / Time.deltaTime
        : 0f;

    float velocidadeNormalizada =
        velocidadeReal > velocidadeMinimaAnimacao
        ? Mathf.Clamp01(velocidadeReal / Mathf.Max(speed, 0.01f))
        : 0f;

    animator.SetFloat(
        VelocidadeAnim,
        velocidadeNormalizada,
        suavizacaoAnimacao,
        Time.deltaTime
    );

    animator.speed = EmMovimentoRapido ? 1.5f : 1f;
}

    void ProcurarJoystick()
{
    JoystickMovimento[] joysticks =
        FindObjectsByType<JoystickMovimento>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

    foreach (JoystickMovimento j in joysticks)
    {
        if (j == null)
            continue;

        // Prioriza o analógico que está ativo na cena
        if (j.gameObject.activeInHierarchy)
        {
            joystick = j;
            Debug.Log("[PlayerMovement] Analógico ativo encontrado: " + j.name);
            return;
        }
    }

    // Se não houver nenhum ativo, guarda o primeiro encontrado
    if (joysticks.Length > 0)
    {
        joystick = joysticks[0];
        Debug.Log("[PlayerMovement] Analógico encontrado, mas está inativo.");
    }
}

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        ProcurarJoystick();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        posicaoAnterior = transform.position;
    }

    void LateUpdate()
    {
        AtualizarAnimacao();
        posicaoAnterior = transform.position;
    }

    void OnEnable()
    {
        posicaoAnterior = transform.position;
    }

    void Update()
    {
        if (CarregamentoPartida.EmAndamento) return;
        if (ModoInfinito.Instance != null && !ModoInfinito.Instance.PodeExplorar) return;
        if (joystick == null || !joystick.gameObject.activeInHierarchy)
        {
            ProcurarJoystick();
        }
        
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
                AlternarCorrida();
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

        Vector3 movimento = direcao * (CorridaLigada ? speed * corridaMultiplicador : speed);
        movimento.y = velocidadeY;

        Vector3 antes = transform.position;
        controller.Move(movimento * Time.deltaTime);
        Vector3 deslocamento = transform.position - antes;
        deslocamento.y = 0f;
        correndo = CorridaLigada && direcao.sqrMagnitude > 0f && deslocamento.sqrMagnitude > 0f;
    }

    public bool EmMovimentoRapido => isActiveAndEnabled && Time.timeScale > 0f && correndo;

    public void AlternarCorrida()
    {
        if (isActiveAndEnabled && Time.timeScale > 0f)
            CorridaLigada = !CorridaLigada;
    }

    void OnDisable()
    {
        correndo = false;
        if (animator != null) animator.speed = 1f;
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
