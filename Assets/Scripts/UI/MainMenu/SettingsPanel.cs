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
        sliderSFX.value = SoundManager.Scale;
        this.HideMe();
        
    }

    private void Start()
    {
        //button onclick: back to main
        btnBack.onClick.AddListener(() => {
            SoundManager.PlaySound(SoundType.BTN_CLICK);
            this.HideMe();
            MainPanel.Instance.ShowMe();
        });

        // TO DO: value change        
        sliderSFX.onValueChanged.AddListener((value) => {            
            SoundManager.UpdateBGMVolume(value);
            SoundManager.Scale = value;
        });
        //sliderVFX.onValueChanged.AddListener((value) => { });
    }
}
