using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Le a pixel art de Assets/Art/Mapa/ e monta o mapa com os prefabs configurados no Inspector.
// Cor do inimigo/player parecida com o piso ao redor = nasce "escondido" ali.
// Botão "Gerar Preview no Editor" mostra o resultado sem precisar dar Play.

public class MapaGerador : MonoBehaviour
{
    [Header("Pixel Art do Mapa")]
    [SerializeField] Texture2D imagemDoMapa;
    [Tooltip("Tamanho de 1 pixel no mundo, em metros")]
    [SerializeField] float tamanhoDoTile = 2f;

    [Header("Tiles")]
    [SerializeField] TileDefinicao grama   = new TileDefinicao { nome = "Grama",   altura = 0.2f };
    [SerializeField] TileDefinicao rua     = new TileDefinicao { nome = "Rua",     altura = 0.2f };
    [SerializeField] TileDefinicao calcada = new TileDefinicao { nome = "Calçada", altura = 0.35f };
    [SerializeField] TileDefinicao predio  = new TileDefinicao { nome = "Prédio",  altura = 6f };
    [SerializeField] TileDefinicao chao    = new TileDefinicao { nome = "Chão",    altura = 0.2f };
    [SerializeField] TileDefinicao arvore  = new TileDefinicao { nome = "Árvore",  redimensionar = false };

    [Header("Relevo (desnível e ladeiras)")]
    [Tooltip("Áreas do mapa mais baixas, mais altas ou em ladeira. Medidas em pixels da imagem, a partir do canto inferior esquerdo (0,0). Regiões que se cruzam somam.")]
    [SerializeField] List<RegiaoDefinicao> regioes = new List<RegiaoDefinicao>();
    [Tooltip("Estilo voxel: a ladeira vira degraus pequenos (inclusive na grama) em vez de um plano liso.")]
    [SerializeField] bool ladeiraEmDegraus = true;
    [Tooltip("Altura de cada degrau da ladeira, em metros. Precisa ser menor que o 'Step Offset' do CharacterController (0,3).")]
    [SerializeField] float alturaDoDegrauDaLadeira = 0.25f;
    [Tooltip("Imagem do mesmo tamanho do mapa, em Assets/Art/Mapa/. Cada cor é uma zona de relevo (plano -10, plano -5, escada, ladeira...). Sem cor = nível 0.")]
    [SerializeField] Texture2D imagemDeRelevo;
    [Tooltip("Vazio = usa a paleta padrão do código (azul escuro -10, azul -5, azul claro -2,5, verdes +2,5 e +5, laranja = escada, roxo = ladeira). Preencha só se quiser outras cores.")]
    [SerializeField] List<ZonaDeRelevo> zonasDeRelevo = new List<ZonaDeRelevo>();

    [Header("Objetos (casas, bloqueios de rua...)")]
    [Tooltip("Onde ficam os objetos colocados à mão. O gerador nunca apaga o que está aqui. Se ficar vazio, ele é criado ao usar a paleta.")]
    [SerializeField] Transform raizDosObjetos;
    [Tooltip("Prefabs pra criar na cena pelos botões do Inspector. Aceita prefab do ProBuilder, do Blender ou qualquer outro.")]
    [SerializeField] List<GameObject> paletaDeObjetos = new List<GameObject>();

    [Header("Bordas")]
    [Tooltip("Paredes invisíveis onde o chão acaba, pra ninguém cair do mapa. As ruas bloqueadas ficam por conta dos prefabs de bloqueio.")]
    [SerializeField] bool bloquearBordas = true;
    [SerializeField] float alturaDasBordas = 4f;

    [Header("Inimigos (pontos de encontro escondidos, colocados à mão por vocês)")]
    [SerializeField] List<InimigoDefinicao> inimigos = new List<InimigoDefinicao>();

    public BilixinsData[] bilixins;

    

    [Header("Player")]
    [SerializeField] SpawnDefinicao player;

    [Header("Batalha")]
    public GameObject battlePrefab;

    [Header("Evento apos gerar")]
    public UnityEvent aoTerminarDeGerar;

    // O player instanciado no spawn, disponível depois que aoTerminarDeGerar dispara.
    public GameObject PlayerInstanciado { get; private set; }

    const int ToleranciaCor = 900;
    const float SegundosPorFrame = 0.008f;
    const float TamanhoTriggerPadrao = 3f;
    const float AlturaTrigger = 3f;

    float inicioDoFrame;

    void Start()
    {
        StartCoroutine(GerarMapaInterno(instantaneo: false));
    }

    // Gera o mapa na hora, sem pausas por frame — pra ver o resultado no Editor sem apertar Play.
    public void GerarPreviewNoEditor()
    {
        var rotina = GerarMapaInterno(instantaneo: true);
        while (rotina.MoveNext()) { }
    }

