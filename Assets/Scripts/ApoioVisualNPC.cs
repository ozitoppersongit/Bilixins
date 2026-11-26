using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Ajusta só o filho visual. As posições, saltos e poses de Corpo continuam nas cutscenes.
[DefaultExecutionOrder(1000)]
public class ApoioVisualNPC : MonoBehaviour
{
    [SerializeField] Transform raiz;
    [SerializeField] Transform modelo;
    [Tooltip("Escala Y do pai na pose base, antes dos sustos e saltos.")]
    [SerializeField, Min(0.001f)] float escalaBasePaiY = 1f;
    [SerializeField] float alturaBasePaiSobreRaiz;
    [SerializeField] float posicaoBaseY = -1f;
    [SerializeField] LayerMask camadasPiso = ~0;
    [SerializeField, Min(0f)] float folgaPiso = 0.002f;
    [SerializeField, Min(0f)] float alcanceAcima = 0.35f;
    [SerializeField, Min(0.1f)] float alcanceAbaixo = 3f;
    [SerializeField, Range(0f, 89f)] float inclinacaoMaxima = 55f;
    [SerializeField, Min(0f)] float limiteCorrecao = 1f;
    [SerializeField, Min(0f)] float velocidadeDescida = 6f;

    struct PontoSola
    {
        public Transform osso;
        public Vector3 local;
    }

    readonly List<PontoSola> solas = new List<PontoSola>(8);
    readonly RaycastHit[] hits = new RaycastHit[64];
    bool inicializado;
    int tentativasSolas;
    float proximaTentativa;
    bool avisoSolas;
    Animator animatorCorpo;
    readonly List<AnimatorClipInfo> clipesCorpo = new List<AnimatorClipInfo>(4);
    bool temPiso;
    bool primeiroAjuste = true;
    float ultimoPiso;

    public int QuantidadePontosSola => solas.Count;

    void OnEnable()
    {
        primeiroAjuste = true;
        temPiso = false;
        inicializado = solas.Count > 0;
        tentativasSolas = 0;
        proximaTentativa = 0f;
        animatorCorpo = raiz != null ? raiz.GetComponent<Animator>() : null;
    }

    void LateUpdate() => AtualizarApoio(Time.deltaTime);

    public void AtualizarApoio(float deltaTime)
    {
        if (raiz == null || modelo == null) return;
        if (!inicializado && Time.unscaledTime >= proximaTentativa)
        {
            if (animatorCorpo == null) animatorCorpo = raiz.GetComponent<Animator>();
            ExtrairSolas();
            tentativasSolas++;
            inicializado = solas.Count > 0 || tentativasSolas >= 8;
            proximaTentativa = Time.unscaledTime + 0.25f;
            if (inicializado && solas.Count == 0 && !avisoSolas)
            {
                avisoSolas = true;
                Debug.LogWarning("ApoioVisualNPC: não foi possível obter as solas de " + raiz.name + ". Confira Read/Write e os bones Leg L/Leg R.", this);
            }
        }

        if (EncontrarPiso(out float piso))
        {
            ultimoPiso = piso;
            temPiso = true;
        }
        if (!temPiso) return;

        float minimo = float.PositiveInfinity;
        foreach (PontoSola ponto in solas)
        {
            // Retira apenas a transformação externa de Corpo. O movimento dos bones permanece.
            Vector3 local = transform.InverseTransformPoint(ponto.osso.TransformPoint(ponto.local));
            minimo = Mathf.Min(minimo, local.y);
        }
        if (float.IsInfinity(minimo)) minimo = 0f;
        float desejado = (ultimoPiso + folgaPiso - raiz.position.y - alturaBasePaiSobreRaiz) / escalaBasePaiY
            - transform.localScale.y * minimo;
        // Idle anima Corpo para cima. Compensa só esse repouso; Susto e Correr conservam suas poses.
        if (solas.Count > 0 && CorpoEmIdle())
        {
            float solaMundo = float.PositiveInfinity;
            foreach (PontoSola ponto in solas)
                solaMundo = Mathf.Min(solaMundo, ponto.osso.TransformPoint(ponto.local).y);
            float escalaVertical = transform.parent != null ? transform.parent.TransformVector(Vector3.up).y : 1f;
            if (escalaVertical > 0.001f)
                desejado = transform.localPosition.y + (ultimoPiso + folgaPiso - solaMundo) / escalaVertical;
        }
        Vector3 posicao = transform.localPosition;
        // Subir imediatamente evita atravessar degraus; descer pode ser suavizado.
        posicao.y = primeiroAjuste || desejado >= posicao.y || velocidadeDescida <= 0f ? desejado :
            Mathf.MoveTowards(posicao.y, desejado, velocidadeDescida * Mathf.Max(0f, deltaTime) / escalaBasePaiY);
        transform.localPosition = posicao;
        primeiroAjuste = false;
    }

