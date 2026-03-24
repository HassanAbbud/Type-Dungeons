using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pause menu for the gameplay scene.
/// Mirrors the volume sliders from the Main Menu settings panel.
///
/// SCENE SETUP:
///   Attach to your PauseScreen GameObject.
///   Wire in the Inspector:
///     - sliderSFX  → your BGM/music slider
///     - sliderVFX  → your sound effects slider
///     - btnResume  → Resume button
///     - btnQuit    → Quit to Main Menu button
/// </summary>
public class PauseMenuPanel : MonoBehaviour
{
    [Header("Volume Sliders")]
    [Tooltip("Controls BGM/music volume (labelled SFX in your UI)")]
    [SerializeField] private Slider sliderSFX;

    [Tooltip("Controls sound effects volume (labelled VFX in your UI)")]
    [SerializeField] private Slider sliderVFX;

    [Header("Buttons")]
    [SerializeField] private Button btnResume;
    [SerializeField] private Button btnQuit;

    [Header("Settings")]
    [SerializeField] private string mainMenuSceneName = "MenuScene";

    private void OnEnable()
    {
        // Sync sliders to current values every time panel opens
        if (sliderSFX != null) sliderSFX.value = SoundManager.BGMVolume;
        if (sliderVFX != null) sliderVFX.value = SoundManager.SFXVolume;
    }

    private void Start()
    {
        if (sliderSFX != null)
            sliderSFX.onValueChanged.AddListener((value) =>
            {
                SoundManager.UpdateBGMVolume(value);
                SoundManager.Scale = value;
            });

        if (sliderVFX != null)
            sliderVFX.onValueChanged.AddListener((value) =>
            {
                SoundManager.UpdateSFXVolume(value);
            });

        if (btnResume != null)
            btnResume.onClick.AddListener(() =>
            {
                SoundManager.PlaySound(SoundType.BTN_CLICK);
                GameManager.Instance?.ResumeGame();
                gameObject.SetActive(false);
            });

        if (btnQuit != null)
            btnQuit.onClick.AddListener(() =>
            {
                SoundManager.PlaySound(SoundType.BTN_CLICK);
                Time.timeScale = 1f;
                UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
            });
    }
}