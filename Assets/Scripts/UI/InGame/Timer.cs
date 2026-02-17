using TMPro;
using UnityEngine;

public class Timer : MonoBehaviour
{
    [SerializeField] private TMP_Text labelTime;

    private float totalTime = 0f;
    private int currentTime = 0;
    void Start()
    {
        labelTime.text = "";
    }

    
    void Update()
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
}
