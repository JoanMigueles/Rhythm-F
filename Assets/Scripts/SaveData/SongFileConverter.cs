using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

public static class SongFileConverter
{
    public static void SaveToTextFormat(SongData songData, string filePath)
    {
        using (StreamWriter writer = new StreamWriter(filePath)) {
            // Write Song Data section
            writer.WriteLine("[Song Data]");
            writer.WriteLine($"Name:{songData.metadata.songName}");
            writer.WriteLine($"Artist:{songData.metadata.artist}");
            writer.WriteLine($"GUID:{songData.metadata.songGUID}");
            writer.WriteLine($"AudioFile:{songData.metadata.audioFileName}");
            writer.WriteLine($"CoverFile:{songData.metadata.coverFileName}");
            writer.WriteLine($"PreviewStart:{songData.metadata.previewStartTime}");
            writer.WriteLine($"StageBackground:{songData.metadata.stage}");
            writer.WriteLine();

            // Write BPM Changes section
            writer.WriteLine("[BPM Flags]");
            foreach (var bpmChange in songData.BPMFlags) {
                writer.WriteLine($"{bpmChange.offset}:{bpmChange.BPM}");
            }
            writer.WriteLine();

            // Write Easy Notes section
            writer.WriteLine("[Easy Notes]");
            foreach (NoteData note in songData.easyNotes) {
                writer.WriteLine($"{note.time}:{note.lane}:{note.type}:{note.duration}");
            }
            writer.WriteLine();

            // Write Normal Notes section
            writer.WriteLine("[Normal Notes]");
            foreach (NoteData note in songData.normalNotes) {
                writer.WriteLine($"{note.time}:{note.lane}:{note.type}:{note.duration}");
            }
            writer.WriteLine();

            // Write Hard Notes section
            writer.WriteLine("[Hard Notes]");
            foreach (NoteData note in songData.hardNotes) {
                writer.WriteLine($"{note.time}:{note.lane}:{note.type}:{note.duration}");
            }
            writer.WriteLine();

            // Write Rumble Notes section
            writer.WriteLine("[Rumble Notes]");
            foreach (NoteData note in songData.rumbleNotes) {
                writer.WriteLine($"{note.time}:{note.lane}:{note.type}:{note.duration}");
            }
            writer.WriteLine();
        }
    }

    public static SongData LoadFromTextFormat(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("File not found", filePath);

        SongData songData = new SongData();
        songData.metadata.localPath = filePath;
        string currentSection = "";

        foreach (string line in File.ReadAllLines(filePath)) {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Check for section headers
            if (line.StartsWith("[")) {
                currentSection = line.Trim('[', ']');
                continue;
            }

            switch (currentSection) {
                case "Song Data":
                    ParseSongData(line, songData);
                    break;
                case "BPM Flags":
                    ParseBpmChange(line, songData);
                    break;
                case "Easy Notes":
                    ParseNote(line, songData.easyNotes);
                    break;
                case "Normal Notes":
                    ParseNote(line, songData.normalNotes);
                    break;
                case "Hard Notes":
                    ParseNote(line, songData.hardNotes);
                    break;
                case "Rumble Notes":
                    ParseNote(line, songData.rumbleNotes);
                    break;
            }
        }

        return songData;
    }

    private static void ParseSongData(string line, SongData songData)
    {
        string[] parts = line.Split(':', 2);
        if (parts.Length != 2) return;

        string key = parts[0].Trim();
        string value = parts[1].Trim();

        switch (key) {
            case "Name":
                songData.metadata.songName = value;
                break;
            case "Artist":
                songData.metadata.artist = value;
                break;
            case "GUID":
                songData.metadata.songGUID = Guid.Parse(value);
                break;
            case "AudioFile":
                songData.metadata.audioFileName = value;
                break;
            case "CoverFile":
                songData.metadata.coverFileName = value;
                break;
            case "PreviewStart":
                int.TryParse(value, out songData.metadata.previewStartTime);
                break;
            case "StageBackground":
                Enum.TryParse(value, out songData.metadata.stage);
                break;
        }
    }

    private static void ParseBpmChange(string line, SongData songData)
    {
        string[] parts = line.Split(':');
        if (parts.Length != 2) return;

        if (int.TryParse(parts[0], out int offset) &&
            float.TryParse(parts[1], out float bpm)) {
            songData.BPMFlags.Add(new BPMFlag { offset = offset, BPM = bpm });
        }
    }

    private static void ParseNote(string line, List<NoteData> songDataNotes)
    {
        string[] parts = line.Split(':');
        if (parts.Length != 4) return;

        int.TryParse(parts[0], out int time);
        int.TryParse(parts[1], out int lane);
        Enum.TryParse(parts[2], out NoteType type);
        int.TryParse(parts[3], out int duration);

        NoteData note = new NoteData(time, lane, type, duration);
        songDataNotes.Add(note);
    }
}