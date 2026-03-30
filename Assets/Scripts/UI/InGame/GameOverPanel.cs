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
        score = GameManager.Instance.Score;
        accuracy = GameManager.Instance.Accuracy;

        backToMain.onClick.AddListener(() => {
            Leaderboard.Instance.SaveLeaderboard(InputName.text, score, accuracy);
            SceneManager.LoadScene("MenuScene");
        });
    }

}
