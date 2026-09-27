using UnityEditor;
using UnityEngine;

// Qualquer imagem colocada em Assets/Art/Mapa/ é importada já pronta para o MapaGerador.
public class MapaImportador : AssetPostprocessor
{
    const string PastaMapas = "Assets/Art/Mapa/";

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(PastaMapas)) return;

        var importador = (TextureImporter)assetImporter;
        importador.textureType = TextureImporterType.Default;
        importador.isReadable = true;
        // Sem isso o Unity redimensiona para potencia de 2 (12x23 vira 16x32) e mistura as cores
        importador.npotScale = TextureImporterNPOTScale.None;
        importador.mipmapEnabled = false;
        importador.filterMode = FilterMode.Point;
        importador.textureCompression = TextureImporterCompression.Uncompressed;

        var android = importador.GetPlatformTextureSettings("Android");
        android.overridden = false;
        importador.SetPlatformTextureSettings(android);
    }
}
