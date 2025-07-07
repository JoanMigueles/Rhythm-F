using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DifficultySelector : MonoBehaviour
{
    public Difficulty selectedDifficulty;
    [SerializeField] private Color evenColor;
    [SerializeField] private Color oddColor;
    [SerializeField] private List<Color> difficultyColors;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetSelectedDifficulty((int)GameManager.instance.GetSelectedDifficulty());
    }

    public void SetSelectedDifficulty(int diff)
    {
        Difficulty difficulty = (Difficulty)diff;
        selectedDifficulty = difficulty;
        GameManager.instance.SetSelectedDifficulty(selectedDifficulty);
        LevelListUI.instance.DisplayHoveredLeaderboard();
        LevelListUI.instance.DisplayRanks();
        Image[] images = GetComponentsInChildren<Image>();
        bool isEven;
        for (int i = 0; i < images.Length; i++) {
            if (diff == i) {
                images[i].color = difficultyColors[i];
                images[i].GetComponent<Pulsator>().enabled = true;
            }
            else {
                isEven = i % 2 == 0;
                if (isEven) {
                    images[i].color = evenColor;
                }
                else {
                    images[i].color = oddColor;
                }
                images[i].GetComponent<Pulsator>().enabled = false;
            }
        }
        
    }
}
