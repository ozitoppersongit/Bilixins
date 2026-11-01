using UnityEngine;

// Parte do GeradorDeCasasEditor: props de quintal e de rua (voxel fino de 0,125 m, frente = +Z, origem no meio da base).
// Mesa, roda, engradados e tambor ficam no fim do arquivo.
//
// ESCALA: o Player tem 2 m de altura, então o mundo é cerca de 10% maior que o real. Objetos de uso: mesa a 0,9 m,
// cadeira de 0,5 m de assento, bujão de 0,55 m, tambor de 0,9 m, pneu de 0,65 m, balde de 0,4 m.
public static partial class GeradorDeCasasEditor
{
    // ---------------------------------------------------------------- cadeira de plástico (+ mesinha)
    static void MontarCadeira(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();
        AdicionarColisor(raiz, -2, 0, -2, 2, 8, 2);

        ModeloDaPeca Cadeira(string n, Material cor) => Variacao(raiz.transform, n, v => CadeiraVoxel(v, 0, cor), 1);

        Slot(modular, "Cadeira",
            Cadeira("Cadeira_Branca", m.roupaBranca),
            Cadeira("Cadeira_Azul", m.roupaAzul),
            Cadeira("Cadeira_Vermelha", m.roupaVermelha),
            Cadeira("Cadeira_Amarela", m.roupaAmarela));

        // Companhia: uma mesinha redonda ao lado, ou uma segunda cadeira
        Slot(modular, "Companhia",
            Variacao(raiz.transform, "Companhia_Mesinha", v =>
            {
                Disco(v, 6, 0, 4, 1, 2, m.roupaBranca);
                v.Caixa(6, 1, 0, 1, 3, 1, m.metal);
                Disco(v, 6, 0, 0, 1, 1, m.metal);
            }, 1),
            Variacao(raiz.transform, "Companhia_OutraCadeira", v => CadeiraVoxel(v, 5, m.roupaBranca), 1));

        Concluir(raiz, nome);
    }

    // Cadeira de 0,5 m x 0,5 m, assento a 0,5 m e encosto até 1 m. dx desloca em X.
    static void CadeiraVoxel(Voxels v, int dx, Material cor)
    {
        v.Caixa(dx - 2, 4, -2, 4, 1, 4, cor);
        foreach (int x in new[] { dx - 2, dx + 1 })
            foreach (int z in new[] { -2, 1 })
                v.Caixa(x, 0, z, 1, 4, 1, cor);
        v.Caixa(dx - 2, 5, -2, 1, 3, 1, cor);
        v.Caixa(dx + 1, 5, -2, 1, 3, 1, cor);
        v.Caixa(dx - 1, 7, -2, 2, 1, 1, cor);
    }

    // ---------------------------------------------------------------- churrasqueira (1,5 m x 1 m, 1 m de altura)
    static void MontarChurrasqueira(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();
        AdicionarColisor(raiz, -6, 0, -4, 6, 8, 4);

        ModeloDaPeca Corpo(string n, Material base_, Material borda) => Variacao(raiz.transform, n, v =>
        {
            v.Caixa(-6, 0, -4, 12, 6, 8, base_);
            v.Caixa(-6, 6, -4, 12, 2, 2, borda);
            v.Caixa(-6, 6, 2, 12, 2, 2, borda);
            v.Caixa(-6, 6, -2, 2, 2, 4, borda);
            v.Caixa(4, 6, -2, 2, 2, 4, borda);
            v.Caixa(-4, 6, -2, 8, 1, 4, m.terra);
            var r = new System.Random(5);
            for (int x = -4; x < 4; x++)
                for (int z = -2; z < 2; z++)
                    if (r.NextDouble() < 0.25) v.Celula(x, 6, z, r.NextDouble() < 0.5 ? m.laranja : m.roupaVermelha);
        }, 1);

        Slot(modular, "Corpo",
            Corpo("Corpo_Bloco", m.bloco, m.concretoSujo),
            Corpo("Corpo_Tijolo", m.tijolo, m.ferrugem),
            Corpo("Corpo_Rebocado", m.amarelo, m.concretoSujo));

        Slot(modular, "Topo",
            Variacao(raiz.transform, "Topo_Grelha", v =>
            {
                for (int x = -5; x <= 5; x += 2) v.Caixa(x, 8, -3, 1, 1, 6, m.ferro);
                v.Caixa(-5, 8, -3, 11, 1, 1, m.ferro);
                v.Caixa(-5, 8, 2, 11, 1, 1, m.ferro);
            }, 1),
            Variacao(raiz.transform, "Topo_Espetos", v =>
            {
                foreach (int x in new[] { -3, 0, 3 })
                {
                    v.Caixa(x, 8, -3, 1, 1, 6, m.ferro);
                    v.Caixa(x, 9, -2, 1, 1, 4, x == 0 ? m.ferrugem : m.tijolo);
                }
            }, 1),
            Variacao(raiz.transform, "Topo_Chamine", v =>
            {
                for (int x = -5; x <= 5; x += 2) v.Caixa(x, 8, -3, 1, 1, 4, m.ferro);
                v.Caixa(-3, 8, 1, 6, 12, 2, m.tijolo);
                v.Caixa(-4, 20, 1, 8, 1, 3, m.concretoSujo);
            }, 1));

        Slot(modular, "Extras",
            Variacao(raiz.transform, "Extras_SacoDeCarvao", v =>
            {
                v.Caixa(8, 0, -1, 4, 5, 3, m.ferro);
                v.Caixa(9, 2, 2, 2, 2, 1, m.brancoToldo);
            }, 1),
            Variacao(raiz.transform, "Extras_Engradado", v => Engradado(v, 8, 0, -2, m.madeira, m.madeiraEscura), 1));

        Concluir(raiz, nome);
    }

