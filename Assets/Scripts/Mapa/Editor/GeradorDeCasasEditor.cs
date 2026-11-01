using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;

// Menu Bilixins > Gerar prefabs de casas e bloqueios.
//
// Cria prefabs de exemplo em Assets/Art/Prefab/Casas/, já com o ObjetoModular configurado: cada peça (porta, janela,
// calha, cano, musgo, topo) é um slot com variações. Estilo VOXEL: tudo numa grade de 0,25 m (uma casa de 6 m tem 24 voxels
// de lado), cor lisa, sem textura.
//
// Como a modelagem evita faces sobrepostas (o "flick" / z-fighting):
//  - Cada variação é UMA malha ProBuilder de verdade (editável com as ferramentas do ProBuilder), não cubos esticados.
//  - O modelo é uma grade de voxels em que cada célula existe uma vez só. A malha só leva as faces que aparecem,
//    juntadas em retângulos maiores. Peças da MESMA variação, portanto, nunca se atravessam.
//  - Peças de slots DIFERENTES (ex: janela e cano) podem estar ligadas ao mesmo tempo. O gerador confere, pra todas as
//    combinações, se alguma ocupa a mesma célula que outra, e avisa no Console. A fachada é dividida em camadas
//    (parede, camada 0, camada 1...) e o musgo desvia da porta e das janelas.
//
// Depois a arte pode trocar qualquer peça por um modelo do Blender: é só pôr o modelo como filho do prefab e
// acrescentá-lo à lista do slot. Rodar de novo recria os prefabs (os materiais são reaproveitados).
public static partial class GeradorDeCasasEditor
{
    const string PastaPai = "Assets/Art/Prefab";
    const string Pasta = PastaPai + "/Casas";
    const string PastaMateriais = Pasta + "/Materiais";

    // Tamanho do voxel FINO, em metros. Tudo que é novo é desenhado nessa unidade.
    const float V = 0.125f;
    // As peças antigas (Casa1, Casa2, tapume, árvore...) foram desenhadas em voxels de 0,25 m. Elas continuam
    // iguais: cada medida delas é dobrada ao entrar na grade fina (veja o parâmetro "escala" de Voxels).
    const float VGrosso = 0.25f;

    class Materiais
    {
        public Material parede, laje, telhado, cumeeira, moldura, portaMadeira, portaAzul, metal, vidro, calha, cano, musgo, madeira, azulTapume, concreto, faixa;
        public Material folha, folhaEscura, folhaClara, tronco, capimSeco, florRosa, florAmarela, florVermelha, baldeCinza, terra;

        // Barraco e prédios: paredes (cores das referências), desgaste, zinco, roupas
        public Material laranja, verdeAgua, vermelhoDesbotado, amarelo, azulParede, concretoSujo, turquesa;
        public Material bloco, tijolo, ferrugem, zincoA, zincoB, brancoToldo, madeiraEscura, ferro, antena, telhaA, telhaB, portaTurquesa;
        public Material roupaAzul, roupaBranca, roupaAmarela, roupaVermelha;
    }

    [MenuItem("Bilixins/Gerar prefabs de casas e bloqueios")]
    static void Gerar()
    {
        CriarPasta(PastaPai, "Casas");
        CriarPasta(Pasta, "Materiais");

        var m = new Materiais
        {
            parede = Material("Parede", new Color32(232, 218, 190, 255)),
            laje = Material("Laje", new Color32(170, 166, 160, 255)),
            telhado = Material("Telhado", new Color32(184, 98, 62, 255)),
            cumeeira = Material("Cumeeira", new Color32(140, 70, 46, 255)),
            moldura = Material("Moldura", new Color32(245, 240, 232, 255)),
            portaMadeira = Material("PortaMadeira", new Color32(130, 84, 48, 255)),
            portaAzul = Material("PortaAzul", new Color32(48, 102, 168, 255)),
            metal = Material("Metal", new Color32(96, 100, 108, 255)),
            vidro = Material("Vidro", new Color32(150, 196, 222, 255)),
            calha = Material("Calha", new Color32(196, 198, 200, 255)),
            cano = Material("Cano", new Color32(236, 236, 232, 255)),
            musgo = Material("Musgo", new Color32(70, 118, 52, 255)),
            madeira = Material("Madeira", new Color32(176, 138, 92, 255)),
            azulTapume = Material("TapumeAzul", new Color32(52, 98, 150, 255)),
            concreto = Material("Concreto", new Color32(176, 174, 170, 255)),
            faixa = Material("FaixaAmarela", new Color32(232, 190, 40, 255)),
            folha = Material("Folha", new Color32(86, 158, 66, 255)),
            folhaEscura = Material("FolhaEscura", new Color32(56, 118, 52, 255)),
            folhaClara = Material("FolhaClara", new Color32(130, 196, 90, 255)),
            tronco = Material("Tronco", new Color32(110, 76, 46, 255)),
            capimSeco = Material("CapimSeco", new Color32(190, 170, 90, 255)),
            florRosa = Material("FlorRosa", new Color32(232, 100, 150, 255)),
            florAmarela = Material("FlorAmarela", new Color32(244, 204, 60, 255)),
            florVermelha = Material("FlorVermelha", new Color32(214, 60, 60, 255)),
            baldeCinza = Material("BaldeCinza", new Color32(150, 154, 160, 255)),
            terra = Material("Terra", new Color32(64, 44, 28, 255)),

            laranja = Material("ParedeLaranja", new Color32(214, 112, 56, 255)),
            verdeAgua = Material("ParedeVerdeAgua", new Color32(144, 196, 176, 255)),
            vermelhoDesbotado = Material("ParedeVermelha", new Color32(172, 76, 58, 255)),
            amarelo = Material("ParedeAmarela", new Color32(228, 186, 72, 255)),
            azulParede = Material("ParedeAzul", new Color32(92, 142, 184, 255)),
            concretoSujo = Material("ParedeConcreto", new Color32(158, 156, 150, 255)),
            turquesa = Material("ParedeTurquesa", new Color32(80, 164, 168, 255)),
            bloco = Material("Bloco", new Color32(150, 150, 146, 255)),
            tijolo = Material("Tijolo", new Color32(168, 86, 62, 255)),
            ferrugem = Material("Ferrugem", new Color32(138, 72, 42, 255)),
            zincoA = Material("ZincoA", new Color32(196, 126, 76, 255)),
            zincoB = Material("ZincoB", new Color32(170, 100, 62, 255)),
            brancoToldo = Material("ToldoBranco", new Color32(236, 232, 224, 255)),
            madeiraEscura = Material("MadeiraEscura", new Color32(140, 104, 68, 255)),
            ferro = Material("Ferro", new Color32(60, 64, 72, 255)),
            antena = Material("Antena", new Color32(150, 154, 160, 255)),
            telhaA = Material("TelhaA", new Color32(178, 92, 62, 255)),
            telhaB = Material("TelhaB", new Color32(152, 74, 50, 255)),
            portaTurquesa = Material("PortaTurquesa", new Color32(70, 150, 150, 255)),
            roupaAzul = Material("RoupaAzul", new Color32(74, 112, 196, 255)),
            roupaBranca = Material("RoupaBranca", new Color32(240, 240, 236, 255)),
            roupaAmarela = Material("RoupaAmarela", new Color32(238, 200, 70, 255)),
            roupaVermelha = Material("RoupaVermelha", new Color32(206, 70, 66, 255)),
        };

        // Medidas em voxels: largura (X), profundidade (Z), altura (Y)
        MontarCasa("Casa1", 24, 24, 25, comCano: true, m);
        MontarCasa("Casa2", 20, 20, 14, comCano: false, m);
        MontarTapume("BloqueioTapume", m);
        MontarBarreiras("BloqueioBarreiras", m);
        MontarBarraco("BarracoDoisAndares", m);
        MontarSobrado("Sobrado", m);
        MontarPredioAlto("PredioAlto", m);
        MontarCasaDeLaje("CasaDeLaje", m);
        MontarBlocoDeVarandas("BlocoDeVarandas", m);

        MontarMesa("MesaComBancos", m);
        MontarVaralDeQuintal("VaralDeQuintal", m);
        MontarPedras("PedrasDePassagem", m);
        MontarMuro("MuroDeBloco", m);
        MontarRodaDeBicicleta("RodaDeBicicleta", m);
        MontarEscadaMetalica("EscadaMetalica", m);
        MontarEngradados("Engradados", m);
        MontarTambor("Tambor", m);

        MontarArvore("ArvoreVoxel", m);
        MontarArbusto("Arbusto", m);
        MontarTouceira("Touceira", m);
        MontarVaso("VasoComPlanta", m);
        MontarBalde("Balde", m);
        MontarCadeira("CadeiraDePlastico", m);
        MontarChurrasqueira("Churrasqueira", m);
        MontarTanqueDeLavar("TanqueDeLavar", m);
        MontarCasinhaDeCachorro("CasinhaDeCachorro", m);
        MontarCarrinhoDeMao("CarrinhoDeMao", m);
        MontarMaterialDeObra("MaterialDeObra", m);
        MontarPneus("Pneus", m);
        MontarBujao("BujaoDeGas", m);
        MontarPoste("PosteDeRua", m);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Bilixins: prefabs voxel (ProBuilder) gerados em " + Pasta + " (Casa1, Casa2, BloqueioTapume, BloqueioBarreiras, ArvoreVoxel, Arbusto, Touceira, VasoComPlanta, Balde).");
    }

    // ---------------------------------------------------------------- casas

