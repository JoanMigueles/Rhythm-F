using System.Collections;
using TMPro;
using UnityEngine;

public class Dialogue : MonoBehaviour
{
    public TMP_Text speechText;
    public string[] lines;
    public float characterInterval = 0.05f;
    public int index;

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && DialogueMissionManager.instance.CanAdvanceDialogue()) {
            if (speechText.text == lines[index]) {
                NextLine();
            }
            else {
                StopAllCoroutines();
                speechText.text = lines[index];

                // Trigger wait after mission marker
                if (lines[index].Contains("[")) {
                    DialogueMissionManager.instance.WaitForNextMission();
                }
            }
        }
    }

    public void StartDialogue()
    {
        index = 0;
        gameObject.SetActive(true);
        DialogueMissionManager.instance.SetState(DialogState.Active);
        StartCoroutine(TypeLine());
    }

    IEnumerator TypeLine()
    {
        speechText.text = "";
        foreach (char c in lines[index].ToCharArray()) {
            speechText.text += c;
            yield return new WaitForSeconds(characterInterval);
        }

        // Trigger wait after mission marker
        if (lines[index].Contains("[")) {
            DialogueMissionManager.instance.WaitForNextMission();
        }
    }

    public bool NextLine()
    {
        if (DialogueMissionManager.instance.state == DialogState.WaitingForCondition)
            return false; // Don’t advance if waiting

        if (index < lines.Length - 1) {
            index++;
            StartCoroutine(TypeLine());
            return false;
        }
        else {
            DialogueMissionManager.instance.SetState(DialogState.Inactive);
            StartCoroutine(GameplayUI.instance.WinLevel());
            GetComponent<CanvasGroup>().alpha = 0;
            return true;
        }
    }
}