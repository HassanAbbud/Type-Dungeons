using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class MainPanel : BasePanel<MainPanel>
{
    [SerializeField] private Button btnStart;
    [SerializeField] private Button btnSettings;
    [SerializeField] private Button btnLeaderboard;
    [SerializeField] private Button btnQuit;

    private void Start()
    {

        //go to gameScene
        btnStart.onClick.AddListener(() =>
        {
            SoundManager.PlaySound(SoundType.BTN_CLICK);
            SoundManager.PlaySound(SoundType.ANNOUNCER, AnnouncerType.GAME_START);
            SceneManager.LoadScene("GameplayScene");
        });

        //TO DO: go to leaderboard
        btnLeaderboard.onClick.AddListener(() => {
            SoundManager.PlaySound(SoundType.BTN_CLICK);
            this.HideMe();
            LeaderboardPanel.Instance.ShowMe();
        });

        //go to settings
        btnSettings.onClick.AddListener(() => {
            SoundManager.PlaySound(SoundType.BTN_CLICK);
            this.HideMe();
            SettingsPanel.Instance.ShowMe();
        });

        //quit game
        btnQuit.onClick.AddListener(() => {
            if (Application.isEditor)
            {
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #endif
            }
            else
            {
                Application.Quit();
            }
        });
    }

  
}