    // Origem no meio da base. A frente da casa é o lado +Z. Tudo em voxels (1 voxel = 0,25 m).
    // Camadas da fachada, de dentro pra fora: parede | z = zf (porta, janela, musgo) | z = zf + 1 (calha, cano, grade).
    static void MontarCasa(string nome, int L, int P, int A, bool comCano, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();

        int xe = -L / 2;      // borda esquerda (primeira célula da parede)
        int xd = L / 2;       // primeira célula à direita da parede
        int zt = -P / 2;      // fundo
        int zf = P / 2;       // primeira célula à frente da parede

        // Corpo: uma caixa só, com a colisão da casa
        var corpo = new Voxels();
        corpo.Caixa(xe, 0, zt, L, A, P, m.parede);
        GameObject objetoCorpo = Fixo(raiz.transform, "Corpo", corpo);
        var colisor = objetoCorpo.AddComponent<BoxCollider>();
        colisor.center = new Vector3((xe + L / 2f) * VGrosso, A / 2f * VGrosso, (zt + P / 2f) * VGrosso);
        colisor.size = new Vector3(L * VGrosso, A * VGrosso, P * VGrosso);

        // Centro de cada janela, a partir do meio. Deixa pelo menos 1 voxel de parede entre a janela e a porta
        // (casa estreita: janela mais pra fora, pra molduras diferentes não se juntarem num bloco só)
        int deslocamento = xd - (L < 22 ? 3 : 4);
        int larguraPortao = 2 * (deslocamento - 4);     // o portão é a faixa entre as janelas, com 1 voxel de folga de cada lado
        int yJanela = A > 20 ? 14 : 5;

        // Onde o musgo NÃO pode ficar (camada z = zf): porta, portão e janelas
        var reservada = new HashSet<Vector3Int>();
        Reservar(reservada, Mathf.Min(-3, -larguraPortao / 2), 0, zf, Mathf.Max(6, larguraPortao), 9, 1);
        foreach (int s in new[] { -1, 1 })
            Reservar(reservada, s * deslocamento - 3, yJanela, zf, 6, 5, 1);

        // Topo: laje com parapeito, laje com caixa d'água ou telhado de duas águas em degraus
        Slot(modular, "Topo",
            Variacao(raiz.transform, "Topo_Laje", v => Laje(v, m, xe, xd, zt, zf, A)),
            Variacao(raiz.transform, "Topo_CaixaDagua", v =>
            {
                Laje(v, m, xe, xd, zt, zf, A);

                // Caixa d'água: um bloco em cruz (cantos cortados) sobre quatro pés, tudo no interior da laje
                int cx = xd - 9, cz = zt + 3;
                foreach (int px in new[] { cx + 1, cx + 4 })
                    foreach (int pz in new[] { cz + 1, cz + 4 })
                        v.Celula(px, A + 1, pz, m.laje);

                v.Caixa(cx, A + 2, cz + 1, 6, 5, 4, m.metal);
                v.Caixa(cx + 1, A + 2, cz, 4, 5, 6, m.metal);
                v.Caixa(cx + 1, A + 7, cz + 1, 4, 1, 4, m.calha);
            }),
            Variacao(raiz.transform, "Topo_TelhadoDuasAguas", v =>
            {
                // Camadas de 1 voxel que vão estreitando: telhado em degraus, cumeeira ao longo de X.
                // A primeira camada tem a mesma pegada da laje.
                int camada = 0;
                for (int metade = zf + 1; metade >= 1; metade -= 2, camada++)
                    v.Caixa(xe - 1, A + camada, -metade, L + 2, 1, metade * 2, metade <= 2 ? m.cumeeira : m.telhado);
            }));

        // Porta (camada z = zf)
        Slot(modular, "Porta",
            Variacao(raiz.transform, "Porta_Madeira", v => Moldurado(v, -3, 0, 6, 9, zf, m.moldura, m.portaMadeira, semBase: true)),
            Variacao(raiz.transform, "Porta_Azul", v => Moldurado(v, -3, 0, 6, 9, zf, m.moldura, m.portaAzul, semBase: true)),
            Variacao(raiz.transform, "Porta_Portao", v =>
            {
                v.Caixa(-larguraPortao / 2, 0, zf, larguraPortao, 9, 1, m.metal);
                foreach (int y in new[] { 1, 4, 7 })
                    v.Caixa(-larguraPortao / 2, y, zf + 1, larguraPortao, 1, 1, m.moldura);
            }));

        // Janelas, uma de cada lado da porta (camada z = zf; grade e persiana na camada z = zf + 1)
        Slot(modular, "Janela",
            Variacao(raiz.transform, "Janela_Simples", v =>
            {
                foreach (int s in new[] { -1, 1 })
                    Moldurado(v, s * deslocamento - 3, yJanela, 6, 5, zf, m.moldura, m.vidro);
            }),
            Variacao(raiz.transform, "Janela_Grade", v =>
            {
                foreach (int s in new[] { -1, 1 })
                {
                    int x0 = s * deslocamento - 3;
                    Moldurado(v, x0, yJanela, 6, 5, zf, m.moldura, m.vidro);
                    for (int i = 0; i < 3; i++)
                        v.Caixa(x0 + 1 + i * 2, yJanela + 1, zf + 1, 1, 3, 1, m.metal);
                }
            }),
            Variacao(raiz.transform, "Janela_Persiana", v =>
            {
                foreach (int s in new[] { -1, 1 })
                {
                    int x0 = s * deslocamento - 3;
                    Moldurado(v, x0, yJanela, 6, 5, zf, m.moldura, m.madeira);
                    for (int i = 0; i < 3; i++)
                        v.Caixa(x0 + 1, yJanela + 1 + i * 2, zf + 1, 4, 1, 1, m.moldura);
                }
            }));

        // Calha: fileira de voxels no beiral da frente (camada z = zf + 1), em dois tons alternados
        Slot(modular, "Calha",
            Variacao(raiz.transform, "Calha_Reta", v => FileiraDeCalha(v, m, xe - 1, L + 2, A - 1, zf + 1)),
            Variacao(raiz.transform, "Calha_ComBeiral", v =>
            {
                FileiraDeCalha(v, m, xe - 1, L + 2, A - 1, zf + 1);
                // O beiral começa em zf + 1: a laje termina em zf, então as duas peças não se tocam
                v.Caixa(xe - 1, A, zf + 1, L + 2, 1, 2, m.laje);
            }));

        // Cano de descida no canto da frente, bem embaixo da ponta da calha (quadrado, como tudo no voxel)
        if (comCano)
        {
            int zc = zf + 1;
            Slot(modular, "Cano",
                Variacao(raiz.transform, "Cano_Reto", v =>
                {
                    v.Caixa(xd, 0, zc, 1, A - 1, 1, m.cano);
                    foreach (int y in new[] { 3, A / 2, A - 5 })
                        v.Caixa(xd - 1, y, zc, 3, 1, 1, m.metal);
                }),
                Variacao(raiz.transform, "Cano_Cotovelo", v =>
                {
                    v.Caixa(xd, 3, zc, 1, A - 4, 1, m.cano);
                    v.Celula(xd, 2, zc, m.calha);
                    v.Caixa(xd, 2, zc + 1, 1, 1, 3, m.cano);
                }),
                Variacao(raiz.transform, "Cano_Duplo", v =>
                {
                    v.Caixa(xd - 1, 0, zc, 1, A - 1, 1, m.cano);
                    v.Caixa(xd + 1, 0, zc, 1, A - 1, 1, m.cano);
                }));
        }

        // Musgo: manchas de voxels verdes, cada uma com seu jeito de ser irregular. Desviam da porta e das janelas.
        Slot(modular, "Musgo",
            Variacao(raiz.transform, "Musgo_Base", v =>
            {
                Mancha(v, xe + 3, 0, 7, 2, zf, lateral: false, seed: 11, chance: 0.75f, m.musgo, reservada);
                Mancha(v, xd - 12, 0, 8, 3, zf, lateral: false, seed: 23, chance: 0.7f, m.musgo, reservada);
                Mancha(v, 2, 0, 7, 2, xd, lateral: true, seed: 37, chance: 0.7f, m.musgo, reservada);
            }),
            Variacao(raiz.transform, "Musgo_Quina", v =>
            {
                Mancha(v, xe, 0, 3, A * 2 / 3, zf, lateral: false, seed: 5, chance: 0.7f, m.musgo, reservada);
                Mancha(v, zf - 4, 0, 4, A / 2, xe - 1, lateral: true, seed: 9, chance: 0.65f, m.musgo, reservada);
            }),
            Variacao(raiz.transform, "Musgo_SobCalha", v =>
            {
                // Escorridos verdes saindo da calha (a calha fica uma camada à frente, em zf + 1)
                var r = new System.Random(3);
                for (int x = xe + 2; x < xd - 1; x += 4)
                {
                    int comprimento = 3 + r.Next(0, 6);
                    for (int y = A - 2 - comprimento; y <= A - 2; y++)
                        if (!reservada.Contains(new Vector3Int(x, y, zf))) v.Celula(x, y, zf, m.musgo);
                }
            }));

        Concluir(raiz, nome);
    }

    static void Laje(Voxels v, Materiais m, int xe, int xd, int zt, int zf, int A)
    {
        v.Caixa(xe - 1, A, zt - 1, xd - xe + 2, 1, zf - zt + 2, m.laje);

        // Parapeito de 1 voxel em volta, em cima da laje
        for (int x = xe - 1; x <= xd; x++)
        {
            for (int z = zt - 1; z <= zf; z++)
            {
                bool borda = x == xe - 1 || x == xd || z == zt - 1 || z == zf;
                if (borda) v.Celula(x, A + 1, z, m.laje);
            }
        }
    }

    // Moldura de 1 voxel em volta de um miolo (porta ou janela), tudo na camada z. As cinco peças não se tocam em volume.
    static void Moldurado(Voxels v, int x, int y, int w, int h, int z, Material moldura, Material miolo, bool semBase = false)
    {
        v.Caixa(x + 1, semBase ? y : y + 1, z, w - 2, semBase ? h - 1 : h - 2, 1, miolo);
        v.Caixa(x, y, z, 1, h, 1, moldura);
        v.Caixa(x + w - 1, y, z, 1, h, 1, moldura);
        v.Caixa(x + 1, y + h - 1, z, w - 2, 1, 1, moldura);
        if (!semBase) v.Caixa(x + 1, y, z, w - 2, 1, 1, moldura);
    }

    // Voxels de 1 em 1, alternando dois tons, ao longo do beiral
    static void FileiraDeCalha(Voxels v, Materiais m, int x0, int comprimento, int y, int z)
    {
        for (int i = 0; i < comprimento; i++)
            v.Celula(x0 + i, y, z, (i / 2) % 2 == 0 ? m.calha : m.cano);
    }

    // Mancha irregular: cada voxel da área w x h existe com uma chance, mais forte embaixo e mais rala em cima.
    // Fica numa camada só (colada na parede da frente, em z = parede, ou na lateral, em x = parede).
    // Em lateral, x0 é a posição ao longo de Z. Células reservadas (porta, janela) ficam de fora.
    static void Mancha(Voxels v, int x0, int y0, int w, int h, int parede, bool lateral, int seed, float chance, Material mat,
        HashSet<Vector3Int> reservada)
    {
        var r = new System.Random(seed);
        for (int j = 0; j < h; j++)
        {
            for (int i = 0; i < w; i++)
            {
                float probabilidade = chance * (1f - j / (float)(h + 1));
                if (r.NextDouble() > probabilidade) continue;

                var celula = lateral ? new Vector3Int(parede, y0 + j, x0 + i) : new Vector3Int(x0 + i, y0 + j, parede);
                if (reservada.Contains(celula)) continue;

                v.Celula(celula.x, celula.y, celula.z, mat);
            }
        }
    }

    static void Reservar(HashSet<Vector3Int> conjunto, int x, int y, int z, int w, int h, int d)
    {
        for (int k = 0; k < d; k++)
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                    conjunto.Add(new Vector3Int(x + i, y + j, z + k));
    }

    // ---------------------------------------------------------------- bloqueios de rua

    // Tapume de obra de 14 m de largura (56 voxels; a rua larga tem 7 tiles). Origem no meio da base; a frente é +Z.
    // Camadas: poste (z = -1), tábua (z = 0), faixa amarela (z = 1).
    static void MontarTapume(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();

        var caixa = raiz.AddComponent<BoxCollider>();
        caixa.center = new Vector3(0f, 1.25f, 0f);
        caixa.size = new Vector3(14f, 2.5f, 0.5f);

        Slot(modular, "Painel",
            Variacao(raiz.transform, "Painel_Madeira", v => Tapume(v, m.madeira, m.metal)),
            Variacao(raiz.transform, "Painel_Azul", v => Tapume(v, m.azulTapume, m.metal)),
            Variacao(raiz.transform, "Painel_Concreto", v => Tapume(v, m.concreto, m.metal)));

        Slot(modular, "Faixa",
            Variacao(raiz.transform, "Faixa_Amarela", v =>
            {
                for (int x = -28; x < 28; x += 8)
                    v.Caixa(x, 7, 1, 4, 1, 1, m.faixa);
            }));

        Concluir(raiz, nome);
    }

    static void Tapume(Voxels v, Material painel, Material poste)
    {
        for (int x = -28; x < 28; x += 4)
            v.Caixa(x, 0, 0, 4, 9, 1, painel);
        for (int x = -28; x <= 27; x += 8)
            v.Caixa(x, 0, -1, 1, 10, 1, poste);
    }

    // Fileira de barreiras de concreto em dois degraus de largura, com colisão contínua
    static void MontarBarreiras(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();

        var caixa = raiz.AddComponent<BoxCollider>();
        caixa.center = new Vector3(0f, 0.5f, 0f);
        caixa.size = new Vector3(14f, 1.0f, 0.75f);

        Slot(modular, "Barreiras",
            Variacao(raiz.transform, "Barreiras_Concreto", v => Barreiras(v, m, m.concreto)),
            Variacao(raiz.transform, "Barreiras_Metal", v => Barreiras(v, m, m.metal)));

        Concluir(raiz, nome);
    }

    static void Barreiras(Voxels v, Materiais m, Material bloco)
    {
        for (int i = 0; i < 6; i++)
        {
            int x = -28 + i * 9 + 1;
            v.Caixa(x, 0, -1, 8, 2, 3, bloco);
            v.Caixa(x + 1, 2, 0, 6, 2, 1, bloco);
            v.Caixa(x + 1, 3, 1, 6, 1, 1, m.faixa);
        }
    }

    // ---------------------------------------------------------------- vegetação e objetos pequenos

