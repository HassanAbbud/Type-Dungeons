using UnityEngine;
using UnityEngine.UI;

public class GameUITutorial : MonoBehaviour
{
    [SerializeField] private Button btnTutorial;
    [SerializeField] private Button btnBack;
    [SerializeField] private GameObject tutorialPanel;

    void Start()
    {
        btnTutorial.onClick.AddListener(ShowTutorial);
        btnBack.onClick.AddListener(HideTutorial);

        HideTutorial(); // Ensure the tutorial panel is hidden at the start
    }

    private void ShowTutorial()
    {
        tutorialPanel.SetActive(true);
        Time.timeScale = 0f; // Pause the game
    }

    private void HideTutorial()
    {
        tutorialPanel.SetActive(false); 
        Time.timeScale = 1f; // Resume the game
    }

    private void OnDestroy()
    {
        btnTutorial.onClick.RemoveListener(ShowTutorial);
        btnBack.onClick.RemoveListener(HideTutorial);
    }
}
