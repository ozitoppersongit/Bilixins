
using UnityEngine;
using UnityEngine.InputSystem;

// Orbita ao redor do player.
// PC: mouse.
// Mobile: arrastar somente o lado direito da tela.
public class CameraSeguirPlayer : MonoBehaviour
{
    [SerializeField] MapaGerador mapaGerador;
    [SerializeField] float distancia = 10f;
    [SerializeField] float sensibilidade = 0.2f;
    [SerializeField] float anguloMinimo = -9f;
    [SerializeField] float anguloMaximo = 60f;
    [SerializeField] float anguloInicial = 35f;
    [SerializeField] float raioColisao = 0.3f;

    [Tooltip("Ponto que a câmera mira acima do centro do player.")]
    [SerializeField] Vector3 pivo = Vector3.zero;

    Transform player;

    float rotX;
    float rotY;

    [Header("Controle Mobile da Câmera")]
    private int dedoCamera = -1;
    private Vector2 deltaCameraMobile;

    void Awake()
    {
        rotY = anguloInicial;

        if (Touchscreen.current == null)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void OnEnable()
    {
        if (mapaGerador != null)
            mapaGerador.aoTerminarDeGerar.AddListener(PegarPlayer);
    }

    void OnDisable()
    {
        if (mapaGerador != null)
            mapaGerador.aoTerminarDeGerar.RemoveListener(PegarPlayer);
    }

    void PegarPlayer()
    {
        if (mapaGerador.PlayerInstanciado != null)
            player = mapaGerador.PlayerInstanciado.transform;
    }

    void LateUpdate()
    {
        if (player == null)
            return;

        Vector2 delta = Vector2.zero;

        // Mobile
        if (Touchscreen.current != null)
        {
            delta = LerArrastoMobile();
        }
        // PC
        else if (Mouse.current != null)
        {
            delta = Mouse.current.delta.ReadValue();
        }

        delta *= sensibilidade;

        rotX += delta.x;
        rotY -= delta.y;

        rotY = Mathf.Clamp(rotY, anguloMinimo, anguloMaximo);

        Quaternion rotacao = Quaternion.Euler(rotY, rotX, 0);

        Vector3 alvo = player.position + pivo;

        Vector3 posicaoDesejada =
            alvo - (rotacao * Vector3.forward * distancia);

        transform.position = EvitarParedes(alvo, posicaoDesejada);
        transform.rotation = rotacao;
    }

    Vector2 LerArrastoMobile()
    {
        deltaCameraMobile = Vector2.zero;

        if (Touchscreen.current == null)
            return Vector2.zero;

        foreach (var toque in Touchscreen.current.touches)
        {
            int id = toque.touchId.ReadValue();

            Vector2 posicao = toque.position.ReadValue();

            var fase = toque.phase.ReadValue();

            // O dedo precisa começar no lado direito.
            if (fase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                if (posicao.x > Screen.width / 2f && dedoCamera == -1)
                {
                    dedoCamera = id;
                }
            }

            // Somente o dedo registrado controla a câmera.
            if (id == dedoCamera)
            {
                if (fase == UnityEngine.InputSystem.TouchPhase.Moved)
                {
                    deltaCameraMobile = toque.delta.ReadValue();
                }

                if (fase == UnityEngine.InputSystem.TouchPhase.Ended ||
                    fase == UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    dedoCamera = -1;
                }
            }
        }

        return deltaCameraMobile;
    }

    Vector3 EvitarParedes(Vector3 origemJogador, Vector3 destino)
    {
        Vector3 direcao = destino - origemJogador;

        float distanciaTotal = direcao.magnitude;

        if (distanciaTotal < 0.01f)
            return destino;

        Vector3 dir = direcao / distanciaTotal;

        float margem = 0.5f;

        Vector3 origem = origemJogador + dir * margem;

        float restante = distanciaTotal - margem;

        if (restante > 0f &&
            Physics.SphereCast(
                origem,
                raioColisao,
                dir,
                out RaycastHit hit,
                restante))
        {
            return origem + dir * Mathf.Max(
                hit.distance - raioColisao,
                0f
            );
        }

        return destino;
    }
}