    public void LimparMapa()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject filho = transform.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(filho);
            else DestroyImmediate(filho);
        }

        // O player não é filho do MapaGerador (não pode entrar na combinação de meshes), então
        // limpa à parte. Remove qualquer Player solto na cena por tag, não só pela referência
        // guardada — se o preview do Editor sobreviver ao entrar em Play (depende da config de
        // Enter Play Mode Settings da Unity), a referência antiga ainda é válida, mas é mais
        // seguro garantir que não sobra nenhum player duplicado de qualquer jeito.
        foreach (GameObject p in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (Application.isPlaying) Destroy(p);
            else DestroyImmediate(p);
        }
        PlayerInstanciado = null;
    }

    IEnumerator GerarMapaInterno(bool instantaneo)
    {
        LimparMapa();

        if (imagemDoMapa == null)
        {
            Debug.LogError("MapaGerador: atribua a pixel art no Inspector.");
            yield break;
        }
        if (!imagemDoMapa.isReadable)
        {
            Debug.LogError($"MapaGerador: a imagem '{imagemDoMapa.name}' não está legível. Coloque-a em Assets/Art/Mapa/ e clique com o botão direito > Reimport.");
            yield break;
        }

        TileDefinicao[] tiles = TilesValidos();
        List<InimigoDefinicao> inimigosValidos = InimigosValidos();

        int largura = imagemDoMapa.width;
        int altura  = imagemDoMapa.height;
        Color32[] pixels = imagemDoMapa.GetPixels32();

        // Altura e inclinação de cada pixel (soma de todas as regiões que o cobrem)
        Relevo relevo = CalcularRelevo(largura, altura, pixels, tiles);

        bool playerValido = player != null && player.prefab != null &&
            !(player.cor.r == 0 && player.cor.g == 0 && player.cor.b == 0 && player.cor.a == 0);

        // Camada de chão (vira retângulos), árvores (um por pixel) e inimigos (um por pixel, com trigger)
        // Guarda também qual piso nasceu embaixo de cada ponto, pra apoiar o objeto na altura certa dele
        TileDefinicao[] camadaChao = new TileDefinicao[largura * altura];
        var arvores = new List<(int x, int y, TileDefinicao tile, TileDefinicao piso, TileDefinicao contexto)>();
        var inimigosAGerar = new List<(int x, int y, InimigoDefinicao inimigo, TileDefinicao piso)>();
        Vector2Int? posicaoPlayer = null;
        TileDefinicao pisoDoPlayer = null;

        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].a < 10) continue;

            int x = i % largura;
            int y = i / largura;
            Color32 cor = pixels[i];

            if (playerValido && posicaoPlayer == null && DiferencaCor(cor, player.cor) < ToleranciaCor)
            {
                posicaoPlayer = new Vector2Int(x, y);
                pisoDoPlayer = ResolverPiso(player.piso, x, y, largura, altura, pixels);
                if (pisoDoPlayer != null && pisoDoPlayer.prefab != null) camadaChao[i] = pisoDoPlayer;
                continue;
            }

            InimigoDefinicao inimigo = EncontrarInimigo(cor, inimigosValidos);
            if (inimigo != null)
            {
                TileDefinicao piso = ResolverPiso(inimigo.piso, x, y, largura, altura, pixels);
                inimigosAGerar.Add((x, y, inimigo, piso));
                if (piso != null && piso.prefab != null) camadaChao[i] = piso;
                continue;
            }

            TileDefinicao tile = EncontrarTile(cor, tiles);
            if (tile == null) continue;

            if (tile.redimensionar)
            {
                camadaChao[i] = tile;
            }
            else
            {
                TileDefinicao contexto = PisoDoContexto(x, y, largura, altura, pixels);
                arvores.Add((x, y, tile, grama, contexto));
                if (grama.prefab != null) camadaChao[i] = grama;
            }
        }

        bool[] usado = new bool[largura * altura];
        int total = 0;
        inicioDoFrame = Time.realtimeSinceStartup;

        for (int y = 0; y < altura; y++)
        {
            for (int x = 0; x < largura; x++)
            {
                int i = y * largura + x;
                TileDefinicao tile = camadaChao[i];
                if (tile == null || usado[i]) continue;

                // Piso na ladeira continua o mesmo plano inclinado; o resto só junta pixels no mesmo nível
                bool piso = EhPiso(tile);
                float nivel = relevo.nivel[i];
                float gx = relevo.gx[i];
                float gz = relevo.gz[i];
                float passo = relevo.passo[i];

                // Estica para a direita enquanto a cor e o plano forem os mesmos
                int w = 1;
                while (x + w < largura && !usado[i + w] && camadaChao[i + w] == tile &&
                       relevo.NoMesmoPlano(i + w, piso ? nivel + gx * w : nivel, gx, gz, passo)) w++;

                // Estica para baixo enquanto a linha inteira for igual
                int h = 1;
                while (y + h < altura && LinhaIgual(camadaChao, relevo, usado, largura, x, y + h, w, tile, piso, i, h)) h++;

                for (int dy = 0; dy < h; dy++)
                    for (int dx = 0; dx < w; dx++)
                        usado[(y + dy) * largura + x + dx] = true;

                bool inclinado = Mathf.Abs(gx) > 0.00001f || Mathf.Abs(gz) > 0.00001f;

                // Fundação: o bloco desce até o vizinho mais baixo, pra não ficar flutuando sobre outro nível
                float vizinho = BaseDoVizinhoMaisBaixo(x, y, w, h, camadaChao, relevo, largura, altura);

                if (inclinado && piso && passo > 0.001f && (tile != grama || relevo.degrausNaGrama[i]))
                {
                    CriarEscada(tile, x, y, w, h, nivel, gx, gz, passo, vizinho);
                }
                else if (inclinado && piso)
                {
                    CriarPlacaInclinada(tile, x, y, w, h, nivel, gx, gz, vizinho);
                }
                else if (inclinado)
                {
                    // Construção na ladeira: a base desce até o ponto mais baixo do terreno e o topo fica reto
                    float desce = Mathf.Abs(gx) * w + Mathf.Abs(gz) * h;
                    float centro = nivel + gx * (w - 1) / 2f + gz * (h - 1) / 2f;
                    float baseDoBloco = tile.offsetY + centro - desce / 2f;
                    float extra = Mathf.Max(0f, baseDoBloco - vizinho);

                    GameObject obj = Instantiate(tile.prefab, transform);
                    Vector3 tamanho = new Vector3(w * tamanhoDoTile, tile.altura + desce + extra, h * tamanhoDoTile);
                    Encaixar(obj, CentroDaArea(x, y, w, h), tamanho, baseDoBloco - extra);
                }
                else
                {
                    float baseDoBloco = tile.offsetY + nivel;
                    float extra = Mathf.Max(0f, baseDoBloco - vizinho);

                    GameObject obj = Instantiate(tile.prefab, transform);
                    Vector3 tamanho = new Vector3(w * tamanhoDoTile, tile.altura + extra, h * tamanhoDoTile);
                    Encaixar(obj, CentroDaArea(x, y, w, h), tamanho, baseDoBloco - extra);
                }
                total++;

                if (!instantaneo && PassouDoTempo()) { yield return null; inicioDoFrame = Time.realtimeSinceStartup; }
            }
        }

        foreach (var (x, y, tile, piso, contexto) in arvores)
        {
            GameObject obj = Instantiate(tile.prefab, transform);

            // Se o prefab for modular (ex: árvore voxel), cada um sai com variações diferentes, sempre as mesmas pra mesma posição
            ObjetoModular modular = obj.GetComponent<ObjetoModular>();
            if (modular != null) modular.Aleatorizar(x * 73856093 ^ y * 19349663);

            float nivel = relevo.nivel[y * largura + x];
            float baseY = AlturaDoPiso(piso) + nivel;
            Encaixar(obj, CentroDaArea(x, y, 1, 1), Vector3.zero, baseY);
            AdicionarPreenchimentoDeColisao(x, y, baseY, AlturaDoPiso(contexto) + nivel);
            total++;

            if (!instantaneo && PassouDoTempo()) { yield return null; inicioDoFrame = Time.realtimeSinceStartup; }
        }

        foreach (var (x, y, inimigo, piso) in inimigosAGerar)
        {
            GameObject obj = Instantiate(inimigo.prefab, transform);
            Encaixar(obj, CentroDaArea(x, y, 1, 1), Vector3.zero, AlturaDoPiso(piso) + relevo.nivel[y * largura + x]);
            AdicionarGatilhoDeEncontro(
            obj,
            inimigo,
            inimigo.tamanhoTrigger > 0f
                ? inimigo.tamanhoTrigger
                : TamanhoTriggerPadrao
        );
            total++;

            if (!instantaneo && PassouDoTempo()) { yield return null; inicioDoFrame = Time.realtimeSinceStartup; }
        }

        if (bloquearBordas) CriarParedesInvisiveis(camadaChao, relevo, largura, altura);

        // Combinar meshes só faz sentido em runtime de verdade, não no preview do Editor
        if (Application.isPlaying)
        {
            StaticBatchingUtility.Combine(gameObject);

            // As casas e bloqueios colocados à mão também entram, pra poupar draw calls no celular
            if (raizDosObjetos != null && raizDosObjetos.childCount > 0) StaticBatchingUtility.Combine(raizDosObjetos.gameObject);
        }

        // Player fica fora da combinação acima (ele se move, não pode ser "assado" junto com o mapa)
        if (posicaoPlayer.HasValue)
        {
            PlayerInstanciado = Instantiate(player.prefab);

            // Desliga o CharacterController antes de teleportar e religa depois — mover o
            // Transform direto com o controller ligado deixa o estado interno dele (chão,
            // colisão) inconsistente de vez em quando, causando esse "nasce fora do mapa"
            // só em algumas rodadas.
            CharacterController controller = PlayerInstanciado.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;

            Encaixar(PlayerInstanciado, CentroDaArea(posicaoPlayer.Value.x, posicaoPlayer.Value.y, 1, 1), Vector3.zero,
                AlturaDoPiso(pisoDoPlayer) + relevo.nivel[posicaoPlayer.Value.y * largura + posicaoPlayer.Value.x]);

            if (controller != null) controller.enabled = true;
        }
        else if (playerValido)
        {
            Debug.LogWarning("MapaGerador: nenhum pixel com a cor do Player foi encontrado na pixel art.");
        }

        Debug.Log($"MapaGerador: mapa {largura}x{altura} gerado com {total} objetos.");
        aoTerminarDeGerar?.Invoke();
    }

    // BoxCollider.size é em espaço local, mas queremos um tamanho exato no mundo.
    // Divide pela escala do objeto pra garantir isso, não importa a escala do prefab.
    void AdicionarGatilhoDeEncontro(
    GameObject obj,
    InimigoDefinicao inimigo,
    float tamanhoEmTiles)
{
    float ladoMundo = tamanhoEmTiles * tamanhoDoTile;
    Vector3 escala = obj.transform.lossyScale;

    BoxCollider trigger = obj.AddComponent<BoxCollider>();
    trigger.isTrigger = true;

    trigger.size = new Vector3(
        ladoMundo / Mathf.Max(escala.x, 0.0001f),
        AlturaTrigger / Mathf.Max(escala.y, 0.0001f),
        ladoMundo / Mathf.Max(escala.z, 0.0001f)
    );

    trigger.center = new Vector3(
        0f,
        (AlturaTrigger / 2f) / Mathf.Max(escala.y, 0.0001f),
        0f
    );

    GatilhoDeEncontro gatilho =
        obj.AddComponent<GatilhoDeEncontro>();

    if (bilixins != null &&
        inimigo.idBilixin >= 0 &&
        inimigo.idBilixin < bilixins.Length)
    {
        gatilho.bilixinInimigo =
            bilixins[inimigo.idBilixin];
    }

    gatilho.battlePrefab = battlePrefab;
}

    // No Editor, com o MapaGerador selecionado, desenha cada região: um plano reto e transparente na altura
    // do lado mais alto (a referência do "reto"), e um triângulo mostrando a ladeira: linha reta (azul) a partir
    // do ponto mais alto, a descida (vermelho) e o chão inclinado de verdade (amarelo).
    void OnDrawGizmosSelected()
    {
        if (regioes == null) return;

        foreach (RegiaoDefinicao r in regioes)
        {
            if (r == null || r.largura <= 0 || r.altura <= 0) continue;

            Vector3 origem = transform.position + new Vector3(r.x * tamanhoDoTile, r.desnivel, r.y * tamanhoDoTile);
            Vector3 tamanho = new Vector3(r.largura * tamanhoDoTile, 0.02f, r.altura * tamanhoDoTile);
            Vector3 centro = origem + new Vector3(tamanho.x / 2f, 0f, tamanho.z / 2f);

            Color azul = new Color(0.2f, 0.8f, 1f, 1f);
            Gizmos.color = new Color(azul.r, azul.g, azul.b, 0.15f);
            Gizmos.DrawCube(centro, tamanho);
            Gizmos.color = azul;
            Gizmos.DrawWireCube(centro, tamanho);

            string texto = string.IsNullOrEmpty(r.nome) ? "Região" : r.nome;

            if (r.inclinacao > 0f)
            {
                // Pontos do meio do lado mais alto (alto) e do mais baixo (baixo), no chão
                Vector3 alto, baixo;
                float comprimento;
                switch (r.direcaoDaDescida)
                {
                    case DirecaoDaDescida.Norte:
                        alto = new Vector3(centro.x, origem.y, origem.z);
                        baixo = new Vector3(centro.x, origem.y - r.inclinacao, origem.z + tamanho.z);
                        comprimento = tamanho.z;
                        break;
                    case DirecaoDaDescida.Leste:
                        alto = new Vector3(origem.x, origem.y, centro.z);
                        baixo = new Vector3(origem.x + tamanho.x, origem.y - r.inclinacao, centro.z);
                        comprimento = tamanho.x;
                        break;
                    case DirecaoDaDescida.Oeste:
                        alto = new Vector3(origem.x + tamanho.x, origem.y, centro.z);
                        baixo = new Vector3(origem.x, origem.y - r.inclinacao, centro.z);
                        comprimento = tamanho.x;
                        break;
                    default: // Sul
                        alto = new Vector3(centro.x, origem.y, origem.z + tamanho.z);
                        baixo = new Vector3(centro.x, origem.y - r.inclinacao, origem.z);
                        comprimento = tamanho.z;
                        break;
                }

                Vector3 mesmoNivelDoAlto = new Vector3(baixo.x, alto.y, baixo.z);

                Gizmos.color = azul;
                Gizmos.DrawLine(alto, mesmoNivelDoAlto);   // o reto
                Gizmos.color = Color.red;
                Gizmos.DrawLine(mesmoNivelDoAlto, baixo);  // quanto desce
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(alto, baixo);              // o chão

                float graus = Mathf.Atan2(r.inclinacao, comprimento) * Mathf.Rad2Deg;
                texto += $": desce {r.inclinacao:0.#} m em {comprimento:0.#} m ({graus:0.#}°)";
            }
            else
            {
                texto += $": plana, a {r.desnivel:0.#} m";
            }

#if UNITY_EDITOR
            UnityEditor.Handles.Label(centro + Vector3.up * 0.5f, texto);
#endif
        }
    }

    // Paredes invisíveis ao redor de onde o chão acaba, pra ninguém cair do mapa. Cada trecho reto vira uma caixa só.
    // Ficam na layer "Ignore Raycast": o personagem esbarra nelas, mas a câmera (que usa SphereCast) passa por dentro.
    void CriarParedesInvisiveis(TileDefinicao[] camada, Relevo relevo, int largura, int altura)
    {
        GameObject raiz = new GameObject("BordasInvisiveis");
        raiz.transform.SetParent(transform);
        raiz.layer = 2;

        bool Chao(int x, int y) => x >= 0 && x < largura && y >= 0 && y < altura && camada[y * largura + x] != null;

        // Bordas de cima e de baixo: varre cada linha
        for (int y = 0; y < altura; y++)
        {
            for (int lado = -1; lado <= 1; lado += 2)
            {
                int x = 0;
                while (x < largura)
                {
                    if (!Chao(x, y) || Chao(x, y + lado)) { x++; continue; }

                    int inicio = x;
                    float nivel = relevo.nivel[y * largura + x];
                    while (x < largura && Chao(x, y) && !Chao(x, y + lado) && Mathf.Abs(relevo.nivel[y * largura + x] - nivel) < 0.01f) x++;

                    AdicionarParedeInvisivel(raiz.transform, inicio, y + lado, x - inicio, 1, nivel);
                }
            }
        }

        // Bordas da esquerda e da direita: varre cada coluna
        for (int x = 0; x < largura; x++)
        {
            for (int lado = -1; lado <= 1; lado += 2)
            {
                int y = 0;
                while (y < altura)
                {
                    if (!Chao(x, y) || Chao(x + lado, y)) { y++; continue; }

                    int inicio = y;
                    float nivel = relevo.nivel[y * largura + x];
                    while (y < altura && Chao(x, y) && !Chao(x + lado, y) && Mathf.Abs(relevo.nivel[y * largura + x] - nivel) < 0.01f) y++;

                    AdicionarParedeInvisivel(raiz.transform, x + lado, inicio, 1, y - inicio, nivel);
                }
            }
        }
    }

    // Caixa sobre o retângulo de pixels (x, y, w, h), do nível do chão menos uma folga até a altura das bordas
    void AdicionarParedeInvisivel(Transform pai, int x, int y, int w, int h, float nivel)
    {
        float folga = 2f;
        float alturaTotal = alturaDasBordas + folga * 2f;

        GameObject parede = new GameObject("Borda");
        parede.layer = 2;
        parede.transform.SetParent(pai);

        Vector3 centro = CentroDaArea(x, y, w, h);
        parede.transform.position = new Vector3(centro.x, nivel - folga + alturaTotal / 2f, centro.z);

        BoxCollider caixa = parede.AddComponent<BoxCollider>();
        caixa.size = new Vector3(w * tamanhoDoTile, alturaTotal, h * tamanhoDoTile);
    }

