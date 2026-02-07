using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : BasePanel<SettingsPanel>
{
    [SerializeField] private Slider sliderSFX;
    [SerializeField] private Slider sliderVFX;

    [SerializeField] private Button btnBack;

    protected override void Awake()
    {
        base.Awake();
        this.HideMe();
    }

    private void Start()
    {
        //button onclick: back to main
        btnBack.onClick.AddListener(() => { 
            this.HideMe();
            MainPanel.Instance.ShowMe();
        });

        // TO DO: value change
        //sliderSFX.onValueChanged.AddListener((value) => { });
        //sliderVFX.onValueChanged.AddListener((value) => { });
    }
}