    // Esfera de voxels: camadas horizontais cujo lado diminui com a distância ao centro, com voxels soltos na borda
    // pra quebrar o contorno liso. yBase é onde a esfera começa (o fundo dela).
    static void BolaDeVoxels(Voxels v, int cx, int yBase, int cz, int raio, Material clara, Material escura, int seed)
    {
        var r = new System.Random(seed);

        for (int k = -raio; k <= raio; k++)
        {
            int s = Mathf.RoundToInt(Mathf.Sqrt(raio * raio - k * k));
            int lado = 2 * s + 1;
            int y = yBase + k + raio;

            v.Caixa(cx - s, y, cz - s, lado, 1, lado, k < 0 && r.NextDouble() < 0.6 ? escura : clara);

            for (int n = 0; n < 2; n++)
            {
                if (r.NextDouble() < 0.5) continue;

                int borda = r.Next(0, 4);
                int ao = r.Next(-s, s + 1);
                int bx = borda == 0 ? -s - 1 : borda == 1 ? s + 1 : ao;
                int bz = borda == 2 ? -s - 1 : borda == 3 ? s + 1 : ao;
                v.Celula(cx + bx, y, cz + bz, escura);
            }
        }
    }

    // Árvore. O tronco termina em y = 6; a copa tem um pescoço de tronco (2x2, y = 7 e 8) e começa em y = 9. Assim
    // tronco e copa não dividem nenhuma célula. O colisor só cobre o tronco.
    static void MontarArvore(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();

        var caixa = raiz.AddComponent<BoxCollider>();
        caixa.center = new Vector3(0f, 0.875f, 0f);
        caixa.size = new Vector3(0.75f, 1.75f, 0.75f);

        Slot(modular, "Tronco",
            Variacao(raiz.transform, "Tronco_Reto", v => v.Caixa(-1, 0, -1, 2, 7, 2, m.tronco)),
            Variacao(raiz.transform, "Tronco_Grosso", v =>
            {
                v.Caixa(-1, 0, -1, 3, 4, 3, m.tronco);
                v.Caixa(-1, 4, -1, 2, 3, 2, m.tronco);
                v.Celula(-2, 0, 0, m.tronco);
                v.Celula(2, 0, -1, m.tronco);
            }),
            Variacao(raiz.transform, "Tronco_Torto", v =>
            {
                v.Caixa(-1, 0, -1, 2, 3, 2, m.tronco);
                v.Caixa(0, 3, -1, 2, 4, 2, m.tronco);
            }));

        Slot(modular, "Copa",
            Variacao(raiz.transform, "Copa_Redonda", v =>
            {
                v.Caixa(-1, 7, -1, 2, 2, 2, m.tronco);
                BolaDeVoxels(v, 0, 9, 0, 5, m.folha, m.folhaEscura, 1);
            }),
            Variacao(raiz.transform, "Copa_Larga", v =>
            {
                v.Caixa(-1, 7, -1, 2, 2, 2, m.tronco);
                BolaDeVoxels(v, -3, 9, 0, 4, m.folha, m.folhaEscura, 2);
                BolaDeVoxels(v, 3, 10, 0, 4, m.folhaClara, m.folha, 3);
            }),
            Variacao(raiz.transform, "Copa_Alta", v =>
            {
                v.Caixa(-1, 7, -1, 2, 2, 2, m.tronco);
                BolaDeVoxels(v, 0, 9, 0, 4, m.folha, m.folhaEscura, 4);
                BolaDeVoxels(v, 0, 15, 0, 3, m.folhaClara, m.folha, 5);
            }));

        Concluir(raiz, nome);
    }

    static void MontarArbusto(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();

        Slot(modular, "Forma",
            Variacao(raiz.transform, "Arbusto_Baixo", v => BolaDeVoxels(v, 0, 0, 0, 2, m.folha, m.folhaEscura, 11)),
            Variacao(raiz.transform, "Arbusto_Largo", v =>
            {
                for (int i = -1; i <= 1; i++)
                    BolaDeVoxels(v, i * 4, 0, 0, 2, i == 0 ? m.folhaClara : m.folha, m.folhaEscura, 20 + i);
            }),
            Variacao(raiz.transform, "Arbusto_Alto", v => BolaDeVoxels(v, 0, 0, 0, 3, m.folha, m.folhaEscura, 31)),
            Variacao(raiz.transform, "Arbusto_Florido", v =>
            {
                BolaDeVoxels(v, 0, 0, 0, 2, m.folha, m.folhaEscura, 41);
                var r = new System.Random(7);
                Material[] flores = { m.florRosa, m.florAmarela, m.florVermelha };
                for (int i = 0; i < 7; i++)
                    v.Celula(r.Next(-2, 3), r.Next(1, 5), r.Next(-2, 3), flores[r.Next(0, flores.Length)]);
            }));

        Concluir(raiz, nome);
    }

    // Touceira de capim: lâminas finas de alturas diferentes
    static void MontarTouceira(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();

        Slot(modular, "Touceira",
            Variacao(raiz.transform, "Touceira_Verde", v => Capim(v, m.folha, m.folhaClara, 2, 5, 12, 1)),
            Variacao(raiz.transform, "Touceira_Alta", v => Capim(v, m.folhaEscura, m.folha, 4, 9, 12, 2)),
            Variacao(raiz.transform, "Touceira_Seca", v => Capim(v, m.capimSeco, m.madeira, 2, 6, 10, 3)));

        Concluir(raiz, nome);
    }

    static void Capim(Voxels v, Material a, Material b, int minimo, int maximo, int laminas, int seed)
    {
        var r = new System.Random(seed);
        for (int i = 0; i < laminas; i++)
            v.Caixa(r.Next(-3, 4), 0, r.Next(-3, 4), 1, r.Next(minimo, maximo + 1), 1, r.NextDouble() < 0.5 ? a : b);
    }

    // Vaso de barro afunilado (degraus de voxel, y = 0 a 4) com uma planta por cima (y >= 5). Dois slots: o vaso e a planta.
    static void MontarVaso(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();

        Slot(modular, "Vaso",
            Variacao(raiz.transform, "Vaso_Barro", v => Vaso(v, m.telhado, m.cumeeira, m.terra)),
            Variacao(raiz.transform, "Vaso_Branco", v => Vaso(v, m.moldura, m.calha, m.terra)),
            Variacao(raiz.transform, "Vaso_Azul", v => Vaso(v, m.portaAzul, m.metal, m.terra)));

        Slot(modular, "Planta",
            Variacao(raiz.transform, "Planta_Folhas", v => BolaDeVoxels(v, 0, 5, 0, 2, m.folha, m.folhaEscura, 51)),
            Variacao(raiz.transform, "Planta_Flor", v =>
            {
                BolaDeVoxels(v, 0, 5, 0, 2, m.folha, m.folhaEscura, 52);
                v.Celula(0, 10, 0, m.florRosa);
                v.Celula(2, 8, 0, m.florAmarela);
                v.Celula(-2, 8, 1, m.florVermelha);
            }),
            Variacao(raiz.transform, "Planta_Cacto", v =>
            {
                v.Caixa(-1, 5, -1, 2, 7, 2, m.folhaEscura);
                v.Caixa(1, 8, -1, 2, 1, 2, m.folhaEscura);
                v.Caixa(3, 8, -1, 1, 3, 2, m.folhaEscura);
                v.Caixa(-1, 12, -1, 2, 1, 2, m.florRosa);
            }));

        Concluir(raiz, nome);
    }

    static void Vaso(Voxels v, Material corpo, Material borda, Material terra)
    {
        v.Caixa(-2, 0, -2, 4, 3, 4, corpo);
        v.Caixa(-3, 3, -3, 6, 1, 6, borda);
        v.Caixa(-2, 4, -2, 4, 1, 4, terra);
    }

    // Balde de plástico (afunilado) com alça de voxels
    static void MontarBalde(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();

        Slot(modular, "Cor",
            Variacao(raiz.transform, "Balde_Amarelo", v => Balde(v, m.faixa, m.metal)),
            Variacao(raiz.transform, "Balde_Cinza", v => Balde(v, m.baldeCinza, m.metal)),
            Variacao(raiz.transform, "Balde_Azul", v => Balde(v, m.portaAzul, m.metal)));

        Concluir(raiz, nome);
    }

    static void Balde(Voxels v, Material corpo, Material alca)
    {
        v.Caixa(-1, 0, -1, 3, 1, 3, corpo);
        v.Caixa(-2, 1, -2, 4, 4, 4, corpo);
        v.Caixa(-2, 5, -2, 5, 1, 5, corpo);
        v.Caixa(-3, 5, 0, 1, 3, 1, alca);
        v.Caixa(3, 5, 0, 1, 3, 1, alca);
        v.Caixa(-3, 8, 0, 7, 1, 1, alca);
    }

    // ---------------------------------------------------------------- barraco de dois andares (voxel fino, 0,125 m)

    enum TipoJanela { Caixilho, Grade, Persiana, Tapada }

