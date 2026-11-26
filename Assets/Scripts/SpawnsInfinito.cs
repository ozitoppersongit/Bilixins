using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class SpawnsInfinito : MonoBehaviour
{
    [Serializable]
    public class Regiao
    {
        public string nome;
        public Transform[] pontos;
    }

    [Serializable]
    public class ModeloInimigo
    {
        public GameObject prefab;
        public BilixinsData dados;
        [Min(0), Tooltip("Chance relativa. Zero exclui este tipo, inclusive nas alternativas de spawn.")]
        public float peso = 19f;
        [Min(0)] public float raioRonda;
    }

    public sealed class Rodada
    {
        public readonly List<GameObject> objetos = new List<GameObject>();
        public readonly List<GameObject> itens = new List<GameObject>();
        public readonly HashSet<GatilhoDeEncontro> inimigos = new HashSet<GatilhoDeEncontro>();
        public readonly List<BilixinsData> dados = new List<BilixinsData>();
        public string regiao;
        public Vector3 centro;
        public float distancia;
    }

    [SerializeField] Regiao[] regioes;
    [SerializeField] ModeloInimigo[] modelos;
    [SerializeField] GameObject[] lixos;
    [SerializeField] GameObject pacote;
    [SerializeField] GameObject telaBatalha;
    [SerializeField, Min(12f)] float distanciaMinimaInimigos = 12f;
    [SerializeField] RuntimeAnimatorController[] animacoesLixo;
    [SerializeField] Material materialBrilho;
    [SerializeField] GameObject efeitoColeta;
    CharacterController jogadorController;
    Transform raizJogador;
    int mascaraFisica;
    float raioJogador;
    float alturaJogador;
    float degrauJogador;
    float inclinacaoJogador;
    NavMeshSurface superficie;
    NavMeshPath caminho;
    public bool NavegacaoPronta { get; private set; }

    public int QuantidadeRegioes => regioes != null ? regioes.Length : 0;
    public bool TemModelosValidos => Candidatos().Count > 0;

    List<ModeloInimigo> Candidatos()
    {
        var candidatos = new List<ModeloInimigo>();
        if (modelos != null)
            foreach (ModeloInimigo modelo in modelos)
                if (modelo != null && modelo.prefab != null && modelo.dados != null && SorteioPonderado.Valido(modelo.peso))
                    candidatos.Add(modelo);
        return candidatos;
    }

    public bool ConstruirNavegacao(GameObject jogador)
    {
        NavegacaoPronta = false;
        if (jogador == null) return false;
        jogadorController = jogador.GetComponent<CharacterController>();
        if (jogadorController == null) return false;
        raizJogador = jogador.transform;
        Vector3 escala = jogador.transform.lossyScale;
        raioJogador = jogadorController.radius * Mathf.Max(Mathf.Abs(escala.x), Mathf.Abs(escala.z));
        alturaJogador = Mathf.Max(raioJogador * 2f, jogadorController.height * Mathf.Abs(escala.y));
        degrauJogador = jogadorController.stepOffset * Mathf.Abs(escala.y);
        inclinacaoJogador = jogadorController.slopeLimit;
        mascaraFisica = 0;
        for (int camada = 0; camada < 32; camada++)
            if (!Physics.GetIgnoreLayerCollision(jogador.layer, camada)) mascaraFisica |= 1 << camada;
        mascaraFisica = (mascaraFisica | jogadorController.includeLayers.value) & ~jogadorController.excludeLayers.value;
        Physics.SyncTransforms();
        Collider[] colliders = jogador.GetComponentsInChildren<Collider>();
        var estados = new bool[colliders.Length];
        for (int i = 0; i < colliders.Length; i++)
        {
            estados[i] = colliders[i].enabled;
            colliders[i].enabled = false;
        }
        try
        {
            // NavMeshPath possui estado nativo e precisa nascer na thread principal.
            caminho = new NavMeshPath();
            if (superficie == null) superficie = gameObject.AddComponent<NavMeshSurface>();
            superficie.collectObjects = CollectObjects.All;
            superficie.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            superficie.layerMask = mascaraFisica;
            foreach (MeshCollider collider in FindObjectsByType<MeshCollider>(FindObjectsSortMode.None))
                if (collider.enabled && !collider.isTrigger && collider.sharedMesh != null &&
                    (superficie.layerMask.value & (1 << collider.gameObject.layer)) != 0 &&
                    !collider.sharedMesh.isReadable)
                    throw new InvalidOperationException($"O mesh '{collider.sharedMesh.name}' precisa de Read/Write para a navegação.");
            superficie.BuildNavMesh();
            NavMeshTriangulation malha = NavMesh.CalculateTriangulation();
            if (superficie.navMeshData == null || malha.indices.Length < 3 ||
                !NavMesh.SamplePosition(jogador.transform.position, out _, 4f, NavMesh.AllAreas))
                throw new InvalidOperationException("A navegação não contém uma área caminhável próxima ao jogador.");
            NavegacaoPronta = true;
            return true;
        }
        catch (Exception erro)
        {
            Debug.LogError("Infinito: falha ao construir a navegação. " + erro.Message, this);
            if (superficie != null) superficie.RemoveData();
            return false;
        }
        finally
        {
            for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = estados[i];
        }
    }

    public Rodada Criar(ObjetivoInfinito tipo, int quantidade, int inimigos, int nivel, int indice, Transform jogador)
    {
        if (!NavegacaoPronta || indice < 0 || indice >= QuantidadeRegioes || !TemModelosValidos) return null;
        Regiao regiao = regioes[indice];
        if (regiao == null || regiao.pontos == null || regiao.pontos.Length == 0) return null;
        var pontos = new List<Vector3>();
        var modelosEscolhidos = new List<ModeloInimigo>();
        var raios = new List<float>();
        int itens = tipo == ObjetivoInfinito.Combate ? 0 : quantidade;
        int totalInimigos = Mathf.Max(inimigos, tipo == ObjetivoInfinito.Combate ? quantidade : 0);
        float distancia = 0;
        Vector3 origem = jogador.position;
        for (int i = 0; i < totalInimigos + itens; i++)
        {
            bool inimigo = i < totalInimigos;
            ModeloInimigo modelo = null;
            float ronda = 0;
            Vector3 posicao = default;
            bool encontrou = false;
            var candidatos = inimigo ? Candidatos() : new List<ModeloInimigo>();
            var pesos = new List<float>();
            foreach (ModeloInimigo candidato in candidatos) pesos.Add(candidato.peso);
            int alternativas = inimigo ? candidatos.Count : 1;
            for (int alternativa = 0; alternativa < alternativas; alternativa++)
            {
                if (inimigo)
                {
                    int escolhido = SorteioPonderado.Sortear(pesos, UnityEngine.Random.value);
                    if (escolhido < 0) break;
                    modelo = candidatos[escolhido];
                    candidatos.RemoveAt(escolhido);
                    pesos.RemoveAt(escolhido);
                }
                ronda = modelo != null ? modelo.raioRonda : 0;
                if (!Encontrar(regiao, jogador, pontos, raios, inimigo, ronda, out posicao)) continue;
                encontrou = true;
                break;
            }
            if (!encontrou) return null;
            pontos.Add(posicao);
            raios.Add(inimigo ? Mathf.Max(0f, ronda) : 0.75f);
            modelosEscolhidos.Add(modelo);
        }

        // A rota aproxima uma visita a todos os objetivos, pelo vizinho mais próximo.
        var objetivos = tipo == ObjetivoInfinito.Combate
            ? pontos.GetRange(0, quantidade) : pontos.GetRange(totalInimigos, itens);
        Vector3 centro = Vector3.zero;
        int ancoras = 0;
        foreach (Transform ponto in regiao.pontos)
            if (ponto != null)
            {
                centro += ponto.position;
                ancoras++;
            }
        centro /= Mathf.Max(1, ancoras);
        while (objetivos.Count > 0)
        {
            int melhor = 0;
            float menor = float.MaxValue;
            for (int i = 0; i < objetivos.Count; i++)
                if (Distancia(origem, objetivos[i], out float metros) && metros < menor)
                {
                    menor = metros;
                    melhor = i;
                }
            if (menor == float.MaxValue) return null;
            distancia += menor;
            origem = objetivos[melhor];
            objetivos.RemoveAt(melhor);
        }

        var rodada = new Rodada { regiao = regiao.nome, centro = centro, distancia = distancia };
        for (int i = 0; i < pontos.Count; i++)
        {
            ModeloInimigo modelo = modelosEscolhidos[i];
            if (modelo != null)
            {
                GameObject objeto = Instantiate(modelo.prefab, pontos[i], Quaternion.identity);
                rodada.objetos.Add(objeto);
                objeto.name = "Infinito_" + modelo.dados.nome;
                foreach (Animator animador in objeto.GetComponentsInChildren<Animator>(true)) animador.Update(0f);
                Assentar(objeto, pontos[i]);
                if (!RondaCabeNoLocal(objeto, modelo.raioRonda, pontos[i]) || !CabeNoLocal(objeto)) { Limpar(rodada); return null; }
                Ferralha ferralha = objeto.GetComponentInChildren<Ferralha>(true);
                GameObject alvo = ferralha != null ? ferralha.gameObject : objeto;
                foreach (Collider collider in objeto.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                Rigidbody corpoEncontro = alvo.GetComponentInParent<Rigidbody>();
                if (corpoEncontro == null) corpoEncontro = alvo.AddComponent<Rigidbody>();
                corpoEncontro.isKinematic = true;
                corpoEncontro.useGravity = false;
                SphereCollider volume = alvo.AddComponent<SphereCollider>();
                volume.isTrigger = true;
                volume.radius = 1.4f;
                volume.center = Vector3.up * 0.75f;
                GatilhoDeEncontro encontro = alvo.GetComponent<GatilhoDeEncontro>();
                if (encontro == null) encontro = alvo.AddComponent<GatilhoDeEncontro>();
                BilixinsData dados = Instantiate(modelo.dados);
                dados.nivel = nivel;
                encontro.ConfigurarInfinito(dados, telaBatalha, volume);
                rodada.inimigos.Add(encontro);
                rodada.dados.Add(dados);
            }
            else
            {
                int indiceLixo = tipo == ObjetivoInfinito.Busca ? -1 : UnityEngine.Random.Range(0, lixos.Length);
                GameObject prefab = indiceLixo < 0 ? pacote : lixos[indiceLixo];
                // A âncora nunca gira nem flutua: coleta e minimapa usam sempre o mesmo ponto.
                GameObject objeto = new GameObject("Coletável infinito");
                objeto.transform.position = pontos[i];
                rodada.objetos.Add(objeto);
                GameObject visual = Instantiate(prefab, objeto.transform);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);
                foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                Animator animador = null;
                if (indiceLixo >= 0 && animacoesLixo != null && indiceLixo < animacoesLixo.Length &&
                    animacoesLixo[indiceLixo] != null)
                {
                    animador = visual.GetComponent<Animator>();
                    if (animador == null) animador = visual.AddComponent<Animator>();
                    animador.runtimeAnimatorController = animacoesLixo[indiceLixo];
                    animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    animador.applyRootMotion = false;
                    animador.Update(0f);
                    animador.speed = 0f;
                }
                Assentar(visual, pontos[i] + Vector3.up * 0.65f);
                if (!CabeGirando(objeto) || !AcessoColeta(objeto))
                {
                    Limpar(rodada);
                    return null;
                }
                objeto.AddComponent<ItemInfinito>().Configurar(visual.transform, animador, materialBrilho);
                rodada.itens.Add(objeto);
            }
        }
        return rodada;
    }

    bool CabeNoLocal(GameObject objeto)
    {
        Physics.SyncTransforms();
        foreach (Renderer renderer in objeto.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer is ParticleSystemRenderer || !renderer.enabled) continue;
            Bounds limites = renderer.bounds;
            Vector3 tamanho = Vector3.Max(limites.extents - Vector3.one * 0.02f, Vector3.one * 0.01f);
            foreach (Collider collider in Physics.OverlapBox(limites.center, tamanho, Quaternion.identity,
                mascaraFisica, QueryTriggerInteraction.Ignore))
                if (Bloqueia(collider, objeto.transform)) return false;
        }
        return true;
    }

    bool RondaCabeNoLocal(GameObject objeto, float raio, Vector3 piso)
    {
        if (raio <= 0f) return true;
        Animator animador = objeto.GetComponentInChildren<Animator>();
        int ronda = Animator.StringToHash("Base Layer.Ronda");
        if (animador == null || !animador.HasState(0, ronda)) return false;
        float solaMinima = float.PositiveInfinity;
        Renderer[] partes = objeto.GetComponentsInChildren<Renderer>(true);
        // Alguns ciclos abaixam as pernas depois do frame inicial. Reserva essa altura também.
        for (int amostra = 0; amostra < 48; amostra++)
        {
            animador.Play(ronda, 0, amostra / 48f);
            animador.Update(0f);
            foreach (Renderer parte in partes)
                if (!(parte is ParticleSystemRenderer) && parte.enabled && parte.gameObject.activeInHierarchy)
                    solaMinima = Mathf.Min(solaMinima, parte.bounds.min.y);
        }
        if (!float.IsInfinity(solaMinima))
            objeto.transform.position += Vector3.up * Mathf.Max(0, piso.y + 0.03f - solaMinima);
        bool livre = true;
        // Confere o volume do modelo real durante toda a ronda, além do caminho reservado.
        for (int amostra = 0; amostra < 48; amostra++)
        {
            animador.Play(ronda, 0, amostra / 48f);
            animador.Update(0f);
            if (!CabeNoLocal(objeto)) { livre = false; break; }
        }
        animador.Play(ronda, 0, 0f);
        animador.Update(0f);
        return livre;
    }

    bool Encontrar(Regiao regiao, Transform jogador, List<Vector3> usados, List<float> raios,
        bool inimigo, float ronda, out Vector3 posicao)
    {
        for (int tentativa = 0; tentativa < 64; tentativa++)
        {
            Transform ancora = regiao.pontos[UnityEngine.Random.Range(0, regiao.pontos.Length)];
            if (ancora == null) continue;
            Vector2 deslocamento = UnityEngine.Random.insideUnitCircle * 12f;
            Vector3 candidato = ancora.position + new Vector3(deslocamento.x, 0, deslocamento.y);
            if (!NavMesh.SamplePosition(candidato, out NavMeshHit hit, 3f, NavMesh.AllAreas)) continue;
            if (Vector3.Distance(hit.position, jogador.position) < (inimigo ? 8f + ronda : 4f)) continue;
            bool separado = true;
            for (int i = 0; i < usados.Count; i++)
            {
                // Inimigos são escolhidos antes dos itens. Seus raios reservam a ronda inteira.
                Vector3 diferenca = hit.position - usados[i];
                diferenca.y = 0f;
                float distanciaMinima = inimigo
                    ? Mathf.Max(12f, distanciaMinimaInimigos) + raios[i] + ronda
                    : Mathf.Max(2f, raios[i] + ronda);
                if (diferenca.sqrMagnitude < distanciaMinima * distanciaMinima)
                {
                    separado = false;
                    break;
                }
            }
            if (!separado || !Livre(hit.position) || !Distancia(jogador.position, hit.position, out _)) continue;
            bool rondaLivre = true;
            for (int i = 0; i < 12 && ronda > 0; i++)
            {
                Vector3 ponto = hit.position + Quaternion.Euler(0, i * 30f, 0) * Vector3.forward * ronda;
                if (!NavMesh.SamplePosition(ponto, out NavMeshHit vizinho, 0.4f, NavMesh.AllAreas) ||
                    Mathf.Abs(vizinho.position.y - hit.position.y) > 0.3f || !Livre(vizinho.position) ||
                    !Distancia(hit.position, vizinho.position, out _))
                {
                    rondaLivre = false;
                    break;
                }
            }
            if (!rondaLivre) continue;
            posicao = hit.position;
            return true;
        }
        posicao = default;
        return false;
    }

    bool Bloqueia(Collider collider, Transform ignorado = null)
    {
        return collider != null && !collider.transform.IsChildOf(raizJogador) &&
            (ignorado == null || !collider.transform.IsChildOf(ignorado));
    }

    bool Piso(Vector3 ponto, out Vector3 piso, Transform ignorado = null)
    {
        piso = default;
        float alcance = Mathf.Max(0.4f, degrauJogador + 0.15f);
        float menor = float.PositiveInfinity;
        foreach (RaycastHit hit in Physics.RaycastAll(ponto + Vector3.up * alcance, Vector3.down,
            alcance * 2f, mascaraFisica, QueryTriggerInteraction.Ignore))
        {
            if (!Bloqueia(hit.collider, ignorado) || hit.distance >= menor) continue;
            menor = hit.distance;
            piso = hit.point;
            if (Vector3.Angle(hit.normal, Vector3.up) > inclinacaoJogador) piso = Vector3.positiveInfinity;
        }
        return menor < float.PositiveInfinity && !float.IsInfinity(piso.x);
    }

    void Capsula(Vector3 piso, out Vector3 baixo, out Vector3 alto)
    {
        // A parte inferior pode subir degraus; o restante precisa estar desobstruído.
        float folga = Mathf.Min(degrauJogador, alturaJogador - raioJogador * 2f) + 0.04f;
        baixo = piso + Vector3.up * (raioJogador + folga);
        alto = piso + Vector3.up * Mathf.Max(raioJogador + folga, alturaJogador - raioJogador);
    }

    bool Livre(Vector3 ponto, Transform ignorado = null)
    {
        if (!Piso(ponto, out Vector3 piso, ignorado)) return false;
        Capsula(piso, out Vector3 baixo, out Vector3 alto);
        foreach (Collider collider in Physics.OverlapCapsule(baixo, alto, raioJogador,
            mascaraFisica, QueryTriggerInteraction.Ignore))
            if (Bloqueia(collider, ignorado)) return false;
        return true;
    }

    bool TrechoLivre(Vector3 inicio, Vector3 fim, Transform ignorado = null)
    {
        Vector3 deslocamento = fim - inicio;
        if (deslocamento.sqrMagnitude < 0.0001f) return true;
        Capsula(inicio, out Vector3 baixo, out Vector3 alto);
        foreach (RaycastHit hit in Physics.CapsuleCastAll(baixo, alto, raioJogador,
            deslocamento.normalized, deslocamento.magnitude, mascaraFisica, QueryTriggerInteraction.Ignore))
            if (Bloqueia(hit.collider, ignorado)) return false;
        return true;
    }

    bool CaminhoFisico(Vector3[] cantos, Transform ignorado)
    {
        if (cantos.Length == 0 || !Piso(cantos[0], out Vector3 anterior, ignorado) ||
            !Livre(anterior, ignorado)) return false;
        for (int i = 1; i < cantos.Length; i++)
        {
            int amostras = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(cantos[i - 1], cantos[i]) / 0.5f));
            for (int j = 1; j <= amostras; j++)
            {
                Vector3 ponto = Vector3.Lerp(cantos[i - 1], cantos[i], (float)j / amostras);
                if (!Piso(ponto, out Vector3 atual, ignorado) || !Livre(atual, ignorado)) return false;
                Vector3 horizontal = atual - anterior;
                horizontal.y = 0f;
                float subida = degrauJogador + horizontal.magnitude * Mathf.Tan(inclinacaoJogador * Mathf.Deg2Rad) + 0.05f;
                if (Mathf.Abs(atual.y - anterior.y) > subida || !TrechoLivre(anterior, atual, ignorado)) return false;
                anterior = atual;
            }
        }
        return true;
    }

    bool Distancia(Vector3 origem, Vector3 destino, out float metros, Transform ignorado = null)
    {
        metros = 0;
        if (raizJogador != null && (origem - raizJogador.position).sqrMagnitude < 0.01f)
            origem = jogadorController.transform.TransformPoint(jogadorController.center) - Vector3.up * (alturaJogador * 0.5f);
        if (!NavegacaoPronta || caminho == null ||
            !NavMesh.SamplePosition(origem, out NavMeshHit inicio, 0.6f, NavMesh.AllAreas) ||
            !NavMesh.SamplePosition(destino, out NavMeshHit fim, 0.4f, NavMesh.AllAreas) ||
            !NavMesh.CalculatePath(inicio.position, fim.position, NavMesh.AllAreas, caminho) ||
            caminho.status != NavMeshPathStatus.PathComplete) return false;
        Vector3[] cantos = caminho.corners;
        if (!Piso(origem, out Vector3 pisoOrigem, ignorado) ||
            !TrechoLivre(pisoOrigem, inicio.position, ignorado)) return false;
        if (!CaminhoFisico(cantos, ignorado)) return false;
        for (int i = 1; i < cantos.Length; i++) metros += Vector3.Distance(cantos[i - 1], cantos[i]);
        return true;
    }

    bool VisaoLivre(Vector3 inicio, Vector3 fim, Transform ignorado)
    {
        Vector3 direcao = fim - inicio;
        if (direcao.sqrMagnitude < 0.0001f) return true;
        foreach (RaycastHit hit in Physics.RaycastAll(inicio, direcao.normalized, direcao.magnitude,
            mascaraFisica, QueryTriggerInteraction.Ignore))
            if (Bloqueia(hit.collider, ignorado)) return false;
        return true;
    }

    bool AcessoColeta(GameObject item)
    {
        for (int i = 0; i < 9; i++)
        {
            Vector3 candidato = item.transform.position;
            if (i > 0) candidato += Quaternion.Euler(0f, (i - 1) * 45f, 0f) * Vector3.forward;
            if (!NavMesh.SamplePosition(candidato, out NavMeshHit hit, 0.35f, NavMesh.AllAreas) ||
                (hit.position - item.transform.position).sqrMagnitude > 1.5f * 1.5f ||
                !Livre(hit.position, item.transform) ||
                !VisaoLivre(hit.position + Vector3.up * 0.6f, item.transform.position + Vector3.up * 0.6f, item.transform) ||
                !Distancia(raizJogador.position, hit.position, out _, item.transform)) continue;
            return true;
        }
        return false;
    }

    public bool PodeColetar(GameObject item)
    {
        if (item == null || raizJogador == null) return false;
        Vector3 centro = jogadorController.transform.TransformPoint(jogadorController.center);
        return VisaoLivre(centro, item.transform.position + Vector3.up * 0.6f, item.transform);
    }

    bool CabeGirando(GameObject objeto)
    {
        Physics.SyncTransforms();
        bool achou = false;
        float raio = 0f;
        float minimo = float.PositiveInfinity, maximo = float.NegativeInfinity;
        foreach (Renderer renderer in objeto.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer is ParticleSystemRenderer || !renderer.enabled) continue;
            Bounds b = renderer.bounds;
            Vector3 distancia = b.center - objeto.transform.position;
            raio = Mathf.Max(raio, new Vector2(Mathf.Abs(distancia.x) + b.extents.x,
                Mathf.Abs(distancia.z) + b.extents.z).magnitude);
            minimo = Mathf.Min(minimo, b.min.y);
            maximo = Mathf.Max(maximo, b.max.y + 0.16f);
            achou = true;
        }
        if (!achou) return false;
        Vector3 centro = objeto.transform.position;
        centro.y = (minimo + maximo) * 0.5f;
        foreach (Collider collider in Physics.OverlapBox(centro,
            new Vector3(raio, (maximo - minimo) * 0.5f, raio), Quaternion.identity,
            mascaraFisica, QueryTriggerInteraction.Ignore))
            if (Bloqueia(collider, objeto.transform)) return false;
        return true;
    }

    public void CriarRajada(GameObject item, Rodada atual)
    {
        if (efeitoColeta == null || item == null || atual == null) return;
        GameObject efeito = Instantiate(efeitoColeta, item.transform.position + Vector3.up * 1.9f, Quaternion.identity);
        atual.objetos.Add(efeito);
        efeito.AddComponent<ItemInfinito>().ConfigurarRajada();
    }

    static void Assentar(GameObject objeto, Vector3 piso)
    {
        foreach (Rigidbody corpo in objeto.GetComponentsInChildren<Rigidbody>(true))
        {
            corpo.isKinematic = true;
            corpo.useGravity = false;
        }
        bool achou = false;
        Bounds limites = default;
        foreach (Renderer renderer in objeto.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer is ParticleSystemRenderer || !renderer.enabled) continue;
            if (!achou) limites = renderer.bounds;
            else limites.Encapsulate(renderer.bounds);
            achou = true;
        }
        if (achou) objeto.transform.position += Vector3.up * (piso.y - limites.min.y + 0.03f);
    }

    public static void Limpar(Rodada rodada)
    {
        if (rodada == null) return;
        foreach (GameObject objeto in rodada.objetos)
            if (objeto != null) { objeto.SetActive(false); Destroy(objeto); }
        foreach (BilixinsData dados in rodada.dados)
            if (dados != null) Destroy(dados);
    }

    void OnDestroy()
    {
        NavegacaoPronta = false;
        caminho = null;
        if (superficie != null)
        {
            superficie.RemoveData();
            if (superficie.navMeshData != null)
            {
                if (Application.isPlaying) Destroy(superficie.navMeshData);
                else DestroyImmediate(superficie.navMeshData);
            }
        }
    }
}
