using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.VisualScripting.FullSerializer;
using UnityEditor.Overlays;
using UnityEngine;

public class Leaderboard
{
    #region singleton
    private static Leaderboard _instance = new Leaderboard();
    public static Leaderboard Instance => _instance;
    private Leaderboard() { }
    #endregion

    LeaderboardList dataList = new LeaderboardList();
    private string filePath => Application.persistentDataPath + "/leaderboard.json";


    #region public method
    public void SaveLeaderboard(string playerName, int Score, float accuracy)
    {
        // Load existing data if exists, otherwise create a new list
        if (File.Exists(filePath))
        {
            string str = File.ReadAllText(filePath);
            dataList = JsonUtility.FromJson<LeaderboardList>(str);
        }
        else
        {
            dataList = new LeaderboardList();
        }

        // Add new entry and sort the list
        LeaderboardData entry = new LeaderboardData(playerName, Score, accuracy);
        dataList.entries.Add(entry);
        dataList.entries = dataList.entries.OrderByDescending(x => x.score)
                                                 .ThenByDescending(x => x.accuracy)
                                                 .ToList();

        if (dataList.entries.Count > 10)
        {
            dataList.entries.RemoveRange(10, dataList.entries.Count - 100);
        }

        // save to file
        string json = JsonUtility.ToJson(dataList,true);
        File.WriteAllText(filePath, json);
        
        Debug.Log(filePath);
    }   



    public List<LeaderboardData> GetLeaderboard(int count = 10)
    {
        if (File.Exists(filePath))
        {
            string str = File.ReadAllText(filePath);
            dataList = JsonUtility.FromJson<LeaderboardList>(str);
            return dataList.entries.Take(count).ToList();
        }
        return new List<LeaderboardData>();
    }
    #endregion
}

[System.Serializable]
public class LeaderboardData
{
    public string playerName;
    public int score;
    public float accuracy;

    public LeaderboardData(string playerName, int score, float accuracy)
    {
        this.playerName = playerName;
        this.score = score;
        this.accuracy = accuracy;
    }
}

[System.Serializable]
public class LeaderboardList
{
    public List<LeaderboardData> entries = new List<LeaderboardData>();
}