    // Barraco de dois andares com varanda em balanço (referências de comunidade: térreo com porta e janelas, laje que avança
    // formando a varanda, andar de cima recuado, toldo de zinco, telhado de chapa com antena, varal).
    // Medidas em voxels finos (1 = 0,125 m). Origem no meio da base; a frente é +Z.
    //   térreo:  x -24..23, z -20..19, y 0..21        laje (piso da varanda): y 22..23, avança até z = 21
    //   andar de cima: x -24..23, z -20..7, y 24..45  (a varanda é a faixa z 8..21 em frente a ele)
    // Camadas da fachada do térreo: z = 20 (porta, janela, desgaste) e z = 21 (grade). Do andar de cima: z = 8 e z = 9.
    static void MontarBarraco(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();

        const int L = 48, P = 40, alturaTerreo = 22, alturaSup = 22;
        const int xe = -24, xd = 24, zt = -20, zf = 20;
        const int yLaje = 22, ySup = 24, yTopo = 46, zs = 8;

        // Laje do piso de cima e da varanda: sempre ligada. As paredes ficam no slot Pintura.
        var laje = new Voxels(1);
        laje.Caixa(xe - 1, yLaje, zt, L + 2, 2, zf + 2 - zt, m.laje);
        Fixo(raiz.transform, "Laje", laje);

        AdicionarColisor(raiz, xe - 1, 0, zt, xd + 1, yLaje + 2, zf + 2);   // térreo e laje
        AdicionarColisor(raiz, xe, ySup, zt, xd, yTopo, zs);                // andar de cima

        // Regiões de parede que o desgaste não pode cobrir: portas, janelas e os aparelhos de ar-condicionado
        var reservadaTerreo = new HashSet<Vector3Int>();
        Reservar(reservadaTerreo, -5, 0, zf, 10, 20, 1);
        Reservar(reservadaTerreo, -23, 7, zf, 16, 12, 1);
        Reservar(reservadaTerreo, 7, 7, zf, 16, 12, 1);

        var reservadaSup = new HashSet<Vector3Int>();
        Reservar(reservadaSup, -5, ySup, zs, 10, 18, 1);
        Reservar(reservadaSup, -22, 30, zs, 14, 12, 1);
        Reservar(reservadaSup, 8, 30, zs, 14, 12, 1);
        Reservar(reservadaSup, -20, ySup, zs, 9, 6, 1);
        Reservar(reservadaSup, 12, ySup, zs, 9, 6, 1);

        // Pintura: cor do térreo e do andar de cima
        ModeloDaPeca Paredes(string n, Material terreo, Material sup) => Variacao(raiz.transform, n, v =>
        {
            v.Caixa(xe, 0, zt, L, alturaTerreo, P, terreo);
            v.Caixa(xe, ySup, zt, L, alturaSup, zs - zt, sup);
        }, 1);

        Slot(modular, "Pintura",
            Paredes("Pintura_LaranjaEVerdeAgua", m.laranja, m.verdeAgua),
            Paredes("Pintura_Vermelho", m.vermelhoDesbotado, m.vermelhoDesbotado),
            Paredes("Pintura_AmareloELaranja", m.amarelo, m.laranja),
            Paredes("Pintura_AzulEAmarelo", m.azulParede, m.amarelo),
            Paredes("Pintura_TurquesaELaranja", m.turquesa, m.laranja),
            Paredes("Pintura_Concreto", m.concretoSujo, m.concretoSujo));

        // Porta: a do térreo e a do andar de cima (que abre pra varanda) sempre do mesmo estilo
        Slot(modular, "Porta",
            Variacao(raiz.transform, "Porta_Madeira", v =>
            {
                PortaFina(v, -5, 0, 10, 20, zf, m.moldura, m.portaMadeira, null, null);
                PortaFina(v, -5, ySup, 10, 18, zs, m.moldura, m.portaMadeira, null, null);
            }, 1),
            Variacao(raiz.transform, "Porta_FerroComGrade", v =>
            {
                PortaFina(v, -5, 0, 10, 20, zf, m.moldura, m.portaTurquesa, m.ferro, null);
                PortaFina(v, -5, ySup, 10, 18, zs, m.moldura, m.portaTurquesa, m.ferro, null);
            }, 1),
            Variacao(raiz.transform, "Porta_BrancaRemendada", v =>
            {
                PortaFina(v, -5, 0, 10, 20, zf, m.moldura, m.roupaBranca, null, m.bloco);
                PortaFina(v, -5, ySup, 10, 18, zs, m.moldura, m.roupaBranca, null, m.bloco);
            }, 1));

        // Janelas do térreo (duas, uma de cada lado da porta) e do andar de cima
        ModeloDaPeca JanelasTerreo(string n, TipoJanela tipo) => Variacao(raiz.transform, n, v =>
        {
            JanelaFina(v, tipo, -23, 7, 16, 12, zf, m);
            JanelaFina(v, tipo, 7, 7, 16, 12, zf, m);
        }, 1);

        ModeloDaPeca JanelasSup(string n, TipoJanela tipo) => Variacao(raiz.transform, n, v =>
        {
            JanelaFina(v, tipo, -22, 30, 14, 12, zs, m);
            JanelaFina(v, tipo, 8, 30, 14, 12, zs, m);
        }, 1);

        Slot(modular, "JanelaTerreo",
            JanelasTerreo("JanelaTerreo_Grade", TipoJanela.Grade),
            JanelasTerreo("JanelaTerreo_Persiana", TipoJanela.Persiana),
            JanelasTerreo("JanelaTerreo_Tapada", TipoJanela.Tapada),
            JanelasTerreo("JanelaTerreo_Caixilho", TipoJanela.Caixilho));

        Slot(modular, "JanelaSuperior",
            JanelasSup("JanelaSuperior_Caixilho", TipoJanela.Caixilho),
            JanelasSup("JanelaSuperior_Grade", TipoJanela.Grade),
            JanelasSup("JanelaSuperior_Persiana", TipoJanela.Persiana),
            JanelasSup("JanelaSuperior_Tapada", TipoJanela.Tapada));

        // Varanda: guarda-corpo na ponta da laje (z = 20), por cima do piso da varanda (y >= 24).
        // Tem uma PASSAGEM de 12 voxels (x -6..5), bem na frente da porta de cima, pra uma escada chegar (veja a EscadaMetalica).
        Slot(modular, "Varanda",
            Variacao(raiz.transform, "Varanda_Ferro", v =>
            {
                foreach (int y in new[] { 31, 27 })
                {
                    v.Caixa(xe, y, 20, -6 - xe, 1, 1, m.ferro);
                    v.Caixa(6, y, 20, xd - 6, 1, 1, m.ferro);
                }
                for (int x = xe; x < xd; x += 8)
                    if (x < -7 || x > 6) v.Caixa(x, ySup, 20, 1, 7, 1, m.ferro);
                v.Caixa(xd - 1, ySup, 20, 1, 7, 1, m.ferro);
                v.Caixa(-7, ySup, 20, 1, 7, 1, m.ferro);
                v.Caixa(6, ySup, 20, 1, 7, 1, m.ferro);
                for (int x = xe + 3; x < xd - 1; x += 3)
                    if ((x - xe) % 8 != 0 && (x < -7 || x > 6)) v.Caixa(x, ySup, 20, 1, 7, 1, m.ferro);
            }, 1),
            Variacao(raiz.transform, "Varanda_Bloco", v =>
            {
                v.Caixa(xe, ySup, 19, -6 - xe, 7, 2, m.bloco);
                v.Caixa(6, ySup, 19, xd - 6, 7, 2, m.bloco);
                v.Caixa(xe, 31, 19, -6 - xe, 1, 2, m.concretoSujo);
                v.Caixa(6, 31, 19, xd - 6, 1, 2, m.concretoSujo);
            }, 1),
            Variacao(raiz.transform, "Varanda_Madeira", v =>
            {
                for (int x = xe; x < xd; x += 8)
                    if (x < -9 || x > 6) v.Caixa(x, ySup, 19, 2, 8, 2, m.madeiraEscura);
                v.Caixa(xd - 2, ySup, 19, 2, 8, 2, m.madeiraEscura);
                v.Caixa(-8, ySup, 19, 2, 8, 2, m.madeiraEscura);
                v.Caixa(6, ySup, 19, 2, 8, 2, m.madeiraEscura);
                v.Caixa(xe, 31, 19, -6 - xe, 2, 2, m.madeira);
                v.Caixa(6, 31, 19, xd - 6, 2, 2, m.madeira);
                v.Caixa(xe, 27, 19, -6 - xe, 1, 2, m.madeira);
                v.Caixa(6, 27, 19, xd - 6, 1, 2, m.madeira);
            }, 1));

        // Toldo de zinco em cima das janelas do andar de cima: desce em degraus saindo da parede (z de 9 a 14)
        Slot(modular, "Toldo",
            Variacao(raiz.transform, "Toldo_Zinco", v => ToldoFino(v, m.zincoA, m.zincoB, zs), 1),
            Variacao(raiz.transform, "Toldo_Branco", v => ToldoFino(v, m.brancoToldo, m.concretoSujo, zs), 1));

        // Ar-condicionado no piso da varanda, encostado na parede
        Slot(modular, "ArCondicionado",
            Variacao(raiz.transform, "Ar_Um", v => ArCondicionado(v, -20, ySup, zs, m), 1),
            Variacao(raiz.transform, "Ar_Dois", v =>
            {
                ArCondicionado(v, -20, ySup, zs, m);
                ArCondicionado(v, 12, ySup, zs, m);
            }, 1));

        // Varal: dois postes no meio da varanda (z = 15) e a linha entre eles, com ou sem roupa
        Slot(modular, "Varal",
            Variacao(raiz.transform, "Varal_Roupas", v => Varal(v, m, comRoupa: true), 1),
            Variacao(raiz.transform, "Varal_Vazio", v => Varal(v, m, comRoupa: false), 1));

        // Cobertura (por cima de y = 46)
        Slot(modular, "Cobertura",
            Variacao(raiz.transform, "Cobertura_ChapaComAntena", v => CoberturaChapa(v, m), 1),
            Variacao(raiz.transform, "Cobertura_TelhaDuasAguas", v =>
            {
                for (int k = 0; -22 + k <= 9 - k; k++)
                    v.Caixa(-26, yTopo + k, -22 + k, 52, 1, (9 - k) - (-22 + k) + 1, k % 2 == 0 ? m.telhaA : m.telhaB);
            }, 1),
            Variacao(raiz.transform, "Cobertura_LajeComPlantas", v => CoberturaLaje(v, m), 1));

        // Desgaste: reboco caído mostrando o bloco, ou escorridos de ferrugem. Só na camada da parede, desviando de portas e janelas.
        Slot(modular, "Desgaste",
            Variacao(raiz.transform, "Desgaste_RebocoCaido", v =>
            {
                ManchaFina(v, xe + 1, 0, 14, 6, zf, 101, 0.55f, m.bloco, reservadaTerreo);
                ManchaFina(v, 9, 0, 14, 5, zf, 102, 0.5f, m.tijolo, reservadaTerreo);
                ManchaFina(v, xe + 1, 12, 18, 6, zf, 103, 0.35f, m.bloco, reservadaTerreo);
                ManchaFina(v, -2, ySup, 6, 4, zs, 104, 0.5f, m.bloco, reservadaSup);
                ManchaFina(v, 14, 38, 8, 6, zs, 105, 0.5f, m.tijolo, reservadaSup);
            }, 1),
            Variacao(raiz.transform, "Desgaste_Ferrugem", v =>
            {
                Escorrido(v, xe + 1, xd - 1, 19, zf, 111, m.ferrugem, reservadaTerreo);
                Escorrido(v, xe + 1, xd - 1, 44, zs, 112, m.ferrugem, reservadaSup);
            }, 1));

        Concluir(raiz, nome);
    }

    // Caixa de colisão em coordenadas de voxel fino: do canto (x0, y0, z0) até (x1, y1, z1), exclusivo
    static void AdicionarColisor(GameObject objeto, int x0, int y0, int z0, int x1, int y1, int z1)
    {
        var colisor = objeto.AddComponent<BoxCollider>();
        colisor.center = new Vector3((x0 + x1) / 2f * V, (y0 + y1) / 2f * V, (z0 + z1) / 2f * V);
        colisor.size = new Vector3((x1 - x0) * V, (y1 - y0) * V, (z1 - z0) * V);
    }

    // Porta com moldura de 2 voxels. grade: barras de ferro por fora da folha. remendo: bloco aparente no canto de baixo.
    static void PortaFina(Voxels v, int x, int y, int w, int h, int z, Material moldura, Material folha, Material grade, Material remendo)
    {
        const int e = 2;
        v.Caixa(x, y, z, e, h, 1, moldura);
        v.Caixa(x + w - e, y, z, e, h, 1, moldura);
        v.Caixa(x + e, y + h - e, z, w - 2 * e, e, 1, moldura);
        v.Caixa(x + e, y, z, w - 2 * e, h - e, 1, folha);

        if (remendo != null) v.Caixa(x + w - e - 3, y, z, 3, 6, 1, remendo);

        if (grade != null)
            for (int i = 1; i < w - 2 * e; i += 3)
                v.Caixa(x + e + i, y + 1, z + 1, 1, h - e - 2, 1, grade);
    }

    // Janela com moldura de 2 voxels na camada z. Grade, barras e travessas ficam em z + 1.
    static void JanelaFina(Voxels v, TipoJanela tipo, int x, int y, int w, int h, int z, Materiais m)
    {
        const int e = 2;
        v.Caixa(x, y, z, e, h, 1, m.moldura);
        v.Caixa(x + w - e, y, z, e, h, 1, m.moldura);
        v.Caixa(x + e, y + h - e, z, w - 2 * e, e, 1, m.moldura);
        v.Caixa(x + e, y, z, w - 2 * e, e, 1, m.moldura);

        int ix = x + e, iy = y + e, iw = w - 2 * e, ih = h - 2 * e;

        switch (tipo)
        {
            case TipoJanela.Caixilho:
                v.Caixa(ix, iy, z, iw, ih, 1, m.vidro);
                v.Caixa(ix + iw / 2, iy, z, 1, ih, 1, m.moldura);
                v.Caixa(ix, iy + ih / 2, z, iw, 1, 1, m.moldura);
                break;

            case TipoJanela.Grade:
                v.Caixa(ix, iy, z, iw, ih, 1, m.vidro);
                for (int i = 1; i < iw; i += 3) v.Caixa(ix + i, iy, z + 1, 1, ih, 1, m.ferro);
                v.Caixa(ix, iy + ih / 2, z + 1, iw, 1, 1, m.ferro);
                break;

            case TipoJanela.Persiana:
                for (int j = 0; j < ih; j++)
                    v.Caixa(ix, iy + j, z, iw, 1, 1, j % 2 == 0 ? m.madeira : m.madeiraEscura);
                break;

            case TipoJanela.Tapada:
                for (int i = 0; i < iw; i += 3)
                    v.Caixa(ix + i, iy, z, Mathf.Min(3, iw - i), ih, 1, (i / 3) % 2 == 0 ? m.madeira : m.madeiraEscura);
                v.Caixa(ix, iy + 2, z + 1, iw, 1, 1, m.madeiraEscura);
                v.Caixa(ix, iy + ih - 3, z + 1, iw, 1, 1, m.madeiraEscura);
                break;
        }
    }

    // Toldo ondulado em cima das duas janelas do andar de cima: seis degraus saindo da parede, de z + 1 a z + 6
    static void ToldoFino(Voxels v, Material a, Material b, int zs)
    {
        foreach (int x0 in new[] { -23, 7 })
        {
            for (int k = 0; k < 6; k++)
                for (int x = x0; x < x0 + 16; x++)
                    v.Celula(x, 45 - k, zs + 1 + k, ((x - x0) / 2) % 2 == 0 ? a : b);
        }
    }

    // Aparelho de janela: caixa com grelha na frente (linhas de dois tons), apoiado no piso da varanda
    static void ArCondicionado(Voxels v, int x, int y, int z, Materiais m)
    {
        v.Caixa(x, y, z, 9, 6, 6, m.brancoToldo);
        for (int j = 1; j < 5; j += 2) v.Caixa(x + 1, y + j, z + 6, 7, 1, 1, m.ferro);
        v.Caixa(x + 1, y + 5, z + 6, 7, 1, 1, m.concretoSujo);
    }

