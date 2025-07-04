using UnityEngine;

public class Timeline : MonoBehaviour
{
    private Material material;
    private EditorManager em;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SpriteRenderer img = GetComponent<SpriteRenderer>();
        em = NoteManager.instance as EditorManager;
        material = new Material(img.material);
        img.material = material;

        material.SetFloat("_Tiling", Metronome.instance.GetCurrentBPM() / 60f / EditorManager.instance.noteSpeed);
        material.SetFloat("_Subdivision", em.noteSubdivisionSnapping);
        material.SetFloat("_Offset", -Metronome.instance.GetTimelineBeatPosition());
    }

    // Update is called once per frame
    void Update()
    {
        material.SetFloat("_Tiling", Metronome.instance.GetCurrentBPM() / 60f / EditorManager.instance.noteSpeed);
        material.SetFloat("_Subdivision", em.noteSubdivisionSnapping);
        material.SetFloat("_Offset", -Metronome.instance.GetTimelineBeatPosition());
    }
}
