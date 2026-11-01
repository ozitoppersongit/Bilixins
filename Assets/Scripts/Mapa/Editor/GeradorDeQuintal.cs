using UnityEngine;

// Parte do GeradorDeCasasEditor: extras de quintal e de rua (voxel fino de 0,125 m, frente = +Z, origem no meio da base).
// Cadeiras, churrasqueira, tanque de lavar, casinha de cachorro, carrinho de mão, material de obra, pneus, bujão e poste.
public static partial class GeradorDeCasasEditor
{
    // ---------------------------------------------------------------- cadeira de plástico (+ mesinha)
    static void MontarCadeira(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();
        AdicionarColisor(raiz, -4, 0, -4, 4, 20, 4);

        ModeloDaPeca Cadeira(string n, Material cor) => Variacao(raiz.transform, n, v =>
        {
            v.Caixa(-4, 8, -4, 8, 2, 8, cor);                       // assento
            foreach (int x in new[] { -4, 3 })
                foreach (int z in new[] { -4, 3 })
                    v.Caixa(x, 0, z, 1, 8, 1, cor);                 // pernas
            v.Caixa(-4, 10, -4, 8, 3, 1, cor);                      // encosto: base cheia e duas ripas
            v.Caixa(-4, 13, -4, 2, 6, 1, cor);
            v.Caixa(-1, 13, -4, 2, 6, 1, cor);
            v.Caixa(2, 13, -4, 2, 6, 1, cor);
            v.Caixa(-4, 19, -4, 8, 1, 1, cor);
        }, 1);

        Slot(modular, "Cadeira",
            Cadeira("Cadeira_Branca", m.roupaBranca),
            Cadeira("Cadeira_Azul", m.roupaAzul),
            Cadeira("Cadeira_Vermelha", m.roupaVermelha),
            Cadeira("Cadeira_Amarela", m.roupaAmarela));

        // Companhia: uma mesinha redonda ao lado, ou uma segunda cadeira de frente pra primeira
        Slot(modular, "Companhia",
            Variacao(raiz.transform, "Companhia_Mesinha", v =>
            {
                Disco(v, 10, 0, 10, 1, 4, m.roupaBranca);
                v.Caixa(10, 0, 0, 1, 10, 1, m.metal);
                Disco(v, 10, 0, 0, 1, 2, m.metal);
            }, 1),
            Variacao(raiz.transform, "Companhia_OutraCadeira", v =>
            {
                v.Caixa(7, 8, -4, 8, 2, 8, m.roupaBranca);
                foreach (int x in new[] { 7, 14 })
                    foreach (int z in new[] { -4, 3 })
                        v.Caixa(x, 0, z, 1, 8, 1, m.roupaBranca);
                v.Caixa(14, 10, -4, 1, 10, 8, m.roupaBranca);
            }, 1));

        Concluir(raiz, nome);
    }