    // ---------------------------------------------------------------- tanque de lavar roupa (1,5 m x 0,6 m, 1,1 m de altura)
    static void MontarTanqueDeLavar(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();
        AdicionarColisor(raiz, -6, 0, -2, 6, 10, 3);

        ModeloDaPeca Tanque(string n, Material cor) => Variacao(raiz.transform, n, v =>
        {
            foreach (int x in new[] { -6, 4 })
                foreach (int z in new[] { -2, 1 })
                    v.Caixa(x, 0, z, 2, 4, 2, cor);
            v.Caixa(-6, 4, -2, 12, 1, 5, cor);

            v.Caixa(-6, 5, -2, 6, 4, 1, cor);           // tanque: paredes
            v.Caixa(-6, 5, 2, 6, 4, 1, cor);
            v.Caixa(-6, 5, -1, 1, 4, 3, cor);
            v.Caixa(-1, 5, -1, 1, 4, 3, cor);
            v.Caixa(-5, 5, -1, 4, 2, 3, m.vidro);       // água

            v.Caixa(0, 5, -2, 6, 3, 5, cor);            // tábua de lavar, com as ranhuras
            v.Caixa(0, 8, 2, 6, 2, 1, cor);
            for (int z = -2; z < 2; z += 2) v.Caixa(1, 8, z, 4, 1, 1, m.concretoSujo);

            v.Caixa(-1, 9, 2, 1, 2, 1, m.calha);        // torneira
            v.Caixa(-1, 10, 0, 1, 1, 3, m.calha);
        }, 1);

        Slot(modular, "Pintura",
            Tanque("Pintura_Concreto", m.concreto),
            Tanque("Pintura_Azul", m.azulParede),
            Tanque("Pintura_VerdeAgua", m.verdeAgua),
            Tanque("Pintura_Branca", m.moldura));

        Slot(modular, "Extras",
            Variacao(raiz.transform, "Extras_Roupa", v =>
            {
                v.Caixa(1, 9, -2, 4, 2, 3, m.roupaAzul);
                v.Caixa(2, 11, -1, 2, 1, 2, m.roupaBranca);
            }, 1),
            Variacao(raiz.transform, "Extras_Sabao", v =>
            {
                v.Caixa(2, 9, -1, 2, 1, 1, m.amarelo);
                v.Caixa(4, 9, 0, 2, 1, 1, m.moldura);
            }, 1),
            Variacao(raiz.transform, "Extras_Balde", v => Balde(v, m.baldeCinza, m.metal, 9, 0), 1));

        Concluir(raiz, nome);
    }