#if UNITY_EDITOR
    public IReadOnlyList<GameObject> PaletaDeObjetos => paletaDeObjetos;

    // O objeto que guarda as casas e bloqueios colocados à mão (se ainda não foi atribuído, procura um chamado "Objetos")
    public Transform RaizDosObjetos
    {
        get
        {
            if (raizDosObjetos == null)
            {
                GameObject achado = GameObject.Find("Objetos");
                if (achado != null) raizDosObjetos = achado.transform;
            }
            return raizDosObjetos;
        }
    }

    // Cria uma cópia do prefab na cena, no ponto que a Scene view está olhando, e já encaixa no chão
    public void CriarObjetoNaCena(GameObject prefab)
    {
        if (prefab == null) return;

        if (raizDosObjetos == null)
        {
            GameObject raiz = new GameObject("Objetos");
            UnityEditor.Undo.RegisterCreatedObjectUndo(raiz, "Criar raiz dos objetos");
            raizDosObjetos = raiz.transform;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        var instancia = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, raizDosObjetos);
        UnityEditor.Undo.RegisterCreatedObjectUndo(instancia, "Criar objeto");

        var cena = UnityEditor.SceneView.lastActiveSceneView;
        if (cena != null) instancia.transform.position = cena.pivot;

        var modular = instancia.GetComponent<ObjetoModular>();
        if (modular != null) modular.EncaixarAgora();

        UnityEditor.Selection.activeGameObject = instancia;
    }
