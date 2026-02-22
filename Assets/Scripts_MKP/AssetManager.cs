using TMPro;
using UnityEngine;

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

    public GameMode GetGameMode()
    {
        //Debug.Log((GameMode)settingsDropdown.value);
        return gameMode;
    }

    public void SetGameMode(int value)
    {
        //Debug.Log("Dropdown reference is: " + settingsDropdown);
        gameMode = (GameMode)value;
        //Debug.Log("Game mode changed to: " + gameMode);
    }
}
