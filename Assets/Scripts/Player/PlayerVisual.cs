using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Transform jogador;
    [SerializeField] private Transform modelo;
    [SerializeField] private Animator animator;

    [Header("Animação")]
    [SerializeField] private float velocidadeMinima = 0.05f;
    [SerializeField] private float velocidadeReferencia = 5f;
    [SerializeField] private float suavizacaoAnimacao = 0.1f;

    [Header("Rotação")]
    [SerializeField] private float velocidadeRotacao = 12f;
    [SerializeField] private float ajusteRotacaoY = 0f;

    private Vector3 posicaoAnterior;

    private static readonly int VelocidadeID =
        Animator.StringToHash("Velocidade");

    void Awake()
    {
        if (jogador == null)
            jogador = transform.root;

        if (modelo == null)
            modelo = transform;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        posicaoAnterior = jogador.position;
    }

    void OnEnable()
    {
        if (jogador != null)
            posicaoAnterior = jogador.position;
    }

    void LateUpdate()
    {
        if (jogador == null)
            return;

        Vector3 deslocamento =
            jogador.position - posicaoAnterior;

        deslocamento.y = 0f;

        float velocidade = Time.deltaTime > 0f
            ? deslocamento.magnitude / Time.deltaTime
            : 0f;

        bool andando = velocidade > velocidadeMinima;

        // Atualiza a animação
        if (animator != null)
        {
            float valor = andando
                ? Mathf.Clamp01(
                    velocidade / Mathf.Max(velocidadeReferencia, 0.01f))
                : 0f;

            animator.SetFloat(
                VelocidadeID,
                valor,
                suavizacaoAnimacao,
                Time.deltaTime
            );
        }

        // Gira o modelo para a direção do deslocamento
        if (andando && modelo != null)
        {
            Quaternion rotacaoAlvo =
                Quaternion.LookRotation(deslocamento.normalized) *
                Quaternion.Euler(0f, ajusteRotacaoY, 0f);

            modelo.rotation = Quaternion.Slerp(
                modelo.rotation,
                rotacaoAlvo,
                1f - Mathf.Exp(
                    -velocidadeRotacao * Time.deltaTime)
            );
        }

        posicaoAnterior = jogador.position;
    }
}