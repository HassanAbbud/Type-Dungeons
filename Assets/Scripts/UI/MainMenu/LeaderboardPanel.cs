using UnityEngine;
using UnityEngine.UI;

public class LeaderboardPanel : BasePanel<LeaderboardPanel>
{
    [SerializeField] private Button btnBack;

    protected override void Awake()
    {
        base.Awake();
        this.HideMe();
    }

    private void Start()
    {
        btnBack.onClick.AddListener(() => { 
            this.HideMe();
            MainPanel.Instance.ShowMe();
        });
    }
}
