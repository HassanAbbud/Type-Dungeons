using TMPro;
using UnityEngine;

public class Timer : MonoBehaviour
{
    [SerializeField] private TMP_Text labelTime;

    //total time related
    private float totalTime = 0f;
    private int currentTime = 0;

    //count down related
    private float countdownTotalTime = 0f;
    private int currentCountdown = 0;
    private bool isCountdownActive = false;
    void Start()
    {
        labelTime.text = "";
    }

    
    void Update()
    {
        GetTotalTime();


        if (isCountdownActive)
        {
            UpdateCountdown();
        }
    }
    #region BasicTimer
    public void GetTotalTime()
    {
        totalTime += Time.deltaTime;
        currentTime = (int)totalTime;
        labelTime.text = "";

        // hour 
        if (currentTime / 3600 > 0)
        {
            labelTime.text += currentTime / 3600 + ":";
        }
        // minutes: 01 02 ...
        if (currentTime % 3600 / 60 >= 10) labelTime.text += currentTime % 3600 / 60 + ":";
        else labelTime.text += "0" + currentTime % 3600 / 60 + ":";
        // seconds: 01 02 ...
        if (currentTime % 60 >= 10) labelTime.text += currentTime % 60;
        else labelTime.text += "0" + currentTime % 60;
    }
    #endregion

    #region CountDownTimer
    public void StartCountDown(int levelTime)
    {
        countdownTotalTime = levelTime;
        currentCountdown = levelTime;
        isCountdownActive = true;
    }

    public void StopCountdown()
    {
        isCountdownActive = false;
    }

    // reset timer
    public void ResetCountdown()
    {
        isCountdownActive = false;
        countdownTotalTime = 0;
        currentCountdown = 0;
    }

    private void UpdateCountdown()
    {
        countdownTotalTime -= Time.deltaTime;

        // countdown no less than 0
        if (countdownTotalTime <= 0)
        {
            countdownTotalTime = 0;
            isCountdownActive = false;
            // trigger count down over event here
        }

        currentCountdown = (int)countdownTotalTime;
        DisplayCountdownTime();
    }

    private void DisplayCountdownTime()
    {
        int hours = currentCountdown / 3600;
        int minutes = (currentCountdown % 3600) / 60;
        int seconds = currentCountdown % 60;

        string timeString = "";

        // hours
        if (hours > 0)
        {
            timeString += hours + ":";
        }

        // mins
        timeString += minutes.ToString("D2") + ":";

        // seconds
        timeString += seconds.ToString("D2");


        // need UI component for this
        // countdownText.text = timeString;
    }
    #endregion
}
