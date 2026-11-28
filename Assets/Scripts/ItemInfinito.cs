using UnityEngine;

// Apenas as instâncias do infinito recebem este componente; a âncora de coleta não se move.
public class ItemInfinito : MonoBehaviour
{
    Transform visual;
    Animator animador;
    Vector3 posicaoInicial;
    float tempoVisual;
    ParticleSystem[] particulas;

    public void Configurar(Transform modelo, Animator animacao, Material brilho)
    {
        visual = modelo;
        animador = animacao;
        posicaoInicial = visual.localPosition;

        var efeito = CriarBrilho(transform, brilho, new Color(1f, 0.92f, 0.5f, 0.8f));
        efeito.Pause();
        particulas = new[] { efeito };
    }

    internal static ParticleSystem CriarBrilho(Transform pai, Material material, Color corInicial)
    {
        var objeto = new GameObject("Brilho do coletável");
        objeto.transform.SetParent(pai, false);
        objeto.transform.localPosition = Vector3.up * 0.9f;
        objeto.SetActive(false);
        var efeito = objeto.AddComponent<ParticleSystem>();
        var main = efeito.main;
        main.playOnAwake = false;
        main.loop = true;
        main.maxParticles = 12;
        main.startLifetime = 1.2f;
        main.startSpeed = 0.18f;
        main.startSize = 0.09f;
        main.startColor = corInicial;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.useUnscaledTime = true;
        var emission = efeito.emission;
        emission.rateOverTime = 4f;
        var shape = efeito.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.4f;
        var cor = efeito.colorOverLifetime;
        cor.enabled = true;
        var gradiente = new Gradient();
        gradiente.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
        cor.color = gradiente;
        efeito.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
        objeto.SetActive(true);
        efeito.Play();
        return efeito;
    }

    public void ConfigurarRajada()
    {
        particulas = GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem efeito in particulas)
        {
            var main = efeito.main;
            main.useUnscaledTime = true;
        }
    }

    void Update()
    {
        bool podeAnimar = ModoInfinito.Instance != null && ModoInfinito.Instance.PodeAnimarEfeitos;
        if (animador != null) animador.speed = podeAnimar ? 1f : 0f;
        if (visual != null && animador == null && podeAnimar)
        {
            tempoVisual += Time.deltaTime;
            visual.Rotate(Vector3.up, 60f * Time.deltaTime, Space.Self);
            visual.localPosition = posicaoInicial + Vector3.up * (0.08f - 0.08f * Mathf.Cos(tempoVisual * Mathf.PI / 1.5f));
        }
        if (particulas == null) return;
        foreach (ParticleSystem efeito in particulas)
        {
            if (efeito == null) continue;
            if (!podeAnimar && efeito.isPlaying) efeito.Pause();
            else if (podeAnimar && efeito.isPaused) efeito.Play();
        }
    }
}
