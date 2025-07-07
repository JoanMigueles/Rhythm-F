using System.Collections;
using TMPro;
using UnityEngine;

public class Dialogue : MonoBehaviour
{

    public TMP_Text speechText;
    public string[] lines;
    public float characterInterval;
    private Coroutine lineCoroutine;

    public int index;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartDialogue()
    {
        index = 0;
        lineCoroutine = StartCoroutine(TypeLine());
    }

    IEnumerator TypeLine()
    {
        foreach (char c in lines[index].ToCharArray()) {
            speechText.text += c;
            yield return new WaitForSeconds(characterInterval);
        }
    }
}
