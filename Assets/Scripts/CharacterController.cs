using System.Collections.Generic;
using UnityEngine;

public class CharacterController : MonoBehaviour
{
    public int lane;
    private const int PERFECT_WINDOW = 30;
    private const int GREAT_WINDOW = 60;
    private const int OK_WINDOW = 100;
    private GameManager gm;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lane = 1;
        gm = GameManager.instance;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) {
            lane = lane == 0 ? 1 : 0;
        }
        transform.position = new Vector3(transform.position.x, lane == 0 ? 1.5f : -1.5f, 0f);


        int currentTime = Metronome.instance.GetTimelinePosition();
        if (gm.notes != null && gm.notes.Count > 0) {
            HandleMissedNotes(currentTime);

            if (Input.GetMouseButtonDown(0)) {
                TryHitNotes(currentTime, lane);
            }
        }

    }

    void TryHitNotes(int currentTime, int inputLane)
    {
        foreach (var note in gm.notes) {
            if (!note.gameObject.activeSelf)
                continue;

            int timeDiff = note.data.time - currentTime;
            if (timeDiff > OK_WINDOW)
                break; // las siguientes notas están demasiado adelante en el tiempo

            if (note.data.lane != inputLane)
                continue;

            int delta = Mathf.Abs(timeDiff);
            if (delta <= OK_WINDOW) {
                if (delta <= PERFECT_WINDOW)
                    Debug.Log("Perfect!");
                else if (delta <= GREAT_WINDOW)
                    Debug.Log("Great!");
                else
                    Debug.Log("OK!");

                note.gameObject.SetActive(false);
                break; // o quitar si quieres permitir múltiples hits
            }
        }
    }

    void HandleMissedNotes(int currentTime)
    {
        foreach (var note in gm.notes) {
            if (!note.gameObject.activeSelf)
                continue;

            if (currentTime - note.data.time > OK_WINDOW) {
                Debug.Log($"Miss on lane {note.data.lane} at time {note.data.time}");
                note.gameObject.SetActive(false);
            }
            else if (note.data.time > currentTime) {
                break; // resto de notas aún no se han pasado
            }
        }
    }
}
