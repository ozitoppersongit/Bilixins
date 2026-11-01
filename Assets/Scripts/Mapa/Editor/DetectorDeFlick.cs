using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// Menu Bilixins > Procurar faces sobrepostas (flick).
//
// Varre todas as malhas ativas da cena e procura o que causa o piscar (z-fighting): duas faces de objetos DIFERENTES,
// no mesmo plano, viradas pro mesmo lado, e que se sobrepõem em área. Só olha faces alinhadas aos eixos (as de voxel,
// de piso, de parede e de degrau). Cada par achado vai pro Console com os dois objetos, e clicar na linha seleciona o primeiro.
public static class DetectorDeFlick
{
    // Tolerância: faces a menos de 1 mm uma da outra, no mesmo plano, brigam
    const float Precisao = 1000f;
    const float AreaMinima = 0.001f;   // 10 cm²: abaixo disso é arredondamento de borda, não dá pra ver piscar

    struct Retangulo
    {
        public float uMin, uMax, vMin, vMax;
        public Renderer renderer;
    }

    class Par
    {
        public Renderer a, b;
        public float area;
        public int faces;
        public Vector3 exemplo;
    }

    [MenuItem("Bilixins/Procurar faces sobrepostas (flick)")]
    static void Procurar()
    {
        string relatorio = Executar(20, out _);
        Debug.Log(relatorio);
    }

    // Devolve o relatório. pares = quantos pares de objetos brigam.
    public static string Executar(int mostrar, out int pares)
    {
        var faces = new Dictionary<(int eixo, int sinal, int plano), List<Retangulo>>();
        int renderers = 0, ilegiveis = 0;

        foreach (MeshRenderer renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;

            var filtro = renderer.GetComponent<MeshFilter>();
            if (filtro == null || filtro.sharedMesh == null) continue;

            if (!filtro.sharedMesh.isReadable) { ilegiveis++; continue; }

            renderers++;
            ColetarFaces(renderer, filtro.sharedMesh, faces);
        }

        var achados = new Dictionary<(Renderer, Renderer), Par>();

        foreach (var grupo in faces)
        {
            List<Retangulo> lista = grupo.Value;
            if (lista.Count < 2) continue;

            lista.Sort((p, q) => p.uMin.CompareTo(q.uMin));

            for (int i = 0; i < lista.Count; i++)
            {
                for (int j = i + 1; j < lista.Count; j++)
                {
                    if (lista[j].uMin >= lista[i].uMax) break;
                    if (lista[i].renderer == lista[j].renderer) continue;

                    float du = Mathf.Min(lista[i].uMax, lista[j].uMax) - Mathf.Max(lista[i].uMin, lista[j].uMin);
                    float dv = Mathf.Min(lista[i].vMax, lista[j].vMax) - Mathf.Max(lista[i].vMin, lista[j].vMin);
                    if (du <= 0f || dv <= 0f || du * dv < AreaMinima) continue;

                    Renderer a = lista[i].renderer, b = lista[j].renderer;
                    var chave = a.GetHashCode() <= b.GetHashCode() ? (a, b) : (b, a);

                    if (!achados.TryGetValue(chave, out Par par))
                    {
                        par = new Par { a = chave.Item1, b = chave.Item2 };
                        achados[chave] = par;

                        // Ponto de exemplo no mundo
                        var centro = new float[3];
                        centro[grupo.Key.eixo] = grupo.Key.plano / Precisao;
                        centro[(grupo.Key.eixo + 1) % 3] = (Mathf.Max(lista[i].uMin, lista[j].uMin) + Mathf.Min(lista[i].uMax, lista[j].uMax)) / 2f;
                        centro[(grupo.Key.eixo + 2) % 3] = (Mathf.Max(lista[i].vMin, lista[j].vMin) + Mathf.Min(lista[i].vMax, lista[j].vMax)) / 2f;
                        par.exemplo = new Vector3(centro[0], centro[1], centro[2]);
                    }

                    par.area += du * dv;
                    par.faces++;
                }
            }
        }

        List<Par> ordenados = achados.Values.OrderByDescending(p => p.area).ToList();
        pares = ordenados.Count;

        var texto = new StringBuilder();
        texto.AppendLine($"Bilixins [flick]: {renderers} malhas analisadas ({ilegiveis} ilegíveis ignoradas), {ordenados.Count} par(es) de objetos com faces sobrepostas.");

        foreach (Par par in ordenados.Take(mostrar))
        {
            texto.AppendLine($"  - {Nome(par.a)}  x  {Nome(par.b)}: {par.faces} face(s), {par.area:0.###} m² em comum, perto de {par.exemplo:0.0}");
            Debug.LogWarning($"[flick] {Nome(par.a)} x {Nome(par.b)} ({par.faces} faces, {par.area:0.###} m²) perto de {par.exemplo:0.0}", par.a);
        }

        if (ordenados.Count > mostrar) texto.AppendLine($"  ... e mais {ordenados.Count - mostrar}.");
        return texto.ToString();
    }