#endif

    bool PassouDoTempo()
    {
        return Time.realtimeSinceStartup - inicioDoFrame > SegundosPorFrame;
    }

    // Continua o retângulo para baixo só se a linha de baixo estiver inteira no mesmo plano.
    // origem = primeiro pixel do retângulo; descida = quantas linhas ele já desceu.
    static bool LinhaIgual(TileDefinicao[] camada, Relevo relevo, bool[] usado, int largura, int x, int y, int w, TileDefinicao tile, bool piso, int origem, int descida)
    {
        float nivel = relevo.nivel[origem];
        float gx = relevo.gx[origem];
        float gz = relevo.gz[origem];
        float passo = relevo.passo[origem];

        for (int dx = 0; dx < w; dx++)
        {
            int j = y * largura + x + dx;
            if (usado[j] || camada[j] != tile) return false;

            float esperado = piso ? nivel + gx * dx + gz * descida : nivel;
            if (!relevo.NoMesmoPlano(j, esperado, gx, gz, passo)) return false;
        }
        return true;
    }

    // Altura do centro de cada pixel e quanto ela sobe por pixel em x e em z (a ladeira)
    class Relevo
    {
        public readonly float[] nivel;
        public readonly float[] gx;
        public readonly float[] gz;
        public readonly float[] passo;   // altura do degrau, 0 = ladeira lisa
        public readonly bool[] degrausNaGrama;   // a grama também vira degrau (ladeira em voxel)

        public Relevo(int tamanho)
        {
            nivel = new float[tamanho];
            gx = new float[tamanho];
            gz = new float[tamanho];
            passo = new float[tamanho];
            degrausNaGrama = new bool[tamanho];
        }

        public bool NoMesmoPlano(int i, float nivelEsperado, float gx0, float gz0, float passo0)
        {
            return Mathf.Abs(nivel[i] - nivelEsperado) < 0.001f
                && Mathf.Abs(gx[i] - gx0) < 0.00001f
                && Mathf.Abs(gz[i] - gz0) < 0.00001f
                && Mathf.Abs(passo[i] - passo0) < 0.0001f;
        }
    }

    // Soma, pixel a pixel, o desnível e a ladeira das regiões do Inspector e da imagem de relevo.
    Relevo CalcularRelevo(int largura, int altura, Color32[] pixelsMapa, TileDefinicao[] tiles)
    {
        var relevo = new Relevo(largura * altura);

        foreach (RegiaoDefinicao regiao in regioes)
        {
            if (regiao == null) continue;
            AplicarRegiao(relevo, regiao, largura, altura, null);
        }

        AplicarImagemDeRelevo(relevo, largura, altura, pixelsMapa, tiles);

        // Resumo no Console: se o mapa sair plano, aqui dá pra ver por quê
        int comAltura = 0, comLadeira = 0;
        for (int i = 0; i < relevo.nivel.Length; i++)
        {
            if (!Mathf.Approximately(relevo.nivel[i], 0f)) comAltura++;
            if (relevo.gx[i] != 0f || relevo.gz[i] != 0f) comLadeira++;
        }

        string imagem = imagemDeRelevo != null ? imagemDeRelevo.name : "nenhuma";
        Debug.Log($"MapaGerador: relevo — {regioes.Count} regiões do Inspector, imagem de relevo: {imagem}, {comAltura} pixels fora do nível 0, {comLadeira} em ladeira ou escada.");
        if (comAltura == 0)
            Debug.LogWarning("MapaGerador: nenhum relevo foi aplicado, então o mapa sai todo plano. Atribua a 'Imagem De Relevo' (ou crie regiões no Inspector).");

        return relevo;
    }

#if UNITY_EDITOR
    // Ao escolher a imagem do mapa, procura sozinho a imagem de relevo com o mesmo nome + "_relevo"
    // (ex: mapa_tutorial.png -> mapa_tutorial_relevo.png), pra não depender de lembrar de atribuir.
    void OnValidate()
    {
        if (imagemDoMapa == null || imagemDeRelevo != null) return;

        string caminho = UnityEditor.AssetDatabase.GetAssetPath(imagemDoMapa);
        if (string.IsNullOrEmpty(caminho)) return;

        string candidato = System.IO.Path.ChangeExtension(caminho, null) + "_relevo.png";
        imagemDeRelevo = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(candidato);
    }