    // Balde pequeno (0,4 m) numa posição qualquer
    static void Balde(Voxels v, Material corpo, Material alca, int dx, int dz)
    {
        v.Caixa(dx - 1, 0, dz, 3, 3, 1, corpo);
        v.Caixa(dx, 0, dz - 1, 1, 3, 3, corpo);
        v.Caixa(dx - 1, 3, dz, 1, 1, 1, alca);
        v.Caixa(dx + 1, 3, dz, 1, 1, 1, alca);
        v.Caixa(dx - 1, 4, dz, 3, 1, 1, alca);
    }

    // ---------------------------------------------------------------- casinha de cachorro (1 m x 1,2 m, 1,1 m de altura)
    static void MontarCasinhaDeCachorro(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();
        AdicionarColisor(raiz, -4, 0, -4, 4, 6, 4);

        var porta = new Voxels(1);
        porta.Caixa(-2, 0, 4, 4, 5, 1, m.madeiraEscura);
        porta.Caixa(-1, 0, 4, 2, 4, 1, m.ferro);
        Fixo(raiz.transform, "Porta", porta);

        ModeloDaPeca Corpo(string n, Material cor) => Variacao(raiz.transform, n, v => v.Caixa(-4, 0, -4, 8, 6, 8, cor), 1);

        Slot(modular, "Pintura",
            Corpo("Pintura_Madeira", m.madeira),
            Corpo("Pintura_Azul", m.azulParede),
            Corpo("Pintura_Vermelha", m.vermelhoDesbotado),
            Corpo("Pintura_Verde", m.verdeAgua));

        Slot(modular, "Telhado",
            Variacao(raiz.transform, "Telhado_Telha", v =>
            {
                for (int k = 0; k < 4; k++)
                    v.Caixa(-5 + k, 6 + k, -5, 10 - 2 * k, 1, 10, k % 2 == 0 ? m.telhaA : m.telhaB);
            }, 1),
            Variacao(raiz.transform, "Telhado_Zinco", v =>
            {
                for (int k = 0; k < 4; k++)
                    v.Caixa(-5 + k, 6 + k, -5, 10 - 2 * k, 1, 10, k % 2 == 0 ? m.zincoA : m.zincoB);
            }, 1));

        ModeloDaPeca Cachorro(string n, Material corpo, Material detalhe) => Variacao(raiz.transform, n, v =>
        {
            const int x = 6, z = 0;
            v.Caixa(x, 2, z, 5, 3, 2, corpo);
            foreach (int dx in new[] { 0, 4 })
                foreach (int dz in new[] { 0, 1 })
                    v.Caixa(x + dx, 0, z + dz, 1, 2, 1, corpo);
            v.Caixa(x + 5, 3, z, 2, 3, 2, corpo);
            v.Caixa(x + 7, 3, z, 1, 2, 2, detalhe);
            v.Caixa(x + 5, 6, z, 1, 1, 1, detalhe);
            v.Caixa(x + 5, 6, z + 1, 1, 1, 1, detalhe);
            v.Caixa(x - 1, 4, z, 1, 2, 1, corpo);
            v.Caixa(x + 9, 0, z, 2, 1, 2, m.baldeCinza);   // tigela
        }, 1);

        Slot(modular, "Cachorro",
            Cachorro("Cachorro_Caramelo", m.amarelo, m.madeira),
            Cachorro("Cachorro_Preto", m.ferro, m.metal),
            Cachorro("Cachorro_Branco", m.moldura, m.concretoSujo));

        Concluir(raiz, nome);
    }

