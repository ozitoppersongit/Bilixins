using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Bilixin de metal (Ferralha). O Animator fica na raiz do prefab (centro da ronda); este script e o gatilho de batalha ficam no filho "Monstro", que anda em círculo.
// As animações estão no Animator: "Ronda" (anda em círculo, cauda balançando e picando de vez em quando),
// "Ataque" (vira para o player, cauda recua e bate; no fim a batalha abre) e "Derrotado" (desaba, a cauda espeta o corpo e vira pó).
// Este script só dispara esses estados nos momentos certos e diz ao GatilhoDeEncontro quanto tempo esperar.
public class Ferralha : MonoBehaviour, IDerrotaAnimada, IAntesDaBatalha
{
    [SerializeField] Animator animator;
    [SerializeField] string estadoRonda = "Ronda";
    [SerializeField] string estadoAtaque = "Ataque";
    [SerializeField] string estadoDerrota = "Derrotado";
    [Tooltip("Segundos a mais depois do clipe, para o pó terminar de sumir.")]
    [SerializeField] float tempoExtra = 0.2f;
    [Tooltip("Opcional: o que mais acontece quando ela é derrotada (som, câmera...).")]
    public UnityEvent aoDerrotar;
    [Tooltip("Opcional: o que mais acontece quando ela começa o ataque, antes da batalha.")]
    public UnityEvent aoAtacar;

    bool derrotada;

    void Reset()
    {
        animator = GetComponentInParent<Animator>();
    }

    // ---------- antes da batalha: encara o player e ataca; a batalha abre quando o ataque termina ----------
    public float IniciarAntesDaBatalha(Transform player)
    {
        if (derrotada) return 0f;
        if (animator == null) animator = GetComponentInParent<Animator>();
        if (animator == null) return 0f;

        aoAtacar?.Invoke();
        animator.CrossFadeInFixedTime(estadoAtaque, 0.25f);
        if (player != null) Encarar(player);
        return DuracaoDoClipe(estadoAtaque) + 0.1f;
    }

    public void VoltarAoNormal()
    {
        if (derrotada || animator == null) return;
        encarando = false;   // volta a obedecer a ronda
        animator.CrossFadeInFixedTime(estadoRonda, 0.5f);
    }

    // O Animator reescreve a rotação do Monstro a cada quadro (a ronda anima o giro), então o script guarda o próprio ângulo
    // e aplica depois do Animator, em LateUpdate, girando até o player.
    bool encarando;
    Quaternion rotAtual, rotAlvo;

    void Encarar(Transform player)
    {
        Vector3 dir = player.position - transform.position; dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        rotAlvo = Quaternion.LookRotation(dir);
        rotAtual = transform.rotation;
        encarando = true;
    }

    void LateUpdate()
    {
        if (!encarando) return;
        rotAtual = Quaternion.RotateTowards(rotAtual, rotAlvo, 420f * Time.deltaTime);
        transform.rotation = rotAtual;
    }

    // ---------- derrota ----------
    public float IniciarDerrota()
    {
        if (derrotada) return 0f;
        derrotada = true;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;   // não encosta mais no player enquanto desmonta

        aoDerrotar?.Invoke();
        if (animator == null) animator = GetComponentInParent<Animator>();
        if (animator == null) return 0f;

        animator.Play(estadoDerrota, 0, 0f);
        return DuracaoDoClipe(estadoDerrota) + tempoExtra;
    }

    float DuracaoDoClipe(string nome)
    {
        if (animator.runtimeAnimatorController == null) return 0f;
        foreach (AnimationClip c in animator.runtimeAnimatorController.animationClips)
            if (c.name == nome) return c.length;
        return 0f;
    }
}