#endif

    // Aplica uma região ao relevo. Se vier uma máscara, só os pixels marcados nela entram (mas as
    // distâncias da ladeira continuam sendo medidas pelo retângulo inteiro da região).
    static void AplicarRegiao(Relevo relevo, RegiaoDefinicao regiao, int largura, int altura, bool[] mascara)
    {
        if (Mathf.Approximately(regiao.desnivel, 0f) && Mathf.Approximately(regiao.inclinacao, 0f)) return;

        int xIni = Mathf.Max(regiao.x, 0);
        int yIni = Mathf.Max(regiao.y, 0);
        int xFim = Mathf.Min(regiao.x + regiao.largura, largura);
        int yFim = Mathf.Min(regiao.y + regiao.altura, altura);

        // Quantos metros o chão desce a cada pixel, ao longo do comprimento da região
        DirecaoDaDescida lado = regiao.direcaoDaDescida;
        bool pelaZ = lado == DirecaoDaDescida.Norte || lado == DirecaoDaDescida.Sul;
        int comprimento = pelaZ ? regiao.altura : regiao.largura;
        float porPixel = comprimento > 0 ? regiao.inclinacao / comprimento : 0f;

        for (int y = yIni; y < yFim; y++)
        {
            for (int x = xIni; x < xFim; x++)
            {
                int i = y * largura + x;
                if (mascara != null && !mascara[i]) continue;

                // Distância, em pixels, do lado mais alto da região até o centro deste pixel
                float distancia;
                if (lado == DirecaoDaDescida.Sul)        { distancia = (regiao.y + regiao.altura) - (y + 0.5f); relevo.gz[i] += porPixel; }
                else if (lado == DirecaoDaDescida.Norte) { distancia = (y + 0.5f) - regiao.y;                   relevo.gz[i] -= porPixel; }
                else if (lado == DirecaoDaDescida.Leste) { distancia = (x + 0.5f) - regiao.x;                   relevo.gx[i] -= porPixel; }
                else                                     { distancia = (regiao.x + regiao.largura) - (x + 0.5f); relevo.gx[i] += porPixel; }

                relevo.nivel[i] += regiao.desnivel - porPixel * distancia;
                if (regiao.inclinacao > 0f)
                {
                    relevo.passo[i] = regiao.alturaDoDegrau;
                    relevo.degrausNaGrama[i] = regiao.degrausNaGrama;
                }
            }
        }
    }

    // Cores padrão do relevo, usadas quando a lista do Inspector está vazia. Pixel transparente ou sem cor = nível 0.
    static List<ZonaDeRelevo> PaletaPadraoDeRelevo()
    {
        return new List<ZonaDeRelevo>
        {
            new ZonaDeRelevo { nome = "Plano -10",  cor = new Color32(0, 0, 139, 255),     desnivel = -10f },
            new ZonaDeRelevo { nome = "Plano -7,5", cor = new Color32(70, 70, 200, 255),   desnivel = -7.5f },
            new ZonaDeRelevo { nome = "Plano -5",   cor = new Color32(0, 100, 255, 255),   desnivel = -5f },
            new ZonaDeRelevo { nome = "Plano -2,5", cor = new Color32(120, 200, 255, 255), desnivel = -2.5f },
            new ZonaDeRelevo { nome = "Plano +2,5", cor = new Color32(0, 170, 0, 255),     desnivel = 2.5f },
            new ZonaDeRelevo { nome = "Plano +5",   cor = new Color32(140, 230, 120, 255), desnivel = 5f },
            new ZonaDeRelevo { nome = "Escada",     cor = new Color32(255, 140, 0, 255),   tipo = TipoDeRelevo.Escada, alturaDoDegrau = 0.2f },
            new ZonaDeRelevo { nome = "Ladeira",    cor = new Color32(160, 32, 240, 255),  tipo = TipoDeRelevo.Ladeira },
        };
    }

    // Lê a imagem de relevo (mesmo tamanho do mapa). Planos pintados viram altura. Escada e ladeira ligam sozinhas
    // os planos que estão nas pontas delas: a altura e o lado da descida saem da diferença entre os dois planos.
    void AplicarImagemDeRelevo(Relevo relevo, int largura, int altura, Color32[] pixelsMapa, TileDefinicao[] tiles)
    {
        if (imagemDeRelevo == null) return;

        if (!imagemDeRelevo.isReadable || imagemDeRelevo.width != largura || imagemDeRelevo.height != altura)
        {
            Debug.LogError($"MapaGerador: a imagem de relevo '{imagemDeRelevo.name}' precisa estar legível (em Assets/Art/Mapa/) e ter o mesmo tamanho do mapa ({largura}x{altura}). Ela foi ignorada.");
            return;
        }

        List<ZonaDeRelevo> zonas = zonasDeRelevo != null && zonasDeRelevo.Count > 0 ? zonasDeRelevo : PaletaPadraoDeRelevo();
        Color32[] pixels = imagemDeRelevo.GetPixels32();
        int[] zonaDoPixel = new int[pixels.Length];   // -1 = sem relevo (nível normal)
        int desconhecidos = 0;

        for (int i = 0; i < pixels.Length; i++)
        {
            zonaDoPixel[i] = -1;
            if (pixels[i].a < 10) continue;

            int melhor = -1;
            int menorDist = ToleranciaCor;
            for (int z = 0; z < zonas.Count; z++)
            {
                int dist = DiferencaCor(pixels[i], zonas[z].cor);
                if (dist < menorDist) { menorDist = dist; melhor = z; }
            }

            if (melhor < 0) { desconhecidos++; continue; }

            zonaDoPixel[i] = melhor;
            if (zonas[melhor].tipo == TipoDeRelevo.Plano) relevo.nivel[i] += zonas[melhor].desnivel;
        }

        if (desconhecidos > 0)
            Debug.LogWarning($"MapaGerador: {desconhecidos} pixels da imagem de relevo têm uma cor que não está na paleta e foram ignorados (ficam no nível 0).");

        // Cada mancha conectada de escada ou ladeira vira uma região
        bool[] visitado = new bool[pixels.Length];
        var pilha = new Stack<int>();
        int[] vizinhoX = { -1, 1, 0, 0 };
        int[] vizinhoY = { 0, 0, -1, 1 };

        for (int inicio = 0; inicio < pixels.Length; inicio++)
        {
            int zona = zonaDoPixel[inicio];
            if (zona < 0 || visitado[inicio] || zonas[zona].tipo == TipoDeRelevo.Plano) continue;

            var mascara = new bool[pixels.Length];
            int xMin = largura, xMax = -1, yMin = altura, yMax = -1;

            pilha.Push(inicio);
            visitado[inicio] = true;
            while (pilha.Count > 0)
            {
                int p = pilha.Pop();
                int px = p % largura;
                int py = p / largura;
                mascara[p] = true;
                xMin = Mathf.Min(xMin, px); xMax = Mathf.Max(xMax, px);
                yMin = Mathf.Min(yMin, py); yMax = Mathf.Max(yMax, py);

                for (int d = 0; d < 4; d++)
                {
                    int nx = px + vizinhoX[d];
                    int ny = py + vizinhoY[d];
                    if (nx < 0 || nx >= largura || ny < 0 || ny >= altura) continue;

                    int n = ny * largura + nx;
                    if (visitado[n] || zonaDoPixel[n] != zona) continue;

                    visitado[n] = true;
                    pilha.Push(n);
                }
            }

            AplicarMancha(relevo, zonas[zona], mascara, xMin, yMin, xMax, yMax, zonaDoPixel, zonas, pixelsMapa, tiles, largura, altura);
        }
    }

    // O mapa tem chão neste pixel? Parede (prédio) e vazio não contam; spawn, inimigo e árvore nascem sobre um piso.
    // soPavimento = só rua, calçada e chão (onde se anda de verdade); grama não conta.
    bool TemPisoNoMapa(int i, Color32[] pixelsMapa, TileDefinicao[] tiles, bool soPavimento)
    {
        if (pixelsMapa == null) return true;
        if (pixelsMapa[i].a < 10) return false;

        TileDefinicao tile = EncontrarTile(pixelsMapa[i], tiles);
        if (soPavimento) return tile == rua || tile == calcada || tile == chao;
        return tile == null || EhPiso(tile) || !tile.redimensionar;
    }

    // Nível do plano ao longo de um lado da mancha (um segmento de pixels): só conta onde o mapa tem piso,
    // então uma casa encostada na escada não é confundida com o chão em que a escada chega.
    // Devolve o nível mais comum no segmento, ou null se ali não tem piso.
    float? NivelDoLado(int xIni, int yIni, int xFim, int yFim, int[] zonaDoPixel, List<ZonaDeRelevo> zonas,
        Color32[] pixelsMapa, TileDefinicao[] tiles, int largura, int altura, bool soPavimento)
    {
        var contagem = new Dictionary<float, int>();

        for (int y = yIni; y <= yFim; y++)
        {
            for (int x = xIni; x <= xFim; x++)
            {
                if (x < 0 || x >= largura || y < 0 || y >= altura) continue;

                int i = y * largura + x;
                if (!TemPisoNoMapa(i, pixelsMapa, tiles, soPavimento)) continue;

                int zona = zonaDoPixel[i];
                float nivel;
                if (zona < 0) nivel = 0f;
                else if (zonas[zona].tipo == TipoDeRelevo.Plano) nivel = zonas[zona].desnivel;
                else continue;   // outra escada ou ladeira

                contagem.TryGetValue(nivel, out int atual);
                contagem[nivel] = atual + 1;
            }
        }

        float? melhor = null;
        int mais = 0;
        foreach (var par in contagem)
        {
            if (par.Value > mais) { mais = par.Value; melhor = par.Key; }
        }
        return melhor;
    }

    void AplicarMancha(Relevo relevo, ZonaDeRelevo zona, bool[] mascara, int xMin, int yMin, int xMax, int yMax,
        int[] zonaDoPixel, List<ZonaDeRelevo> zonas, Color32[] pixelsMapa, TileDefinicao[] tiles, int largura, int altura)
    {
        // Primeiro olha só o pavimento (rua e calçada). Se não achar planos diferentes, aceita qualquer piso, grama inclusive.
        float? sul = null, norte = null, oeste = null, leste = null;
        float difZ = 0f, difX = 0f;
        bool pelaZ = true;
        float dif = 0f;

        for (int tentativa = 0; tentativa < 2; tentativa++)
        {
            bool soPavimento = tentativa == 0;

            sul   = NivelDoLado(xMin, yMin - 1, xMax, yMin - 1, zonaDoPixel, zonas, pixelsMapa, tiles, largura, altura, soPavimento);
            norte = NivelDoLado(xMin, yMax + 1, xMax, yMax + 1, zonaDoPixel, zonas, pixelsMapa, tiles, largura, altura, soPavimento);
            oeste = NivelDoLado(xMin - 1, yMin, xMin - 1, yMax, zonaDoPixel, zonas, pixelsMapa, tiles, largura, altura, soPavimento);
            leste = NivelDoLado(xMax + 1, yMin, xMax + 1, yMax, zonaDoPixel, zonas, pixelsMapa, tiles, largura, altura, soPavimento);

            difZ = sul.HasValue && norte.HasValue ? norte.Value - sul.Value : 0f;
            difX = oeste.HasValue && leste.HasValue ? leste.Value - oeste.Value : 0f;

            pelaZ = Mathf.Abs(difZ) >= Mathf.Abs(difX);
            dif = pelaZ ? difZ : difX;

            if (Mathf.Abs(dif) >= 0.001f) break;
        }

        if (Mathf.Abs(dif) < 0.001f)
        {
            Debug.LogWarning($"MapaGerador: '{zona.nome}' em ({xMin},{yMin}) a ({xMax},{yMax}) não achou planos de alturas diferentes nas pontas e ficou plana. Pinte um plano mais baixo de um lado e um mais alto do outro.");
            return;
        }

        var regiao = new RegiaoDefinicao
        {
            nome = zona.nome,
            x = xMin,
            y = yMin,
            largura = xMax - xMin + 1,
            altura = yMax - yMin + 1,
            inclinacao = Mathf.Abs(dif),
            alturaDoDegrau = zona.tipo == TipoDeRelevo.Escada ? zona.alturaDoDegrau : (ladeiraEmDegraus ? alturaDoDegrauDaLadeira : 0f),
            degrausNaGrama = zona.tipo == TipoDeRelevo.Ladeira && ladeiraEmDegraus
        };

        // O lado mais alto dá o desnível; o chão desce pro lado do plano mais baixo
        if (pelaZ)
        {
            regiao.direcaoDaDescida = dif > 0f ? DirecaoDaDescida.Sul : DirecaoDaDescida.Norte;
            regiao.desnivel = dif > 0f ? norte.Value : sul.Value;
        }
        else
        {
            regiao.direcaoDaDescida = dif > 0f ? DirecaoDaDescida.Oeste : DirecaoDaDescida.Leste;
            regiao.desnivel = dif > 0f ? leste.Value : oeste.Value;
        }

        AplicarRegiao(relevo, regiao, largura, altura, mascara);
    }

    // Os 4 pisos são o chão: na ladeira eles seguem a inclinação. Prédio e árvore não.
    bool EhPiso(TileDefinicao tile)
    {
        return tile != null && (tile == grama || tile == rua || tile == calcada || tile == chao);
    }

    // Piso na ladeira: um bloco com o topo no plano inclinado, com o material do prefab do próprio tile.
    // x, y, w, h em pixels; nivel = altura do centro do primeiro pixel; gx, gz = subida por pixel.
    void CriarPlacaInclinada(TileDefinicao tile, int x, int y, int w, int h, float nivel, float gx, float gz, float baseVizinho)
    {
        float topo = tile.offsetY + tile.altura;
        float Canto(float px, float pz) => topo + nivel + gx * (px - (x + 0.5f)) + gz * (pz - (y + 0.5f));

        float sw = Canto(x, y), se = Canto(x + w, y), nw = Canto(x, y + h), ne = Canto(x + w, y + h);
        float fundo = Mathf.Min(Mathf.Min(Mathf.Min(sw, se), Mathf.Min(nw, ne)) - Mathf.Max(tile.altura, 0.05f), baseVizinho);

        GameObject obj = new GameObject($"{Nome(tile.nome, tile.prefab)} (ladeira)");
        obj.transform.SetParent(transform);
        obj.transform.position = CentroDaArea(x, y, w, h);

        Mesh malha = MalhaDaPlaca(w * tamanhoDoTile, h * tamanhoDoTile, fundo, sw, se, nw, ne);
        obj.AddComponent<MeshFilter>().sharedMesh = malha;
        obj.AddComponent<MeshRenderer>().sharedMaterial = MaterialDoTile(tile);
        obj.AddComponent<MeshCollider>().sharedMesh = malha;
    }

    // Base mais baixa entre os pixels vizinhos de um retângulo (só conta onde existe tile). Serve de fundação:
    // o bloco desce até ali em vez de ficar flutuando sobre um nível mais baixo (vira um muro de arrimo).
    static float BaseDoVizinhoMaisBaixo(int x, int y, int w, int h, TileDefinicao[] camada, Relevo relevo, int largura, int altura)
    {
        float menor = float.PositiveInfinity;

        void Ver(int px, int py)
        {
            if (px < 0 || px >= largura || py < 0 || py >= altura) return;

            int j = py * largura + px;
            if (camada[j] == null) return;

            float baixo = relevo.nivel[j] - (Mathf.Abs(relevo.gx[j]) + Mathf.Abs(relevo.gz[j])) / 2f;
            if (baixo < menor) menor = baixo;
        }

        for (int dx = 0; dx < w; dx++) { Ver(x + dx, y - 1); Ver(x + dx, y + h); }
        for (int dy = 0; dy < h; dy++) { Ver(x - 1, y + dy); Ver(x + w, y + dy); }

        return menor;
    }

    // Escada de verdade: um bloco por degrau, subindo na direção em que o plano sobe.
    // O número de degraus vem da altura que ela vence dividida pela altura do degrau (passo).
    void CriarEscada(TileDefinicao tile, int x, int y, int w, int h, float nivel, float gx, float gz, float passo, float baseVizinho)
    {
        float topo = tile.offsetY + tile.altura;
        float Canto(float px, float pz) => topo + nivel + gx * (px - (x + 0.5f)) + gz * (pz - (y + 0.5f));

        // A subida é por um eixo só (z ou x). O giro vira a escada pra direção em que o chão sobe.
        bool pelaZ = Mathf.Abs(gz) >= Mathf.Abs(gx);
        float sobe = pelaZ ? gz : gx;
        float giro = pelaZ ? (sobe > 0f ? 0f : 180f) : (sobe > 0f ? 90f : 270f);

        float pontaA = pelaZ ? Canto(x + w / 2f, y) : Canto(x, y + h / 2f);
        float pontaB = pelaZ ? Canto(x + w / 2f, y + h) : Canto(x + w, y + h / 2f);
        float topoBaixo = Mathf.Min(pontaA, pontaB);
        float topoAlto = Mathf.Max(pontaA, pontaB);

        float comprimento = (pelaZ ? h : w) * tamanhoDoTile;
        float largo = (pelaZ ? w : h) * tamanhoDoTile;
        int degraus = Mathf.Max(1, Mathf.RoundToInt((topoAlto - topoBaixo) / passo));
        float fundo = Mathf.Min(topoBaixo - Mathf.Max(tile.altura, 0.05f), baseVizinho);

        GameObject obj = new GameObject($"{Nome(tile.nome, tile.prefab)} (escada)");
        obj.transform.SetParent(transform);
        obj.transform.position = CentroDaArea(x, y, w, h);
        obj.transform.rotation = Quaternion.Euler(0f, giro, 0f);

        Mesh malha = MalhaDeDegraus(largo, comprimento, fundo, topoBaixo, topoAlto, degraus);
        obj.AddComponent<MeshFilter>().sharedMesh = malha;
        obj.AddComponent<MeshRenderer>().sharedMaterial = MaterialDoTile(tile);
        obj.AddComponent<MeshCollider>().sharedMesh = malha;
    }

    // Degraus subindo pro +Z local. O primeiro degrau fica um passo acima do piso de baixo e o último
    // fica na altura do piso de cima. Só desenha as faces que aparecem (piso, espelho e as duas laterais).
    static Mesh MalhaDeDegraus(float largo, float comprimento, float fundo, float topoBaixo, float topoAlto, int degraus)
    {
        float lx = largo / 2f;
        float lz = comprimento / 2f;
        float profundidade = comprimento / degraus;
        float subida = (topoAlto - topoBaixo) / degraus;

        var vertices = new List<Vector3>();
        var triangulos = new List<int>();
        var uvs = new List<Vector2>();

        for (int d = 0; d < degraus; d++)
        {
            float z0 = -lz + d * profundidade;
            float z1 = z0 + profundidade;
            float topoDoDegrau = topoBaixo + (d + 1) * subida;
            float baseDoEspelho = d == 0 ? fundo : topoBaixo + d * subida;

            Vector3 t00 = new Vector3(-lx, topoDoDegrau, z0), t10 = new Vector3(lx, topoDoDegrau, z0);
            Vector3 t01 = new Vector3(-lx, topoDoDegrau, z1), t11 = new Vector3(lx, topoDoDegrau, z1);

            AdicionarFace(vertices, triangulos, uvs, t00, t01, t11, t10, Vector3.up);   // piso do degrau
            AdicionarFace(vertices, triangulos, uvs, new Vector3(-lx, baseDoEspelho, z0), t00, t10,
                new Vector3(lx, baseDoEspelho, z0), Vector3.back);                        // espelho
            AdicionarFace(vertices, triangulos, uvs, new Vector3(-lx, fundo, z0), t00, t01,
                new Vector3(-lx, fundo, z1), Vector3.left);                               // lateral esquerda
            AdicionarFace(vertices, triangulos, uvs, new Vector3(lx, fundo, z0), t10, t11,
                new Vector3(lx, fundo, z1), Vector3.right);                               // lateral direita
        }

        // Fundo da escada, no último degrau
        AdicionarFace(vertices, triangulos, uvs, new Vector3(-lx, fundo, lz), new Vector3(-lx, topoAlto, lz),
            new Vector3(lx, topoAlto, lz), new Vector3(lx, fundo, lz), Vector3.forward);

        var malha = new Mesh { name = "Escada" };
        malha.SetVertices(vertices);
        malha.SetTriangles(triangulos, 0);
        malha.SetUVs(0, uvs);
        malha.RecalculateNormals();
        malha.RecalculateBounds();
        return malha;
    }

    // Prisma com o fundo reto e o topo nas alturas dos 4 cantos (sudoeste, sudeste, noroeste, nordeste).
    // As coordenadas são locais: centro da placa em X/Z, altura Y já em coordenada de mundo.
    static Mesh MalhaDaPlaca(float largo, float comprimento, float fundo, float sw, float se, float nw, float ne)
    {
        float lx = largo / 2f;
        float lz = comprimento / 2f;

        Vector3 bSW = new Vector3(-lx, fundo, -lz), bSE = new Vector3(lx, fundo, -lz);
        Vector3 bNW = new Vector3(-lx, fundo,  lz), bNE = new Vector3(lx, fundo,  lz);
        Vector3 tSW = new Vector3(-lx, sw, -lz), tSE = new Vector3(lx, se, -lz);
        Vector3 tNW = new Vector3(-lx, nw,  lz), tNE = new Vector3(lx, ne,  lz);

        var vertices = new List<Vector3>();
        var triangulos = new List<int>();
        var uvs = new List<Vector2>();

        AdicionarFace(vertices, triangulos, uvs, tSW, tNW, tNE, tSE, Vector3.up);
        AdicionarFace(vertices, triangulos, uvs, bSW, tSW, tSE, bSE, Vector3.back);
        AdicionarFace(vertices, triangulos, uvs, bNE, tNE, tNW, bNW, Vector3.forward);
        AdicionarFace(vertices, triangulos, uvs, bNW, tNW, tSW, bSW, Vector3.left);
        AdicionarFace(vertices, triangulos, uvs, bSE, tSE, tNE, bNE, Vector3.right);
        AdicionarFace(vertices, triangulos, uvs, bSW, bSE, bNE, bNW, Vector3.down);

        var malha = new Mesh { name = "Ladeira" };
        malha.SetVertices(vertices);
        malha.SetTriangles(triangulos, 0);
        malha.SetUVs(0, uvs);
        malha.RecalculateNormals();
        malha.RecalculateBounds();
        return malha;
    }

    // Uma face de 4 pontos. Se a ordem dos pontos deixaria ela virada pra dentro, inverte.
    static void AdicionarFace(List<Vector3> vertices, List<int> triangulos, List<Vector2> uvs,
        Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normalParaFora)
    {
        if (Vector3.Dot(Vector3.Cross(b - a, c - a), normalParaFora) < 0f)
        {
            Vector3 troca = b; b = d; d = troca;
        }

        int inicio = vertices.Count;
        vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
        uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(1f, 0f)); uvs.Add(new Vector2(1f, 1f)); uvs.Add(new Vector2(0f, 1f));
        triangulos.Add(inicio); triangulos.Add(inicio + 1); triangulos.Add(inicio + 2);
        triangulos.Add(inicio); triangulos.Add(inicio + 2); triangulos.Add(inicio + 3);
    }

    // A ladeira usa o material do prefab do próprio tile (grama, rua...), pra ter a mesma cara do piso reto
    static Material MaterialDoTile(TileDefinicao tile)
    {
        Renderer renderer = tile.prefab != null ? tile.prefab.GetComponentInChildren<Renderer>() : null;
        if (renderer != null && renderer.sharedMaterial != null) return renderer.sharedMaterial;

        Debug.LogWarning($"MapaGerador: não achei o material do prefab de '{Nome(tile.nome, tile.prefab)}' pra montar a ladeira.");
        return null;
    }

    Vector3 CentroDaArea(int x, int y, int w, int h)
    {
        return transform.position + new Vector3((x + w / 2f) * tamanhoDoTile, 0f, (y + h / 2f) * tamanhoDoTile);
    }

    // Redimensiona pelo tamanho real do objeto e o apoia no chão, independente de onde está o pivô.
    // Componentes do tamanhoAlvo <= 0 mantêm o tamanho original naquele eixo.
    static void Encaixar(GameObject obj, Vector3 centro, Vector3 tamanhoAlvo, float baseY)
    {
        if (!CalcularBounds(obj, out Bounds b))
        {
            obj.transform.position = new Vector3(centro.x, centro.y + baseY, centro.z);
            return;
        }

        Vector3 escala = obj.transform.localScale;
        if (tamanhoAlvo.x > 0f && b.size.x > 0.0001f) escala.x *= tamanhoAlvo.x / b.size.x;
        if (tamanhoAlvo.y > 0f && b.size.y > 0.0001f) escala.y *= tamanhoAlvo.y / b.size.y;
        if (tamanhoAlvo.z > 0f && b.size.z > 0.0001f) escala.z *= tamanhoAlvo.z / b.size.z;
        obj.transform.localScale = escala;

        CalcularBounds(obj, out b);
        obj.transform.position += new Vector3(
            centro.x - b.center.x,
            centro.y + baseY - b.min.y,
            centro.z - b.center.z
        );

        SincronizarColliders(obj);
    }

    // Prefabs do ProBuilder guardam a malha num componente próprio e recriam ela sozinhos
    // ao nascer, mas o MeshCollider às vezes fica apontando pra malha antiga/vazia. Aqui
    // forço o collider a usar a mesma malha que está sendo desenhada de verdade.
    static void SincronizarColliders(GameObject obj)
    {
        foreach (MeshCollider colisor in obj.GetComponentsInChildren<MeshCollider>())
        {
            MeshFilter filtro = colisor.GetComponent<MeshFilter>();
            if (filtro != null && filtro.sharedMesh != null)
                colisor.sharedMesh = filtro.sharedMesh;
        }
    }

    // Preenche, só na colisão (sem nada visível), a diferença entre a altura visual da
    // árvore (sempre grama) e a altura real do que está ao redor dela na pixel art.
    // Só cria o preenchimento quando faz diferença — árvore cercada de grama normal não
    // ganha nada, só a que forma uma ilha (cercada de algo mais alto, tipo calçada).
    void AdicionarPreenchimentoDeColisao(int x, int y, float alturaBase, float alturaContexto)
    {
        float alturaPreenchimento = alturaContexto - alturaBase;
        if (alturaPreenchimento <= 0.001f) return;

        GameObject preenchimento = new GameObject("ColisaoInvisivel");
        preenchimento.transform.SetParent(transform);

        Vector3 centro = CentroDaArea(x, y, 1, 1);
        preenchimento.transform.position = new Vector3(centro.x, alturaBase + alturaPreenchimento / 2f, centro.z);

        BoxCollider caixa = preenchimento.AddComponent<BoxCollider>();
        caixa.size = new Vector3(tamanhoDoTile, alturaPreenchimento, tamanhoDoTile);
    }

    static bool CalcularBounds(GameObject obj, out Bounds bounds)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        bounds = default;
        if (renderers.Length == 0) return false;

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return true;
    }

    // Altura da superfície de cima do piso, pra apoiar árvore/inimigo/player em cima dele, não na base
    static float AlturaDoPiso(TileDefinicao piso)
    {
        return piso != null ? piso.offsetY + piso.altura : 0f;
    }

    TileDefinicao ResolverPiso(PisoForcado escolha, int x, int y, int largura, int altura, Color32[] pixels)
    {
        switch (escolha)
        {
            case PisoForcado.Grama:   return grama;
            case PisoForcado.Rua:     return rua;
            case PisoForcado.Calcada: return calcada;
            case PisoForcado.Chao:    return chao;
            default:                  return PisoDoContexto(x, y, largura, altura, pixels);
        }
    }

    // O piso embaixo do inimigo é o mais comum entre os 8 pixels vizinhos na pixel art (incluindo diagonais).
    // Assim ele nasce disfarçado no piso que já está ao redor dele (rua, calçada, grama...).
    // Prédio não entra na votação — não faz sentido o inimigo "parecer" prédio.
    TileDefinicao PisoDoContexto(int x, int y, int largura, int altura, Color32[] pixels)
    {
        TileDefinicao[] pisos = { grama, rua, calcada, chao };
        var votos = new Dictionary<TileDefinicao, int>();

        int[] dx = { -1, 1, 0, 0, -1, -1, 1, 1 };
        int[] dy = { 0, 0, -1, 1, -1, 1, -1, 1 };

        for (int d = 0; d < 8; d++)
        {
            int nx = x + dx[d];
            int ny = y + dy[d];
            if (nx < 0 || nx >= largura || ny < 0 || ny >= altura) continue;

            Color32 corVizinho = pixels[ny * largura + nx];
            TileDefinicao pisoVizinho = EncontrarTile(corVizinho, pisos);
            if (pisoVizinho == null) continue;

            votos.TryGetValue(pisoVizinho, out int atual);
            votos[pisoVizinho] = atual + 1;
        }

        TileDefinicao melhor = null;
        int maisVotado = 0;
        foreach (var par in votos)
        {
            if (par.Value > maisVotado)
            {
                maisVotado = par.Value;
                melhor = par.Key;
            }
        }

        return melhor ?? chao;
    }

    TileDefinicao[] TilesValidos()
    {
        var todos = new[] { grama, rua, calcada, predio, chao, arvore };
        var validos = new List<TileDefinicao>();

        foreach (TileDefinicao tile in todos)
        {
            if (tile == null || tile.prefab == null) continue;

            Color32 c = tile.cor;
            if (c.r == 0 && c.g == 0 && c.b == 0 && c.a == 0)
            {
                Debug.LogWarning($"MapaGerador: '{Nome(tile.nome, tile.prefab)}' tem prefab mas a cor não foi configurada. Tile ignorado.");
                continue;
            }

            foreach (TileDefinicao outro in validos)
            {
                if (DiferencaCor(tile.cor, outro.cor) < ToleranciaCor)
                    Debug.LogWarning($"MapaGerador: '{Nome(tile.nome, tile.prefab)}' e '{Nome(outro.nome, outro.prefab)}' têm cores iguais ou muito parecidas. Só '{Nome(outro.nome, outro.prefab)}' será usado.");
            }

            validos.Add(tile);
        }

        return validos.ToArray();
    }

    List<InimigoDefinicao> InimigosValidos()
    {
        var validos = new List<InimigoDefinicao>();

        foreach (InimigoDefinicao inimigo in inimigos)
        {
            if (inimigo == null || inimigo.prefab == null) continue;

            Color32 c = inimigo.cor;
            if (c.r == 0 && c.g == 0 && c.b == 0 && c.a == 0)
            {
                Debug.LogWarning($"MapaGerador: inimigo '{Nome(inimigo.nome, inimigo.prefab)}' tem prefab mas a cor não foi configurada. Ignorado.");
                continue;
            }

            validos.Add(inimigo);
        }

        return validos;
    }

    static string Nome(string nome, GameObject prefab)
    {
        if (!string.IsNullOrEmpty(nome)) return nome;
        return prefab != null ? prefab.name : "(sem nome)";
    }

    static TileDefinicao EncontrarTile(Color32 cor, TileDefinicao[] tiles)
    {
        TileDefinicao melhor = null;
        int menorDist = ToleranciaCor;

        foreach (TileDefinicao tile in tiles)
        {
            int dist = DiferencaCor(cor, tile.cor);
            if (dist < menorDist)
            {
                menorDist = dist;
                melhor = tile;
            }
        }

        return melhor;
    }

    static InimigoDefinicao EncontrarInimigo(Color32 cor, List<InimigoDefinicao> inimigos)
    {
        InimigoDefinicao melhor = null;
        int menorDist = ToleranciaCor;

        foreach (InimigoDefinicao inimigo in inimigos)
        {
            int dist = DiferencaCor(cor, inimigo.cor);
            if (dist < menorDist)
            {
                menorDist = dist;
                melhor = inimigo;
            }
        }

        return melhor;
    }

    static int DiferencaCor(Color32 a, Color32 b)
    {
        int dr = a.r - b.r;
        int dg = a.g - b.g;
        int db = a.b - b.b;
        return dr * dr + dg * dg + db * db;
    }
}

