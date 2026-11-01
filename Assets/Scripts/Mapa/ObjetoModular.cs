using System.Collections.Generic;
using UnityEngine;

// Objeto montável por peças: casa, bloqueio de rua, o que for. Cada peça (calha, musgo, porta...) é um "slot"
// com uma lista de variações; só a variação escolhida fica ligada. As variações são objetos filhos do prefab,
// então podem vir do ProBuilder, de um FBX exportado do Blender ou de qualquer modelo — o script não liga pra isso.
//
// No Editor, ao arrastar o objeto na Scene view e soltar, ele encaixa na grade, gira em passos de 90° e assenta
// no chão (na altura do terreno daquele ponto) e fica onde você deixou. O MapaGerador não mexe nele.
[ExecuteAlways]
public class ObjetoModular : MonoBehaviour
{
    [System.Serializable]
    public class Slot
    {
        public string nome = "Calha";
        [Tooltip("Objetos filhos do prefab. Só o escolhido fica ligado.")]
        public List<GameObject> variacoes = new List<GameObject>();
        [Tooltip("Qual variação está ligada (começa em 0). -1 = nenhuma, a peça fica desligada.")]
        public int escolhida = 0;
    }

    public List<Slot> slots = new List<Slot>();

    [Header("Posicionamento (só no Editor, ao arrastar na Scene view)")]
    [Tooltip("Encaixa X e Z em múltiplos do passo")]
    public bool encaixarNaGrade = true;
    [Min(0.125f)] public float passoDaGrade = 0.125f;
    [Tooltip("Ajusta a altura pro chão que está embaixo (precisa do preview do mapa gerado)")]
    public bool assentarNoChao = true;
    [Tooltip("Gira só em múltiplos de 90° e mantém em pé")]
    public bool girarEm90 = true;

    void Start()
    {
        // Em jogo, joga fora as variações que não foram escolhidas: não pesam nem entram no static batching.
        // É no Start (e não no Awake) pra quem criou o objeto poder sortear as variações antes disso.
        if (Application.isPlaying) AplicarVariacoes(removerAsNaoEscolhidas: true);
    }

    // Liga a variação escolhida de cada slot e desliga as outras
    public void AplicarVariacoes(bool removerAsNaoEscolhidas = false)
    {
        foreach (Slot slot in slots)
        {
            if (slot == null) continue;

            for (int i = 0; i < slot.variacoes.Count; i++)
            {
                GameObject variacao = slot.variacoes[i];
                if (variacao == null) continue;

                bool ligada = i == slot.escolhida;
                if (removerAsNaoEscolhidas && !ligada)
                {
                    Destroy(variacao);
                    continue;
                }

                if (variacao.activeSelf == ligada) continue;
#if UNITY_EDITOR
                if (!Application.isPlaying) UnityEditor.Undo.RecordObject(variacao, "Trocar variação");
#endif
                variacao.SetActive(ligada);
            }
        }
    }

    // Sorteia uma variação em cada slot (slot sem variações fica como está)
    public void Aleatorizar()
    {
        Aleatorizar(Random.Range(int.MinValue, int.MaxValue));
    }

    // Mesmo sorteio, mas repetível: a mesma semente dá sempre as mesmas variações
    // (o MapaGerador usa a posição no mapa, pra um mapa gerado duas vezes sair igual)
    public void Aleatorizar(int semente)
    {
        var sorteio = new System.Random(semente);
        foreach (Slot slot in slots)
        {
            if (slot == null || slot.variacoes.Count == 0) continue;
            slot.escolhida = sorteio.Next(0, slot.variacoes.Count);
        }
        AplicarVariacoes();
    }

#if UNITY_EDITOR
    Vector3 ultimaPosicao;
    Quaternion ultimaRotacao;
    double instanteDaMudanca;
    bool aguardando;

    // Só encaixa quando o mouse está solto: assim não briga com você enquanto arrasta
    static bool mouseSegurado;
    static double ultimoEventoDoMouse;

