using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// SETUP:
// 1. Coloque a pixel art em Assets/Art/Mapa/ (o MapaImportador configura a importação sozinho)
// 2. Adicione este componente em um GameObject vazio na cena
// 3. Preencha os slots no Inspector (cor + prefab de cada tile)
// 4. O mapa é gerado ao carregar a cena; use aoTerminarDeGerar para esconder o loading

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
    [SerializeField] TileDefinicao arvore  = new TileDefinicao { nome = "Árvore",  offsetY = 0.2f, redimensionar = false };

    [Header("Evento apos gerar")]
    public UnityEvent aoTerminarDeGerar;

    const int ToleranciaCor = 900;
    const float SegundosPorFrame = 0.008f;

    float inicioDoFrame;

    void Start()
    {
        StartCoroutine(GerarMapa());
    }

    IEnumerator GerarMapa()
    {
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
        bool gramaValida = System.Array.IndexOf(tiles, grama) >= 0;

        int largura = imagemDoMapa.width;
        int altura  = imagemDoMapa.height;
        Color32[] pixels = imagemDoMapa.GetPixels32();

        // Camada de chão (vira retângulos) e camada de objetos (um por pixel)
        TileDefinicao[] camadaChao = new TileDefinicao[largura * altura];
        var objetos = new List<(int x, int y, TileDefinicao tile)>();

        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].a < 10) continue;

            TileDefinicao tile = EncontrarTile(pixels[i], tiles);
            if (tile == null) continue;

            if (tile.redimensionar)
            {
                camadaChao[i] = tile;
            }
            else
            {
                objetos.Add((i % largura, i / largura, tile));
                if (gramaValida) camadaChao[i] = grama;
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

                // Estica para a direita enquanto a cor for igual
                int w = 1;
                while (x + w < largura && !usado[i + w] && camadaChao[i + w] == tile) w++;

                // Estica para cima enquanto a linha inteira for igual
                int h = 1;
                while (y + h < altura && LinhaIgual(camadaChao, usado, largura, x, y + h, w, tile)) h++;

                for (int dy = 0; dy < h; dy++)
                    for (int dx = 0; dx < w; dx++)
                        usado[(y + dy) * largura + x + dx] = true;

                GameObject obj = Instantiate(tile.prefab, transform);
                Vector3 tamanho = new Vector3(w * tamanhoDoTile, tile.altura, h * tamanhoDoTile);
                Encaixar(obj, CentroDaArea(x, y, w, h), tamanho, tile.offsetY);
                total++;

                if (PassouDoTempo()) { yield return null; inicioDoFrame = Time.realtimeSinceStartup; }
            }
        }

        foreach (var (x, y, tile) in objetos)
        {
            GameObject obj = Instantiate(tile.prefab, transform);
            Encaixar(obj, CentroDaArea(x, y, 1, 1), Vector3.zero, tile.offsetY);
            total++;

            if (PassouDoTempo()) { yield return null; inicioDoFrame = Time.realtimeSinceStartup; }
        }

        StaticBatchingUtility.Combine(gameObject);
        Debug.Log($"MapaGerador: mapa {largura}x{altura} gerado com {total} objetos.");
        aoTerminarDeGerar?.Invoke();
    }

    bool PassouDoTempo()
    {
        return Time.realtimeSinceStartup - inicioDoFrame > SegundosPorFrame;
    }

    static bool LinhaIgual(TileDefinicao[] camada, bool[] usado, int largura, int x, int y, int w, TileDefinicao tile)
    {
        for (int dx = 0; dx < w; dx++)
        {
            int i = y * largura + x + dx;
            if (usado[i] || camada[i] != tile) return false;
        }
        return true;
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
                Debug.LogWarning($"MapaGerador: '{Nome(tile)}' tem prefab mas a cor não foi configurada. Tile ignorado.");
                continue;
            }

            foreach (TileDefinicao outro in validos)
            {
                if (DiferencaCor(tile.cor, outro.cor) < ToleranciaCor)
                    Debug.LogWarning($"MapaGerador: '{Nome(tile)}' e '{Nome(outro)}' têm cores iguais ou muito parecidas. Só '{Nome(outro)}' será usado.");
            }

            validos.Add(tile);
        }

        return validos.ToArray();
    }

    static string Nome(TileDefinicao tile)
    {
        return string.IsNullOrEmpty(tile.nome) ? tile.prefab.name : tile.nome;
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
    [Tooltip("Redimensiona para cobrir a área (grama, rua, prédio...). Desative para objetos individuais como árvores, que ganham grama embaixo.")]
    public bool redimensionar = true;
}