    // ---------------------------------------------------------------- churrasqueira
    static void MontarChurrasqueira(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();
        AdicionarColisor(raiz, -8, 0, -5, 8, 12, 5);

        ModeloDaPeca Corpo(string n, Material base_, Material borda) => Variacao(raiz.transform, n, v =>
        {
            v.Caixa(-8, 0, -5, 16, 10, 10, base_);
            v.Caixa(-8, 10, -5, 16, 2, 2, borda);
            v.Caixa(-8, 10, 3, 16, 2, 2, borda);
            v.Caixa(-8, 10, -3, 2, 2, 6, borda);
            v.Caixa(6, 10, -3, 2, 2, 6, borda);
            v.Caixa(-6, 10, -3, 12, 1, 6, m.terra);
            var r = new System.Random(5);
            for (int x = -6; x < 6; x++)
                for (int z = -3; z < 3; z++)
                    if (r.NextDouble() < 0.25) v.Celula(x, 10, z, r.NextDouble() < 0.5 ? m.laranja : m.roupaVermelha);
        }, 1);

        Slot(modular, "Corpo",
            Corpo("Corpo_Bloco", m.bloco, m.concretoSujo),
            Corpo("Corpo_Tijolo", m.tijolo, m.ferrugem),
            Corpo("Corpo_Rebocado", m.amarelo, m.concretoSujo));

        Slot(modular, "Topo",
            Variacao(raiz.transform, "Topo_Grelha", v =>
            {
                for (int x = -7; x < 7; x += 2) v.Caixa(x, 12, -4, 1, 1, 8, m.ferro);
                v.Caixa(-7, 12, -4, 14, 1, 1, m.ferro);
                v.Caixa(-7, 12, 3, 14, 1, 1, m.ferro);
            }, 1),
            Variacao(raiz.transform, "Topo_Espetos", v =>
            {
                foreach (int x in new[] { -5, -1, 3 })
                {
                    v.Caixa(x, 12, -4, 1, 1, 8, m.ferro);
                    v.Caixa(x, 13, -3, 1, 1, 6, x == -1 ? m.ferrugem : m.tijolo);
                }
            }, 1),
            Variacao(raiz.transform, "Topo_Chamine", v =>
            {
                for (int x = -7; x < 7; x += 2) v.Caixa(x, 12, -4, 1, 1, 5, m.ferro);
                v.Caixa(-4, 12, 1, 8, 18, 3, m.tijolo);
                v.Caixa(-5, 30, 1, 10, 2, 4, m.concretoSujo);
            }, 1));

        Slot(modular, "Extras",
            Variacao(raiz.transform, "Extras_SacoDeCarvao", v =>
            {
                v.Caixa(10, 0, -2, 5, 8, 4, m.ferro);
                v.Caixa(11, 3, 2, 3, 3, 1, m.brancoToldo);
            }, 1),
            Variacao(raiz.transform, "Extras_Engradado", v => Engradado(v, 9, 0, -4, m.madeira, m.madeiraEscura), 1));

        Concluir(raiz, nome);
    }

    // ---------------------------------------------------------------- tanque de lavar roupa
    static void MontarTanqueDeLavar(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();
        AdicionarColisor(raiz, -10, 0, -5, 10, 22, 5);

        ModeloDaPeca Tanque(string n, Material cor) => Variacao(raiz.transform, n, v =>
        {
            foreach (int x in new[] { -10, 7 })
                foreach (int z in new[] { -5, 2 })
                    v.Caixa(x, 0, z, 3, 10, 3, cor);
            v.Caixa(-10, 10, -5, 20, 2, 10, cor);

            v.Caixa(-10, 12, -5, 10, 10, 2, cor);       // tanque: paredes
            v.Caixa(-10, 12, 3, 10, 10, 2, cor);
            v.Caixa(-10, 12, -3, 2, 10, 6, cor);
            v.Caixa(-2, 12, -3, 2, 10, 6, cor);
            v.Caixa(-8, 12, -3, 6, 5, 6, m.vidro);      // água

            v.Caixa(0, 12, -5, 10, 6, 10, cor);         // tábua de lavar, com as ranhuras
            v.Caixa(0, 18, 3, 10, 3, 2, cor);
            for (int z = -4; z < 3; z += 2) v.Caixa(1, 18, z, 8, 1, 1, m.concretoSujo);

            v.Caixa(-2, 22, 3, 1, 3, 1, m.calha);       // torneira
            v.Caixa(-2, 24, 1, 1, 1, 3, m.calha);
        }, 1);

        Slot(modular, "Pintura",
            Tanque("Pintura_Concreto", m.concreto),
            Tanque("Pintura_Azul", m.azulParede),
            Tanque("Pintura_VerdeAgua", m.verdeAgua),
            Tanque("Pintura_Branca", m.moldura));

        Slot(modular, "Extras",
            Variacao(raiz.transform, "Extras_Roupa", v =>
            {
                v.Caixa(1, 19, -3, 6, 2, 5, m.roupaAzul);
                v.Caixa(2, 21, -2, 4, 1, 3, m.roupaBranca);
            }, 1),
            Variacao(raiz.transform, "Extras_Sabao", v =>
            {
                v.Caixa(3, 19, -2, 3, 1, 2, m.amarelo);
                v.Caixa(6, 19, 0, 3, 1, 2, m.moldura);
            }, 1),
            Variacao(raiz.transform, "Extras_Balde", v =>
            {
                Disco(v, 14, 0, 0, 6, 2, m.baldeCinza);
                v.Caixa(12, 6, 0, 5, 1, 1, m.ferro);
            }, 1));

        Concluir(raiz, nome);
    }