    // ---------------------------------------------------------------- carrinho de mão (1,5 m de comprimento, 0,8 m de largura)
    static void MontarCarrinhoDeMao(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();
        AdicionarColisor(raiz, -9, 0, -3, 6, 6, 3);

        ModeloDaPeca Corpo(string n, Material cor) => Variacao(raiz.transform, n, v =>
        {
            v.Caixa(-4, 3, -3, 8, 1, 6, cor);                        // fundo da caçamba
            v.Caixa(-4, 4, -3, 8, 2, 1, cor);                        // laterais
            v.Caixa(-4, 4, 2, 8, 2, 1, cor);
            v.Caixa(-4, 4, -2, 1, 2, 4, cor);                        // fundo e frente
            v.Caixa(3, 4, -2, 1, 2, 4, cor);
            v.Caixa(-9, 4, -2, 5, 1, 1, m.madeira);                  // cabos
            v.Caixa(-9, 4, 1, 5, 1, 1, m.madeira);
            v.Caixa(-6, 0, -2, 1, 4, 1, m.madeiraEscura);            // pés
            v.Caixa(-6, 0, 1, 1, 4, 1, m.madeiraEscura);
            v.Caixa(3, 2, -3, 2, 1, 1, m.ferro);                     // garfo da roda
            v.Caixa(3, 2, 2, 2, 1, 1, m.ferro);
            for (int y = 0; y < 5; y++)
                for (int z = -2; z <= 2; z++)
                    if ((y - 2) * (y - 2) + z * z <= 5) v.Caixa(4, y, z, 1, 1, 1, y == 2 && z == 0 ? m.metal : m.ferro);
        }, 1);

        Slot(modular, "Cor",
            Corpo("Cor_Azul", m.roupaAzul),
            Corpo("Cor_Vermelha", m.roupaVermelha),
            Corpo("Cor_Ferrugem", m.ferrugem));

        Slot(modular, "Carga",
            Variacao(raiz.transform, "Carga_Terra", v =>
            {
                v.Caixa(-3, 4, -2, 6, 1, 4, m.terra);
                v.Caixa(-2, 5, -1, 4, 1, 2, m.terra);
            }, 1),
            Variacao(raiz.transform, "Carga_Tijolos", v =>
            {
                v.Caixa(-3, 4, -2, 6, 1, 4, m.tijolo);
                v.Caixa(-2, 5, -1, 4, 1, 2, m.ferrugem);
            }, 1),
            Variacao(raiz.transform, "Carga_Folhas", v =>
            {
                v.Caixa(-3, 4, -2, 6, 1, 4, m.folha);
                v.Caixa(-2, 5, -1, 4, 2, 2, m.folhaEscura);
            }, 1));

        Concluir(raiz, nome);
    }

    // ---------------------------------------------------------------- material de obra (pallet de 1,5 m x 1 m)
    static void MontarMaterialDeObra(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();
        AdicionarColisor(raiz, 0, 0, 0, 12, 7, 8);

        Slot(modular, "Pilha",
            Variacao(raiz.transform, "Pilha_Tijolos", v =>
            {
                v.Caixa(0, 0, 0, 12, 1, 8, m.madeira);
                var r = new System.Random(9);
                for (int y = 1; y <= 5; y++)
                    for (int x = 0; x < 12; x += 2)
                        for (int z = 0; z < 8; z += 2)
                            if (y < 5 || r.NextDouble() < 0.5)
                                v.Caixa(x, y, z, 2, 1, 2, (y + x / 2 + z / 2) % 2 == 0 ? m.tijolo : m.ferrugem);
            }, 1),
            Variacao(raiz.transform, "Pilha_Blocos", v =>
            {
                v.Caixa(0, 0, 0, 12, 1, 8, m.madeira);
                for (int y = 1; y <= 4; y++)
                    for (int x = 0; x < 12; x += 3)
                        for (int z = 0; z < 8; z += 2)
                            v.Caixa(x, y, z, 3, 1, 2, (y + x / 3 + z / 2) % 2 == 0 ? m.bloco : m.concretoSujo);
            }, 1),
            Variacao(raiz.transform, "Pilha_Areia", v =>
            {
                for (int k = 0; k < 4; k++) v.Caixa(k, k, k, 12 - 2 * k, 1, 8 - 2 * k, m.capimSeco);
            }, 1),
            Variacao(raiz.transform, "Pilha_SacosDeCimento", v =>
            {
                v.Caixa(0, 0, 0, 12, 1, 8, m.madeira);
                for (int i = 0; i < 3; i++)
                    foreach (int x in new[] { 0, 6 })
                        foreach (int z in new[] { 0, 4 })
                        {
                            v.Caixa(x, 1 + 2 * i, z, 6, 2, 4, m.concreto);
                            if (z == 0) v.Caixa(x + 2, 1 + 2 * i, -1, 2, 1, 1, m.brancoToldo);
                        }
            }, 1));

        Concluir(raiz, nome);
    }

