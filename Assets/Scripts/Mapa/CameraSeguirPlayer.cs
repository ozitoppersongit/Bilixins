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
    [SerializeField] float raioColisao = 0.3f;

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
        Vector3 posicaoDesejada = player.position - (rotacao * Vector3.forward * distancia);

        transform.position = EvitarParedes(player.position, posicaoDesejada);
        transform.rotation = rotacao;
    }

    // Lança um raio do player até a posição desejada da câmera. Se bater em algo no meio
    // do caminho (parede, prédio), para a câmera ali em vez de atravessar.
    Vector3 EvitarParedes(Vector3 origemJogador, Vector3 destino)
    {
        Vector3 direcao = destino - origemJogador;
        float distanciaTotal = direcao.magnitude;
        if (distanciaTotal < 0.01f) return destino;

        Vector3 dir = direcao / distanciaTotal;

        // Começa um pouco afastado do centro do player, pra não bater no próprio collider dele
        float margem = 0.5f;
        Vector3 origem = origemJogador + dir * margem;
        float restante = distanciaTotal - margem;

        if (restante > 0f && Physics.SphereCast(origem, raioColisao, dir, out RaycastHit hit, restante))
            return origem + dir * Mathf.Max(hit.distance - raioColisao, 0f);

        return destino;
    }

    int toqueCamera = -1;

Vector2 LerArrasto()
{
    if (Touchscreen.current != null)
    {
        var toques = Touchscreen.current.touches;

        // Procurar um toque que começou no lado direito
        for (int i = 0; i < toques.Count; i++)
        {
            var toque = toques[i];

            if (toque.press.wasPressedThisFrame)
            {
                Vector2 posicaoInicial = toque.position.ReadValue();

                if (posicaoInicial.x > Screen.width / 2f)
                {
                    toqueCamera = i;
                }
            }
        }

        // Se temos um toque controlando a câmera
        if (toqueCamera >= 0 && toqueCamera < toques.Count)
        {
            var toque = toques[toqueCamera];

            if (toque.press.isPressed)
                return toque.delta.ReadValue();

            toqueCamera = -1;
        }

        return Vector2.zero;
    }

    // PC continua funcionando normalmente
    if (Mouse.current != null)
        return Mouse.current.delta.ReadValue();

    return Vector2.zero;
}
}
