using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : BasePanel<SettingsPanel>
{
    [Header("Sliders")]
    [Tooltip("SFX slider in your UI = controls BGM/music volume")]
    [SerializeField] private Slider sliderSFX;

    [Tooltip("VFX slider in your UI = controls sound effects volume")]
    [SerializeField] private Slider sliderVFX;

    [SerializeField] private Button btnBack;

    protected override void Awake()
    {
        base.Awake();

        // Init slider positions to match current SoundManager values
        if (sliderSFX != null) sliderSFX.value = SoundManager.BGMVolume;
        if (sliderVFX != null) sliderVFX.value = SoundManager.SFXVolume;

        this.HideMe();
    }

    private void Start()
    {
        btnBack.onClick.AddListener(() =>
        {
            SoundManager.PlaySound(SoundType.BTN_CLICK);
            this.HideMe();
            MainPanel.Instance.ShowMe();
        });

        // SFX slider in UI = music/BGM
        sliderSFX.onValueChanged.AddListener((value) =>
        {
            SoundManager.UpdateBGMVolume(value);
            SoundManager.Scale = value;
        });

        // VFX slider in UI = sound effects (was commented out before — now fixed)
        sliderVFX.onValueChanged.AddListener((value) =>
        {
            SoundManager.UpdateSFXVolume(value);
        });
    }
}