    // ---------------------------------------------------------------- pneus (0,65 m de diâmetro, 0,25 m cada)
    static void MontarPneus(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();
        AdicionarColisor(raiz, -3, 0, -3, 3, 6, 3);

        ModeloDaPeca Pilha(string n, Material cor) => Variacao(raiz.transform, n, v =>
        {
            for (int i = 0; i < 3; i++) Anel(v, 0, 0, i * 2, 2, 1.6f, 2.8f, cor);
        }, 1);

        ModeloDaPeca Planta(string n, Material cor) => Variacao(raiz.transform, n, v =>
        {
            Anel(v, 0, 0, 0, 2, 1.6f, 2.8f, cor);
            Anel(v, 0, 0, 2, 2, 1.6f, 2.8f, cor);
            Disco(v, 0, 0, 0, 3, 1, m.terra);
            v.Caixa(-1, 3, -1, 3, 2, 3, m.folha);
            v.Celula(0, 5, 0, m.florRosa);
        }, 1);

        Slot(modular, "Pneus",
            Pilha("Pneus_Pilha_Preta", m.ferro),
            Pilha("Pneus_Pilha_Branca", m.roupaBranca),
            Planta("Pneus_Planta_Branca", m.roupaBranca),
            Planta("Pneus_Planta_Azul", m.roupaAzul));

        Concluir(raiz, nome);
    }

    // ---------------------------------------------------------------- bujão de gás (0,4 m de diâmetro, 0,7 m com a válvula)
    static void MontarBujao(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();
        AdicionarColisor(raiz, -2, 0, -2, 7, 5, 2);

        void Bujao(Voxels v, int cx, Material cor)
        {
            v.Caixa(cx - 1, 0, 0, 3, 4, 1, cor);
            v.Caixa(cx, 0, -1, 1, 4, 3, cor);
            v.Caixa(cx - 1, 2, 0, 3, 1, 1, m.metal);
            v.Caixa(cx, 2, -1, 1, 1, 3, m.metal);
            v.Caixa(cx, 4, 0, 1, 1, 1, cor);
            v.Caixa(cx, 5, 0, 1, 2, 1, m.metal);
        }

        Slot(modular, "Bujoes",
            Variacao(raiz.transform, "Bujao_Azul", v => Bujao(v, 0, m.portaAzul), 1),
            Variacao(raiz.transform, "Bujao_Verde", v => Bujao(v, 0, m.folhaEscura), 1),
            Variacao(raiz.transform, "Bujao_Laranja", v => Bujao(v, 0, m.laranja), 1),
            Variacao(raiz.transform, "Bujao_DoisAzuis", v => { Bujao(v, 0, m.portaAzul); Bujao(v, 5, m.portaAzul); }, 1),
            Variacao(raiz.transform, "Bujao_AzulEVerde", v => { Bujao(v, 0, m.portaAzul); Bujao(v, 5, m.folhaEscura); }, 1));

        Concluir(raiz, nome);
    }

    // ---------------------------------------------------------------- poste de rua
    static void MontarPoste(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();
        AdicionarColisor(raiz, -1, 0, -1, 2, 48, 2);

        ModeloDaPeca Poste(string n, Material cor, Material braco) => Variacao(raiz.transform, n, v =>
        {
            v.Caixa(-1, 0, -1, 3, 48, 3, cor);
            v.Caixa(-8, 41, -1, 17, 2, 2, braco);                         // cruzeta
            foreach (int x in new[] { -7, -3, 3, 7 }) v.Caixa(x, 43, 0, 1, 2, 1, m.calha);   // isoladores
        }, 1);

        Slot(modular, "Poste",
            Poste("Poste_Concreto", m.concreto, m.madeiraEscura),
            Poste("Poste_Madeira", m.madeiraEscura, m.madeira));

        Slot(modular, "Equipamento",
            Variacao(raiz.transform, "Equip_Transformador", v =>
            {
                v.Caixa(-3, 30, 2, 7, 9, 4, m.calha);
                v.Caixa(-2, 39, 3, 5, 1, 2, m.metal);
                v.Caixa(-1, 36, 6, 3, 2, 1, m.ferro);
            }, 1),
            Variacao(raiz.transform, "Equip_Luminaria", v =>
            {
                v.Caixa(2, 46, 0, 9, 1, 1, m.ferro);
                v.Caixa(9, 45, -1, 5, 1, 3, m.ferro);
                v.Caixa(10, 44, 0, 3, 1, 1, m.roupaAmarela);
            }, 1),
            Variacao(raiz.transform, "Equip_FiosEmaranhados", v =>
            {
                var r = new System.Random(3);
                for (int i = 0; i < 7; i++)
                {
                    int x = r.Next(-7, 8), comp = 3 + r.Next(0, 7);
                    v.Caixa(x, 41 - comp, r.Next(-2, 3) == 0 ? 2 : -2, 1, comp, 1, m.ferro);
                }
                v.Caixa(-6, 33, 2, 12, 1, 1, m.ferro);
            }, 1));

        Concluir(raiz, nome);
    }