    // Postes nas duas pontas da varanda, a linha entre eles e (opcional) as roupas penduradas
    static void Varal(Voxels v, Materiais m, bool comRoupa)
    {
        v.Caixa(-23, 24, 15, 1, 15, 1, m.ferro);
        v.Caixa(22, 24, 15, 1, 15, 1, m.ferro);
        v.Caixa(-23, 38, 15, 46, 1, 1, m.ferro);

        if (!comRoupa) return;

        Material[] cores = { m.roupaAzul, m.roupaBranca, m.roupaAmarela, m.roupaVermelha };
        var r = new System.Random(8);
        for (int x = -20, i = 0; x < 19; x += 6, i++)
        {
            int largura = 3 + r.Next(0, 3);
            int altura = 5 + r.Next(0, 3);
            v.Caixa(x, 38 - altura, 15, largura, altura, 1, cores[i % cores.Length]);
        }
    }

    // Telhado de chapa levemente inclinado pra trás, com faixas de dois tons, e uma antena de TV
    static void CoberturaChapa(Voxels v, Materiais m)
    {
        // Para cada coluna em z, a chapa fica em y = 46 + (11 - z) / 8. Embaixo dela, sobre o andar de cima (z -20..7), um calço.
        for (int z = -22; z <= 11; z++)
        {
            int yChapa = 46 + (11 - z) / 8;

            for (int x = -26; x <= 25; x++)
                v.Celula(x, yChapa, z, ((x + 26) / 3) % 2 == 0 ? m.zincoA : m.zincoB);

            if (z >= -20 && z <= 7)
                for (int y = 46; y < yChapa; y++)
                    v.Caixa(-24, y, z, 48, 1, 1, m.laje);
        }

        // Antena: mastro e três travessas
        int baseAntena = 46 + (11 + 8) / 8 + 1;
        v.Caixa(10, baseAntena, -8, 1, 24, 1, m.antena);
        foreach (int dy in new[] { 22, 18, 14 })
            v.Caixa(10 - (dy / 4), baseAntena + dy, -8, (dy / 4) * 2 + 1, 1, 1, m.antena);
    }

    // Laje plana com platibanda de 1 voxel e moitas de plantas em cima
    static void CoberturaLaje(Voxels v, Materiais m)
    {
        v.Caixa(-25, 46, -21, 50, 2, 30, m.laje);

        for (int x = -25; x <= 24; x++)
            for (int z = -21; z <= 8; z++)
                if (x == -25 || x == 24 || z == -21 || z == 8) v.Celula(x, 48, z, m.laje);

        var r = new System.Random(17);
        for (int i = 0; i < 9; i++)
        {
            int px = r.Next(-22, 20), pz = r.Next(-18, 4), h = 2 + r.Next(0, 3);
            v.Caixa(px, 48, pz, 2, h, 2, r.NextDouble() < 0.5 ? m.folha : m.folhaEscura);
        }
    }

    // Mancha irregular de voxels finos numa camada só, desviando das células reservadas
    static void ManchaFina(Voxels v, int x0, int y0, int w, int h, int z, int seed, float chance, Material mat, HashSet<Vector3Int> reservada)
    {
        var r = new System.Random(seed);
        for (int j = 0; j < h; j++)
        {
            for (int i = 0; i < w; i++)
            {
                if (r.NextDouble() > chance * (1f - j / (float)(h + 1))) continue;
                if (reservada.Contains(new Vector3Int(x0 + i, y0 + j, z))) continue;
                v.Celula(x0 + i, y0 + j, z, mat);
            }
        }
    }

    // Escorridos verticais que descem de uma altura, de tamanhos diferentes, numa camada só
    static void Escorrido(Voxels v, int xIni, int xFim, int yTopo, int z, int seed, Material mat, HashSet<Vector3Int> reservada)
    {
        var r = new System.Random(seed);
        for (int x = xIni; x < xFim; x += 3 + r.Next(0, 4))
        {
            int comprimento = 3 + r.Next(0, 9);
            for (int y = yTopo; y > yTopo - comprimento; y--)
                if (!reservada.Contains(new Vector3Int(x, y, z))) v.Celula(x, y, z, mat);
        }
    }

    // ---------------------------------------------------------------- sobrado (referência 14), voxel fino de 0,125 m

    // Sobrado: térreo com porta e duas janelas, cornija (laje) entre os andares, andar de cima com uma varanda recuada
    // no canto da esquerda, telhado com beiral. Origem no meio da base; a frente é +Z. Voxels finos.
    //   térreo: x -24..23, z -20..19, y 0..21      laje: y 22..23 (avança 1 voxel em volta; o piso da varanda é o topo dela)
    //   andar de cima: bloco dos fundos x -24..23, z -20..3; bloco da frente x -8..23, z 4..19; varanda x -24..-9, z 4..19
    // Camadas da fachada: térreo z = 20 (y <= 21), andar de cima z = 20 (y >= 24), fundo da varanda z = 4.
    static void MontarSobrado(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();

        const int L = 48, alturaTerreo = 22, alturaSup = 22;
        const int xe = -24, xd = 24, zt = -20, zf = 20;
        const int yLaje = 22, ySup = 24, yTopo = 46;
        const int xB = -8, zB = 4;

        var laje = new Voxels(1);
        laje.Caixa(xe - 1, yLaje, zt - 1, L + 2, 2, zf - (zt - 1) + 1, m.laje);
        Fixo(raiz.transform, "Laje", laje);

        AdicionarColisor(raiz, xe - 1, 0, zt - 1, xd + 1, yLaje + 2, zf + 1);
        AdicionarColisor(raiz, xe, ySup, zt, xd, yTopo, zB);
        AdicionarColisor(raiz, xB, ySup, zB, xd, yTopo, zf);

        // Regiões de parede que o desgaste e os detalhes não podem cobrir
        var resTerreo = new HashSet<Vector3Int>();
        Reservar(resTerreo, -6, 0, zf, 10, 20, 1);
        Reservar(resTerreo, -23, 7, zf, 14, 12, 1);
        Reservar(resTerreo, 7, 7, zf, 16, 12, 1);
        Reservar(resTerreo, -9, 10, zf, 3, 5, 1);
        Reservar(resTerreo, 5, 13, zf, 2, 2, 1);

        var resSup = new HashSet<Vector3Int>();
        Reservar(resSup, -7, 32, zf, 8, 10, 1);
        Reservar(resSup, 3, 30, zf, 18, 12, 1);

        // Pintura: térreo e andar de cima (as duas partes do andar de cima levam a mesma cor)
        ModeloDaPeca Paredes(string n, Material terreo, Material sup) => Variacao(raiz.transform, n, v =>
        {
            v.Caixa(xe, 0, zt, L, alturaTerreo, zf - zt, terreo);
            v.Caixa(xe, ySup, zt, L, alturaSup, zB - zt, sup);
            v.Caixa(xB, ySup, zB, xd - xB, alturaSup, zf - zB, sup);
        }, 1);

        Slot(modular, "Pintura",
            Paredes("Pintura_LaranjaEVerdeAgua", m.laranja, m.verdeAgua),
            Paredes("Pintura_AmareloEAzul", m.amarelo, m.azulParede),
            Paredes("Pintura_VermelhoEConcreto", m.vermelhoDesbotado, m.concretoSujo),
            Paredes("Pintura_TurquesaEAmarelo", m.turquesa, m.amarelo),
            Paredes("Pintura_Concreto", m.concretoSujo, m.concretoSujo));

        Slot(modular, "Porta",
            Variacao(raiz.transform, "Porta_BrancaRemendada", v => PortaFina(v, -6, 0, 10, 20, zf, m.moldura, m.roupaBranca, null, m.bloco), 1),
            Variacao(raiz.transform, "Porta_Madeira", v => PortaFina(v, -6, 0, 10, 20, zf, m.moldura, m.portaMadeira, null, null), 1),
            Variacao(raiz.transform, "Porta_FerroComGrade", v => PortaFina(v, -6, 0, 10, 20, zf, m.moldura, m.portaTurquesa, m.ferro, null), 1));

        ModeloDaPeca JanelasTerreo(string n, TipoJanela tipo) => Variacao(raiz.transform, n, v =>
        {
            JanelaFina(v, tipo, -23, 7, 14, 12, zf, m);
            JanelaFina(v, tipo, 7, 7, 16, 12, zf, m);
        }, 1);

        // As janelas de cima: uma estreita, uma larga e a do fundo da varanda
        ModeloDaPeca JanelasSup(string n, TipoJanela tipo) => Variacao(raiz.transform, n, v =>
        {
            JanelaFina(v, tipo, -7, 32, 8, 10, zf, m);
            JanelaFina(v, tipo, 3, 30, 18, 12, zf, m);
            JanelaFina(v, tipo, -21, 29, 10, 13, zB, m);
        }, 1);

        Slot(modular, "JanelaTerreo",
            JanelasTerreo("JanelaTerreo_Grade", TipoJanela.Grade),
            JanelasTerreo("JanelaTerreo_Persiana", TipoJanela.Persiana),
            JanelasTerreo("JanelaTerreo_Tapada", TipoJanela.Tapada),
            JanelasTerreo("JanelaTerreo_Caixilho", TipoJanela.Caixilho));

        Slot(modular, "JanelaSuperior",
            JanelasSup("JanelaSuperior_Caixilho", TipoJanela.Caixilho),
            JanelasSup("JanelaSuperior_Grade", TipoJanela.Grade),
            JanelasSup("JanelaSuperior_Persiana", TipoJanela.Persiana),
            JanelasSup("JanelaSuperior_Tapada", TipoJanela.Tapada));

        // Parapeito da varanda: a frente (z 18..19) e a lateral esquerda (x -24..-23), de y 24 até 30, com tampa em y = 31.
        // A frente tem uma PASSAGEM de 12 voxels (x -21..-10) pra uma escada chegar (veja a EscadaMetalica).
        Slot(modular, "Parapeito",
            Variacao(raiz.transform, "Parapeito_Bloco", v =>
            {
                for (int y = ySup; y <= 30; y++)
                {
                    for (int x = xe; x < xB; x++)
                    {
                        if (x >= -21 && x <= -10) continue;
                        for (int z = 18; z <= 19; z++)
                            v.Celula(x, y, z, BlocoDeParede(x, y, m));
                    }
                    for (int z = zB; z < 18; z++)
                        for (int x = xe; x <= xe + 1; x++)
                            v.Celula(x, y, z, BlocoDeParede(z, y, m));
                }
                // Tampa só em cima do parapeito. O resto da varanda e a passagem ficam abertos.
                v.Caixa(xe, 31, 18, 3, 1, 2, m.concretoSujo);
                v.Caixa(-9, 31, 18, 1, 1, 2, m.concretoSujo);
                v.Caixa(xe, 31, zB, 2, 1, 18 - zB, m.concretoSujo);
            }, 1),
            Variacao(raiz.transform, "Parapeito_Ferro", v =>
            {
                foreach (int y in new[] { 31, 27 })
                {
                    v.Caixa(xe, y, 19, 3, 1, 1, m.ferro);
                    v.Caixa(-9, y, 19, 1, 1, 1, m.ferro);
                    v.Caixa(xe, y, zB, 1, 1, 19 - zB, m.ferro);
                }
                for (int x = xe; x < xB; x += 3)
                    if (x < -21 || x > -10) v.Caixa(x, ySup, 19, 1, 7, 1, m.ferro);
                v.Caixa(-22, ySup, 19, 1, 7, 1, m.ferro);
                for (int z = zB; z < 19; z += 3) v.Caixa(xe, ySup, z, 1, 7, 1, m.ferro);
            }, 1),
            Variacao(raiz.transform, "Parapeito_Madeira", v =>
            {
                foreach (int y in new[] { 31, 27 })
                {
                    int altura = y == 31 ? 1 : 1;
                    v.Caixa(xe, y, 18, 3, altura, 2, m.madeira);
                    v.Caixa(-9, y, 18, 1, altura, 2, m.madeira);
                    v.Caixa(xe, y, zB, 2, altura, 18 - zB, m.madeira);
                }
                v.Caixa(xe, ySup, 18, 2, 7, 2, m.madeiraEscura);
                v.Caixa(-9, ySup, 18, 1, 7, 2, m.madeiraEscura);
                for (int z = zB; z < 18; z += 8) v.Caixa(xe, ySup, z, 2, 7, 2, m.madeiraEscura);
            }, 1));

        // Cobertura: telha cerâmica de duas águas (cumeeira ao longo de X), com ou sem os toldos de chapa nas laterais,
        // chapa de zinco ou laje com plantas. Dois postes seguram o canto da varanda (y 32..45).
        Slot(modular, "Cobertura",
            Variacao(raiz.transform, "Cobertura_TelhaCeramica", v =>
            {
                TelhadoDeTelha(v, m, yTopo);
                PostesDaVaranda(v, m, xe, xB, zf);
            }, 1),
            Variacao(raiz.transform, "Cobertura_TelhaComToldoLateral", v =>
            {
                TelhadoDeTelha(v, m, yTopo);
                PostesDaVaranda(v, m, xe, xB, zf);
                // Toldo de chapa saindo de cada lado do telhado, descendo em degraus
                for (int k = 0; k < 4; k++)
                {
                    for (int z = -22; z <= 21; z++)
                    {
                        Material mat = (z / 3) % 2 == 0 ? m.zincoA : m.zincoB;
                        v.Celula(-27 - k, yTopo - k, z, mat);
                        v.Celula(26 + k, yTopo - k, z, mat);
                    }
                }
            }, 1),
            Variacao(raiz.transform, "Cobertura_ChapaDeZinco", v =>
            {
                PostesDaVaranda(v, m, xe, xB, zf);
                for (int z = -22; z <= 21; z++)
                {
                    int yChapa = yTopo + (21 - z) / 10;
                    for (int x = -26; x <= 25; x++)
                        v.Celula(x, yChapa, z, ((x + 26) / 3) % 2 == 0 ? m.zincoA : m.zincoB);

                    // Calço sobre as paredes (não sobre a varanda, que é aberta)
                    for (int y = yTopo; y < yChapa; y++)
                    {
                        int xIni = z >= zB ? xB : xe;
                        if (z >= zt && z < zf) v.Caixa(xIni, y, z, xd - xIni, 1, 1, m.laje);
                    }
                }
            }, 1),
            Variacao(raiz.transform, "Cobertura_LajeComPlantas", v =>
            {
                PostesDaVaranda(v, m, xe, xB, zf);
                v.Caixa(-25, yTopo, -21, 50, 2, 42, m.laje);
                for (int x = -25; x <= 24; x++)
                    for (int z = -21; z <= 20; z++)
                        if (x == -25 || x == 24 || z == -21 || z == 20) v.Celula(x, yTopo + 2, z, m.laje);

                var r = new System.Random(23);
                for (int i = 0; i < 9; i++)
                    v.Caixa(r.Next(-22, 20), yTopo + 2, r.Next(-18, 16), 2, 2 + r.Next(0, 3), 2, r.NextDouble() < 0.5 ? m.folha : m.folhaEscura);
            }, 1));

        Slot(modular, "Desgaste",
            Variacao(raiz.transform, "Desgaste_RebocoRachado", v =>
            {
                ManchaFina(v, xe + 1, 0, 12, 6, zf, 201, 0.55f, m.bloco, resTerreo);
                ManchaFina(v, 8, 0, 14, 5, zf, 202, 0.5f, m.tijolo, resTerreo);
                ManchaFina(v, -6, 21, 12, 1, zf, 203, 0.9f, m.bloco, resTerreo);
                ManchaFina(v, xB + 1, ySup, 10, 4, zf, 204, 0.45f, m.bloco, resSup);
                ManchaFina(v, 8, 42, 14, 3, zf, 205, 0.4f, m.bloco, resSup);
            }, 1),
            Variacao(raiz.transform, "Desgaste_Ferrugem", v =>
            {
                Escorrido(v, xe + 1, xd - 1, 21, zf, 211, m.ferrugem, resTerreo);
                Escorrido(v, xB + 1, xd - 1, 44, zf, 212, m.ferrugem, resSup);
            }, 1));

        // Detalhe ao lado da porta: caixa de correio (entre a janela e a porta) ou o número da casa (entre a porta e a outra janela)
        Slot(modular, "Detalhe",
            Variacao(raiz.transform, "Detalhe_CaixaDeCorreio", v =>
            {
                v.Caixa(-9, 10, zf, 3, 5, 1, m.portaAzul);
                v.Caixa(-8, 12, zf + 1, 1, 1, 1, m.ferro);
            }, 1),
            Variacao(raiz.transform, "Detalhe_NumeroDaCasa", v =>
            {
                v.Caixa(5, 13, zf, 2, 2, 1, m.brancoToldo);
                v.Celula(5, 13, zf + 1, m.roupaVermelha);
            }, 1));

        Concluir(raiz, nome);
    }

