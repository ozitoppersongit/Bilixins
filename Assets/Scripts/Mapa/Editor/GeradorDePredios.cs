using System.Collections.Generic;
using UnityEngine;

// Parte do GeradorDeCasasEditor: prédio alto, casa de laje e bloco de varandas. Mesmas regras das outras casas:
// voxel fino de 0,125 m, uma célula só por voxel, camadas da fachada pra nunca duas peças ocuparem a mesma célula.
public static partial class GeradorDeCasasEditor
{
    // ---------------------------------------------------------------- prédio alto
    // 5 andares de 3 m (altura 15 m), 7 m de frente por 5 m de fundo. Origem no meio da base; a frente é +Z.
    //   paredes: x -28..27, z -20..19, y 0..119     cornijas (z = 20): y 22..23, 46..47, 70..71, 94..95
    //   laje do topo: y 120..121 (platibanda y 122..123)
    //   cada andar de cima (n = 1..4, base y = 24 n): janela esquerda x -24..-13, janela direita x 12..23,
    //   no meio a sacada (laje x -10..9, z 20..29, y base..base+1) com a porta de vidro, ou só uma janela.
    static void MontarPredioAlto(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();

        const int L = 56, P = 40, xe = -28, xd = 28, zt = -20, zf = 20, pisos = 5, altura = 24 * pisos;

        var corpo = new Voxels(1);
        for (int n = 1; n < pisos; n++) corpo.Caixa(xe, 24 * n - 2, zf, L, 2, 1, m.laje);
        corpo.Caixa(xe - 1, altura, zt - 1, L + 2, 2, P + 2, m.laje);
        corpo.Caixa(xe - 1, altura + 2, zt - 1, L + 2, 2, 1, m.laje);
        corpo.Caixa(xe - 1, altura + 2, zf, L + 2, 2, 1, m.laje);
        corpo.Caixa(xe - 1, altura + 2, zt, 1, 2, P, m.laje);
        corpo.Caixa(xd, altura + 2, zt, 1, 2, P, m.laje);
        Fixo(raiz.transform, "Corpo", corpo);

        AdicionarColisor(raiz, xe, 0, zt, xd, altura, zf);

        // Regiões da fachada (camada z = 20) que o desgaste não pode cobrir
        var res = new HashSet<Vector3Int>();
        Reservar(res, -24, 0, zf, 48, 22, 1);
        for (int n = 1; n < pisos; n++)
        {
            Reservar(res, xe, 24 * n - 2, zf, L, 2, 1);
            Reservar(res, -24, 24 * n + 2, zf, 12, 18, 1);
            Reservar(res, 12, 24 * n + 2, zf, 12, 18, 1);
            Reservar(res, -10, 24 * n, zf, 20, 20, 1);
        }

        ModeloDaPeca Paredes(string n, Material terreo, Material sup) => Variacao(raiz.transform, n, v =>
        {
            v.Caixa(xe, 0, zt, L, 24, P, terreo);
            v.Caixa(xe, 24, zt, L, altura - 24, P, sup);
        }, 1);

        Slot(modular, "Pintura",
            Paredes("Pintura_AmareloEConcreto", m.concretoSujo, m.amarelo),
            Paredes("Pintura_AzulEBranco", m.azulParede, m.moldura),
            Paredes("Pintura_Laranja", m.laranja, m.laranja),
            Paredes("Pintura_VerdeAgua", m.concretoSujo, m.verdeAgua),
            Paredes("Pintura_Vermelho", m.bloco, m.vermelhoDesbotado),
            Paredes("Pintura_Concreto", m.concretoSujo, m.concretoSujo));

        Slot(modular, "Terreo",
            Variacao(raiz.transform, "Terreo_Portaria", v =>
            {
                PortaFina(v, -5, 0, 10, 20, zf, m.moldura, m.portaTurquesa, m.ferro, null);
                JanelaFina(v, TipoJanela.Grade, -24, 8, 12, 12, zf, m);
                JanelaFina(v, TipoJanela.Grade, 12, 8, 12, 12, zf, m);
            }, 1),
            Variacao(raiz.transform, "Terreo_Garagem", v =>
            {
                v.Caixa(-23, 0, zf, 1, 20, 1, m.moldura);
                v.Caixa(-2, 0, zf, 1, 20, 1, m.moldura);
                v.Caixa(-23, 18, zf, 22, 2, 1, m.moldura);
                for (int y = 0; y < 18; y++) v.Caixa(-22, y, zf, 20, 1, 1, y % 2 == 0 ? m.zincoA : m.zincoB);
                PortaFina(v, 6, 0, 10, 20, zf, m.moldura, m.portaMadeira, null, null);
            }, 1),
            Variacao(raiz.transform, "Terreo_Loja", v =>
            {
                v.Caixa(-24, 2, zf, 23, 18, 1, m.moldura);
                v.Caixa(-22, 4, zf, 19, 14, 1, m.vidro);
                v.Caixa(-13, 4, zf, 1, 14, 1, m.moldura);
                PortaFina(v, 6, 0, 10, 20, zf, m.moldura, m.portaAzul, null, null);
                for (int k = 0; k < 5; k++)
                    for (int x = -24; x < 0; x++)
                        v.Celula(x, 20 - k, zf + 1 + k, ((x + 24) / 3) % 2 == 0 ? m.roupaVermelha : m.brancoToldo);
            }, 1));

        ModeloDaPeca Janelas(string n, TipoJanela tipo) => Variacao(raiz.transform, n, v =>
        {
            for (int andar = 1; andar < pisos; andar++)
            {
                JanelaFina(v, tipo, -24, 24 * andar + 8, 12, 12, zf, m);
                JanelaFina(v, tipo, 12, 24 * andar + 8, 12, 12, zf, m);
            }
        }, 1);

        Slot(modular, "Janelas",
            Janelas("Janelas_Caixilho", TipoJanela.Caixilho),
            Janelas("Janelas_Grade", TipoJanela.Grade),
            Janelas("Janelas_Persiana", TipoJanela.Persiana),
            Janelas("Janelas_Tapada", TipoJanela.Tapada));

        // Sacada: laje, porta de vidro e guarda-corpo, nos andares 1 a 4
        ModeloDaPeca Sacadas(string n, int tipo) => Variacao(raiz.transform, n, v =>
        {
            for (int andar = 1; andar < pisos; andar++)
            {
                int b = 24 * andar;

                if (tipo == 3)
                {
                    JanelaFina(v, TipoJanela.Caixilho, -6, b + 8, 12, 12, zf, m);
                    continue;
                }

                v.Caixa(-10, b, zf, 20, 2, 10, m.laje);
                PortaFina(v, -5, b + 2, 10, 18, zf, m.moldura, m.vidro, null, null);

                if (tipo == 0)
                {
                    v.Caixa(-10, b + 9, 28, 20, 1, 1, m.ferro);
                    v.Caixa(-10, b + 5, 28, 20, 1, 1, m.ferro);
                    v.Caixa(-10, b + 9, zf, 1, 1, 8, m.ferro);
                    v.Caixa(9, b + 9, zf, 1, 1, 8, m.ferro);
                    v.Caixa(-10, b + 5, zf, 1, 1, 8, m.ferro);
                    v.Caixa(9, b + 5, zf, 1, 1, 8, m.ferro);
                    foreach (int x in new[] { -10, -6, -2, 2, 6, 9 }) v.Caixa(x, b + 2, 28, 1, 7, 1, m.ferro);
                    v.Caixa(-10, b + 2, 24, 1, 7, 1, m.ferro);
                    v.Caixa(9, b + 2, 24, 1, 7, 1, m.ferro);
                }
                else if (tipo == 1)
                {
                    v.Caixa(-10, b + 2, 27, 20, 7, 2, m.bloco);
                    v.Caixa(-10, b + 2, zf, 2, 7, 7, m.bloco);
                    v.Caixa(8, b + 2, zf, 2, 7, 7, m.bloco);
                    v.Caixa(-10, b + 9, 27, 20, 1, 2, m.concretoSujo);
                    v.Caixa(-10, b + 9, zf, 2, 1, 7, m.concretoSujo);
                    v.Caixa(8, b + 9, zf, 2, 1, 7, m.concretoSujo);
                }
                else
                {
                    v.Caixa(-10, b + 2, 28, 20, 7, 1, m.vidro);
                    v.Caixa(-10, b + 2, zf, 1, 7, 8, m.vidro);
                    v.Caixa(9, b + 2, zf, 1, 7, 8, m.vidro);
                    v.Caixa(-10, b + 9, 28, 20, 1, 1, m.ferro);
                    v.Caixa(-10, b + 9, zf, 1, 1, 8, m.ferro);
                    v.Caixa(9, b + 9, zf, 1, 1, 8, m.ferro);
                }
            }
        }, 1);

        Slot(modular, "Sacadas",
            Sacadas("Sacadas_Ferro", 0),
            Sacadas("Sacadas_Bloco", 1),
            Sacadas("Sacadas_Vidro", 2),
            Sacadas("Sacadas_Nenhuma", 3));

        Slot(modular, "ArCondicionado",
            Variacao(raiz.transform, "Ar_Poucos", v =>
            {
                ArCondicionado(v, -23, 24 * 2 + 2, zf, m);
                ArCondicionado(v, 14, 24 * 3 + 2, zf, m);
                ArCondicionado(v, -23, 24 * 4 + 2, zf, m);
            }, 1),
            Variacao(raiz.transform, "Ar_Muitos", v =>
            {
                for (int andar = 1; andar < pisos; andar++)
                {
                    ArCondicionado(v, -23, 24 * andar + 2, zf, m);
                    ArCondicionado(v, 14, 24 * andar + 2, zf, m);
                }
            }, 1));

        int topo = altura + 2;   // y da superfície da laje do topo

        Slot(modular, "Cobertura",
            Variacao(raiz.transform, "Cobertura_CaixaDagua", v =>
            {
                foreach (int x in new[] { -21, -9 })
                    foreach (int z in new[] { -13, -1 })
                        v.Caixa(x, topo, z, 2, 6, 2, m.ferro);
                v.Caixa(-21, topo + 6, -13, 14, 2, 14, m.concreto);
                Disco(v, -14, -6, topo + 8, 12, 5, m.roupaAzul);
                Disco(v, -14, -6, topo + 20, 1, 4, m.brancoToldo);
            }, 1),
            Variacao(raiz.transform, "Cobertura_Antenas", v =>
            {
                v.Caixa(10, topo, -8, 1, 24, 1, m.antena);
                foreach (int dy in new[] { 22, 18, 14 })
                    v.Caixa(10 - dy / 4, topo + dy, -8, (dy / 4) * 2 + 1, 1, 1, m.antena);
                v.Caixa(-14, topo, -6, 2, 5, 2, m.ferro);
                Disco(v, -13, -5, topo + 5, 1, 4, m.brancoToldo);
            }, 1),
            Variacao(raiz.transform, "Cobertura_VaralEPlantas", v =>
            {
                v.Caixa(-27, topo, -4, 1, 16, 1, m.ferro);
                v.Caixa(26, topo, -4, 1, 16, 1, m.ferro);
                v.Caixa(-27, topo + 15, -4, 54, 1, 1, m.ferro);

                Material[] cores = { m.roupaAzul, m.roupaBranca, m.roupaAmarela, m.roupaVermelha };
                var r = new System.Random(31);
                for (int x = -24, i = 0; x < 22; x += 6, i++)
                {
                    int largura = 3 + r.Next(0, 3), alt = 5 + r.Next(0, 3);
                    v.Caixa(x, topo + 15 - alt, -4, largura, alt, 1, cores[i % cores.Length]);
                }

                for (int i = 0; i < 8; i++)
                    v.Caixa(r.Next(-26, 24), topo, r.Next(-16, -8), 2, 2 + r.Next(0, 3), 2, r.NextDouble() < 0.5 ? m.folha : m.folhaEscura);
            }, 1));

        Slot(modular, "Desgaste",
            Variacao(raiz.transform, "Desgaste_Escorrido", v =>
            {
                for (int n = 1; n < pisos; n++) Escorrido(v, xe + 1, xd - 1, 24 * n - 3, zf, 100 + n, m.ferrugem, res);
            }, 1),
            Variacao(raiz.transform, "Desgaste_Mofo", v =>
            {
                for (int n = 1; n < pisos; n++) Escorrido(v, xe + 1, xd - 1, 24 * n - 3, zf, 120 + n, m.musgo, res);
                ManchaFina(v, xe, 0, 4, 20, zf, 131, 0.6f, m.musgo, res);
                ManchaFina(v, 24, 0, 4, 20, zf, 132, 0.6f, m.musgo, res);
            }, 1));

        Concluir(raiz, nome);
    }