    // Mesa de piquenique de 2 m x 1 m, tampo a 0,9 m, com bancos de 0,5 m. Origem no meio da base.
    //   tampo y 5..6 (superfície em y = 7), bancos z -8..-6 e 5..7 (assento em y = 4). Itens: a partir de y = 7.
    static void MontarMesa(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();
        AdicionarColisor(raiz, -8, 0, -4, 8, 7, 4);

        Slot(modular, "Mesa",
            Variacao(raiz.transform, "Mesa_Madeira", v => Mesa(v, m, null, null), 1),
            Variacao(raiz.transform, "Mesa_ToalhaXadrez", v => Mesa(v, m, m.roupaVermelha, m.roupaBranca), 1),
            Variacao(raiz.transform, "Mesa_ToalhaLisa", v => Mesa(v, m, m.roupaAzul, m.roupaAzul), 1));

        Slot(modular, "Bancos",
            Variacao(raiz.transform, "Bancos_DoisLados", v =>
            {
                Banco(v, m, -8);
                Banco(v, m, 5);
            }, 1),
            Variacao(raiz.transform, "Bancos_UmLado", v => Banco(v, m, 5), 1));

        Slot(modular, "Itens",
            Variacao(raiz.transform, "Itens_PratoECopo", v =>
            {
                Disco(v, -4, 0, 7, 1, 2, m.brancoToldo);
                v.Caixa(2, 7, -1, 2, 2, 2, m.metal);
                v.Caixa(2, 9, 0, 1, 1, 1, m.ferro);
            }, 1),
            Variacao(raiz.transform, "Itens_VasoComFlor", v =>
            {
                v.Caixa(-1, 7, -1, 3, 3, 3, m.portaAzul);
                v.Caixa(0, 10, 0, 1, 2, 1, m.folha);
                v.Celula(0, 12, 0, m.florRosa);
                v.Celula(-1, 11, 0, m.florAmarela);
                v.Celula(1, 11, 0, m.florVermelha);
            }, 1));

        Concluir(raiz, nome);
    }

    static void Mesa(Voxels v, Materiais m, Material toalhaA, Material toalhaB)
    {
        foreach (int x in new[] { -7, 5 })
            foreach (int z in new[] { -3, 1 })
                v.Caixa(x, 0, z, 2, 5, 2, m.madeiraEscura);

        v.Caixa(-8, 5, -4, 16, 2, 8, m.madeira);

        if (toalhaA == null) return;

        // Toalha: cobre o tampo (y = 6) e cai 3 voxels dos dois lados compridos, em xadrez de dois tons
        for (int x = -8; x < 8; x++)
        {
            for (int z = -4; z < 4; z++)
                v.Celula(x, 6, z, ((x + 8) / 2 + (z + 4) / 2) % 2 == 0 ? toalhaA : toalhaB);
            for (int y = 3; y <= 5; y++)
            {
                v.Celula(x, y, -5, ((x + 8) / 2) % 2 == 0 ? toalhaA : toalhaB);
                v.Celula(x, y, 4, ((x + 8) / 2) % 2 == 0 ? toalhaA : toalhaB);
            }
        }
    }

    static void Banco(Voxels v, Materiais m, int z0)
    {
        v.Caixa(-8, 3, z0, 16, 1, 3, m.madeira);
        foreach (int x in new[] { -7, 5 })
            v.Caixa(x, 0, z0 + 1, 2, 3, 1, m.madeiraEscura);
    }

    // Roda de bicicleta em pé (aro, raios e cubo), no plano XY: 0,9 m de diâmetro. Origem no meio da base.
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
        const int cy = 3;

