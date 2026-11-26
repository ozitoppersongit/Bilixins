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
    [Tooltip("Valor mínimo enviado ao Animator ao andar. Deve superar o limiar de entrada de Andar.")]
    [Range(0f, 1f)] public float valorMinimoAndando;
    public bool zerarAoParar;
    [Tooltip("Deslocamentos maiores que este valor num frame são reposicionamentos, não caminhada. Zero desativa.")]
    [Min(0f)] public float distanciaDeTeleporte;

    [Header("Rotação")]
    public bool girarAoAndar = true;
    public float velocidadeRotacao = 10f;
    public float ajusteRotacaoY = 0f;
    [Tooltip("Orienta o modelo no plano da raiz, preservando a inclinação animada em Corpo.")]
    public bool preservarPoseDoCorpo;

    private Vector3 posicaoAnterior;

    private static readonly int VelocidadeID =
        Animator.StringToHash("Velocidade");

    void Awake()
    {
        if (modelo == null)
            modelo = transform;

        // Corpo tem um Animator de cutscene; a caminhada pertence ao modelo filho.
        if (animator == null)
            animator = modelo.GetComponentInChildren<Animator>(true);

        posicaoAnterior = transform.position;
    }

    void OnEnable()
    {
        posicaoAnterior = transform.position;
        if (zerarAoParar && animator != null && animator.isInitialized)
            animator.SetFloat(VelocidadeID, 0f);
    }

    void LateUpdate()
    {
        AtualizarMovimento(Time.deltaTime);
    }

    void AtualizarMovimento(float deltaTime)
    {
        // Calcula quanto o NPC se moveu
        Vector3 deslocamento = transform.position - posicaoAnterior;
        deslocamento.y = 0f;

        float velocidade = deltaTime > 0f
            ? deslocamento.magnitude / deltaTime
            : 0f;

        bool teleporte = distanciaDeTeleporte > 0f && deslocamento.sqrMagnitude > distanciaDeTeleporte * distanciaDeTeleporte;
        bool andando = !teleporte && velocidade > velocidadeMinima;

        // Controla a animação
        if (animator != null)
        {
            float valor = andando
                ? Mathf.Clamp01(
                    Mathf.Max(valorMinimoAndando, velocidade / Mathf.Max(velocidadeReferencia, 0.01f)))
                : 0f;

            if (!andando && zerarAoParar) animator.SetFloat(VelocidadeID, 0f);
            else animator.SetFloat(
                VelocidadeID,
                valor,
                suavizacaoAnimacao,
                deltaTime
            );
        }

        // Faz o vilão olhar para onde está andando
        if (andando && girarAoAndar && modelo != null)
        {
            Quaternion rotacaoAlvo =
                Quaternion.LookRotation(deslocamento.normalized) *
                Quaternion.Euler(0f, ajusteRotacaoY, 0f);

            float mistura = 1f - Mathf.Exp(-velocidadeRotacao * deltaTime);
            if (preservarPoseDoCorpo)
                modelo.localRotation = Quaternion.Slerp(modelo.localRotation,
                    Quaternion.Inverse(transform.rotation) * rotacaoAlvo, mistura);
            else modelo.rotation = Quaternion.Slerp(modelo.rotation, rotacaoAlvo, mistura);
        }

        posicaoAnterior = transform.position;
    }
}