    static void ColetarFaces(Renderer renderer, Mesh malha, Dictionary<(int, int, int), List<Retangulo>> faces)
    {
        Vector3[] vertices = malha.vertices;
        int[] triangulos = malha.triangles;
        Matrix4x4 paraMundo = renderer.transform.localToWorldMatrix;
        var vistos = new HashSet<(int, int, int, int, int, int, int)>();

        for (int i = 0; i < triangulos.Length; i += 3)
        {
            Vector3 a = paraMundo.MultiplyPoint3x4(vertices[triangulos[i]]);
            Vector3 b = paraMundo.MultiplyPoint3x4(vertices[triangulos[i + 1]]);
            Vector3 c = paraMundo.MultiplyPoint3x4(vertices[triangulos[i + 2]]);

            Vector3 normal = Vector3.Cross(b - a, c - a);
            float comprimento = normal.magnitude;
            if (comprimento < 1e-9f) continue;
            normal /= comprimento;

            int eixo;
            if (Mathf.Abs(normal.x) > 0.9999f) eixo = 0;
            else if (Mathf.Abs(normal.y) > 0.9999f) eixo = 1;
            else if (Mathf.Abs(normal.z) > 0.9999f) eixo = 2;
            else continue;

            int sinal = normal[eixo] > 0f ? 1 : -1;
            int plano = Mathf.RoundToInt(a[eixo] * Precisao);

            int u = (eixo + 1) % 3, v = (eixo + 2) % 3;
            var r = new Retangulo
            {
                uMin = Mathf.Min(a[u], Mathf.Min(b[u], c[u])),
                uMax = Mathf.Max(a[u], Mathf.Max(b[u], c[u])),
                vMin = Mathf.Min(a[v], Mathf.Min(b[v], c[v])),
                vMax = Mathf.Max(a[v], Mathf.Max(b[v], c[v])),
                renderer = renderer
            };

            // Os dois triângulos de um retângulo têm a mesma caixa: conta uma vez só
            var chaveRetangulo = (eixo * 3 + sinal + 1, plano, Mathf.RoundToInt(r.uMin * Precisao), Mathf.RoundToInt(r.uMax * Precisao),
                Mathf.RoundToInt(r.vMin * Precisao), Mathf.RoundToInt(r.vMax * Precisao), 0);
            if (!vistos.Add(chaveRetangulo)) continue;

            var chave = (eixo, sinal, plano);
            if (!faces.TryGetValue(chave, out List<Retangulo> lista))
                faces[chave] = lista = new List<Retangulo>();
            lista.Add(r);
        }
    }

    static string Nome(Renderer renderer)
    {
        var modular = renderer.GetComponentInParent<ObjetoModular>();
        if (modular != null)
        {
            Transform grupo = renderer.transform.parent;
            return $"{modular.name.Replace("(Clone)", "")}/{(grupo != null && grupo != modular.transform ? grupo.name : renderer.name)}";
        }

        return renderer.transform.parent != null ? $"{renderer.transform.parent.name}/{renderer.name}" : renderer.name;
    }
}
