using System.IO;
using System.IO.Compression;
using UnityEngine;

public static class SongPacker
{
    public static void CreateRmblFile(string songDataPath, string audioFilePath, string outputPath)
    {
        // Ensure output directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

        // Create the .rmbl file (which is just a ZIP)
        using (FileStream zipStream = new FileStream(outputPath, FileMode.Create))
        using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Create)) {
            // Add the song data file
            archive.CreateEntryFromFile(songDataPath, Path.GetFileName(songDataPath));

            // Add the audio file
            archive.CreateEntryFromFile(audioFilePath, Path.GetFileName(audioFilePath));
        }

        Debug.Log($"Created .rmbl package at: {outputPath}");
    }

    public static void ExtractRmblFile(string rmblPath, string outputDirectory)
    {
        if (!File.Exists(rmblPath)) {
            Debug.LogError("File not found: " + rmblPath);
            return;
        }

        // Ensure output directory exists
        Directory.CreateDirectory(outputDirectory);

        // Extract all files
        ZipFile.ExtractToDirectory(rmblPath, outputDirectory);
    }
}