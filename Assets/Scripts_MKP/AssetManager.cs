using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum GameMode
{
    Adult,
    Teenager,
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

    void Start()
    {

    }

    
    void Update()
    {
        
    }

    public static GameMode GetGameMode()
    {
        //Debug.Log((GameMode)settingsDropdown.value);
        return gameMode;
    }

    public void SetGameMode(int value)
    {
        //Debug.Log("Dropdown reference is: " + settingsDropdown);
        gameMode = (GameMode)value;
        //Debug.Log("Game mode changed to: " + gameMode);
        OnModeChanged?.Invoke();
    }

    public static void BindImage(Image image)
    {
        instance.backgroundImg = image;
        instance.SetBackground();
    }

    /* React to Game Event */
    //private void OnEnable()
    //{
    //    SceneManager.sceneLoaded += LoadNewScene;
    //}

    //private void OnDisable()
    //{
    //    SceneManager.sceneLoaded -= LoadNewScene;
    //}

    //private void LoadNewScene(Scene scene, LoadSceneMode mode)
    //{
    //    if (backgroundImg == null) return;
    //    SetBackground();
    //}
    private void SetBackground()
    {
        if (backgroundImg == null) return;

        backgroundImg.sprite = backgrounds[(int)gameMode];
    }
}
