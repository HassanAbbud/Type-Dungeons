using UnityEngine;
using UnityEngine.UI;

public class MainPanel : BasePanel<MainPanel>
{
    [SerializeField] private Button btnStart;
    [SerializeField] private Button btnSettings;
    [SerializeField] private Button btnLeaderboard;
    [SerializeField] private Button btnQuit;

    private void Start()
    {
        //TO DO: go to gameScene
        btnStart.onClick.AddListener(() => { });

        //TO DO: go to leaderboard
        btnLeaderboard.onClick.AddListener(() => {
            this.HideMe();
            LeaderboardPanel.Instance.ShowMe();
        });

        //go to settings
        btnSettings.onClick.AddListener(() => { 
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