        for (int y = -3; y <= 3; y++)
            for (int x = -3; x <= 3; x++)
            {
                float d = Mathf.Sqrt(x * x + y * y);
                if (d >= 2.2f && d <= 3.4f) v.Celula(x, cy + y, 0, aro);
            }

        for (int a = 0; a < 8; a++)
        {
            float ang = a * Mathf.PI / 4f;
            for (int r = 1; r <= 2; r++)
                v.Celula(Mathf.RoundToInt(Mathf.Cos(ang) * r), cy + Mathf.RoundToInt(Mathf.Sin(ang) * r), 0, raios);
        }

        v.Celula(0, cy, 0, m.metal);
    }

    // Pilhas de engradados de 0,5 m x 0,4 m: de madeira ou de plástico colorido
    static void MontarEngradados(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();
        AdicionarColisor(raiz, 0, 0, 0, 9, 6, 3);

        Slot(modular, "Pilha",
            Variacao(raiz.transform, "Pilha_Madeira_Um", v => Engradado(v, 0, 0, 0, m.madeira, m.madeiraEscura), 1),
            Variacao(raiz.transform, "Pilha_Madeira_Pilha", v =>
            {
                Engradado(v, 0, 0, 0, m.madeira, m.madeiraEscura);
                Engradado(v, 5, 0, 0, m.madeira, m.madeiraEscura);
                Engradado(v, 2, 3, 0, m.madeira, m.madeiraEscura);
            }, 1),
            Variacao(raiz.transform, "Pilha_Plastico_Azul", v =>
            {
                Engradado(v, 0, 0, 0, m.portaAzul, m.azulParede);
                Engradado(v, 0, 3, 0, m.portaAzul, m.azulParede);
            }, 1),
            Variacao(raiz.transform, "Pilha_Plastico_Vermelho", v =>
            {
                Engradado(v, 0, 0, 0, m.roupaVermelha, m.vermelhoDesbotado);
                Engradado(v, 5, 0, 0, m.roupaAmarela, m.amarelo);
            }, 1));

        Concluir(raiz, nome);
    }

    // Engradado 4x3x3 (0,5 x 0,4 x 0,4 m): preenchido, com as arestas em outro tom
    static void Engradado(Voxels v, int x, int y, int z, Material corpo, Material borda)
    {
        v.Caixa(x, y, z, 4, 3, 3, corpo);
        foreach (int a in new[] { 0, 2 })
        {
            foreach (int b in new[] { 0, 2 })
            {
                for (int i = 0; i < 4; i++) v.Celula(x + i, y + a, z + b, borda);
                for (int j = 0; j < 3; j++) v.Celula(x + (a == 0 ? 0 : 3), y + j, z + b, borda);
                for (int k = 0; k < 3; k++) v.Celula(x + (a == 0 ? 0 : 3), y + b, z + k, borda);
            }
        }
    }

    // Tambor de 200 litros (0,65 m de diâmetro, 0,9 m de altura) com nervuras, e a tampa fechada ou aberta
    static void MontarTambor(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();

        var colisor = raiz.AddComponent<BoxCollider>();
        colisor.center = new Vector3(0f, 0.44f, 0f);
        colisor.size = new Vector3(0.7f, 0.88f, 0.7f);

        Slot(modular, "Corpo",
            Variacao(raiz.transform, "Tambor_Azul", v => CorpoDoTambor(v, m.portaAzul, m.azulParede), 1),
            Variacao(raiz.transform, "Tambor_Vermelho", v => CorpoDoTambor(v, m.roupaVermelha, m.vermelhoDesbotado), 1),
            Variacao(raiz.transform, "Tambor_Verde", v => CorpoDoTambor(v, m.folhaEscura, m.folha), 1));

        Slot(modular, "Tampa",
            Variacao(raiz.transform, "Tampa_Fechada", v => Disco(v, 0, 0, 7, 1, 2, m.metal), 1),
            Variacao(raiz.transform, "Tampa_Aberta", v => Anel(v, 0, 0, 7, 1, 1.5f, 2.6f, m.metal), 1));

        Concluir(raiz, nome);
    }

    static void CorpoDoTambor(Voxels v, Material corpo, Material nervura)
    {
        Disco(v, 0, 0, 0, 7, 2, corpo);
        foreach (int y in new[] { 2, 5 }) Disco(v, 0, 0, y, 1, 2, nervura);
    }
}
