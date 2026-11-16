using UnityEngine;

public class AnimacaoNPC : MonoBehaviour
{
    [Header("Referências")]
    public Animator animator;
    public Transform modelo;

    [Header("Animação")]
    public float velocidadeMinima = 0.05f;
    public float velocidadeReferencia = 3f;
    public float suavizacaoAnimacao = 0.1f;

    [Header("Rotação")]
    public bool girarAoAndar = true;
    public float velocidadeRotacao = 10f;
    public float ajusteRotacaoY = 0f;

    private Vector3 posicaoAnterior;

    private static readonly int VelocidadeID =
        Animator.StringToHash("Velocidade");

    void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (modelo == null)
            modelo = transform;

        posicaoAnterior = transform.position;
    }

    void OnEnable()
    {
        posicaoAnterior = transform.position;
    }

    void LateUpdate()
    {
        // Calcula quanto o NPC se moveu
        Vector3 deslocamento = transform.position - posicaoAnterior;
        deslocamento.y = 0f;

        float velocidade = Time.deltaTime > 0f
            ? deslocamento.magnitude / Time.deltaTime
            : 0f;

        bool andando = velocidade > velocidadeMinima;

        // Controla a animação
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

        // Faz o vilão olhar para onde está andando
        if (andando && girarAoAndar && modelo != null)
        {
            Quaternion rotacaoAlvo =
                Quaternion.LookRotation(deslocamento.normalized) *
                Quaternion.Euler(0f, ajusteRotacaoY, 0f);

            modelo.rotation = Quaternion.Slerp(
                modelo.rotation,
                rotacaoAlvo,
                1f - Mathf.Exp(-velocidadeRotacao * Time.deltaTime)
            );
        }

        posicaoAnterior = transform.position;
    }
}