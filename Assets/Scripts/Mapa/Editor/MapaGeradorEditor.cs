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
    }
}