    // ---------------------------------------------------------------- casa de laje
    // Um andar de laje de 5 m por 5 m, com a laje (y 22..23) avançando 1 voxel em volta, e a cobertura por cima.
    //   paredes: x -20..19, z -20..19, y 0..21     porta: x -17..-8      janela: x 2..15 (y 7..18)
    static void MontarCasaDeLaje(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();

        const int L = 40, P = 40, xe = -20, xd = 20, zt = -20, zf = 20, altura = 22;

        var laje = new Voxels(1);
        laje.Caixa(xe - 1, altura, zt - 1, L + 2, 2, P + 2, m.laje);
        Fixo(raiz.transform, "Laje", laje);

        AdicionarColisor(raiz, xe - 1, 0, zt - 1, xd + 1, altura + 2, zf + 1);

        var res = new HashSet<Vector3Int>();
        Reservar(res, -17, 0, zf, 10, 20, 1);
        Reservar(res, 2, 7, zf, 14, 12, 1);
        Reservar(res, xe - 1, altura, zf, L + 2, 2, 1);

        ModeloDaPeca Paredes(string n, Material cor) => Variacao(raiz.transform, n, v => v.Caixa(xe, 0, zt, L, altura, P, cor), 1);

        Slot(modular, "Pintura",
            Paredes("Pintura_BlocoAparente", m.bloco),
            Paredes("Pintura_Rebocada", m.concretoSujo),
            Paredes("Pintura_Amarela", m.amarelo),
            Paredes("Pintura_VerdeAgua", m.verdeAgua),
            Paredes("Pintura_Azul", m.azulParede));

        Slot(modular, "Porta",
            Variacao(raiz.transform, "Porta_Madeira", v => PortaFina(v, -17, 0, 10, 20, zf, m.moldura, m.portaMadeira, null, null), 1),
            Variacao(raiz.transform, "Porta_FerroComGrade", v => PortaFina(v, -17, 0, 10, 20, zf, m.moldura, m.portaTurquesa, m.ferro, null), 1),
            Variacao(raiz.transform, "Porta_BrancaRemendada", v => PortaFina(v, -17, 0, 10, 20, zf, m.moldura, m.roupaBranca, null, m.bloco), 1));

        Slot(modular, "Janela",
            Variacao(raiz.transform, "Janela_Caixilho", v => JanelaFina(v, TipoJanela.Caixilho, 2, 7, 14, 12, zf, m), 1),
            Variacao(raiz.transform, "Janela_Grade", v => JanelaFina(v, TipoJanela.Grade, 2, 7, 14, 12, zf, m), 1),
            Variacao(raiz.transform, "Janela_Persiana", v => JanelaFina(v, TipoJanela.Persiana, 2, 7, 14, 12, zf, m), 1),
            Variacao(raiz.transform, "Janela_Tapada", v => JanelaFina(v, TipoJanela.Tapada, 2, 7, 14, 12, zf, m), 1));

        // Entrada: degrau de concreto, ou toldo de zinco sobre a porta (as duas ficam fora da fachada, z >= 21)
        Slot(modular, "Entrada",
            Variacao(raiz.transform, "Entrada_Degrau", v =>
            {
                v.Caixa(-18, 0, zf + 1, 12, 1, 3, m.concreto);
            }, 1),
            Variacao(raiz.transform, "Entrada_Toldo", v =>
            {
                for (int x = -19; x < -5; x++)
                    v.Caixa(x, 21, zf + 1, 1, 1, 4, ((x + 19) / 2) % 2 == 0 ? m.zincoA : m.zincoB);
                v.Caixa(-19, 0, zf + 1, 14, 1, 3, m.concreto);
            }, 1));

        int topo = altura + 2;

        void Platibanda(Voxels v)
        {
            v.Caixa(xe - 1, topo, zt - 1, L + 2, 1, 1, m.laje);
            v.Caixa(xe - 1, topo, zf, L + 2, 1, 1, m.laje);
            v.Caixa(xe - 1, topo, zt, 1, 1, P, m.laje);
            v.Caixa(xd, topo, zt, 1, 1, P, m.laje);
        }

        Slot(modular, "Cobertura",
            Variacao(raiz.transform, "Cobertura_CaixaDagua", v =>
            {
                Platibanda(v);
                v.Caixa(3, topo, -14, 9, 4, 9, m.bloco);
                Disco(v, 7, -10, topo + 4, 12, 4, m.roupaAzul);
                Disco(v, 7, -10, topo + 16, 1, 3, m.brancoToldo);
                v.Caixa(-8, topo, -10, 6, 3, 4, m.tijolo);
            }, 1),
            Variacao(raiz.transform, "Cobertura_PilaresDeVergalhao", v =>
            {
                Platibanda(v);
                foreach (int x in new[] { -20, 17 })
                    foreach (int z in new[] { -20, 17 })
                    {
                        v.Caixa(x, topo, z, 3, 22, 3, m.concreto);
                        v.Caixa(x + 1, topo + 22, z + 1, 1, 8, 1, m.ferrugem);
                    }
                v.Caixa(-17, topo + 20, -20, 34, 2, 3, m.concreto);
                v.Caixa(-17, topo + 20, 17, 34, 2, 3, m.concreto);
                v.Caixa(-20, topo + 20, -17, 3, 2, 34, m.concreto);
                v.Caixa(17, topo + 20, -17, 3, 2, 34, m.concreto);
                v.Caixa(-6, topo, -4, 6, 3, 4, m.tijolo);
                v.Caixa(2, topo, 2, 4, 2, 3, m.bloco);
            }, 1),
            Variacao(raiz.transform, "Cobertura_TelhaDuasAguas", v =>
            {
                for (int k = 0; -23 + 2 * k <= 22 - 2 * k; k++)
                {
                    int zMin = -23 + 2 * k, zMax = 22 - 2 * k;
                    bool cumeeira = zMax - zMin < 3;
                    v.Caixa(-23, topo + k, zMin, 46, 1, zMax - zMin + 1, cumeeira ? m.cumeeira : (k % 2 == 0 ? m.telhaA : m.telhaB));
                }
            }, 1));

        Slot(modular, "Desgaste",
            Variacao(raiz.transform, "Desgaste_RebocoCaido", v =>
            {
                ManchaFina(v, xe + 1, 0, 38, 8, zf, 201, 0.5f, m.tijolo, res);
                ManchaFina(v, xe + 1, 10, 30, 8, zf, 202, 0.3f, m.bloco, res);
            }, 1),
            Variacao(raiz.transform, "Desgaste_Ferrugem", v => Escorrido(v, xe + 1, xd - 1, 21, zf, 203, m.ferrugem, res), 1));

        Concluir(raiz, nome);
    }

