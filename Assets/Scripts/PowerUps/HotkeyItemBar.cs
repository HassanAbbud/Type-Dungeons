using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HotkeyItemBar : MonoBehaviour
{
    [Header("Item Bar Buttons")]
    public Button[] slotButtons;

    [Header("Input Field")]
    public TMP_InputField targetInputField;

    void Start()
    {
        if (targetInputField != null)
        {
            // Block digit input, no focus control needed
            targetInputField.onValidateInput += ValidateNoDigits;
        }
    }

    private char ValidateNoDigits(string text, int charIndex, char addedChar)
    {
        // Block digits directly, never display them
        if (char.IsDigit(addedChar))
        {
            return '\0';
        }
        return addedChar;
    }

    void Update()
    {
        for (int i = 0; i < slotButtons.Length; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                HandleNumberInput(i);
                break;
            }
        }
    }

    void HandleNumberInput(int slotIndex)
    {
        // Trigger the button
        if (slotButtons[slotIndex] != null)
        {
            slotButtons[slotIndex].onClick.Invoke();
        }

        Debug.Log($"Triggered item bar slot {slotIndex + 1}");
    }
}