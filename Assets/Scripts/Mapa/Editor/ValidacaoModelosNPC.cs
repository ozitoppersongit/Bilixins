using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Abre uma cena de prévia descartável; não salva nem modifica a cena em edição.
public static class ValidacaoModelosNPC
{
    static readonly string[] Nomes = { "NPC_Missao", "NPC_SeuEvandro", "NPC_Danilo", "NPC_DonaVania", "NPC_Caio", "NPC_Michele", "NPC_Marcelo" };
    static readonly string[] Modelos = { "Vendedor Voxel", "Voxel Evandro", "Danilo/Voxel Danilo", "Voxel Vania", "Voxel Caio", "Voxel Michele", "Voxel Marcelo" };
    const string Pasta = "Assets/Art/Modelos Blender/NPCs/";
    const BindingFlags MetodosPrivados = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("Tools/Bilixins/Validar modelos e animações dos NPCs")]
    public static void Executar()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var cena = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Game.unity");
        try
        {
            Transform[] objetos = cena.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Transform>(true)).ToArray();
            var referencia = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Prefab/Player.prefab"), cena);
            referencia.SetActive(true);
            foreach (Animator a in referencia.GetComponentsInChildren<Animator>(true)) { a.Rebind(); a.Update(0f); }
            float alturaGalego = MedirMalha(referencia.transform).size.y;
            Exigir(alturaGalego > 1f, "Malha visível do Galego como referência");
            float[] proporcoes = { 1.10f, 1.07f, 0.95f, 1.10f, 0f, 0f, 0f };
            var pisoTeste = new GameObject("Piso da validação de NPCs");
            SceneManager.MoveGameObjectToScene(pisoTeste, cena);
            pisoTeste.transform.position = new Vector3(10000f, 4999.9f, 10000f);
            var piso = pisoTeste.AddComponent<BoxCollider>();
            piso.size = new Vector3(40f, 0.2f, 40f);

            for (int i = 0; i < Nomes.Length; i++)
            {
                string nome = Nomes[i];
                Transform raiz = objetos.Single(t => t.name == nome);
                var script = raiz.GetComponent<AnimacaoNPC>();
                Exigir(script != null && script.animator != null && script.modelo != null, nome + ": referências da animação");
                Exigir(script.animator.transform == script.modelo && script.modelo.IsChildOf(raiz), nome + ": Animator no modelo filho");
                Exigir(!script.animator.applyRootMotion, nome + ": root motion desativado");
                Exigir(raiz.GetComponentsInChildren<MeshRenderer>(true)
                    .Where(r => r.name == "Corpo" || r.transform == raiz || r.name == "Cabelo" || r.name == "Coque")
                    .All(r => !r.enabled), nome + ": cápsulas e cabelos provisórios ocultos");
                Exigir(script.modelo.GetComponentInChildren<SkinnedMeshRenderer>(true) != null, nome + ": malha skinned");
                Exigir(script.modelo.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .All(r => r.sharedMaterials.All(m => m != null)), nome + ": materiais presentes");
                Vector3 escala = script.modelo.lossyScale;
                Exigir(Mathf.Abs(escala.x - escala.y) < 0.001f && Mathf.Abs(escala.y - escala.z) < 0.001f,
                    nome + ": escala uniforme");
                var apoio = script.modelo.parent.GetComponent<ApoioVisualNPC>();
                Exigir(apoio != null, nome + ": apoio visual configurado");

                string caminho = Pasta + Modelos[i] + ".fbx";
                var importer = (ModelImporter)AssetImporter.GetAtPath(caminho);
                var configuracao = importer.clipAnimations.Single();
                Exigir(importer.isReadable && (int)importer.avatarSetup == 1, nome + ": avatar próprio e extração das solas");
                Exigir(importer.animationType == ModelImporterAnimationType.Generic && configuracao.loopTime &&
                    configuracao.firstFrame == 22f && configuracao.lastFrame == 48f, nome + ": ciclo Generic 22–48 em loop");
                var controlador = script.animator.runtimeAnimatorController as AnimatorOverrideController;
                Exigir(controlador != null && controlador.runtimeAnimatorController.name == "NPCAnimator", nome + ": controlador base preservado");
                AnimationClip clipe = AssetDatabase.LoadAllAssetsAtPath(caminho).OfType<AnimationClip>()
                    .Single(c => !c.name.StartsWith("__preview__"));
                Exigir(controlador.animationClips.Contains(clipe), nome + ": clipe do próprio personagem");
                if (i >= 2)
                    Exigir(raiz.GetComponent<Animator>() != null && raiz.GetComponent<Animator>() != script.animator,
                        nome + ": Animator de cutscene preservado");

                // Inclui o Vendedor, que começa inativo na história.
                for (Transform pai = raiz; pai != null; pai = pai.parent) pai.gameObject.SetActive(true);
                script.animator.Rebind();
                script.animator.Update(0f);
                Exigir(script.animator.avatar != null && script.animator.avatar.isValid, nome + ": avatar importado válido");
                if (proporcoes[i] > 0f)
                    Exigir(Mathf.Abs(MedirMalha(script.modelo).size.y / alturaGalego - proporcoes[i]) < 0.025f,
                        nome + ": altura relativa ao Galego");
                else Exigir(Mathf.Abs(escala.y - 0.425f) < 0.001f, nome + ": tamanho das crianças preservado");
                raiz.position = new Vector3(10000f, 5001f, 10000f);
                Physics.SyncTransforms();
                apoio.AtualizarApoio(0f);
                Exigir(apoio.QuantidadePontosSola == 8, nome + ": oito pontos reais nas solas");
                ConferirContato(script.modelo, 5000f, nome + ": apoio em Idle");
                Animator corpoAnimator = raiz.GetComponent<Animator>();
                if (corpoAnimator != null)
                {
                    corpoAnimator.Rebind();
                    for (int amostra = 0; amostra <= 4; amostra++)
                    {
                        corpoAnimator.Play("Idle", 0, amostra / 4f);
                        corpoAnimator.Update(0f);
                        apoio.AtualizarApoio(0.1f);
                        ConferirContato(script.modelo, 5000f, nome + ": Idle completo, amostra " + amostra);
                    }
                    corpoAnimator.Play("Susto", 0, 0.24f);
                    corpoAnimator.Update(0f);
                    apoio.AtualizarApoio(0.1f);
                    Exigir(MedirMalha(script.modelo).min.y > 5000.2f, nome + ": salto real de Susto preservado");
                    corpoAnimator.Play("Idle", 0, 0f);
                    corpoAnimator.Update(0f);
                    apoio.AtualizarApoio(0.1f);
                }
                typeof(AnimacaoNPC).GetMethod("OnEnable", MetodosPrivados).Invoke(script, null);
                var atualizar = typeof(AnimacaoNPC).GetMethod("AtualizarMovimento", MetodosPrivados);

                raiz.position += Vector3.forward * 0.2f;
                atualizar.Invoke(script, new object[] { 0.1f });
                script.animator.Update(0.3f);
                Exigir(EmEstado(script.animator, "Andar"), nome + ": andar durante deslocamento");
                // Verifica o ciclo inteiro, inclusive os frames em que a sola desce abaixo do bind.
                for (int frame = 0; frame < 27; frame++)
                {
                    script.animator.Update(1f / 24f);
                    apoio.AtualizarApoio(1f / 24f);
                    ConferirContato(script.modelo, 5000f, nome + ": sola durante caminhada");
                }
                raiz.position += Vector3.down * 0.15f;
                apoio.AtualizarApoio(0.1f);
                ConferirContato(script.modelo, 5000f, nome + ": altura variável nos trajetos");
                pisoTeste.transform.position += Vector3.up * 0.15f;
                Physics.SyncTransforms();
                apoio.AtualizarApoio(0.1f);
                ConferirContato(script.modelo, 5000.15f, nome + ": piso mais alto");
                pisoTeste.transform.position -= Vector3.up * 0.15f;
                Physics.SyncTransforms();
                apoio.AtualizarApoio(0.1f);
                if (apoio.transform.parent != raiz)
                {
                    // Testa uma pose externa quando o Animator não está executando Idle.
                    if (corpoAnimator != null)
                    {
                        corpoAnimator.Play("Susto", 0, 0f);
                        corpoAnimator.Update(0f);
                    }
                    Transform corpo = apoio.transform.parent;
                    Vector3 posicaoCorpo = corpo.localPosition;
                    Quaternion giroCorpo = corpo.localRotation;
                    corpo.localPosition += Vector3.up * 0.55f;
                    corpo.localRotation = Quaternion.Euler(0f, 0f, -14f);
                    apoio.AtualizarApoio(0.1f);
                    Exigir(MedirMalha(script.modelo).min.y > 5000.2f, nome + ": salto e inclinação preservados");
                    corpo.localPosition = posicaoCorpo;
                    corpo.localRotation = giroCorpo;
                    if (corpoAnimator != null)
                    {
                        corpoAnimator.Play("Idle", 0, 0f);
                        corpoAnimator.Update(0f);
                    }
                }
                atualizar.Invoke(script, new object[] { 0.1f });
                script.animator.Update(0.1f);
                Exigir(script.animator.GetFloat("Velocidade") == 0f && EmEstado(script.animator, "Idle"), nome + ": parar quando imóvel");

                raiz.position += Vector3.forward * 0.0051f;
                for (int passo = 0; passo < 6; passo++)
                {
                    if (passo > 0) raiz.position += Vector3.forward * 0.0051f;
                    atualizar.Invoke(script, new object[] { 0.1f });
                    script.animator.Update(0.1f);
                }
                Exigir(EmEstado(script.animator, "Andar"), nome + ": caminhada lenta");
                raiz.position += Vector3.forward * 10f;
                atualizar.Invoke(script, new object[] { 0.1f });
                script.animator.Update(0.1f);
                Exigir(script.animator.GetFloat("Velocidade") == 0f, nome + ": teletransporte não inicia caminhada");
                raiz.gameObject.SetActive(false);
                raiz.gameObject.SetActive(true);
                script.animator.Update(0f);
                typeof(ApoioVisualNPC).GetMethod("OnEnable", MetodosPrivados).Invoke(apoio, null);
                typeof(AnimacaoNPC).GetMethod("OnEnable", MetodosPrivados).Invoke(script, null);
                atualizar.Invoke(script, new object[] { 0.1f });
                Exigir(script.animator.GetFloat("Velocidade") == 0f, nome + ": reativação sem caminhada falsa");
                apoio.AtualizarApoio(0f);
                ConferirContato(script.modelo, 5000f, nome + ": contato após reativar");
                piso.enabled = false;
                Physics.SyncTransforms();
                raiz.position += Vector3.down * 0.1f;
                apoio.AtualizarApoio(0.1f);
                ConferirContato(script.modelo, 5000f, nome + ": conserva o último piso válido");
                piso.enabled = true;
                Physics.SyncTransforms();
                Debug.Log("NPC validado: " + nome, raiz.gameObject);
            }
            Debug.Log("Validação concluída: sete modelos, alturas, oito pontos de sola, pisos, caminhada, saltos e reativação.");
        }
        finally { EditorSceneManager.ClosePreviewScene(cena); }
    }

    static void ConferirContato(Transform modelo, float piso, string descricao)
    {
        float distancia = MedirMalha(modelo).min.y - piso;
        Exigir(distancia >= -0.001f && distancia <= 0.004f, descricao + " (folga " + distancia + " m)");
    }

    [MenuItem("Tools/Bilixins/Validar retorno dos encontros")]
    public static void ValidarRetornoEncontros()
    {
        var cena = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Game.unity");
        try
        {
            var jogador = new GameObject("Jogador de teste de retorno");
            SceneManager.MoveGameObjectToScene(jogador, cena);
            jogador.AddComponent<CharacterController>();
            int quantidade = 0;
            foreach (var encontro in cena.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<GatilhoDeEncontro>(true)))
            {
                Collider volume = encontro.GetComponent<Collider>();
                if (volume == null) continue;
                for (Transform pai = encontro.transform; pai != null; pai = pai.parent) pai.gameObject.SetActive(true);
                typeof(GatilhoDeEncontro).GetMethod("Awake", MetodosPrivados).Invoke(encontro, null);
                typeof(GatilhoDeEncontro).GetField("jogadorDoEncontro", MetodosPrivados).SetValue(encontro, jogador.transform);
                for (int tentativa = 0; tentativa < 2; tentativa++)
                {
                    encontro.enabled = false; // Configuração original do prefab Lixoso.
                    volume.enabled = false;
                    encontro.Reativar();
                    encontro.ProtegerAposRetorno();
                    Exigir(encontro.enabled && volume.enabled, encontro.name + ": controlador e volume rearmados");
                    jogador.transform.position = volume.bounds.center;
                    Physics.SyncTransforms();
                    typeof(GatilhoDeEncontro).GetField("protegidoAte", MetodosPrivados).SetValue(encontro, -1f);
                    typeof(GatilhoDeEncontro).GetMethod("Update", MetodosPrivados).Invoke(encontro, null);
                    var aguardando = typeof(GatilhoDeEncontro).GetField("aguardandoSaidaAposDerrota", MetodosPrivados);
                    Exigir((bool)aguardando.GetValue(encontro), encontro.name + ": exige separação física");
                    jogador.transform.position = volume.bounds.max + Vector3.one * 100f;
                    Physics.SyncTransforms();
                    typeof(GatilhoDeEncontro).GetField("protegidoAte", MetodosPrivados).SetValue(encontro, Time.unscaledTime + 100f);
                    typeof(GatilhoDeEncontro).GetMethod("Update", MetodosPrivados).Invoke(encontro, null);
                    Exigir((bool)aguardando.GetValue(encontro), encontro.name + ": respeita proteção mesmo fora do volume");
                    typeof(GatilhoDeEncontro).GetField("protegidoAte", MetodosPrivados).SetValue(encontro, -1f);
                    typeof(GatilhoDeEncontro).GetMethod("Update", MetodosPrivados).Invoke(encontro, null);
                    Exigir(!(bool)aguardando.GetValue(encontro), encontro.name + ": libera nova entrada após proteção e separação");
                }
                quantidade++;
            }
            Exigir(quantidade > 0, "Encontros encontrados na cena");
            Debug.Log("Retorno validado em " + quantidade + " encontros, com duas tentativas cada.");
        }
        finally { EditorSceneManager.ClosePreviewScene(cena); }
    }

    static Bounds MedirMalha(Transform modelo)
    {
        var resultado = new Bounds();
        bool iniciou = false;
        var temporaria = new Mesh();
        try
        {
            foreach (SkinnedMeshRenderer renderer in modelo.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                renderer.BakeMesh(temporaria);
                foreach (Vector3 vertice in temporaria.vertices)
                {
                    Vector3 mundo = renderer.transform.TransformPoint(vertice);
                    if (!iniciou) { resultado = new Bounds(mundo, Vector3.zero); iniciou = true; }
                    else resultado.Encapsulate(mundo);
                }
                temporaria.Clear();
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(temporaria); }
        Exigir(iniciou, modelo.name + ": malha visível mensurável");
        return resultado;
    }

    static bool EmEstado(Animator animator, string nome)
    {
        return animator.GetCurrentAnimatorStateInfo(0).IsName(nome) ||
            (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName(nome));
    }

    static void Exigir(bool condicao, string descricao)
    {
        if (!condicao) throw new InvalidOperationException("Falhou: " + descricao);
    }
}
