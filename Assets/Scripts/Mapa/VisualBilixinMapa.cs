using System;
using System.Collections.Generic;
using UnityEngine;

// Mantém o movimento da malha separado dos gatilhos, da posição do encontro e das cutscenes.
[DefaultExecutionOrder(1000)]
public sealed class VisualBilixinMapa : MonoBehaviour
{
    [Serializable]
    public class Configuracao
    {
        public GameObject modelo;
        public RuntimeAnimatorController controlador;
        public string ancoraAnimada;
        [Min(0.01f)] public float proporcaoBoladro = 1f;
    }

    [SerializeField] Configuracao visual;
    [SerializeField] Transform boladro;
    [SerializeField] Transform corpoCutscene;
    [SerializeField] bool ocultarAoIniciar;

    static readonly int Susto = Animator.StringToHash("Susto");
    static readonly int Surgir = Animator.StringToHash("Surgir");
    static readonly int LoopMapa = Animator.StringToHash("Base Layer.LoopMapa");
    readonly List<Vector3> vertices = new List<Vector3>();
    readonly RaycastHit[] pisos = new RaycastHit[32];
    Transform suporte;
    Transform ancoraAnimada;
    Renderer[] partes;
    Mesh malha;
    Animator animadorCutscene;
    Vector3 escalaVisual;
    Vector3 escalaCorpo;
    float? pisoSpawn;
    float alturaPiso;
    bool sendoSolto;
    Quaternion rotacaoDoMovimento;
    int quadroDoMovimento = -1;

    void Awake()
    {
        if (visual == null || visual.modelo == null) return;
        if (corpoCutscene != null)
        {
            animadorCutscene = GetComponent<Animator>();
            escalaCorpo = corpoCutscene.localScale;
        }
        if (Montar(visual, boladro) && ocultarAoIniciar) Mostrar(false);
    }

    public static bool InstalarNoSpawn(GameObject encontro, Configuracao configuracao, Transform boladro, float piso)
    {
        if (configuracao == null || configuracao.modelo == null) return false;
        var visual = encontro.AddComponent<VisualBilixinMapa>();
        visual.pisoSpawn = piso;
        if (visual.Montar(configuracao, boladro)) return true;
        Destroy(visual);
        return false;
    }

    public bool Mostrar(bool mostrar)
    {
        if (suporte == null) return false;
        foreach (Renderer parte in partes) parte.enabled = mostrar;
        return true;
    }

    public void Exibir() => Mostrar(true);

    public void OrientarMovimento(Quaternion rotacao)
    {
        rotacaoDoMovimento = rotacao;
        quadroDoMovimento = Time.frameCount;
    }

    public void PrepararLancamento()
    {
        sendoSolto = true;
    }

    public void AssentarAposLancamento()
    {
        sendoSolto = false;
        pisoSpawn = null;
        if (suporte != null) AjustarApoio();
    }

