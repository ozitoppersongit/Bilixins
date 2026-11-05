using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Gerenciador central de missões. Cada missão é configurada no Inspector (nada de script por missão).
// Tipo atual: coletar N itens (filhos de "itens") e entregar num ponto. Os itens não precisam de script:
// a missão olha a distância do player até eles. Enquanto não há UI, o progresso sai no Console;
// a UI assina aoMudarProgresso e aoMissaoConcluida quando existir.
public class Missoes : MonoBehaviour
{
    public static Missoes Instance { get; private set; }
    // painel de missao
    public Transform missionPanel;

    [Serializable]
    public class Missao
    {
        public string nome;
        [TextArea] public string titulo;
        [Tooltip("Objeto cujos filhos são os itens a coletar.")] public Transform itens;
        [Tooltip("Quantos coletar (0 = todos os filhos).")] public int quantidade;
        [Tooltip("Onde entregar. Vazio = a missão fecha assim que coletar tudo.")] public Transform entrega;
        public float raioColeta = 1.2f;
        public float raioEntrega = 2.5f;
        public UnityEvent aoIniciar;
        public UnityEvent aoConcluir;
    }

    enum Estado { Parada, Coletando, Entregando }

    [SerializeField] MapaGerador mapaGerador;
    [SerializeField] List<Missao> missoes = new List<Missao>();

    // missão, coletados, total
    public event Action<string, int, int> aoMudarProgresso;
    public event Action<string> aoMissaoConcluida;

    Missao atual;
    Estado estado = Estado.Parada;
    public int coletados;
    public int total;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Iniciar(string nome)
    {
        Missao m = missoes.Find(x => x.nome == nome);
        if (m == null) { Debug.LogWarning($"Missoes: não existe missão chamada '{nome}'."); return; }
        if (estado != Estado.Parada) { Debug.LogWarning($"Missoes: '{nome}' ignorada, já tem uma missão em andamento."); return; }

        atual = m;
        coletados = 0;
        total = m.quantidade > 0 ? m.quantidade : (m.itens != null ? m.itens.childCount : 0);
        estado = Estado.Coletando;
        Debug.Log($"[Missão] NOVA: {m.titulo} (0/{total})");
        missionPanel.gameObject.SetActive(true);
        m.aoIniciar?.Invoke();
        aoMudarProgresso?.Invoke(m.nome, coletados, total);
    }

    void Update()
    {
        if (estado == Estado.Parada || atual == null || mapaGerador == null || mapaGerador.PlayerInstanciado == null) return;
        Vector3 jogador = mapaGerador.PlayerInstanciado.transform.position;

        if (estado == Estado.Coletando)
        {
            if (atual.itens == null) return;
            foreach (Transform item in atual.itens)
            {
                if (!item.gameObject.activeSelf) continue;
                Vector3 d = item.position - jogador;
                if (Mathf.Abs(d.y) > 2.5f) continue;
                d.y = 0f;
                if (d.magnitude > atual.raioColeta) continue;

                item.gameObject.SetActive(false);
                coletados++;
                Debug.Log($"[Missão] Lixo coletado: {coletados}/{total}");
                aoMudarProgresso?.Invoke(atual.nome, coletados, total);
                if (coletados >= total) { TerminouDeColetar(); break; }
            }
        }
        else if (estado == Estado.Entregando)
        {
            Vector3 d = atual.entrega.position - jogador;
            d.y = 0f;
            if (d.magnitude <= atual.raioEntrega) Concluir();
        }
    }

    void TerminouDeColetar()
    {
        if (atual.entrega == null) { Concluir(); return; }
        estado = Estado.Entregando;
        Debug.Log($"[Missão] Todo o lixo coletado ({total}/{total}). Leve até: {atual.entrega.name}");
    }

    void Concluir()
    {
        Debug.Log($"[Missão] CONCLUÍDA: {atual.titulo}");
        Missao m = atual;
        estado = Estado.Parada;
        atual = null;
        aoMissaoConcluida?.Invoke(m.nome);
        m.aoConcluir?.Invoke();
        missionPanel.gameObject.SetActive(false);
    }
}
