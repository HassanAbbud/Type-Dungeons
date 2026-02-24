using System;
using UnityEngine;

public class AccuracyTracker : MonoBehaviour
{
    public static AccuracyTracker Instance { get; private set; }

    // ── Events ────────────────────────────────────────────────────────────────
    /// Fired on every keystroke: (isCorrect, mistakes, totalKeys, lettersTyped, accuracy)
    public event Action<bool, int, int, int, float> OnKeystrokeUpdated;

    /// Fired when a word is completed: (wordAccuracy, sessionAccuracy)
    public event Action<float, float> OnWordAccuracyUpdated;

    // ── Stats (read by HighScoreManager and any UI that wants them) ───────────
    public int Mistakes { get; private set; }
    public int LettersTyped { get; private set; }
    public int TotalKeys => LettersTyped + Mistakes;
    public float Accuracy => TotalKeys == 0 ? 1f : (float)LettersTyped / TotalKeys;
    public string AccuracyPercent => $"{Mathf.RoundToInt(Accuracy * 100)}%";

    private float sessionStartTime;

    public float WordsPerMinute
    {
        get
        {
            float elapsed = Time.time - sessionStartTime;
            if (elapsed < 1f) return 0f;
            return (LettersTyped / 5f) / (elapsed / 60f); // standard WPM
        }
    }


    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        // Hook TypingInput events — FindObjectOfType is fine here since this runs once
        var input = FindFirstObjectByType<TypingInput>();
        if (input != null)
        {
            input.OnCorrectKey += _ => RegisterCorrect();
            input.OnWrongKey += (_, __) => RegisterMistake();
            input.OnWordDone += HandleWordDone;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged += HandleStateChange;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= HandleStateChange;
    }


    private void RegisterCorrect()
    {
        LettersTyped++;
        OnKeystrokeUpdated?.Invoke(true, Mistakes, TotalKeys, LettersTyped, Accuracy);
    }

    private void RegisterMistake()
    {
        Mistakes++;
        OnKeystrokeUpdated?.Invoke(false, Mistakes, TotalKeys, LettersTyped, Accuracy);
    }

    private void HandleWordDone(string word, float wordAccuracy)
    {
        OnWordAccuracyUpdated?.Invoke(wordAccuracy, Accuracy);
    }

    private void HandleStateChange(GameManager.GameState state)
    {
        if (state == GameManager.GameState.Playing)
        {
            Mistakes = 0;
            LettersTyped = 0;
            sessionStartTime = Time.time;
        }
    }

    // ── Summary (used by GameUIManager Game Over panel) ───────────────────────
    public string GetSummaryString() =>
        $"Letters Typed: {LettersTyped}\n" +
        $"Mistakes: {Mistakes}\n" +
        $"Accuracy: {AccuracyPercent}\n" +
        $"WPM: {Mathf.RoundToInt(WordsPerMinute)}";
}