    [UnityEditor.InitializeOnLoadMethod]
    static void RastrearMouse()
    {
        UnityEditor.SceneView.duringSceneGui += _ =>
        {
            Event e = Event.current;
            if (e == null) return;

            if (e.rawType == EventType.MouseDown) { mouseSegurado = true; ultimoEventoDoMouse = UnityEditor.EditorApplication.timeSinceStartup; }
            else if (e.rawType == EventType.MouseUp) mouseSegurado = false;
            else if (e.rawType == EventType.MouseDrag) ultimoEventoDoMouse = UnityEditor.EditorApplication.timeSinceStartup;
        };
    }

    void OnEnable()
    {
        if (Application.isPlaying) return;

        ultimaPosicao = transform.position;
        ultimaRotacao = transform.rotation;
        UnityEditor.EditorApplication.update += VerificarMovimento;
    }

    void OnDisable()
    {
        UnityEditor.EditorApplication.update -= VerificarMovimento;
    }

    void OnValidate()
    {
        // Trocar a variação no Inspector liga e desliga as peças na hora
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null && !Application.isPlaying) AplicarVariacoes();
        };
    }

    void VerificarMovimento()
    {
        if (this == null || Application.isPlaying) return;
        if (!encaixarNaGrade && !assentarNoChao && !girarEm90) return;

        // Editando o prefab em si (não uma instância na cena): não mexe
        if (!gameObject.scene.IsValid() || UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(gameObject) != null) return;

        Vector3 posicao = transform.position;
        Quaternion rotacao = transform.rotation;
        double agora = UnityEditor.EditorApplication.timeSinceStartup;

        bool arrastando = mouseSegurado && agora - ultimoEventoDoMouse < 3.0;

        if (posicao != ultimaPosicao || rotacao != ultimaRotacao || arrastando)
        {
            ultimaPosicao = posicao;
            ultimaRotacao = rotacao;
            instanteDaMudanca = agora;
            aguardando = true;
            return;
        }

        // Soltou e ficou parado por um instante: encaixa
        if (aguardando && agora - instanteDaMudanca > 0.4)
        {
            aguardando = false;
            EncaixarAgora();
        }
    }

    // Encaixa na grade, gira em 90° e assenta no chão. Também é chamado pelo botão do Inspector.
    public void EncaixarAgora()
    {
        Vector3 posicao = transform.position;
        Quaternion rotacao = transform.rotation;

        if (girarEm90)
            rotacao = Quaternion.Euler(0f, Mathf.Round(rotacao.eulerAngles.y / 90f) * 90f, 0f);

        if (encaixarNaGrade && passoDaGrade > 0.01f)
        {
            posicao.x = Mathf.Round(posicao.x / passoDaGrade) * passoDaGrade;
            posicao.z = Mathf.Round(posicao.z / passoDaGrade) * passoDaGrade;
        }

        if (assentarNoChao && ProcurarChao(posicao, out float y)) posicao.y = y;

        if (posicao == transform.position && rotacao == transform.rotation) return;

        UnityEditor.Undo.RecordObject(transform, "Encaixar objeto");
        transform.SetPositionAndRotation(posicao, rotacao);
        ultimaPosicao = posicao;
        ultimaRotacao = rotacao;
    }

    // Raio de cima pra baixo: acha a superfície mais alta que não é o próprio objeto nem outro objeto modular
    bool ProcurarChao(Vector3 origem, out float y)
    {
        y = origem.y;
        Physics.SyncTransforms();

        RaycastHit[] acertos = Physics.RaycastAll(origem + Vector3.up * 60f, Vector3.down, 200f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        float melhor = float.NegativeInfinity;
        foreach (RaycastHit acerto in acertos)
        {
            if (acerto.collider.transform.IsChildOf(transform)) continue;
            if (acerto.collider.GetComponentInParent<ObjetoModular>() != null) continue;
            if (acerto.point.y > melhor) melhor = acerto.point.y;
        }

        if (float.IsNegativeInfinity(melhor)) return false;

        y = melhor;
        return true;
    }
#endif
}
