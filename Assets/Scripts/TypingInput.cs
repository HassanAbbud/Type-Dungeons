using System;
using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Core typing input for Type Dungeons.
/// Uses a TMP_InputField as the actual input source.
/// The player types into the input field, this script validates each character.
///
/// SETUP: 
///   1. Attach to any GO in scene
///   2. Drag your TMP_InputField into the "Typing Input Field" slot
///   3. That's it — everything else is automatic
/// </summary>
public class TypingInput : MonoBehaviour
{
    #region Inspector
    [Header("References")]
    [Tooltip("The TMP_InputField the player types into")]
    [SerializeField] private TMP_InputField typingInputField;

    [Header("Input Settings")]
    [SerializeField] private bool ignoreCase = true;

    [Header("Feedback")]
    [SerializeField] private float wrongKeyPauseDuration = 0.05f;
    #endregion

    #region Events
    public event Action<char> OnCorrectKey;
    public event Action<char, char> OnWrongKey;
    public event Action<string, float> OnWordDone;
    public event Action<string> OnNewWord;
    #endregion

    #region Runtime State
    private string currentWord = "";
    private int currentCharIndex = 0;
    private int correctKeysThisWord = 0;
    private int totalKeysThisWord = 0;
    private bool inputActive = false;
    private bool isProcessingInput = false; // guard against re-entrant onValueChanged

    private GameUIManager uiManager;

    public string CurrentWord => currentWord;
    public int CurrentCharIndex => currentCharIndex;
    public bool IsInputActive => inputActive;

    public int WordsPerMinute { get; private set; }
    private int correctKeysSession = 0;
    private float sessionStartTime;
    #endregion

    #region Lifecycle

    private void Start()
    {
        uiManager = FindObjectOfType<GameUIManager>();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            GameManager.Instance.OnLevelChanged += HandleLevelChanged;
        }

        // Wire the input field
        if (typingInputField != null)
        {
            typingInputField.onValueChanged.AddListener(OnInputValueChanged);
            typingInputField.contentType = TMP_InputField.ContentType.Standard;
            typingInputField.lineType = TMP_InputField.LineType.SingleLine;
            typingInputField.richText = false;
        }

