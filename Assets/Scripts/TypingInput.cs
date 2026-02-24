using System;
using System.Collections;
using UnityEngine;
using TMPro;

public class TypingInput : MonoBehaviour
{
    #region Inspector
    [Header("References")]
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
    private bool isProcessingInput = false;

    private GameUIManager uiManager;
    private int correctKeysSession = 0;
    private float sessionStartTime;
    #endregion

    private void Start()
    {
        uiManager = FindObjectOfType<GameUIManager>();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }

        if (typingInputField != null)
        {
            typingInputField.onValueChanged.AddListener(OnInputValueChanged);
            // Crucial: Set to custom to prevent TMP from doing too much "helpful" formatting
            typingInputField.contentType = TMP_InputField.ContentType.Standard;
            typingInputField.lineType = TMP_InputField.LineType.SingleLine;
        }

        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Playing)
        {
            StartCoroutine(StartTypingDelayed());
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;

        if (typingInputField != null)
            typingInputField.onValueChanged.RemoveListener(OnInputValueChanged);
    }

    private void OnInputValueChanged(string newText)
    {
        if (isProcessingInput || !inputActive || string.IsNullOrEmpty(currentWord)) return;

        isProcessingInput = true;

        // 1. Determine what was actually typed
        // If the field is shorter than our index, they backspaced (we don't allow that)
        if (newText.Length <= currentCharIndex)
        {
            ForceCaretToEnd();
            isProcessingInput = false;
            return;
        }

        // Get the latest character
        char typed = newText[newText.Length - 1];
        totalKeysThisWord++;

        char expected = currentWord[currentCharIndex];
        bool isCorrect = ignoreCase
            ? char.ToLower(typed) == char.ToLower(expected)
            : typed == expected;

        if (isCorrect)
        {
            HandleCorrectKey(typed);
        }
        else
        {
            HandleWrongKey(typed, expected);
        }

        isProcessingInput = false;
    }

    private void HandleCorrectKey(char typed)
    {
        currentCharIndex++;
        correctKeysThisWord++;
        correctKeysSession++;

        GameManager.Instance?.RegisterKeystroke(true);
        OnCorrectKey?.Invoke(typed);

        if (uiManager != null)
            uiManager.UpdateTypedProgress(currentWord, currentCharIndex);

        // Update text and force caret
        ForceCaretToEnd();

        if (currentCharIndex >= currentWord.Length)
            CompleteCurrentWord();
    }

    private void HandleWrongKey(char typed, char expected)
    {
        GameManager.Instance?.RegisterKeystroke(false);
        OnWrongKey?.Invoke(typed, expected);

        if (uiManager != null)
            uiManager.FlashWrongKey();

        // Revert text and force caret
        ForceCaretToEnd();

        if (wrongKeyPauseDuration > 0f)
            StartCoroutine(BriefInputPause(wrongKeyPauseDuration));
    }

    /// <summary>
    /// This is the core fix. We wait for the end of the frame to set the text and caret,
    /// which bypasses TMP's internal auto-correction logic.
    /// </summary>
    private void ForceCaretToEnd()
    {
        if (typingInputField == null) return;
        StartCoroutine(YieldSetCaret());
    }

    private IEnumerator YieldSetCaret()
    {
        // Wait for TMP to finish its own internal onValueChanged processing
        yield return new WaitForEndOfFrame();

        string validText = currentWord.Substring(0, currentCharIndex);

        typingInputField.SetTextWithoutNotify(validText);
        typingInputField.caretPosition = currentCharIndex;
        typingInputField.selectionAnchorPosition = currentCharIndex;
        typingInputField.selectionFocusPosition = currentCharIndex;

        // This forces the mesh to update immediately
        typingInputField.ForceLabelUpdate();
    }

    private void CompleteCurrentWord()
    {
        float accuracy = totalKeysThisWord == 0 ? 1f : (float)correctKeysThisWord / totalKeysThisWord;
        GameManager.Instance?.CompleteWord(accuracy, currentWord.Length);
        OnWordDone?.Invoke(currentWord, accuracy);
        LoadNextWord();
    }

    private void LoadNextWord()
    {
        string newWord = GameManager.Instance.RequestNextWord();
        if (string.IsNullOrEmpty(newWord) || newWord == "loading")
        {
            // Simple fallback if word bank is slow
            currentWord = "waiting";
        }
        else
        {
            currentWord = newWord;
        }

        currentCharIndex = 0;
        correctKeysThisWord = 0;
        totalKeysThisWord = 0;

        // Complete reset of the field
        typingInputField.SetTextWithoutNotify("");
        ForceCaretToEnd();

        if (uiManager != null)
            uiManager.DisplayWord(currentWord);

        OnNewWord?.Invoke(currentWord);
    }

    private void HandleGameStateChanged(GameManager.GameState newState)
    {
        inputActive = (newState == GameManager.GameState.Playing);
        typingInputField.interactable = inputActive;

        if (inputActive)
        {
            sessionStartTime = Time.time;
            typingInputField.ActivateInputField();
            ForceCaretToEnd();
        }
    }

    private IEnumerator StartTypingDelayed()
    {
        yield return new WaitForSeconds(0.1f);
        HandleGameStateChanged(GameManager.GameState.Playing);
        LoadNextWord();
    }

    private IEnumerator BriefInputPause(float duration)
    {
        typingInputField.interactable = false;
        yield return new WaitForSecondsRealtime(duration);
        typingInputField.interactable = true;
        typingInputField.ActivateInputField();
        ForceCaretToEnd();
    }
}