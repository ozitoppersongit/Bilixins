using System;
using System.Reflection;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Executar em batchmode: -executeMethod ValidacaoSpawnsInfinito.Executar -quit
public static class ValidacaoSpawnsInfinito
{
    public static void Executar()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Esta validação requer uma sessão batchmode isolada.");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var raiz = new GameObject("Validação dos spawns");
        try
        {
            GameObject piso = Caixa(raiz, "Piso", new Vector3(0f, -0.25f, 0f), new Vector3(30f, 0.5f, 30f));
            var jogador = new GameObject("Jogador");
            jogador.transform.SetParent(raiz.transform);
            jogador.transform.position = Vector3.up;
            var controller = jogador.AddComponent<CharacterController>();
            controller.height = 1.7f;
            controller.radius = 0.5f;
            controller.center = new Vector3(0f, -0.15f, 0f);
            controller.stepOffset = 0.3f;
            var spawns = raiz.AddComponent<SpawnsInfinito>();
            Exigir(spawns.ConstruirNavegacao(jogador), "Navegação no piso plano");
            Exigir((raiz.GetComponent<NavMeshSurface>().layerMask.value & (1 << 2)) != 0, "Incluir Ignore Raycast na navegação");
            Exigir(Caminho(spawns), "Caminho plano deve ser aceito");

            // O NavMesh continua aberto: a física precisa rejeitar o obstáculo novo.
            GameObject parede = Caixa(raiz, "Parede invisível", new Vector3(2.5f, 1f, 0f), new Vector3(0.2f, 2f, 8f));
            parede.layer = 2;
            Physics.SyncTransforms();
            Exigir(!Caminho(spawns), "Parede em Ignore Raycast deve impedir o trajeto");
            parede.SetActive(false);
            GameObject ladoA = Caixa(raiz, "Corredor A", new Vector3(2.5f, 1f, 0.4f), new Vector3(6f, 2f, 0.1f));
            GameObject ladoB = Caixa(raiz, "Corredor B", new Vector3(2.5f, 1f, -0.4f), new Vector3(6f, 2f, 0.1f));
            Physics.SyncTransforms();
            Exigir(!Caminho(spawns), "Corredor menor que a cápsula deve ser rejeitado");
            ladoA.SetActive(false);
            ladoB.SetActive(false);

            GameObject item = new GameObject("Item atrás da parede");
            item.transform.SetParent(raiz.transform);
            item.transform.position = new Vector3(1.2f, 0f, 0f);
            parede.transform.position = new Vector3(0.6f, 1f, 0f);
            parede.SetActive(true);
            Physics.SyncTransforms();
            Exigir(!spawns.PodeColetar(item), "Coleta através da parede deve ser rejeitada");
            parede.SetActive(false);
            Physics.SyncTransforms();
            Exigir(spawns.PodeColetar(item), "Coleta sem parede deve ser aceita");

            piso.SetActive(false);
            Caixa(raiz, "Piso antes do buraco", new Vector3(-6.5f, -0.25f, 0f), new Vector3(17f, 0.5f, 30f));
            Caixa(raiz, "Piso depois do buraco", new Vector3(9f, -0.25f, 0f), new Vector3(12f, 0.5f, 30f));
            Physics.SyncTransforms();
            Exigir(!Caminho(spawns), "Buraco no chão deve invalidar o caminho antigo");

            ConferirAnimacao("Assets/Art/Prefab/Lixo/Lixo_Saco.prefab", "Assets/Art/Efeitos/LixoFlutuar_Saco.anim", "Saco_Preto", raiz);
            ConferirAnimacao("Assets/Art/Prefab/Lixo/Lixo_Entulho.prefab", "Assets/Art/Efeitos/LixoFlutuar_Entulho.anim", "Entulho", raiz);
            Debug.Log("Validação dos itens infinitos: 12 verificações passaram.");
        }
        finally { UnityEngine.Object.DestroyImmediate(raiz); }
    }

    static bool Caminho(SpawnsInfinito spawns)
    {
        Physics.SyncTransforms();
        var metodo = typeof(SpawnsInfinito).GetMethod("Distancia", BindingFlags.Instance | BindingFlags.NonPublic);
        return (bool)metodo.Invoke(spawns, new object[] { Vector3.zero, Vector3.right * 5f, 0f, null });
    }

    static GameObject Caixa(GameObject pai, string nome, Vector3 posicao, Vector3 tamanho)
    {
        var objeto = new GameObject(nome);
        objeto.transform.SetParent(pai.transform);
        objeto.transform.position = posicao;
        objeto.AddComponent<BoxCollider>().size = tamanho;
        return objeto;
    }

    static void ConferirAnimacao(string prefab, string clip, string filho, GameObject pai)
    {
        GameObject visual = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(prefab), pai.transform);
        var animacao = AssetDatabase.LoadAssetAtPath<AnimationClip>(clip);
        Transform alvo = visual.transform.Find(filho);
        Exigir(alvo != null && animacao != null, "Estrutura da animação " + filho);
        animacao.SampleAnimation(visual, 1.5f);
        Exigir(Mathf.Abs(Mathf.DeltaAngle(alvo.localEulerAngles.y, 90f)) < 1f &&
            Mathf.Abs(alvo.localPosition.y - 2.06f) < 0.01f, "Rotação e flutuação " + filho);
    }

    static void Exigir(bool condicao, string descricao)
    {
        if (!condicao) throw new InvalidOperationException("Falhou: " + descricao);
    }
}
