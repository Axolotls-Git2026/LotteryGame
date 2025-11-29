using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class VirtualKeyboard : MonoBehaviour
{
    private TMP_InputField currentInput;

    void Update()
    {
        // Check if a TMP_InputField is selected this frame
        GameObject selectedObj = EventSystem.current.currentSelectedGameObject;
        if (selectedObj != null)
        {
            TMP_InputField tmpInput = selectedObj.GetComponent<TMP_InputField>();
            if (tmpInput != null)
            {
                currentInput = tmpInput; // cache it
            }
        }
    }

    public void InsertText(string key)
    {
        if (currentInput != null)
        {
            string newText = currentInput.text + key;

            // Respect character limit (check the final string)
            if (currentInput.characterLimit > 0 && newText.Length > currentInput.characterLimit)
                return;

            currentInput.text = newText;

            currentInput.ForceLabelUpdate();
            currentInput.MoveTextEnd(false);

            // Fire event so listeners like OnSingleInputChanged work
            currentInput.onValueChanged?.Invoke(newText);
        }
    }



    public void Backspace()
    {
        if (currentInput != null && currentInput.text.Length > 0)
        {
            string newText = currentInput.text.Substring(0, currentInput.text.Length - 1);

            // Only update if changed
            if (newText != currentInput.text)
            {
                currentInput.text = newText;
                currentInput.ForceLabelUpdate();
                currentInput.MoveTextEnd(false);
                currentInput.onValueChanged?.Invoke(newText);
            }
        }
    }

    public void Clear()
    {
        if (currentInput != null && !string.IsNullOrEmpty(currentInput.text))
        {
            currentInput.text = "";

            currentInput.ForceLabelUpdate();
            currentInput.MoveTextEnd(false);

            currentInput.onValueChanged?.Invoke("");
        }
    }



}
