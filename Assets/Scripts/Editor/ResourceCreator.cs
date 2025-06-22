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
}