    // ---------------------------------------------------------------- casinha de cachorro
    static void MontarCasinhaDeCachorro(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();
        AdicionarColisor(raiz, -6, 0, -6, 6, 9, 5);

        var porta = new Voxels(1);
        porta.Caixa(-3, 0, 5, 6, 8, 1, m.madeiraEscura);
        porta.Caixa(-2, 0, 5, 4, 6, 1, m.ferro);
        Fixo(raiz.transform, "Porta", porta);

        ModeloDaPeca Corpo(string n, Material cor) => Variacao(raiz.transform, n, v => v.Caixa(-6, 0, -6, 12, 9, 11, cor), 1);

        Slot(modular, "Pintura",
            Corpo("Pintura_Madeira", m.madeira),
            Corpo("Pintura_Azul", m.azulParede),
            Corpo("Pintura_Vermelha", m.vermelhoDesbotado),
            Corpo("Pintura_Verde", m.verdeAgua));

        Slot(modular, "Telhado",
            Variacao(raiz.transform, "Telhado_Telha", v =>
            {
                for (int k = 0; k < 6; k++)
                    v.Caixa(-7 + k, 9 + k, -7, 14 - 2 * k, 1, 14, k % 2 == 0 ? m.telhaA : m.telhaB);
            }, 1),
            Variacao(raiz.transform, "Telhado_Zinco", v =>
            {
                for (int k = 0; k < 6; k++)
                    v.Caixa(-7 + k, 9 + k, -7, 14 - 2 * k, 1, 14, k % 2 == 0 ? m.zincoA : m.zincoB);
            }, 1));

        ModeloDaPeca Cachorro(string n, Material corpo, Material detalhe) => Variacao(raiz.transform, n, v =>
        {
            const int x = 8, z = -1;
            v.Caixa(x, 3, z, 7, 4, 3, corpo);
            foreach (int dx in new[] { 0, 5 })
                foreach (int dz in new[] { 0, 2 })
                    v.Caixa(x + dx, 0, z + dz, 1, 3, 1, corpo);
            v.Caixa(x + 7, 5, z, 3, 3, 3, corpo);
            v.Caixa(x + 10, 5, z + 1, 1, 2, 1, detalhe);
            v.Caixa(x + 7, 8, z, 1, 2, 1, detalhe);
            v.Caixa(x + 7, 8, z + 2, 1, 2, 1, detalhe);
            v.Caixa(x - 1, 6, z + 1, 1, 3, 1, corpo);
            v.Caixa(x + 12, 0, z, 3, 1, 3, m.baldeCinza);   // tigela
        }, 1);

        Slot(modular, "Cachorro",
            Cachorro("Cachorro_Caramelo", m.amarelo, m.madeira),
            Cachorro("Cachorro_Preto", m.ferro, m.metal),
            Cachorro("Cachorro_Branco", m.moldura, m.concretoSujo));

        Concluir(raiz, nome);
    }