    bool Montar(Configuracao configuracao, Transform referencia)
    {
        if (referencia == null || configuracao.controlador == null) return false;
        Renderer[] provisorios = GetComponentsInChildren<Renderer>(true);
        suporte = new GameObject("Visual do Bilixin").transform;
        suporte.SetParent(transform, false);
        if (!string.IsNullOrEmpty(configuracao.ancoraAnimada))
        {
            ancoraAnimada = new GameObject(configuracao.ancoraAnimada).transform;
            ancoraAnimada.SetParent(suporte, false);
        }
        GameObject modelo = Instantiate(configuracao.modelo, ancoraAnimada != null ? ancoraAnimada : suporte);
        foreach (Collider collider in modelo.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
        GameObject raizAnimada = ancoraAnimada != null ? suporte.gameObject : modelo;
        var animador = raizAnimada.GetComponent<Animator>();
        if (animador == null) animador = raizAnimada.AddComponent<Animator>();
        animador.applyRootMotion = false;
        animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animador.runtimeAnimatorController = configuracao.controlador;
        partes = modelo.GetComponentsInChildren<Renderer>();
        malha = new Mesh();
        animador.Rebind();
        if (animador.HasState(0, LoopMapa)) animador.Play(LoopMapa, 0, 0f);
        animador.Update(0f);

        MedirVisual(out float baseModelo, out float topoModelo);
        float baseReferencia = float.PositiveInfinity;
        float topoReferencia = float.NegativeInfinity;
        foreach (Renderer parte in referencia.GetComponentsInChildren<Renderer>(true))
        {
            baseReferencia = Mathf.Min(baseReferencia, parte.bounds.min.y);
            topoReferencia = Mathf.Max(topoReferencia, parte.bounds.max.y);
        }
        if (!(topoModelo - baseModelo > 0.0001f) || !(topoReferencia > baseReferencia))
        {
            Destroy(suporte.gameObject);
            suporte = null;
            Debug.LogWarning("Não foi possível medir o modelo do Bilixin ou o Boladro de referência.", this);
            return false;
        }

        suporte.localScale *= (topoReferencia - baseReferencia) * configuracao.proporcaoBoladro / (topoModelo - baseModelo);
        escalaVisual = suporte.localScale;
        alturaPiso = pisoSpawn ?? transform.position.y - (topoReferencia - baseReferencia) * 0.5f;
        AjustarApoio();
        foreach (Renderer parte in provisorios)
            if (parte is MeshRenderer) parte.enabled = false;
        return true;
    }

    void LateUpdate()
    {
        if (suporte != null && suporte.gameObject.activeSelf && !sendoSolto) AjustarApoio();
    }

    void AjustarApoio()
    {
        if (!pisoSpawn.HasValue)
        {
            int total = Physics.RaycastNonAlloc(transform.position + Vector3.up * 0.15f, Vector3.down,
                pisos, 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float distancia = float.PositiveInfinity;
            for (int i = 0; i < total; i++)
            {
                RaycastHit hit = pisos[i];
                if (hit.normal.y <= 0.55f || hit.transform.IsChildOf(transform) ||
                    hit.collider is CharacterController || hit.distance >= distancia) continue;
                distancia = hit.distance;
                alturaPiso = hit.point.y;
            }
        }

        float salto = ancoraAnimada != null ? suporte.TransformVector(ancoraAnimada.localPosition).y : 0f;
        if (animadorCutscene != null)
        {
            // O Animator original ainda recebe Play("Susto") e Play("Abana") da cena.
            // Abana anima a raiz. Susto anima a inclinação e o salto de Corpo.
            int estado = animadorCutscene.GetCurrentAnimatorStateInfo(0).shortNameHash;
            bool susto = estado == Susto;
            suporte.localScale = estado == Surgir
                ? Vector3.Scale(escalaVisual, new Vector3(
                    Mathf.Max(0.001f, corpoCutscene.localScale.x / escalaCorpo.x),
                    Mathf.Max(0.001f, corpoCutscene.localScale.y / escalaCorpo.y),
                    Mathf.Max(0.001f, corpoCutscene.localScale.z / escalaCorpo.z)))
                : escalaVisual;
            suporte.localRotation = susto ? Quaternion.Euler(0f, 0f, corpoCutscene.localEulerAngles.z) : Quaternion.identity;
            if (susto) salto = Mathf.Max(0f, corpoCutscene.localPosition.y) * transform.lossyScale.y;
        }
        if (quadroDoMovimento == Time.frameCount)
            suporte.rotation = rotacaoDoMovimento;
        MedirVisual(out float baseModelo, out _);
        suporte.position += Vector3.up * (alturaPiso + salto + 0.005f - baseModelo);
    }

    void MedirVisual(out float baseModelo, out float topoModelo)
    {
        baseModelo = float.PositiveInfinity;
        topoModelo = float.NegativeInfinity;
        foreach (Renderer parte in partes)
        {
            Bounds limites = MedirLimites(parte);
            baseModelo = Mathf.Min(baseModelo, limites.min.y);
            topoModelo = Mathf.Max(topoModelo, limites.max.y);
        }
    }

    public Bounds MedirLimites(Renderer parte)
    {
        if (!(parte is SkinnedMeshRenderer deformada) || malha == null) return parte.bounds;
        // A caixa de renderização do rig pode avançar abaixo do piso mesmo com a malha apoiada.
        deformada.BakeMesh(malha, true);
        malha.GetVertices(vertices);
        if (vertices.Count == 0) return parte.bounds;
        Matrix4x4 mundo = deformada.localToWorldMatrix;
        Vector3 minimo = mundo.MultiplyPoint3x4(vertices[0]);
        Vector3 maximo = minimo;
        for (int i = 1; i < vertices.Count; i++)
        {
            Vector3 ponto = mundo.MultiplyPoint3x4(vertices[i]);
            minimo = Vector3.Min(minimo, ponto);
            maximo = Vector3.Max(maximo, ponto);
        }
        var limites = new Bounds();
        limites.SetMinMax(minimo, maximo);
        return limites;
    }

    void OnDestroy()
    {
        if (malha != null) Destroy(malha);
    }
}
