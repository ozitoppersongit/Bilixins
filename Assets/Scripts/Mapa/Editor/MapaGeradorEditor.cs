using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapaGerador))]
public class MapaGeradorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MapaGerador gerador = (MapaGerador)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Gerar Preview no Editor", GUILayout.Height(32)))
        {
            gerador.GerarPreviewNoEditor();
        }

        if (GUILayout.Button("Limpar Preview"))
        {
            gerador.LimparMapa();
        }

        EditorGUILayout.HelpBox(
            "O preview é só visual, pra você conferir o mapa sem apertar Play. " +
            "Não salve a cena com o preview gerado — ao entrar em Play ele é limpo e regenerado de novo.",
            MessageType.Info);

        // Esconder/mostrar os objetos colocados à mão. Usa a visibilidade da Scene view: não mexe na cena salva
        // nem no jogo, então esquecer escondido não faz as casas sumirem de verdade.
        Transform raizDosObjetos = gerador.RaizDosObjetos;
        if (raizDosObjetos != null)
        {
            GUILayout.Space(10);
            bool escondidos = SceneVisibilityManager.instance.IsHidden(raizDosObjetos.gameObject, true);

            if (GUILayout.Button(escondidos ? "Mostrar objetos (casas, bloqueios...)" : "Esconder objetos (casas, bloqueios...)", GUILayout.Height(24)))
            {
                if (escondidos) SceneVisibilityManager.instance.Show(raizDosObjetos.gameObject, true);
                else SceneVisibilityManager.instance.Hide(raizDosObjetos.gameObject, true);
            }

            EditorGUILayout.HelpBox(
                "Esconder vale só pra Scene view e não é salvo na cena: o jogo continua mostrando os objetos. " +
                "Pra apagar de vez, apague o objeto '" + raizDosObjetos.name + "' na Hierarquia.",
                MessageType.None);
        }

        // Paleta de objetos: um botão por prefab, cria a cópia na cena, no ponto que a Scene view está olhando
        if (gerador.PaletaDeObjetos != null && gerador.PaletaDeObjetos.Count > 0)
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Criar objeto na cena", EditorStyles.boldLabel);

            foreach (GameObject prefab in gerador.PaletaDeObjetos)
            {
                if (prefab == null) continue;
                if (GUILayout.Button(prefab.name)) gerador.CriarObjetoNaCena(prefab);
            }

            EditorGUILayout.HelpBox(
                "O objeto criado fica no objeto 'Objetos' da cena (o gerador nunca apaga o que está ali). " +
                "Arraste na Scene view: ao soltar, ele encaixa na grade e assenta no chão.",
                MessageType.None);
        }
    }
}
