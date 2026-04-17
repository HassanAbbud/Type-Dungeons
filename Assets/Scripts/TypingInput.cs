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
    private EnemySpawner enemySpawner;
    private int correctKeysSession = 0;
    private float sessionStartTime;
    #endregion

    private void Start()
    {
        uiManager = FindObjectOfType<GameUIManager>();
        enemySpawner = FindObjectOfType<EnemySpawner>();

        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;

        if (typingInputField != null)
        {
            typingInputField.onValueChanged.AddListener(OnInputValueChanged);
            typingInputField.contentType = TMP_InputField.ContentType.Standard;
            typingInputField.lineType = TMP_InputField.LineType.SingleLine;
        }

        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Playing)
            StartCoroutine(StartTypingDelayed());
    }

    private void Update()
    {
        if (!inputActive) return;
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        if (string.IsNullOrEmpty(currentWord) || currentWord == "waiting")
            TryLoadWordFromEnemy();

        if (typingInputField != null && !typingInputField.isFocused && typingInputField.interactable)
            typingInputField.ActivateInputField();
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
        if (currentWord == "waiting") return;

        isProcessingInput = true;

        if (newText.Length <= currentCharIndex)
        {
            ForceCaretToEnd();
            isProcessingInput = false;
            return;
        }

        char typed = newText[newText.Length - 1];
        totalKeysThisWord++;

        char expected = currentWord[currentCharIndex];
        bool isCorrect = ignoreCase
            ? char.ToLower(typed) == char.ToLower(expected)
            : typed == expected;

        if (isCorrect)
            HandleCorrectKey(typed);
        else
            HandleWrongKey(typed, expected);

        isProcessingInput = false;
    }

    private void HandleCorrectKey(char typed)
    {
        currentCharIndex++;
        correctKeysThisWord++;
        correctKeysSession++;

        GameManager.Instance?.RegisterKeystroke(true);
        OnCorrectKey?.Invoke(typed);

        // Hit VFX on enemy
        if (enemySpawner != null)
        {
            var target = enemySpawner.GetCurrentTarget();
            if (target != null)
                target.OnLetterHit();
        }

        if (uiManager != null)
            uiManager.UpdateTypedProgress(currentWord, currentCharIndex);

        ForceCaretToEnd();

        if (currentCharIndex >= currentWord.Length)
            CompleteCurrentWord();
    }

    private void HandleWrongKey(char typed, char expected)
    {
        GameManager.Instance?.RegisterKeystroke(false);
        OnWrongKey?.Invoke(typed, expected);

        SoundManager.PlaySound(SoundType.TYPING_ERROR);

        if (uiManager != null)
            uiManager.FlashWrongKey();

        ForceCaretToEnd();

        if (wrongKeyPauseDuration > 0f)
            StartCoroutine(BriefInputPause(wrongKeyPauseDuration));
    }

    private void ForceCaretToEnd()
    {
        if (typingInputField == null) return;
        StartCoroutine(YieldSetCaret());
    }

    private IEnumerator YieldSetCaret()
    {
        yield return new WaitForEndOfFrame();

        string validText = currentWord.Substring(0, currentCharIndex);

        typingInputField.SetTextWithoutNotify(validText);
        typingInputField.caretPosition = currentCharIndex;
        typingInputField.selectionAnchorPosition = currentCharIndex;
        typingInputField.selectionFocusPosition = currentCharIndex;
        typingInputField.ForceLabelUpdate();
    }

    private void CompleteCurrentWord()
    {
        float accuracy = totalKeysThisWord == 0 ? 1f : (float)correctKeysThisWord / totalKeysThisWord;

        GameManager.Instance?.CompleteWord(accuracy, currentWord.Length);
        OnWordDone?.Invoke(currentWord, accuracy);

        SoundManager.PlaySound(SoundType.WORD_COMPLETE);

        // Tell enemy system the word is done
        if (enemySpawner != null)
        {
            enemySpawner.NotifyWordCompleted();
            StartCoroutine(LoadWordFromEnemyNextFrame());
        }
        else
        {
            LoadNextWordDirect();
        }
    }

    private IEnumerator LoadWordFromEnemyNextFrame()
    {
        currentWord = "";
        currentCharIndex = 0;
        correctKeysThisWord = 0;
        totalKeysThisWord = 0;
        typingInputField.SetTextWithoutNotify("");

        yield return null;

        TryLoadWordFromEnemy();
    }

    private void TryLoadWordFromEnemy()
    {
        if (enemySpawner == null) return;

        string enemyWord = enemySpawner.GetCurrentTargetWord();
        if (!string.IsNullOrEmpty(enemyWord) && enemyWord != currentWord)
            SetWord(enemyWord);
    }

    private void LoadNextWordDirect()
    {
        string newWord = GameManager.Instance?.RequestNextWord();
        if (string.IsNullOrEmpty(newWord) || newWord == "loading")
            newWord = "waiting";

        SetWord(newWord);
    }

    private void SetWord(string word)
    {
        currentWord = word;
        currentCharIndex = 0;
        correctKeysThisWord = 0;
        totalKeysThisWord = 0;

        typingInputField.SetTextWithoutNotify("");
        ForceCaretToEnd();

        if (uiManager != null)
            uiManager.DisplayWord(currentWord);

        OnNewWord?.Invoke(currentWord);
    }

    private void HandleGameStateChanged(GameManager.GameState newState)
    {
        inputActive = (newState == GameManager.GameState.Playing);

        if (typingInputField != null)
            typingInputField.interactable = inputActive;

        if (inputActive)
        {
            sessionStartTime = Time.time;
            if (typingInputField != null)
                typingInputField.ActivateInputField();
            ForceCaretToEnd();
        }
    }

    private IEnumerator StartTypingDelayed()
    {
        yield return new WaitForSeconds(0.3f);
        HandleGameStateChanged(GameManager.GameState.Playing);

        if (enemySpawner != null)
        {
            float timeout = 3f;
            while (timeout > 0f)
            {
                string word = enemySpawner.GetCurrentTargetWord();
                if (!string.IsNullOrEmpty(word))
                {
                    SetWord(word);
                    yield break;
                }
                yield return new WaitForSeconds(0.1f);
                timeout -= 0.1f;
            }
        }

        LoadNextWordDirect();
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