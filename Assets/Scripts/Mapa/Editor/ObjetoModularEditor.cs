using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Inspector do ObjetoModular: um menu por slot pra escolher a variação, mais os botões de sortear e encaixar.
[CustomEditor(typeof(ObjetoModular))]
[CanEditMultipleObjects]
public class ObjetoModularEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var objeto = (ObjetoModular)target;

        EditorGUILayout.HelpBox(
            "Cada slot é uma peça (calha, musgo, porta...). Escolha a variação no menu. As variações são objetos filhos deste prefab: " +
            "pode ser ProBuilder, FBX do Blender ou o que for. Pra criar um slot novo ou uma variação nova, use a lista Slots mais abaixo.",
            MessageType.Info);

        bool mudou = false;

        foreach (ObjetoModular.Slot slot in objeto.slots)
        {
            if (slot == null) continue;

            var nomes = new List<string> { "(nenhuma)" };
            foreach (GameObject variacao in slot.variacoes)
                nomes.Add(variacao != null ? variacao.name : "(vazio)");

            int atual = Mathf.Clamp(slot.escolhida + 1, 0, nomes.Count - 1);
            int novo = EditorGUILayout.Popup(slot.nome, atual, nomes.ToArray());

            if (novo != atual)
            {
                Undo.RecordObject(objeto, "Trocar variação");
                slot.escolhida = novo - 1;
                mudou = true;
            }
        }

        if (mudou)
        {
            objeto.AplicarVariacoes();
            EditorUtility.SetDirty(objeto);
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Sortear variações"))
        {
            foreach (Object alvo in targets)
            {
                var o = (ObjetoModular)alvo;
                Undo.RecordObject(o, "Sortear variações");
                o.Aleatorizar();
                EditorUtility.SetDirty(o);
            }
        }

        if (GUILayout.Button("Encaixar no chão agora"))
        {
            foreach (Object alvo in targets)
                ((ObjetoModular)alvo).EncaixarAgora();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        DrawDefaultInspector();
    }
}