[System.Serializable]
public class TileDefinicao
{
    public string nome;
    public Color32 cor;
    public GameObject prefab;
    [Tooltip("Altura da base no eixo Y (0 = nível do chão)")]
    public float offsetY = 0f;
    [Tooltip("Altura final em metros (ex: rua 0.2, calçada 0.35, prédio 6). 0 = mantém a altura do prefab. Só vale para tiles redimensionados.")]
    public float altura = 0f;
    [Tooltip("Redimensiona para cobrir a área (grama, rua, prédio...). Desative para objetos individuais como árvores.")]
    public bool redimensionar = true;
}

// Retângulo da pixel art (a partir do canto inferior esquerdo) que fica mais baixo, mais alto ou em ladeira.
// Ex: rua do tutorial descendo 2 m ao longo de 12 pixels para o sul: desnivel 0, inclinacao 2, descida Sul.
[System.Serializable]
public class RegiaoDefinicao
{
    public string nome;
    [Tooltip("Pixel da esquerda da região (0 = borda esquerda da imagem)")]
    public int x;
    [Tooltip("Pixel de baixo da região (0 = borda de baixo da imagem)")]
    public int y;
    [Tooltip("Largura da região, em pixels")]
    public int largura = 1;
    [Tooltip("Altura da região, em pixels")]
    public int altura = 1;
    [Tooltip("Altura do lado mais alto da região, em metros acima (+) ou abaixo (-) do nível normal. Sem ladeira, é a altura da região inteira.")]
    public float desnivel = 0f;
    [Tooltip("Quantos metros o chão desce, do lado mais alto até o mais baixo da região. 0 = região plana.")]
    public float inclinacao = 0f;
    [Tooltip("Pra que lado o chão desce (só vale se a inclinação for maior que 0)")]
    public DirecaoDaDescida direcaoDaDescida = DirecaoDaDescida.Sul;
    [Tooltip("0 = ladeira lisa. Maior que 0 = escada: altura de cada degrau, em metros (ex: 0.2). Só a rua e a calçada viram degraus; grama e casas seguem a ladeira lisa, a menos que 'Degraus Na Grama' esteja ligado.")]
    public float alturaDoDegrau = 0f;
    [Tooltip("Faz a grama virar degrau também (ladeira em voxel)")]
    public bool degrausNaGrama = false;
}

