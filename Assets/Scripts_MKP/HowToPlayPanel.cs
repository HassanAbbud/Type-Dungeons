using UnityEngine;
using UnityEngine.UI;
public class HowToPlayPanel : BasePanel<HowToPlayPanel>
{
    [SerializeField] private Button btnBack;

    protected override void Awake()
    {
        base.Awake();
        HideMe();
    }

    private void Start()
    {
        btnBack.onClick.AddListener(() =>
        {
            SoundManager.PlaySound(SoundType.BTN_CLICK);
            HideMe();
            MainPanel.Instance.ShowMe();
        });
    }
}
