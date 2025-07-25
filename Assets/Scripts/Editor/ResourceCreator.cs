using UnityEngine;
using UnityEditor;
using System.IO;

public class ResourceCreator : MonoBehaviour
{
    [MenuItem("Tools/Import SongData From Text")]
    public static void ImportSongDataFromText()
    {
        string path = EditorUtility.OpenFilePanel("Select SongData Text File", "", "songdata");
        if (string.IsNullOrEmpty(path)) return;

        // Cargar SongData desde el archivo
        SongData songData = SongFileConverter.LoadFromTextFormat(path);
        if (songData == null) {
            Debug.LogError("Failed to load SongData from file.");
            return;
        }

        // Crear ScriptableObject
        SongDataResource resource = ScriptableObject.CreateInstance<SongDataResource>();
        resource.data = songData;

        // Asegurarse de que la carpeta Resources exista
        string assetPath = "Assets/Resources/SongData.asset";
        Directory.CreateDirectory("Assets/Resources");

        // Guardar el asset
        AssetDatabase.CreateAsset(resource, assetPath);
        AssetDatabase.SaveAssets();

        Debug.Log("SongDataResource saved to " + assetPath);
    }

    [MenuItem("Tools/Export SongData From Asset To Text")]
    public static void ExportSongDataFromAsset()
    {
        // Open file panel to select the ScriptableObject asset
        string assetPath = EditorUtility.OpenFilePanel("Select SongDataResource Asset", "Assets", "asset");
        if (string.IsNullOrEmpty(assetPath)) return;

        // Convert absolute path to relative path (Unity requires this for AssetDatabase)
        string relativePath = "Assets" + assetPath.Substring(Application.dataPath.Length);

        // Load the asset
        SongDataResource resource = AssetDatabase.LoadAssetAtPath<SongDataResource>(relativePath);
        if (resource == null || resource.data == null)
        {
            Debug.LogError("Failed to load SongDataResource from selected file.");
            return;
        }

        // Open a file save panel
        string savePath = EditorUtility.SaveFilePanel("Export SongData To Text", "", "SongDataExport", "songdata");
        if (string.IsNullOrEmpty(savePath)) return;

        // Save the SongData to a text format
        SongFileConverter.SaveToTextFormat(resource.data, savePath);
    }
}