    // ---------------------------------------------------------------- carrinho de mão
    static void MontarCarrinhoDeMao(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();
        AdicionarColisor(raiz, -12, 0, -4, 7, 9, 4);

        ModeloDaPeca Corpo(string n, Material cor) => Variacao(raiz.transform, n, v =>
        {
            v.Caixa(-6, 5, -4, 10, 1, 8, cor);                       // fundo da caçamba
            v.Caixa(-6, 6, -4, 10, 3, 1, cor);                       // laterais
            v.Caixa(-6, 6, 3, 10, 3, 1, cor);
            v.Caixa(-6, 6, -3, 1, 3, 6, cor);                        // fundo e frente
            v.Caixa(3, 6, -3, 1, 3, 6, cor);
            v.Caixa(-12, 6, -3, 6, 1, 1, m.madeira);                 // cabos
            v.Caixa(-12, 6, 2, 6, 1, 1, m.madeira);
            v.Caixa(-8, 0, -3, 1, 6, 1, m.madeiraEscura);            // pés
            v.Caixa(-8, 0, 2, 1, 6, 1, m.madeiraEscura);
            v.Caixa(4, 3, -4, 3, 1, 1, m.ferro);                     // garfo da roda
            v.Caixa(4, 3, 3, 3, 1, 1, m.ferro);
            for (int y = 0; y < 7; y++)
                for (int z = -3; z <= 3; z++)
                    if (y * y + z * z <= 11) v.Caixa(5, y, z, 2, 1, 1, y * y + z * z <= 2 ? m.metal : m.ferro);
        }, 1);

        Slot(modular, "Cor",
            Corpo("Cor_Azul", m.roupaAzul),
            Corpo("Cor_Vermelha", m.roupaVermelha),
            Corpo("Cor_Ferrugem", m.ferrugem));

        Slot(modular, "Carga",
            Variacao(raiz.transform, "Carga_Terra", v =>
            {
                v.Caixa(-5, 6, -3, 8, 2, 6, m.terra);
                v.Caixa(-4, 8, -2, 6, 1, 4, m.terra);
            }, 1),
            Variacao(raiz.transform, "Carga_Tijolos", v =>
            {
                v.Caixa(-5, 6, -3, 8, 2, 6, m.tijolo);
                v.Caixa(-4, 8, -2, 6, 1, 4, m.ferrugem);
            }, 1),
            Variacao(raiz.transform, "Carga_Folhas", v =>
            {
                v.Caixa(-5, 6, -3, 8, 2, 6, m.folha);
                v.Caixa(-4, 8, -2, 6, 2, 4, m.folhaEscura);
            }, 1));

        Concluir(raiz, nome);
    }

    // ---------------------------------------------------------------- material de obra
    static void MontarMaterialDeObra(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();
        AdicionarColisor(raiz, 0, 0, 0, 16, 9, 10);

        Slot(modular, "Pilha",
            Variacao(raiz.transform, "Pilha_Tijolos", v =>
            {
                v.Caixa(0, 0, 0, 16, 2, 10, m.madeira);
                var r = new System.Random(9);
                for (int y = 2; y < 9; y++)
                    for (int x = 0; x < 16; x += 4)
                        for (int z = 0; z < 10; z += 5)
                            if (y < 8 || r.NextDouble() < 0.5)
                                v.Caixa(x, y, z, 4, 1, 5, (y + x / 4 + z / 5) % 2 == 0 ? m.tijolo : m.ferrugem);
            }, 1),
            Variacao(raiz.transform, "Pilha_Blocos", v =>
            {
                v.Caixa(0, 0, 0, 16, 2, 10, m.madeira);
                for (int y = 2; y < 8; y++)
                    for (int x = 0; x < 16; x += 4)
                        for (int z = 0; z < 10; z += 5)
                            v.Caixa(x, y, z, 4, 1, 5, (y + x / 4 + z / 5) % 2 == 0 ? m.bloco : m.concretoSujo);
            }, 1),
            Variacao(raiz.transform, "Pilha_Areia", v =>
            {
                for (int k = 0; k < 6; k++) v.Caixa(k, k, k, 16 - 2 * k, 1, 10 - 2 * k, m.capimSeco);
            }, 1),
            Variacao(raiz.transform, "Pilha_SacosDeCimento", v =>
            {
                v.Caixa(0, 0, 0, 16, 2, 10, m.madeira);
                for (int y = 0; y < 3; y++)
                    for (int x = 0; x < 16; x += 8)
                    {
                        v.Caixa(x, 2 + y * 2, y % 2 == 0 ? 0 : 1, 8, 2, 8, m.concreto);
                        v.Caixa(x + 2, 2 + y * 2, y % 2 == 0 ? 8 : 9, 4, 1, 1, m.brancoToldo);
                    }
            }, 1));

        Concluir(raiz, nome);
    }