        // Catch-up: if game already started before we subscribed
        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState == GameManager.GameState.Playing &&
            !inputActive)
        {
            StartCoroutine(StartTypingDelayed());
        }
    }

    private void Update()
    {
        if (!inputActive) return;
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        // Keep focus on the input field at all times during gameplay
        if (typingInputField != null && !typingInputField.isFocused)
            StartCoroutine(DelayedFocus());

        UpdateWPM();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            GameManager.Instance.OnLevelChanged -= HandleLevelChanged;
        }

        if (typingInputField != null)
            typingInputField.onValueChanged.RemoveListener(OnInputValueChanged);
    }

    #endregion

    #region Input Processing

    private void OnInputValueChanged(string newText)
    {
        // Guard: don't process if we're the ones changing the text
        if (isProcessingInput) return;
        if (!inputActive) return;
        if (string.IsNullOrEmpty(currentWord)) return;
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        isProcessingInput = true;

        try
        {
            // If text was cleared or is shorter (backspace), just reset to valid portion
            if (string.IsNullOrEmpty(newText) || newText.Length <= currentCharIndex)
            {
                typingInputField.SetTextWithoutNotify(currentWord.Substring(0, currentCharIndex));
                typingInputField.caretPosition = currentCharIndex;
                return;
            }

            // Only process the newest character
            char typed = newText[newText.Length - 1];

            if (char.IsControl(typed))
            {
                typingInputField.SetTextWithoutNotify(currentWord.Substring(0, currentCharIndex));
                typingInputField.caretPosition = currentCharIndex;
                return;
            }

            totalKeysThisWord++;

            char expected = currentWord[currentCharIndex];
            bool isCorrect = ignoreCase
                ? char.ToLower(typed) == char.ToLower(expected)
                : typed == expected;

            if (isCorrect)
            {
                correctKeysThisWord++;
                correctKeysSession++;
                currentCharIndex++;

                GameManager.Instance.RegisterKeystroke(true);
                OnCorrectKey?.Invoke(typed);

                // Set field to matched portion (using the word's own casing)
                typingInputField.SetTextWithoutNotify(currentWord.Substring(0, currentCharIndex));
                typingInputField.caretPosition = currentCharIndex;

                if (uiManager != null)
                    uiManager.UpdateTypedProgress(currentWord, currentCharIndex);

                if (currentCharIndex >= currentWord.Length)
                    CompleteCurrentWord();
            }
            else
            {
                GameManager.Instance.RegisterKeystroke(false);
                OnWrongKey?.Invoke(typed, expected);

                // Reject: revert to valid text
                typingInputField.SetTextWithoutNotify(currentWord.Substring(0, currentCharIndex));
                typingInputField.caretPosition = currentCharIndex;

                if (uiManager != null)
                    uiManager.FlashWrongKey();

                if (wrongKeyPauseDuration > 0f)
                    StartCoroutine(BriefInputPause(wrongKeyPauseDuration));
            }
        }
        finally
        {
            isProcessingInput = false;
        }
    }

    #endregion

    #region Word Management

    private void CompleteCurrentWord()
    {
        float accuracy = totalKeysThisWord == 0
            ? 1f
            : (float)correctKeysThisWord / totalKeysThisWord;

        GameManager.Instance.CompleteWord(accuracy, currentWord.Length);
        OnWordDone?.Invoke(currentWord, accuracy);

        LoadNextWord();
    }

    private void LoadNextWord()
    {
        string newWord = GameManager.Instance.RequestNextWord();

        if (newWord == "loading" || newWord == "error" || string.IsNullOrEmpty(newWord))
        {
            StartCoroutine(WaitForWordBank());
            return;
        }

        SetCurrentWord(newWord);
    }

    private void SetCurrentWord(string word)
    {
        currentWord = word;
        currentCharIndex = 0;
        correctKeysThisWord = 0;
        totalKeysThisWord = 0;

        isProcessingInput = true;
        if (typingInputField != null)
        {
            typingInputField.SetTextWithoutNotify("");
            typingInputField.caretPosition = 0;
        }
        isProcessingInput = false;

        if (uiManager != null)
            uiManager.DisplayWord(currentWord);

        // Focus after a frame so Unity has time to process
        StartCoroutine(DelayedFocus());

        OnNewWord?.Invoke(currentWord);

        Debug.Log($"[TypingInput] New word: \"{currentWord}\"");
    }

    /// <summary>
    /// Waits for WordGenerator to finish loading JSON, then gets the first word.
    /// Uses WaitForSecondsRealtime so it works even if timeScale is weird.
    /// </summary>
    private IEnumerator WaitForWordBank()
    {
        if (uiManager != null)
            uiManager.DisplayWord("loading...");

        float timeout = 5f;
        while (timeout > 0f)
        {
            yield return new WaitForSecondsRealtime(0.15f);
            timeout -= 0.15f;

            string word = GameManager.Instance.RequestNextWord();
            if (word != "loading" && word != "error" && !string.IsNullOrEmpty(word))
            {
                SetCurrentWord(word);
                yield break;
            }
        }

        // Absolute fallback
        Debug.LogWarning("[TypingInput] Word bank never loaded, using fallback.");
        SetCurrentWord("type");
    }

    #endregion

    #region Game State

    private void HandleGameStateChanged(GameManager.GameState newState)
    {
        switch (newState)
        {
            case GameManager.GameState.Playing:
                if (!inputActive)
                    StartCoroutine(StartTypingDelayed());
                else
                    EnableInputField(true);
                break;

            case GameManager.GameState.Paused:
                EnableInputField(false);
                break;

            case GameManager.GameState.GameOver:
                inputActive = false;
                EnableInputField(false);
                break;

            case GameManager.GameState.MainMenu:
                inputActive = false;
                EnableInputField(false);
                break;
        }
    }

    private void HandleLevelChanged(int newLevel)
    {
        if (inputActive) LoadNextWord();
    }

    /// <summary>
    /// Delayed start — waits 1 frame so Unity finishes layout,
    /// then activates input and loads the first word.
    /// </summary>
    private IEnumerator StartTypingDelayed()
    {
        // Wait 2 frames for Unity to finish layout and rendering
        yield return null;
        yield return null;

        inputActive = true;
        correctKeysSession = 0;
        sessionStartTime = Time.time;

        EnableInputField(true);
        LoadNextWord();
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Focus the input field after a 1-frame delay.
    /// ActivateInputField() doesn't work on the same frame as enabling.
    /// </summary>
    private IEnumerator DelayedFocus()
    {
        yield return null; // wait 1 frame
        if (typingInputField != null && typingInputField.interactable)
        {
            typingInputField.ActivateInputField();
            typingInputField.Select();
        }
    }

    private void EnableInputField(bool enabled)
    {
        if (typingInputField == null) return;
        typingInputField.interactable = enabled;

        if (enabled)
            StartCoroutine(DelayedFocus());
        else
            typingInputField.DeactivateInputField();
    }

    private void UpdateWPM()
    {
        float elapsed = Time.time - sessionStartTime;
        if (elapsed > 0f)
        {
            float minutes = elapsed / 60f;
            WordsPerMinute = Mathf.RoundToInt(
                (correctKeysSession / 5f) / Mathf.Max(minutes, 0.01f)
            );
        }
    }

    private IEnumerator BriefInputPause(float duration)
    {
        if (typingInputField != null)
            typingInputField.interactable = false;

        yield return new WaitForSecondsRealtime(duration);

        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState == GameManager.GameState.Playing)
        {
            if (typingInputField != null)
            {
                typingInputField.interactable = true;
                StartCoroutine(DelayedFocus());
            }
        }
    }

    #endregion
}