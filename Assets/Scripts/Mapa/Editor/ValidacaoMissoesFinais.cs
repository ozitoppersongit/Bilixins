using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            Exigir(encontros.Count(e => e.EncerraHistoria) == 1 && rei.EncerraHistoria,
                "Apenas o encontro do Rei encerra a história");
            Exigir(configuracao.All(m => m.nome != "FimHistoria"), "Aviso antigo removido");
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
            Exigir(ativas.Count == 1 && ((Missoes.Missao)Campo(ativas[0], "dados")).nome == "LixoTutorial",
                "Vitória conclui a missão do Rei, sem iniciar o aviso antigo");

            ativas.Clear();
            ((HashSet<string>)Campo(missoes, "concluidas")).Clear();
            missoes.ConcluirMissao("Subterraneo_Rei");
            missoes.ConcluirMissao("Fabrica_Lixoso");
            missoes.Iniciar("LixoTutorial");
            viscoso.aoVencer.Invoke();
            Exigir(ativas.Count == 1 && ((Missoes.Missao)Campo(ativas[0], "dados")).nome == "LixoTutorial",
                "Chefes derrotados fora de ordem não deixam missão impossível");

            var objetoInventario = new GameObject("Inventário de validação");
            SceneManager.MoveGameObjectToScene(objetoInventario, cena);
            InventoryManager inventario = objetoInventario.AddComponent<InventoryManager>();
            ItemData pocao = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Scripts/Itens/PocaoCura.asset");
            var coleta = configuracao.Single(m => m.nome == "LixoTutorial");
            Exigir(coleta.quantidade == 12 && coleta.entrega != null && coleta.itemRecompensa == pocao &&
                coleta.quantidadeRecompensa == 2, "A entrega dos 12 lixos recompensa duas Poções de Cura");
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
            Debug.Log("PASS: sequência final, encontro do Rei, recompensa da coleta e reset de inventário.");
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