    // Padrão de blocos: tons alternados, com as juntas deslocadas de uma fiada pra outra
    static Material BlocoDeParede(int ao, int y, Materiais m)
    {
        int fiada = y / 2;
        return ((ao + 100 + (fiada % 2) * 3) / 6 + fiada) % 2 == 0 ? m.bloco : m.concretoSujo;
    }

    // Telhado de telha de duas águas: camadas de 1 voxel que estreitam 2 voxels por lado, tons alternados por fiada
    static void TelhadoDeTelha(Voxels v, Materiais m, int yTopo)
    {
        for (int k = 0; -22 + 2 * k <= 21 - 2 * k; k++)
        {
            int zMin = -22 + 2 * k, zMax = 21 - 2 * k;
            bool cumeeira = zMax - zMin < 3;
            v.Caixa(-26, yTopo + k, zMin, 52, 1, zMax - zMin + 1, cumeeira ? m.cumeeira : (k % 2 == 0 ? m.telhaA : m.telhaB));
        }
    }

    // Dois postes de madeira nos cantos da frente da varanda, do parapeito até o telhado
    static void PostesDaVaranda(Voxels v, Materiais m, int xe, int xB, int zf)
    {
        v.Caixa(xe, 32, zf - 2, 2, 14, 2, m.madeiraEscura);
        v.Caixa(xB - 2, 32, zf - 2, 2, 14, 2, m.madeiraEscura);
    }

    // ---------------------------------------------------------------- props de quintal (referência 15)

    // Disco cheio no plano XZ (centro cx, cz), de y até y + h
    static void Disco(Voxels v, int cx, int cz, int y, int h, int raio, Material mat)
    {
        for (int dz = -raio; dz <= raio; dz++)
            for (int dx = -raio; dx <= raio; dx++)
                if (dx * dx + dz * dz <= raio * raio + raio) v.Caixa(cx + dx, y, cz + dz, 1, h, 1, mat);
    }