    // ---------------------------------------------------------------- bloco de varandas
    // Três andares (térreo + 2) com uma galeria corrida na frente dos dois de cima. 9 m de frente, 4 m de fundo.
    //   paredes: x -36..35, z -16..15, y 0..69      galerias: laje x -37..36, z 16..27, y 22..23 e 46..47
    //   (a superfície da galeria do andar 1 fica em y = 24, igual ao topo da EscadaMetalica)
    //   quatro unidades por andar (x0 = -36, -18, 0, 18): porta x0+1..x0+7, janela x0+9..x0+17
    //   O guarda-corpo do andar 1 tem uma passagem de 12 voxels (x -6..5) pra escada.
    static void MontarBlocoDeVarandas(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();

        const int L = 72, P = 32, xe = -36, xd = 36, zt = -16, zf = 16, altura = 70;

        var corpo = new Voxels(1);
        corpo.Caixa(xe - 1, 22, zf, L + 2, 2, 12, m.laje);
        corpo.Caixa(xe - 1, 46, zf, L + 2, 2, 12, m.laje);
        corpo.Caixa(xe - 1, altura, zt - 1, L + 2, 2, P + 2, m.laje);
        corpo.Caixa(xe - 1, altura + 2, zt - 1, L + 2, 2, 1, m.laje);
        corpo.Caixa(xe - 1, altura + 2, zf, L + 2, 2, 1, m.laje);
        corpo.Caixa(xe - 1, altura + 2, zt, 1, 2, P, m.laje);
        corpo.Caixa(xd, altura + 2, zt, 1, 2, P, m.laje);
        Fixo(raiz.transform, "Corpo", corpo);

        AdicionarColisor(raiz, xe, 0, zt, xd, altura, zf);
        AdicionarColisor(raiz, xe - 1, 22, zf, xd + 1, 24, 28);
        AdicionarColisor(raiz, xe - 1, 46, zf, xd + 1, 48, 28);

        var res = new HashSet<Vector3Int>();
        Reservar(res, xe - 1, 22, zf, L + 2, 2, 1);
        Reservar(res, xe - 1, 46, zf, L + 2, 2, 1);
        for (int andar = 0; andar < 3; andar++)
            for (int u = 0; u < 4; u++)
            {
                int x0 = xe + 18 * u, b = 24 * andar;
                Reservar(res, x0 + 1, b, zf, 7, 20, 1);
                Reservar(res, x0 + 9, b, zf, 8, 19, 1);
            }

        ModeloDaPeca Paredes(string n, Material a, Material b, Material c) => Variacao(raiz.transform, n, v =>
        {
            v.Caixa(xe, 0, zt, L, 24, P, a);
            v.Caixa(xe, 24, zt, L, 24, P, b);
            v.Caixa(xe, 48, zt, L, altura - 48, P, c);
        }, 1);

        Slot(modular, "Pintura",
            Paredes("Pintura_LaranjaVerdeAmarelo", m.laranja, m.verdeAgua, m.amarelo),
            Paredes("Pintura_AzulConcretoAzul", m.azulParede, m.concretoSujo, m.azulParede),
            Paredes("Pintura_TurquesaLaranja", m.turquesa, m.laranja, m.turquesa),
            Paredes("Pintura_Vermelho", m.vermelhoDesbotado, m.vermelhoDesbotado, m.vermelhoDesbotado),
            Paredes("Pintura_Concreto", m.concretoSujo, m.concretoSujo, m.concretoSujo));

        ModeloDaPeca Portas(string n, Material folha, Material grade) => Variacao(raiz.transform, n, v =>
        {
            for (int andar = 0; andar < 3; andar++)
                for (int u = 0; u < 4; u++)
                    PortaFina(v, xe + 18 * u + 1, 24 * andar, 7, 20, zf, m.moldura, folha, grade, null);
        }, 1);

        Slot(modular, "Portas",
            Portas("Portas_Madeira", m.portaMadeira, null),
            Portas("Portas_FerroComGrade", m.portaTurquesa, m.ferro),
            Portas("Portas_Azul", m.portaAzul, null));

        ModeloDaPeca Janelas(string n, TipoJanela tipo) => Variacao(raiz.transform, n, v =>
        {
            for (int andar = 0; andar < 3; andar++)
                for (int u = 0; u < 4; u++)
                    JanelaFina(v, tipo, xe + 18 * u + 9, 24 * andar + 7, 8, 12, zf, m);
        }, 1);

        Slot(modular, "Janelas",
            Janelas("Janelas_Caixilho", TipoJanela.Caixilho),
            Janelas("Janelas_Grade", TipoJanela.Grade),
            Janelas("Janelas_Persiana", TipoJanela.Persiana),
            Janelas("Janelas_Tapada", TipoJanela.Tapada));

        // Guarda-corpo das galerias. Trechos: com passagem no andar 1, inteiro no andar 2.
        void Trechos(bool passagem, System.Action<int, int> fazer)
        {
            if (passagem) { fazer(xe - 1, -6); fazer(6, xd + 1); }
            else fazer(xe - 1, xd + 1);
        }

        ModeloDaPeca Galerias(string n, int tipo) => Variacao(raiz.transform, n, v =>
        {
            foreach (int b in new[] { 24, 48 })
            {
                bool passagem = b == 24;

                if (tipo == 0)
                {
                    Trechos(passagem, (a, e) =>
                    {
                        v.Caixa(a, b + 7, 27, e - a, 1, 1, m.ferro);
                        v.Caixa(a, b + 3, 27, e - a, 1, 1, m.ferro);
                        for (int x = a; x < e; x += 6) v.Caixa(x, b, 27, 1, 7, 1, m.ferro);
                        v.Caixa(e - 1, b, 27, 1, 7, 1, m.ferro);
                    });
                    foreach (int x in new[] { xe - 1, xd })
                    {
                        v.Caixa(x, b + 7, zf, 1, 1, 11, m.ferro);
                        v.Caixa(x, b + 3, zf, 1, 1, 11, m.ferro);
                        for (int z = zf; z < 27; z += 5) v.Caixa(x, b, z, 1, 7, 1, m.ferro);
                    }
                }
                else if (tipo == 1)
                {
                    Trechos(passagem, (a, e) =>
                    {
                        v.Caixa(a, b, 26, e - a, 7, 2, m.bloco);
                        v.Caixa(a, b + 7, 26, e - a, 1, 2, m.concretoSujo);
                    });
                    foreach (int x in new[] { xe - 1, xd - 1 })
                    {
                        v.Caixa(x, b, zf, 2, 7, 10, m.bloco);
                        v.Caixa(x, b + 7, zf, 2, 1, 10, m.concretoSujo);
                    }
                }
                else
                {
                    Trechos(passagem, (a, e) =>
                    {
                        for (int x = a; x < e; x += 8) v.Caixa(x, b, 26, 2, 8, 2, m.madeiraEscura);
                        v.Caixa(e - 2, b, 26, 2, 8, 2, m.madeiraEscura);
                        v.Caixa(a, b + 7, 26, e - a, 1, 2, m.madeira);
                        v.Caixa(a, b + 3, 26, e - a, 1, 2, m.madeira);
                    });
                    foreach (int x in new[] { xe - 1, xd - 1 })
                    {
                        for (int z = zf; z < 26; z += 8) v.Caixa(x, b, z, 2, 8, 2, m.madeiraEscura);
                        v.Caixa(x, b + 7, zf, 2, 1, 10, m.madeira);
                        v.Caixa(x, b + 3, zf, 2, 1, 10, m.madeira);
                    }
                }
            }
        }, 1);

        Slot(modular, "Galerias",
            Galerias("Galerias_Ferro", 0),
            Galerias("Galerias_Bloco", 1),
            Galerias("Galerias_Madeira", 2));

        // Extras na galeria: aparelhos de ar-condicionado (encostados na parede, entre a porta e a janela) ou roupas no varal
        Slot(modular, "Extras",
            Variacao(raiz.transform, "Extras_ArCondicionado", v =>
            {
                foreach (int b in new[] { 24, 48 })
                    foreach (int u in new[] { 0, 2 })
                        ArCondicionado(v, xe + 18 * u + 9, b, zf + 1, m);
            }, 1),
            Variacao(raiz.transform, "Extras_Varal", v =>
            {
                Material[] cores = { m.roupaAzul, m.roupaBranca, m.roupaAmarela, m.roupaVermelha };
                var r = new System.Random(77);
                foreach (int b in new[] { 24, 48 })
                {
                    v.Caixa(-34, b, 22, 1, 15, 1, m.ferro);
                    v.Caixa(33, b, 22, 1, 15, 1, m.ferro);
                    v.Caixa(-34, b + 14, 22, 68, 1, 1, m.ferro);
                    for (int x = -31, i = 0; x < 30; x += 7, i++)
                    {
                        int largura = 3 + r.Next(0, 3), alt = 5 + r.Next(0, 3);
                        v.Caixa(x, b + 14 - alt, 22, largura, alt, 1, cores[(i + b) % cores.Length]);
                    }
                }
            }, 1));

        int topo = altura + 2;

        Slot(modular, "Cobertura",
            Variacao(raiz.transform, "Cobertura_CaixasDagua", v =>
            {
                foreach (int cx in new[] { -20, 16 })
                {
                    v.Caixa(cx - 4, topo, -9, 9, 4, 9, m.bloco);
                    Disco(v, cx, -4, topo + 4, 10, 4, m.roupaAzul);
                    Disco(v, cx, -4, topo + 14, 1, 3, m.brancoToldo);
                }
            }, 1),
            Variacao(raiz.transform, "Cobertura_Antenas", v =>
            {
                foreach (int x in new[] { -24, 6, 28 })
                {
                    v.Caixa(x, topo, -6, 1, 24, 1, m.antena);
                    foreach (int dy in new[] { 22, 18, 14 })
                        v.Caixa(x - dy / 4, topo + dy, -6, (dy / 4) * 2 + 1, 1, 1, m.antena);
                }
                v.Caixa(-8, topo, -8, 2, 5, 2, m.ferro);
                Disco(v, -7, -7, topo + 5, 1, 4, m.brancoToldo);
            }, 1),
            Variacao(raiz.transform, "Cobertura_LajeComPlantas", v =>
            {
                var r = new System.Random(55);
                for (int i = 0; i < 10; i++)
                    v.Caixa(r.Next(-34, 32), topo, r.Next(-14, 12), 2, 2 + r.Next(0, 3), 2, r.NextDouble() < 0.5 ? m.folha : m.folhaEscura);
            }, 1));

        Slot(modular, "Desgaste",
            Variacao(raiz.transform, "Desgaste_Escorrido", v =>
            {
                Escorrido(v, xe + 1, xd - 1, 21, zf, 301, m.ferrugem, res);
                Escorrido(v, xe + 1, xd - 1, 45, zf, 302, m.ferrugem, res);
                Escorrido(v, xe + 1, xd - 1, 69, zf, 303, m.ferrugem, res);
            }, 1),
            Variacao(raiz.transform, "Desgaste_Mofo", v =>
            {
                Escorrido(v, xe + 1, xd - 1, 21, zf, 311, m.musgo, res);
                Escorrido(v, xe + 1, xd - 1, 45, zf, 312, m.musgo, res);
                ManchaFina(v, xe, 0, 72, 6, zf, 313, 0.35f, m.musgo, res);
            }, 1));

        Concluir(raiz, nome);
    }
}