// Sul = pixels de baixo da imagem (y menor), Norte = pixels de cima, Leste = direita, Oeste = esquerda
public enum DirecaoDaDescida { Sul, Norte, Leste, Oeste }

public enum TipoDeRelevo { Plano, Escada, Ladeira }

// Uma cor da imagem de relevo. Plano = altura fixa. Escada e ladeira ligam sozinhas os planos das pontas.
[System.Serializable]
public class ZonaDeRelevo
{
    public string nome;
    public Color32 cor;
    public TipoDeRelevo tipo = TipoDeRelevo.Plano;
    [Tooltip("Só plano: altura em metros (ex: -5 = cinco metros abaixo do mapa principal)")]
    public float desnivel;
    [Tooltip("Só escada: altura de cada degrau, em metros")]
    public float alturaDoDegrau = 0.2f;
}

public enum PisoForcado { Automatico, Grama, Rua, Calcada, Chao }

[System.Serializable]
public class InimigoDefinicao
{
    public string nome;
    public Color32 cor;
    public int idBilixin;
    public GameObject prefab;
    [Tooltip("Tamanho do trigger de encontro, em tiles (ex: 3 = área 3x3). 0 usa o padrão do gerador.")]
    public float tamanhoTrigger = 0f;
    [Tooltip("Automático adivinha pelos pixels ao redor. Force um piso se ele adivinhar errado (ex: cruzamentos de rua).")]
    public PisoForcado piso = PisoForcado.Automatico;
}

[System.Serializable]
public class SpawnDefinicao
{
    public Color32 cor;
    public GameObject prefab;
    [Tooltip("Automático adivinha pelos pixels ao redor. Force um piso se ele adivinhar errado (ex: cruzamentos de rua).")]
    public PisoForcado piso = PisoForcado.Automatico;
}


