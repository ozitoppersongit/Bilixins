using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public static class ValidacaoMissoesFinais
{
    const BindingFlags Privado = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("Tools/Bilixins/Validar missões finais e inventário")]
    public static void Executar()
    {
        Exigir(!Application.isPlaying && !ModoDeJogo.Infinito, "Execute fora de Play Mode, com modo história selecionado");
        var cena = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Game_Mesclado.unity");
        Missoes missoes = null;
        try
        {
            GameObject[] raizes = cena.GetRootGameObjects();
            missoes = raizes.SelectMany(r => r.GetComponentsInChildren<Missoes>(true)).Single();
            var configuracao = (List<Missoes.Missao>)Campo(missoes, "missoes");
            foreach (Missoes.Missao missao in configuracao) PermitirNoEditor(missao.aoConcluir);
            Transform painelModelo = missoes.missionPanel;
            missoes.missionPanel = null; // Testa a sequência sem criar/destruir UI no modo de edição.
            Chamar(missoes, "Awake");
            var ativas = (IList)Campo(missoes, "ativas");
            var encontros = raizes.SelectMany(r => r.GetComponentsInChildren<GatilhoDeEncontro>(true)).ToArray();
            GatilhoDeEncontro Encontro(string argumento) => encontros.Single(e =>
                new SerializedObject(e).FindProperty("aoVencer.m_PersistentCalls.m_Calls") is SerializedProperty eventos &&
                eventos.arraySize == 1 && eventos.GetArrayElementAtIndex(0)
                    .FindPropertyRelative("m_Arguments.m_StringArgument").stringValue == argumento);
            // Viscoso inicia a missão; Lixoso conclui a mesma. Localiza pela operação do evento.
            var viscoso = encontros.Single(e => e.aoVencer.GetPersistentEventCount() == 1 &&
                e.aoVencer.GetPersistentMethodName(0) == "Iniciar");
            var lixoso = encontros.Single(e => e != viscoso &&
                new SerializedObject(e).FindProperty("aoVencer.m_PersistentCalls.m_Calls") is SerializedProperty eventos &&
                eventos.arraySize == 2 && eventos.GetArrayElementAtIndex(0)
                    .FindPropertyRelative("m_Arguments.m_StringArgument").stringValue == "Fabrica_Lixoso");
            var rei = Encontro("Subterraneo_Rei");
            var raizRei = (GameObject)lixoso.aoVencer.GetPersistentTarget(1);
            Exigir(!raizRei.activeSelf && !rei.gameObject.activeInHierarchy, "Rei oculto antes da vitoria no Lixoso");
            foreach (var encontro in new[] { viscoso, lixoso, rei })
            {
                Exigir(encontro.aoVencer.GetPersistentTarget(0) == missoes, "Evento ligado às missões da cena");
                PermitirNoEditor(encontro.aoVencer);
            }

            missoes.Iniciar("LixoTutorial");
            viscoso.aoVencer.Invoke();
            viscoso.aoVencer.Invoke();
            Exigir(ativas.Count == 2, "Coleta preservada e missão do Lixoso sem duplicação");
            object ativaLixoso = ativas[1];
            Chamar(missoes, "AtualizarMissao", ativaLixoso, Vector3.zero);
            Exigir(ativas.Count == 2, "Combate sem itens não conclui sozinho");
            lixoso.aoVencer.Invoke();
            Exigir(raizRei.activeSelf && rei.isActiveAndEnabled && rei.GetComponent<Collider>().enabled,
                "Modelo e gatilho do Rei liberados pela vitoria no Lixoso");
            Exigir(((Missoes.Missao)Campo(ativas[1], "dados")).nome == "Subterraneo_Rei", "Vitória no Lixoso inicia missão do Rei");
            rei.aoVencer.Invoke();
            rei.aoVencer.Invoke();
            Exigir(ativas.Count == 2 && ((Missoes.Missao)Campo(ativas[1], "dados")).tipo == Missoes.TipoMissao.Aviso,
                "Aviso final único, com missão de coleta preservada");
            Chamar(missoes, "AtualizarMissao", ativas[1], Vector3.zero);
            Exigir(ativas.Count == 2, "Aviso não conclui automaticamente");

            ativas.Clear();
            ((HashSet<string>)Campo(missoes, "concluidas")).Clear();
            missoes.ConcluirMissao("Subterraneo_Rei");
            missoes.ConcluirMissao("Fabrica_Lixoso");
            missoes.Iniciar("LixoTutorial");
            viscoso.aoVencer.Invoke();
            Exigir(ativas.Count == 2 && ((Missoes.Missao)Campo(ativas[1], "dados")).nome == "FimHistoria",
                "Chefes derrotados fora de ordem não deixam missão impossível");

            missoes.missionPanel = painelModelo;
            Chamar(missoes, "CriarLista");
            Chamar(missoes, "CriarLinha", ativas[1]);
            var linha = (GameObject)Campo(ativas[1], "linha");
            Exigir(!linha.transform.Find("Ícone").gameObject.activeSelf, "Aviso sem coluna de ícone");
            TMP_Text[] textos = linha.GetComponentsInChildren<TMP_Text>(true);
            Exigir(textos.Length == 1 && textos[0].text == "Parabéns! Você derrotou o Rei do Lixo!", "Somente a frase final no aviso");

            var objetoInventario = new GameObject("Inventário de validação");
            SceneManager.MoveGameObjectToScene(objetoInventario, cena);
            InventoryManager inventario = objetoInventario.AddComponent<InventoryManager>();
            ItemData pocao = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Scripts/Itens/PocaoCura.asset");
            var inicial = (List<ItemStack>)Campo(inventario, "configuracaoInicial");
            inicial.Add(new ItemStack { item = pocao, quantidade = 3 });
            inventario.RestaurarConfiguracaoInicial();
            Exigir(inventario.TentarAdicionarItem(pocao) && inventario.ConsultarQuantidade(pocao) == 4, "Adição de uma recompensa");
            Exigir(!inventario.TentarAdicionarItem(null), "Item nulo não é anunciado como adicionado");
            Chamar(inventario, "AoCarregarCena", cena, LoadSceneMode.Additive);
            Exigir(inventario.ConsultarQuantidade(pocao) == 4, "Cena adicional não reinicia inventário");
            Chamar(inventario, "AoCarregarCena", cena, LoadSceneMode.Single);
            Exigir(inventario.ConsultarQuantidade(pocao) == 3, "Nova partida restaura estoque inicial");
            inventario.RemoverItem(pocao);
            inventario.RestaurarConfiguracaoInicial();
            Exigir(inventario.ConsultarQuantidade(pocao) == 3, "Estoque inicial não foi alterado pelo consumo");
            Debug.Log("PASS: sequência final, eventos das instâncias, aviso sem ícone, coleta preservada e reset de inventário.");
        }
        finally
        {
            if (missoes != null && Campo(missoes, "lista") is RectTransform lista)
            {
                missoes.GetType().GetField("lista", Privado).SetValue(missoes, null);
                UnityEngine.Object.DestroyImmediate(lista.gameObject);
            }
            EditorSceneManager.ClosePreviewScene(cena);
        }
    }

    static void PermitirNoEditor(UnityEvent evento)
    {
        if (evento == null) return;
        for (int i = 0; i < evento.GetPersistentEventCount(); i++)
            evento.SetPersistentListenerState(i, UnityEventCallState.EditorAndRuntime);
    }
    static object Campo(object alvo, string nome) => alvo.GetType().GetField(nome, Privado | BindingFlags.Public).GetValue(alvo);
    static void Chamar(object alvo, string nome, params object[] args) => alvo.GetType().GetMethod(nome, Privado).Invoke(alvo, args);
    static void Exigir(bool ok, string mensagem) { if (!ok) throw new Exception(mensagem); }
}
