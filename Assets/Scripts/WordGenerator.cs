using System.Collections.Generic;
using UnityEngine;

public class WordGenerator : MonoBehaviour
{
    [Header("Word Banks")]
    [SerializeField] private List<string> easyWords;
    [SerializeField] private List<string> mediumWords;
    [SerializeField] private List<string> hardWords;

    [Header("Difficulty Settings")]
    [SerializeField] private int mediumLevelStart = 3;
    [SerializeField] private int hardLevelStart = 6;

    public string GetRandomWord(int currentLevel)
    {
        List<string> selectedBank = GetWordBank(currentLevel);

        if (selectedBank == null || selectedBank.Count == 0)
        {
            Debug.LogWarning("Word bank is empty!");
            return "";
        }

        int index = Random.Range(0, selectedBank.Count);
        return selectedBank[index];
    }

    private List<string> GetWordBank(int currentLevel)
    {
        if (currentLevel < mediumLevelStart)
            return easyWords;

        if (currentLevel < hardLevelStart)
            return mediumWords;

        return hardWords;
    }
}
