using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static HighScoreManager;

public class LeaderboardUI : MonoBehaviour
{
    [Header("Settings")]
    //public string filePath = "leaderboard.json"; 
    public int maxDisplayCount = 8;

    // use UI prefab to display the leaderboard entry
    [Header("Entry parent and prefab")]
    public Transform contentParent;
    public GameObject entryPrefab;

    void Start()
    {
        Invoke(nameof(LoadAndDisplayLeaderboard), 0.1f);
    }

    private void LoadAndDisplayLeaderboard()
    {
        List<LeaderboardData> sortedList = Leaderboard.Instance.GetLeaderboard(maxDisplayCount);
        UpdateUI(sortedList);
    }

    private void UpdateUI(List<LeaderboardData> sortedList)
    {
        // 1. clear previous UI
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        int displayCount = Mathf.Min(sortedList.Count, maxDisplayCount);

        for (int i = 0; i < displayCount; i++)
        {

            GameObject entryObj = Instantiate(entryPrefab, contentParent);

            entryObj.transform.Find("txt_rank").GetComponent<TMP_Text>().text = (i + 1).ToString();
            entryObj.transform.Find("txt_name").GetComponent<TMP_Text>().text = sortedList[i].playerName;
            entryObj.transform.Find("txt_score").GetComponent<TMP_Text>().text = sortedList[i].score.ToString();
            entryObj.transform.Find("txt_accuracy").GetComponent<TMP_Text>().text = $"{sortedList[i].accuracy * 100:F3}%"; 
        }
    }
}
