using UnityEngine;
using System.Collections;
using System.IO;
using SFB;
using FMODUnity;
using FMOD.Studio;
using System.Runtime.InteropServices;
using System;

public class SongUploader : MonoBehaviour
{
    private string savePath;

    [field: SerializeField] public EventReference customSongReference { get; private set; }
    private FMODProgrammerSound programmerSound;

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
        Metronome.instance.ReleaseSongInstance();
        programmerSound = new FMODProgrammerSound(destinationPath, customSongReference);

        Metronome.instance.SetSongInstance(programmerSound.GetEventInstance());
        yield break;
    }
}

public class FMODProgrammerSound
{
    private EventInstance eventInstance;
    private string audioPath;
    private GCHandle handle;

    private EVENT_CALLBACK eventCallback;

    public FMODProgrammerSound(string songPath, EventReference eventRef)
    {
        // Get instances
        audioPath = songPath;
        eventInstance = RuntimeManager.CreateInstance(eventRef);

        // Store GCHandle to this tracker
        handle = GCHandle.Alloc(this);
        IntPtr handlePtr = GCHandle.ToIntPtr(handle);
        eventInstance.setUserData(handlePtr);

        // Set callback
        eventCallback = new EVENT_CALLBACK(ProgrammerSoundCallback);
        eventInstance.setCallback(eventCallback, EVENT_CALLBACK_TYPE.CREATE_PROGRAMMER_SOUND |
                                          EVENT_CALLBACK_TYPE.DESTROY_PROGRAMMER_SOUND |
                                          EVENT_CALLBACK_TYPE.DESTROYED);
    }

    [AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
    private static FMOD.RESULT ProgrammerSoundCallback(EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr parameterPtr)
    {
        // Retrieve the user data
        IntPtr userData;
        EventInstance eventInstance = new EventInstance(instancePtr);
        eventInstance.getUserData(out userData);

        // Get the object to use
        if (userData != IntPtr.Zero) {
            GCHandle handle = GCHandle.FromIntPtr(userData);

            if (type == EVENT_CALLBACK_TYPE.CREATE_PROGRAMMER_SOUND) {
                PROGRAMMER_SOUND_PROPERTIES parameter = (PROGRAMMER_SOUND_PROPERTIES)Marshal.PtrToStructure(parameterPtr, typeof(PROGRAMMER_SOUND_PROPERTIES));

                if (handle.Target is FMODProgrammerSound programmer) {
                    FMOD.Sound sound;
                    RuntimeManager.CoreSystem.createSound(programmer.audioPath, FMOD.MODE.DEFAULT, out sound);
                    parameter.sound = sound.handle;
                }

                Marshal.StructureToPtr(parameter, parameterPtr, false);
            }

            else if (type == EVENT_CALLBACK_TYPE.DESTROY_PROGRAMMER_SOUND) {
                Debug.Log("destroy programmer sound");
                PROGRAMMER_SOUND_PROPERTIES parameter = (PROGRAMMER_SOUND_PROPERTIES)Marshal.PtrToStructure(parameterPtr, typeof(PROGRAMMER_SOUND_PROPERTIES));
                FMOD.Sound sound = new FMOD.Sound(parameter.sound);
                sound.release();
            }
            else if (type == EVENT_CALLBACK_TYPE.DESTROYED) {
                Debug.Log("destroy object");
                handle.Free();
            }
        }

        return FMOD.RESULT.OK;
    }

    public EventInstance GetEventInstance()
    {
        return eventInstance;
    }
}