    // Anel no plano XZ, entre raioMin e raioMax
    static void Anel(Voxels v, int cx, int cz, int y, int h, float raioMin, float raioMax, Material mat)
    {
        int r = Mathf.CeilToInt(raioMax);
        for (int dz = -r; dz <= r; dz++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                if (d >= raioMin && d <= raioMax) v.Caixa(cx + dx, y, cz + dz, 1, h, 1, mat);
            }
        }
    }

    // Mesa de madeira com bancos dos dois lados, toalha e itens em cima. Origem no meio da base.
    //   pernas e tampo: y 0..7 (tampo em y 6..7). Bancos: z -10..-7 e 6..9. Itens: a partir de y = 8.
    static void MontarMesa(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();
        AdicionarColisor(raiz, -10, 0, -5, 10, 8, 5);

        Slot(modular, "Mesa",
            Variacao(raiz.transform, "Mesa_Madeira", v => Mesa(v, m, null, null), 1),
            Variacao(raiz.transform, "Mesa_ToalhaXadrez", v => Mesa(v, m, m.roupaVermelha, m.roupaBranca), 1),
            Variacao(raiz.transform, "Mesa_ToalhaLisa", v => Mesa(v, m, m.roupaAzul, m.roupaAzul), 1));

        Slot(modular, "Bancos",
            Variacao(raiz.transform, "Bancos_DoisLados", v =>
            {
                Banco(v, m, -10);
                Banco(v, m, 6);
            }, 1),
            Variacao(raiz.transform, "Bancos_UmLado", v => Banco(v, m, 6), 1));

        Slot(modular, "Itens",
            Variacao(raiz.transform, "Itens_PratoECopo", v =>
            {
                Disco(v, -5, 0, 8, 1, 3, m.brancoToldo);
                v.Caixa(2, 8, -1, 3, 4, 3, m.metal);
                v.Caixa(3, 12, 0, 1, 1, 1, m.ferro);
            }, 1),
            Variacao(raiz.transform, "Itens_VasoComFlor", v =>
            {
                v.Caixa(-2, 8, -2, 4, 4, 4, m.portaAzul);
                v.Caixa(-1, 12, -1, 2, 3, 2, m.folha);
                v.Celula(0, 15, 0, m.florRosa);
                v.Celula(-2, 14, 1, m.florAmarela);
                v.Celula(2, 14, -1, m.florVermelha);
            }, 1));

        Concluir(raiz, nome);
    }

    static void Mesa(Voxels v, Materiais m, Material toalhaA, Material toalhaB)
    {
        foreach (int x in new[] { -9, 7 })
            foreach (int z in new[] { -4, 2 })
                v.Caixa(x, 0, z, 2, 6, 2, m.madeiraEscura);

        v.Caixa(-10, 6, -5, 20, 2, 10, m.madeira);

        if (toalhaA == null) return;

        // Toalha: cobre o tampo (y = 7) e cai 3 voxels dos dois lados compridos, em xadrez de dois tons
        for (int x = -10; x < 10; x++)
        {
            for (int z = -5; z < 5; z++)
                v.Celula(x, 7, z, ((x + 10) / 2 + (z + 5) / 2) % 2 == 0 ? toalhaA : toalhaB);
            for (int y = 4; y <= 6; y++)
            {
                v.Celula(x, y, -6, ((x + 10) / 2) % 2 == 0 ? toalhaA : toalhaB);
                v.Celula(x, y, 5, ((x + 10) / 2) % 2 == 0 ? toalhaA : toalhaB);
            }
        }
    }

    static void Banco(Voxels v, Materiais m, int z0)
    {
        v.Caixa(-10, 3, z0, 20, 2, 4, m.madeira);
        foreach (int x in new[] { -9, 7 })
            v.Caixa(x, 0, z0 + 1, 2, 3, 2, m.madeiraEscura);
    }

    // Varal de quintal: dois postes, a linha entre eles e as roupas penduradas. Origem no meio da base, varal ao longo de X.
    static void MontarVaralDeQuintal(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();
        AdicionarColisor(raiz, -26, 0, -1, -24, 26, 1);
        AdicionarColisor(raiz, 24, 0, -1, 26, 26, 1);

        Slot(modular, "Poste",
            Variacao(raiz.transform, "Poste_Madeira", v =>
            {
                v.Caixa(-26, 0, -1, 2, 26, 2, m.madeiraEscura);
                v.Caixa(24, 0, -1, 2, 26, 2, m.madeiraEscura);
                v.Caixa(-26, 24, 0, 52, 1, 1, m.antena);
            }, 1),
            Variacao(raiz.transform, "Poste_Ferro", v =>
            {
                v.Caixa(-26, 0, 0, 1, 26, 1, m.ferro);
                v.Caixa(25, 0, 0, 1, 26, 1, m.ferro);
                v.Caixa(-27, 0, -1, 3, 1, 3, m.ferro);
                v.Caixa(24, 0, -1, 3, 1, 3, m.ferro);
                v.Caixa(-26, 24, 0, 52, 1, 1, m.antena);
            }, 1));

        Slot(modular, "Roupas",
            Variacao(raiz.transform, "Roupas_Camisas", v =>
            {
                Material[] cores = { m.roupaAzul, m.roupaBranca, m.roupaAmarela, m.roupaVermelha };
                for (int i = 0, x = -22; x < 16; x += 9, i++) Camisa(v, x, 23, cores[i % cores.Length]);
            }, 1),
            Variacao(raiz.transform, "Roupas_Lencois", v =>
            {
                v.Caixa(-22, 11, 0, 18, 13, 1, m.roupaBranca);
                v.Caixa(0, 11, 0, 20, 13, 1, m.roupaAzul);
            }, 1),
            Variacao(raiz.transform, "Roupas_Mistas", v =>
            {
                Camisa(v, -22, 23, m.roupaAmarela);
                v.Caixa(-10, 11, 0, 3, 13, 1, m.roupaAzul);
                v.Caixa(-6, 11, 0, 3, 13, 1, m.roupaAzul);
                v.Caixa(0, 13, 0, 16, 11, 1, m.roupaBranca);
                Camisa(v, 16, 23, m.roupaVermelha);
            }, 1));

        Concluir(raiz, nome);
    }

    // Camisa pendurada: corpo 6x8 e duas mangas
    static void Camisa(Voxels v, int x, int yTopo, Material cor)
    {
        v.Caixa(x + 1, yTopo - 7, 0, 6, 8, 1, cor);
        v.Caixa(x, yTopo - 3, 0, 1, 4, 1, cor);
        v.Caixa(x + 6, yTopo - 3, 0, 1, 4, 1, cor);
    }

    // Pedras de passagem na grama: lajes baixas (1 voxel) de formas irregulares, com folga entre elas
    static void MontarPedras(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();

        Slot(modular, "Pedras",
            Variacao(raiz.transform, "Pedras_Reta_Concreto", v => Pedras(v, m.concretoSujo, false, 4), 1),
            Variacao(raiz.transform, "Pedras_Reta_Bloco", v => Pedras(v, m.bloco, false, 5), 1),
            Variacao(raiz.transform, "Pedras_Curva_Concreto", v => Pedras(v, m.concretoSujo, true, 6), 1),
            Variacao(raiz.transform, "Pedras_Curva_Bloco", v => Pedras(v, m.bloco, true, 7), 1));

        Concluir(raiz, nome);
    }

    static void Pedras(Voxels v, Material mat, bool curva, int seed)
    {
        var r = new System.Random(seed);
        for (int i = 0; i < 6; i++)
        {
            int w = 7 + r.Next(0, 3), d = 6 + r.Next(0, 3);
            int cx = curva ? (int)(Mathf.Sin(i * 0.7f) * 14f) : r.Next(-2, 3);
            int cz = i * 11 - 28;

            for (int z = 0; z < d; z++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool canto = (x == 0 || x == w - 1) && (z == 0 || z == d - 1);
                    if (!canto) v.Celula(cx - w / 2 + x, 0, cz + z, mat);
                }
            }
        }
    }

    // Segmento de muro de bloco de 2 m (16 voxels de largura), 0,5 m de espessura e 2,5 m de altura. Origem no meio da base.
    // O topo tem a MESMA largura do muro (só avança nos lados de frente e de trás), pra segmentos lado a lado, a 2 m um do outro, não se sobreporem.
    static void MontarMuro(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();
        AdicionarColisor(raiz, -8, 0, -3, 8, 21, 3);

        Slot(modular, "Acabamento",
            Variacao(raiz.transform, "Acabamento_RebocoLiso", v => v.Caixa(-8, 0, -2, 16, 20, 4, m.concretoSujo), 1),
            Variacao(raiz.transform, "Acabamento_BlocoAparente", v =>
            {
                for (int y = 0; y < 20; y++)
                    for (int x = -8; x < 8; x++)
                        for (int z = -2; z < 2; z++)
                            v.Celula(x, y, z, BlocoDeParede(x, y, m));
            }, 1),
            Variacao(raiz.transform, "Acabamento_Pintado", v => v.Caixa(-8, 0, -2, 16, 20, 4, m.laranja), 1));

        Slot(modular, "Topo",
            Variacao(raiz.transform, "Topo_Liso", v => v.Caixa(-8, 20, -3, 16, 1, 6, m.concretoSujo), 1),
            Variacao(raiz.transform, "Topo_Telhadinho", v =>
            {
                v.Caixa(-8, 20, -3, 16, 1, 6, m.telhaA);
                v.Caixa(-8, 21, -2, 16, 1, 4, m.telhaB);
            }, 1),
            Variacao(raiz.transform, "Topo_Grade", v =>
            {
                v.Caixa(-8, 20, -3, 16, 1, 6, m.concretoSujo);
                for (int x = -8; x < 8; x += 3) v.Caixa(x, 21, -1, 1, 6, 1, m.ferro);
                v.Caixa(-8, 26, -1, 16, 1, 1, m.ferro);
            }, 1));

        // Musgo na base, dos dois lados do muro (nas camadas z = 2 e z = -3, que a parede não ocupa)
        Slot(modular, "Musgo",
            Variacao(raiz.transform, "Musgo_Base", v =>
            {
                var r = new System.Random(31);
                for (int x = -8; x < 8; x++)
                {
                    int altura = r.Next(0, 6);
                    for (int y = 0; y < altura; y++)
                        if (r.NextDouble() < 0.8) v.Celula(x, y, 2, m.musgo);
                    altura = r.Next(0, 5);
                    for (int y = 0; y < altura; y++)
                        if (r.NextDouble() < 0.8) v.Celula(x, y, -3, m.musgo);
                }
            }, 1));

        Concluir(raiz, nome);
    }

    // Roda de bicicleta em pé (aro, raios e cubo), no plano XY. Origem no meio da base.
    static void MontarRodaDeBicicleta(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();

        Slot(modular, "Roda",
            Variacao(raiz.transform, "Roda_Preta", v => Roda(v, m, m.ferro, m.antena), 1),
            Variacao(raiz.transform, "Roda_Azul", v => Roda(v, m, m.portaAzul, m.antena), 1),
            Variacao(raiz.transform, "Roda_Enferrujada", v => Roda(v, m, m.ferrugem, m.zincoB), 1));

        Concluir(raiz, nome);
    }

    static void Roda(Voxels v, Materiais m, Material aro, Material raios)
    {
        const int cy = 12, raio = 12;

        for (int y = -raio; y <= raio; y++)
        {
            for (int x = -raio; x <= raio; x++)
            {
                float d = Mathf.Sqrt(x * x + y * y);
                if (d >= raio - 1.5f && d <= raio) v.Caixa(x, cy + y, 0, 1, 1, 2, aro);
            }
        }

        // Oito raios do cubo até o aro
        for (int a = 0; a < 8; a++)
        {
            float ang = a * Mathf.PI / 4f;
            for (int r = 2; r < raio - 1; r++)
                v.Caixa(Mathf.RoundToInt(Mathf.Cos(ang) * r), cy + Mathf.RoundToInt(Mathf.Sin(ang) * r), 0, 1, 1, 1, raios);
        }

        v.Caixa(-1, cy - 1, 0, 3, 3, 2, m.metal);
    }

    // Escada metálica externa de seis degraus (0,5 m cada) com corrimão, e um patamar no topo. Sobe ao longo de +Z.
    static void MontarEscadaMetalica(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();
        AdicionarColisor(raiz, -6, 0, 0, 6, 24, 30);   // decorativa: o degrau de 0,5 m é alto demais pra subir andando

        Slot(modular, "Escada",
            Variacao(raiz.transform, "Escada_Ferro", v => Escada(v, m.ferro, m.antena), 1),
            Variacao(raiz.transform, "Escada_Vermelha", v => Escada(v, m.vermelhoDesbotado, m.ferrugem), 1),
            Variacao(raiz.transform, "Escada_Azul", v => Escada(v, m.portaAzul, m.antena), 1));

        // Patamar depois do último degrau (z 30..41, y 22..23), com corrimão dos lados
        Slot(modular, "Patamar",
            Variacao(raiz.transform, "Patamar_Completo", v =>
            {
                v.Caixa(-6, 22, 30, 12, 2, 12, m.antena);
                v.Caixa(-6, 24, 30, 1, 7, 12, m.ferro);
                v.Caixa(5, 24, 30, 1, 7, 12, m.ferro);
                v.Caixa(-6, 24, 41, 12, 7, 1, m.ferro);
            }, 1),
            Variacao(raiz.transform, "Patamar_Pequeno", v =>
            {
                v.Caixa(-6, 22, 30, 12, 2, 6, m.antena);
                v.Caixa(-6, 24, 30, 1, 7, 6, m.ferro);
                v.Caixa(5, 24, 30, 1, 7, 6, m.ferro);
            }, 1));

        Concluir(raiz, nome);
    }

    // Seis degraus de 5 de profundidade e 4 de altura. Banzos (laterais) e degraus (miolo) não se tocam em volume.
    static void Escada(Voxels v, Material estrutura, Material degrau)
    {
        for (int z = 0; z < 30; z++)
        {
            int passo = z / 5;
            int yBase = passo * 4;

            // Banzo: a lateral em escada, de 4 voxels de altura, nos dois lados
            v.Caixa(-6, yBase, z, 1, 4, 1, estrutura);
            v.Caixa(5, yBase, z, 1, 4, 1, estrutura);

            // Degrau: chapa de 2 voxels no topo de cada passo, entre os banzos
            v.Caixa(-5, yBase + 2, z, 10, 2, 1, degrau);

            // Corrimão: poste a cada passo e barra acompanhando a subida, por cima do banzo
            if (z % 5 == 0)
            {
                v.Caixa(-6, yBase + 4, z, 1, 6, 1, estrutura);
                v.Caixa(5, yBase + 4, z, 1, 6, 1, estrutura);
            }
            v.Caixa(-6, yBase + 10, z, 1, 1, 1, estrutura);
            v.Caixa(5, yBase + 10, z, 1, 1, 1, estrutura);
        }
    }

    // Pilhas de engradados: de madeira ou de plástico colorido
    static void MontarEngradados(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();
        AdicionarColisor(raiz, 0, 0, 0, 17, 16, 8);

        Slot(modular, "Pilha",
            Variacao(raiz.transform, "Pilha_Madeira_Um", v => Engradado(v, 0, 0, 0, m.madeira, m.madeiraEscura), 1),
            Variacao(raiz.transform, "Pilha_Madeira_Pilha", v =>
            {
                Engradado(v, 0, 0, 0, m.madeira, m.madeiraEscura);
                Engradado(v, 9, 0, 0, m.madeira, m.madeiraEscura);
                Engradado(v, 4, 8, 0, m.madeira, m.madeiraEscura);
            }, 1),
            Variacao(raiz.transform, "Pilha_Plastico_Azul", v =>
            {
                Engradado(v, 0, 0, 0, m.portaAzul, m.azulParede);
                Engradado(v, 0, 8, 0, m.portaAzul, m.azulParede);
            }, 1),
            Variacao(raiz.transform, "Pilha_Plastico_Vermelho", v =>
            {
                Engradado(v, 0, 0, 0, m.roupaVermelha, m.vermelhoDesbotado);
                Engradado(v, 9, 0, 0, m.roupaAmarela, m.amarelo);
            }, 1));

        Concluir(raiz, nome);
    }

    // Engradado 8x8x8: preenchido, com as arestas em outro tom
    static void Engradado(Voxels v, int x, int y, int z, Material corpo, Material borda)
    {
        v.Caixa(x, y, z, 8, 8, 8, corpo);
        for (int i = 0; i < 8; i++)
        {
            foreach (int a in new[] { 0, 7 })
            {
                foreach (int b in new[] { 0, 7 })
                {
                    v.Celula(x + i, y + a, z + b, borda);
                    v.Celula(x + a, y + i, z + b, borda);
                    v.Celula(x + a, y + b, z + i, borda);
                }
            }
        }
    }

    // Tambor de 200 litros com nervuras, e a tampa fechada ou aberta
    static void MontarTambor(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();

        var colisor = raiz.AddComponent<BoxCollider>();
        colisor.center = new Vector3(0f, 0.9f, 0f);
        colisor.size = new Vector3(1.4f, 1.8f, 1.4f);

        Slot(modular, "Corpo",
            Variacao(raiz.transform, "Tambor_Azul", v => CorpoDoTambor(v, m.portaAzul, m.azulParede), 1),
            Variacao(raiz.transform, "Tambor_Vermelho", v => CorpoDoTambor(v, m.roupaVermelha, m.vermelhoDesbotado), 1),
            Variacao(raiz.transform, "Tambor_Verde", v => CorpoDoTambor(v, m.folhaEscura, m.folha), 1));

        Slot(modular, "Tampa",
            Variacao(raiz.transform, "Tampa_Fechada", v => Disco(v, 0, 0, 14, 1, 5, m.metal), 1),
            Variacao(raiz.transform, "Tampa_Aberta", v => Anel(v, 0, 0, 14, 1, 4f, 5.5f, m.metal), 1));

        Concluir(raiz, nome);
    }

    static void CorpoDoTambor(Voxels v, Material corpo, Material nervura)
    {
        Disco(v, 0, 0, 0, 14, 5, corpo);
        foreach (int y in new[] { 3, 10 })
            Anel(v, 0, 0, y, 1, 5.6f, 6.4f, nervura);
    }

    // ---------------------------------------------------------------- modelo em voxels e malha ProBuilder

    // Grade de voxels: cada célula (x, y, z) existe uma vez só, com um material. Desenhar uma caixa por cima de outra
    // só troca o material das células, nunca duplica volume. É isso que evita faces sobrepostas.
    //
    // As células ficam sempre na grade FINA (V = 0,125 m). O parâmetro "escala" diz em que unidade as medidas que
    // entram em Celula/Caixa foram desenhadas: 1 = voxel fino (0,125 m); 2 = voxel de 0,25 m (cada medida é dobrada
    // e cada célula vira um bloco de 2x2x2 finos). As peças antigas usam 2, as novas usam 1.
    class Voxels
    {
        public readonly Dictionary<Vector3Int, Material> celulas = new Dictionary<Vector3Int, Material>();
        readonly int escala;

        public Voxels(int escala = 2)
        {
            this.escala = Mathf.Max(1, escala);
        }

        public void Celula(int x, int y, int z, Material material)
        {
            Caixa(x, y, z, 1, 1, 1, material);
        }

        // Caixa a partir do canto de baixo (x, y, z), com tamanho (w, h, d), na unidade da escala deste modelo
        public void Caixa(int x, int y, int z, int w, int h, int d, Material material)
        {
            int e = escala;
            for (int k = z * e; k < (z + d) * e; k++)
                for (int j = y * e; j < (y + h) * e; j++)
                    for (int i = x * e; i < (x + w) * e; i++)
                        celulas[new Vector3Int(i, j, k)] = material;
        }

        // Cria um objeto ProBuilder com uma malha só: só as faces expostas, juntadas em retângulos maiores (greedy meshing),
        // um material por face
        public ProBuilderMesh CriarMalha(Transform pai, string nome)
        {
            var posicoes = new List<Vector3>();
            var faces = new List<Face>();
            var materiais = new List<Material>();

            for (int eixo = 0; eixo < 3; eixo++)
            {
                int u = (eixo + 1) % 3;
                int vv = (eixo + 2) % 3;

                for (int sinal = -1; sinal <= 1; sinal += 2)
                {
                    // Pra cada fatia ao longo do eixo, as células cuja face nessa direção aparece (sem vizinho na frente)
                    var fatias = new Dictionary<int, Dictionary<Vector2Int, Material>>();
                    foreach (var par in celulas)
                    {
                        Vector3Int celula = par.Key;
                        Vector3Int vizinho = celula;
                        vizinho[eixo] += sinal;
                        if (celulas.ContainsKey(vizinho)) continue;

                        if (!fatias.TryGetValue(celula[eixo], out var mapa))
                            fatias[celula[eixo]] = mapa = new Dictionary<Vector2Int, Material>();

                        mapa[new Vector2Int(celula[u], celula[vv])] = par.Value;
                    }

                    foreach (var fatia in fatias)
                        JuntarFatia(fatia.Key, fatia.Value, eixo, u, vv, sinal, posicoes, faces, materiais);
                }
            }

            ProBuilderMesh malha = ProBuilderMesh.Create(posicoes, faces);
            malha.gameObject.name = nome;
            malha.transform.SetParent(pai, false);
            malha.GetComponent<MeshRenderer>().sharedMaterials = materiais.ToArray();
            GameObjectUtility.SetStaticEditorFlags(malha.gameObject, StaticEditorFlags.BatchingStatic);
            return malha;
        }

        // Junta as faces vizinhas do mesmo material, numa fatia, em retângulos grandes
        static void JuntarFatia(int fatia, Dictionary<Vector2Int, Material> mapa, int eixo, int u, int vv, int sinal,
            List<Vector3> posicoes, List<Face> faces, List<Material> materiais)
        {
            int a0 = int.MaxValue, b0 = int.MaxValue, a1 = int.MinValue, b1 = int.MinValue;
            foreach (Vector2Int chave in mapa.Keys)
            {
                a0 = Mathf.Min(a0, chave.x); a1 = Mathf.Max(a1, chave.x);
                b0 = Mathf.Min(b0, chave.y); b1 = Mathf.Max(b1, chave.y);
            }

            var usado = new bool[a1 - a0 + 1, b1 - b0 + 1];

            for (int b = b0; b <= b1; b++)
            {
                for (int a = a0; a <= a1; a++)
                {
                    if (usado[a - a0, b - b0] || !mapa.TryGetValue(new Vector2Int(a, b), out Material material)) continue;

                    // Estica pra um lado enquanto o material for o mesmo
                    int w = 1;
                    while (a + w <= a1 && !usado[a + w - a0, b - b0] &&
                           mapa.TryGetValue(new Vector2Int(a + w, b), out Material lado) && lado == material) w++;

                    // Estica pro outro lado enquanto a linha inteira for igual
                    int h = 1;
                    bool segue = true;
                    while (segue && b + h <= b1)
                    {
                        for (int i = 0; i < w; i++)
                        {
                            if (usado[a + i - a0, b + h - b0] ||
                                !mapa.TryGetValue(new Vector2Int(a + i, b + h), out Material acima) || acima != material)
                            {
                                segue = false;
                                break;
                            }
                        }
                        if (segue) h++;
                    }

                    for (int j = 0; j < h; j++)
                        for (int i = 0; i < w; i++)
                            usado[a + i - a0, b + j - b0] = true;

                    AdicionarFace(fatia, a, b, w, h, eixo, u, vv, sinal, material, posicoes, faces, materiais);
                }
            }
        }

        static void AdicionarFace(int fatia, int a, int b, int w, int h, int eixo, int u, int vv, int sinal, Material material,
            List<Vector3> posicoes, List<Face> faces, List<Material> materiais)
        {
            int plano = fatia + (sinal > 0 ? 1 : 0);

            Vector3 Ponto(int pa, int pb)
            {
                var celula = new Vector3Int();
                celula[eixo] = plano;
                celula[u] = pa;
                celula[vv] = pb;
                return new Vector3(celula.x * V, celula.y * V, celula.z * V);
            }

            Vector3 q0 = Ponto(a, b), q1 = Ponto(a + w, b), q2 = Ponto(a + w, b + h), q3 = Ponto(a, b + h);

            var normal = Vector3.zero;
            normal[eixo] = sinal;
            bool direto = Vector3.Dot(Vector3.Cross(q1 - q0, q2 - q0), normal) >= 0f;

            int inicio = posicoes.Count;
            posicoes.Add(q0); posicoes.Add(q1); posicoes.Add(q2); posicoes.Add(q3);

            int indice = materiais.IndexOf(material);
            if (indice < 0) { materiais.Add(material); indice = materiais.Count - 1; }

            int[] triangulos = direto
                ? new[] { inicio, inicio + 1, inicio + 2, inicio, inicio + 2, inicio + 3 }
                : new[] { inicio, inicio + 2, inicio + 1, inicio, inicio + 3, inicio + 2 };

            faces.Add(new Face(triangulos) { submeshIndex = indice });
        }
    }

    // ---------------------------------------------------------------- utilidades

    // Uma variação (ou o corpo) do prefab que está sendo montado, com as células que ela ocupa
    class ModeloDaPeca
    {
        public string nome;
        public int slot = -1;           // -1 = peça fixa (corpo)
        public GameObject objeto;
        public Voxels voxels;
    }

    static readonly List<ModeloDaPeca> pecasDoPrefab = new List<ModeloDaPeca>();

    static void Iniciar()
    {
        pecasDoPrefab.Clear();
    }

    // Peça que está sempre ligada (o corpo da casa)
    static GameObject Fixo(Transform pai, string nome, Voxels voxels)
    {
        GameObject grupo = new GameObject(nome);
        grupo.transform.SetParent(pai, false);
        voxels.CriarMalha(grupo.transform, nome + "_Malha");
        pecasDoPrefab.Add(new ModeloDaPeca { nome = nome, objeto = grupo, voxels = voxels });
        return grupo;
    }

    // Uma variação de um slot: um grupo filho com uma malha ProBuilder dentro
    // escala: 2 = o desenho está em voxels de 0,25 m (peças antigas); 1 = voxel fino de 0,125 m (peças novas)
    static ModeloDaPeca Variacao(Transform pai, string nome, System.Action<Voxels> montar, int escala = 2)
    {
        GameObject grupo = new GameObject(nome);
        grupo.transform.SetParent(pai, false);

        var voxels = new Voxels(escala);
        montar(voxels);
        voxels.CriarMalha(grupo.transform, nome + "_Malha");

        return new ModeloDaPeca { nome = nome, objeto = grupo, voxels = voxels };
    }

    // Cria um slot com as variações e liga só a primeira
    static void Slot(ObjetoModular modular, string nome, params ModeloDaPeca[] variacoes)
    {
        var slot = new ObjetoModular.Slot { nome = nome, escolhida = 0 };
        modular.slots.Add(slot);
        int indice = modular.slots.Count - 1;

        foreach (ModeloDaPeca variacao in variacoes)
        {
            variacao.slot = indice;
            slot.variacoes.Add(variacao.objeto);
            pecasDoPrefab.Add(variacao);
        }
    }

    // Confere, pra todas as combinações possíveis, se duas peças de slots diferentes (ou uma peça e o corpo) ocupam a
    // mesma célula. Se ocupam, as faces delas brigariam (flick), e o gerador avisa qual par e onde.
    static int ChecarSobreposicao(string prefab)
    {
        int conflitos = 0;

        for (int i = 0; i < pecasDoPrefab.Count; i++)
        {
            for (int j = i + 1; j < pecasDoPrefab.Count; j++)
            {
                ModeloDaPeca a = pecasDoPrefab[i], b = pecasDoPrefab[j];
                if (a.slot == b.slot && a.slot >= 0) continue;   // variações do mesmo slot nunca ficam ligadas juntas

                Dictionary<Vector3Int, Material> menor = a.voxels.celulas.Count <= b.voxels.celulas.Count ? a.voxels.celulas : b.voxels.celulas;
                Dictionary<Vector3Int, Material> maior = ReferenceEquals(menor, a.voxels.celulas) ? b.voxels.celulas : a.voxels.celulas;

                int dentro = 0;
                Vector3Int exemplo = default;
                foreach (Vector3Int celula in menor.Keys)
                {
                    if (!maior.ContainsKey(celula)) continue;
                    if (dentro == 0) exemplo = celula;
                    dentro++;
                }

                if (dentro == 0) continue;

                conflitos++;
                Debug.LogWarning($"Bilixins [{prefab}]: '{a.nome}' e '{b.nome}' ocupam {dentro} célula(s) em comum (ex: {exemplo}). As faces vão brigar (flick).");
            }
        }

        return conflitos;
    }

    // Confere as sobreposições, liga as variações escolhidas, salva o prefab e some com o objeto temporário da cena
    static void Concluir(GameObject raiz, string nome)
    {
        int conflitos = ChecarSobreposicao(nome);
        if (conflitos == 0) Debug.Log($"Bilixins [{nome}]: nenhuma peça se sobrepõe, em nenhuma combinação de variações.");

        var modular = raiz.GetComponent<ObjetoModular>();
        modular.AplicarVariacoes();
        GameObjectUtility.SetStaticEditorFlags(raiz, StaticEditorFlags.BatchingStatic);

        PrefabUtility.SaveAsPrefabAsset(raiz, $"{Pasta}/{nome}.prefab");
        Object.DestroyImmediate(raiz);
    }

    static Material Material(string nome, Color cor)
    {
        string caminho = $"{PastaMateriais}/{nome}.mat";
        var existente = AssetDatabase.LoadAssetAtPath<Material>(caminho);
        if (existente != null) return existente;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        var material = new Material(shader) { color = cor };
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.1f);
        AssetDatabase.CreateAsset(material, caminho);
        return material;
    }

    static void CriarPasta(string pai, string nome)
    {
        if (!AssetDatabase.IsValidFolder(pai + "/" + nome)) AssetDatabase.CreateFolder(pai, nome);
    }
}