    bool CorpoEmIdle()
    {
        if (animatorCorpo == null || !animatorCorpo.isInitialized || animatorCorpo.layerCount == 0) return false;
        animatorCorpo.GetCurrentAnimatorClipInfo(0, clipesCorpo);
        if (!SomenteIdle()) return false;
        if (!animatorCorpo.IsInTransition(0)) return true;
        animatorCorpo.GetNextAnimatorClipInfo(0, clipesCorpo);
        return SomenteIdle();
    }

    bool SomenteIdle()
    {
        if (clipesCorpo.Count == 0) return false;
        foreach (AnimatorClipInfo clipe in clipesCorpo)
            if (clipe.clip == null || clipe.clip.name != "CriancaIdle") return false;
        return true;
    }

    bool EncontrarPiso(out float altura)
    {
        altura = 0f;
        PhysicsScene fisica = gameObject.scene.GetPhysicsScene();
        if (!fisica.IsValid()) return false;
        int quantidade = fisica.Raycast(raiz.position + Vector3.up * alcanceAcima, Vector3.down, hits,
            alcanceAcima + alcanceAbaixo, camadasPiso, QueryTriggerInteraction.Ignore);
        float distancia = float.PositiveInfinity;
        float baseEsperada = raiz.position.y + alturaBasePaiSobreRaiz + escalaBasePaiY * posicaoBaseY;
        float normalMinima = Mathf.Cos(inclinacaoMaxima * Mathf.Deg2Rad);
        for (int i = 0; i < quantidade; i++)
        {
            RaycastHit hit = hits[i];
            Collider collider = hit.collider;
            if (collider == null || collider.transform.IsChildOf(raiz) || hit.normal.y < normalMinima ||
                Mathf.Abs(hit.point.y - baseEsperada) > limiteCorrecao || hit.distance >= distancia) continue;
            if (collider.GetComponentInParent<PlayerMovement>() != null) continue;
            Rigidbody corpo = collider.attachedRigidbody;
            if (corpo != null && !corpo.isKinematic) continue;
            distancia = hit.distance;
            altura = hit.point.y;
        }
        return !float.IsInfinity(distancia);
    }

    void ExtrairSolas()
    {
        solas.Clear();
        foreach (SkinnedMeshRenderer renderer in modelo.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            Mesh malha = renderer.sharedMesh;
            if (malha == null || !malha.isReadable) continue;
            Vector3[] vertices = malha.vertices;
            BoneWeight[] pesos = malha.boneWeights;
            Matrix4x4[] bindposes = malha.bindposes;
            Transform[] ossos = renderer.bones;
            if (pesos.Length != vertices.Length) continue;
            for (int osso = 0; osso < ossos.Length && osso < bindposes.Length; osso++)
            {
                Transform perna = ossos[osso];
                if (perna == null || (perna.name != "Leg L" && perna.name != "Leg R")) continue;
                Vector3 eixo = perna.InverseTransformDirection(-modelo.up).normalized;
                foreach (Transform filho in perna)
                    if (filho.name.EndsWith("_end") && filho.localPosition.sqrMagnitude > 0.000001f)
                    {
                        eixo = filho.localPosition.normalized;
                        break;
                    }
                var candidatos = new List<Vector3>();
                float extremo = float.NegativeInfinity;
                for (int i = 0; i < vertices.Length; i++)
                {
                    BoneWeight peso = pesos[i];
                    bool rigido = (peso.boneIndex0 == osso && peso.weight0 > 0.999f) ||
                        (peso.boneIndex1 == osso && peso.weight1 > 0.999f) ||
                        (peso.boneIndex2 == osso && peso.weight2 > 0.999f) ||
                        (peso.boneIndex3 == osso && peso.weight3 > 0.999f);
                    if (!rigido) continue;
                    Vector3 local = bindposes[osso].MultiplyPoint3x4(vertices[i]);
                    candidatos.Add(local);
                    extremo = Mathf.Max(extremo, Vector3.Dot(local, eixo));
                }
                float tolerancia = Mathf.Max(0.0001f, Mathf.Abs(extremo) * 0.0001f);
                foreach (Vector3 ponto in candidatos)
                {
                    if (extremo - Vector3.Dot(ponto, eixo) > tolerancia) continue;
                    bool duplicado = false;
                    foreach (PontoSola existente in solas)
                        if (existente.osso == perna && (existente.local - ponto).sqrMagnitude < 0.00000001f)
                        {
                            duplicado = true;
                            break;
                        }
                    if (!duplicado) solas.Add(new PontoSola { osso = perna, local = ponto });
                }
            }
        }
    }
}