    // ---------------------------------------------------------------- pneus
    static void MontarPneus(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();
        AdicionarColisor(raiz, -6, 0, -6, 6, 10, 6);

        ModeloDaPeca Pilha(string n, Material cor) => Variacao(raiz.transform, n, v =>
        {
            for (int i = 0; i < 3; i++) Anel(v, 0, 0, i * 3, 3, 3f, 5.6f, cor);
            Anel(v, 0, 0, 9, 1, 3f, 5.6f, cor);
        }, 1);

        ModeloDaPeca Planta(string n, Material cor) => Variacao(raiz.transform, n, v =>
        {
            Anel(v, 0, 0, 0, 3, 3f, 5.6f, cor);
            Anel(v, 0, 0, 3, 3, 3f, 5.6f, cor);
            Disco(v, 0, 0, 0, 5, 2, m.terra);
            Caixa3(v, -1, 5, -1, 3, 4, 3, m.folha);
            v.Caixa(-2, 7, 0, 5, 2, 1, m.folhaClara);
            v.Caixa(0, 8, -1, 1, 1, 3, m.florRosa);
        }, 1);

        Slot(modular, "Pneus",
            Pilha("Pneus_Pilha_Preta", m.ferro),
            Pilha("Pneus_Pilha_Branca", m.roupaBranca),
            Planta("Pneus_Planta_Branca", m.roupaBranca),
            Planta("Pneus_Planta_Azul", m.roupaAzul));

        Concluir(raiz, nome);
    }

    static void Caixa3(Voxels v, int x, int y, int z, int w, int h, int d, Material mat) => v.Caixa(x, y, z, w, h, d, mat);

    // ---------------------------------------------------------------- bujão de gás
    static void MontarBujao(string nome, Materiais m)
    {
        Iniciar();
        GameObject raiz = new GameObject(nome);
        var modular = raiz.AddComponent<ObjetoModular>();
        AdicionarColisor(raiz, -4, 0, -4, 14, 18, 4);

        void Bujao(Voxels v, int cx, Material cor)
        {
            Anel(v, cx, 0, 0, 1, 2f, 3.5f, m.metal);            // base
            Disco(v, cx, 0, 1, 12, 3, cor);                     // corpo
            Anel(v, cx, 0, 6, 1, 3.6f, 4f, m.metal);            // cinta
            Disco(v, cx, 0, 13, 1, 2, cor);                     // ombro
            v.Caixa(cx, 14, 0, 1, 2, 1, m.metal);               // válvula
            v.Caixa(cx - 1, 16, 0, 3, 1, 1, m.metal);
        }

        Slot(modular, "Bujoes",
            Variacao(raiz.transform, "Bujao_Azul", v => Bujao(v, 0, m.portaAzul), 1),
            Variacao(raiz.transform, "Bujao_Verde", v => Bujao(v, 0, m.folhaEscura), 1),
            Variacao(raiz.transform, "Bujao_Laranja", v => Bujao(v, 0, m.laranja), 1),
            Variacao(raiz.transform, "Bujao_DoisAzuis", v => { Bujao(v, 0, m.portaAzul); Bujao(v, 9, m.portaAzul); }, 1),
            Variacao(raiz.transform, "Bujao_AzulEVerde", v => { Bujao(v, 0, m.portaAzul); Bujao(v, 9, m.folhaEscura); }, 1));

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
}
