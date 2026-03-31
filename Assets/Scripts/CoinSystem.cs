using System;
using TMPro;
using UnityEngine;

public class CoinSystem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI txtCoin;

    void Start()
    {
        if (GameManager.Instance != null)
        {
            // register the event handler for coin changes
            GameManager.Instance.OnCoinsChanged += OnCoinsChanged;

        }
        else
        {
            Debug.LogError("GameManager instance not found!");
        }
    }

    private void OnCoinsChanged(int coins)
    {
        txtCoin.text = coins.ToString();
    }
}
