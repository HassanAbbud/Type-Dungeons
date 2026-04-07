using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverPanel : MonoBehaviour
{
    public Button backToMain;
    public TMP_InputField InputName;
    public int score;
    public float accuracy;

    void Start()
    {
        // Wire button only once
        backToMain.onClick.AddListener(() =>
        {
            Leaderboard.Instance.SaveLeaderboard(InputName.text, score, accuracy);
            Time.timeScale = 1f;
            SceneManager.LoadScene("MenuScene");
        });
    }

    void OnEnable()
    {
        // Refresh stats every time the panel is shown (retry, new game over, etc.)
        if (GameManager.Instance != null)
        {
            score = GameManager.Instance.Score;
            accuracy = GameManager.Instance.Accuracy;
        }

        // Clear the name input for a fresh entry
        if (InputName != null)
            InputName.text = "";
    }
}