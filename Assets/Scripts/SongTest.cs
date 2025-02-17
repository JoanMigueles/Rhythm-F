using UnityEngine;
using FMOD.Studio;
using FMODUnity;
using UnityEngine.UI;

public class SongTest : MonoBehaviour
{
    [field: Header("Left Ear")]
    [field: SerializeField] public EventReference rutaCancion { get; private set; }
    [SerializeField] private Slider volumeSlider;

    private EventInstance cancion;

    public EventInstance CreateEventInstance(EventReference eventReference)
    {
        EventInstance eventInstance = RuntimeManager.CreateInstance(eventReference);
        return eventInstance;
    }

    private void Start()
    {
        cancion = CreateEventInstance(rutaCancion);
        cancion.start();
    }

    public void OnChangeVolume()
    {
        Debug.Log("Nuevo volumen, " + volumeSlider.value);
        //cancion.setParameterByName("dB", volumeSlider.value);
        cancion.setTimelinePosition((int)(volumeSlider.value * 1000));
    }
}
