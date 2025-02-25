using UnityEngine;
using System.Collections;
using System.IO;
using SFB;
using FMODUnity;
using FMOD.Studio;
using System.Runtime.InteropServices;
using System;
using System.Data;

public class SongUploader : MonoBehaviour
{
    private string savePath;

    [field: SerializeField] public EventReference eventReference { get; private set; }
    private EventInstance eventInstance;

    void Start()
    {
        savePath = Path.Combine(Application.persistentDataPath, "CustomCharts");
        if (!Directory.Exists(savePath))
            Directory.CreateDirectory(savePath);
    }

    public void OpenFileExplorer()
    {
        var extensions = new[] { new ExtensionFilter("Audio Files", "mp3", "wav", "ogg") };
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Select Audio File", "", extensions, false);

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0])) {
            StartCoroutine(LoadAndSaveAudio(paths[0]));
        }
    }

    private IEnumerator LoadAndSaveAudio(string filePath)
    {
        string fileName = Path.GetFileName(filePath);
        string destinationPath = Path.Combine(savePath, fileName);

        if (!File.Exists(destinationPath)) {
            File.Copy(filePath, destinationPath);
            Debug.Log("File saved to: " + destinationPath);
        }

        // Create a new FMOD sound instance
        var programmerSound = new FMODProgrammerSound(destinationPath, eventReference);
        eventInstance = programmerSound.GetEventInstance();
        eventInstance.start();
        yield break;
    }
}

public class FMODProgrammerSound
{
    private static FMOD.Sound sound;
    private static string audioPath;
    private EventInstance eventInstance;
    private EVENT_CALLBACK eventCallback;

    public FMODProgrammerSound(string songPath, EventReference eventRef)
    {
        // Ruta del audio en StreamingAssets
        audioPath = songPath;
        eventInstance = RuntimeManager.CreateInstance(eventRef);
        eventCallback = new EVENT_CALLBACK(ProgrammerSoundCallback);
        eventInstance.setCallback(eventCallback, EVENT_CALLBACK_TYPE.CREATE_PROGRAMMER_SOUND);
    }

    [AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
    private FMOD.RESULT ProgrammerSoundCallback(EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr parameterPtr)
    {
        if (type == EVENT_CALLBACK_TYPE.CREATE_PROGRAMMER_SOUND) {
            var instance = new EventInstance(instancePtr);
            var parameter = (PROGRAMMER_SOUND_PROPERTIES)Marshal.PtrToStructure(parameterPtr, typeof(PROGRAMMER_SOUND_PROPERTIES));

            FMOD.Sound sound;
            var system = RuntimeManager.CoreSystem;
            system.createSound(audioPath, FMOD.MODE.DEFAULT, out sound);
            parameter.sound = sound.handle;

            Marshal.StructureToPtr(parameter, parameterPtr, false);
        }

        else if (type == EVENT_CALLBACK_TYPE.DESTROY_PROGRAMMER_SOUND) {
            sound.release();
        }

        return FMOD.RESULT.OK;
    }

    public EventInstance GetEventInstance()
    {
        return eventInstance;
    }
}