using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum GameMode
{
    Adult,
    Kid
}

public class AssetManager : MonoBehaviour
{
    private static AssetManager instance;
    private static GameMode gameMode = 0;

    [Header("UI References")]
    public TMP_Dropdown settingsDropdown;

    private Image backgroundImg;

    public Sprite[] backgrounds;

    public static event Action OnModeChanged;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static GameMode GetGameMode()
    {
        return gameMode;
    }

    public void SetGameMode(int value)
    {
        gameMode = (GameMode)value;
        OnModeChanged?.Invoke();
        SetBackground(); // Update background immediately if bound
    }

    public static void BindImage(Image image)
    {
        instance.backgroundImg = image;
        instance.SetBackground();
    }

    private void SetBackground()
    {
        if (backgroundImg == null || backgrounds == null || backgrounds.Length <= (int)gameMode)
            return;

        backgroundImg.sprite = backgrounds[(int)gameMode];
    }
}