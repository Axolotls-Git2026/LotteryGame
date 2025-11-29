using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RangeFilterManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField minInput;
    public TMP_InputField maxInput;
    public Button[] digitButtons; // 10 buttons for digits 0–9
    public Toggle allToggle;

    [Header("Output")]
    public TMP_Text resultText; // optional preview

    public List<int> selectedDigits = new List<int>();

    public OutputNumGenerator outputNumGenerator;

    private void Start()
    {
        // Assign listeners to digit buttons
        for (int i = 0; i < digitButtons.Length; i++)
        {
            int digit = i; // capture loop var
            digitButtons[i].onClick.AddListener(() => OnDigitButtonClicked(digit));
        }

        // Input field listeners
        minInput.onValueChanged.AddListener(OnMinInputChanged);
        maxInput.onValueChanged.AddListener((val) => ProcessRange());
    }

    private void OnDigitButtonClicked(int digit)
    {
        if (selectedDigits.Contains(digit))
        {
            selectedDigits.Remove(digit);
            digitButtons[digit].GetComponent<Image>().color = Color.white;
        }
        else
        {
            selectedDigits.Add(digit);
            digitButtons[digit].GetComponent<Image>().color = Color.green;
        }

        // Update allToggle
        if (selectedDigits.Count > 0)
        {
            allToggle.isOn = false; // at least one digit picked ? "All" disabled
        }
        else
        {
            allToggle.isOn = true;  // none picked ? "All" enabled
        }
    }

    public void OnAllToggleClicked(Toggle toggle)
    {
        if (toggle.isOn)
        {
            // Select ALL
            selectedDigits.Clear();
            for (int i = 0; i < digitButtons.Length; i++)
            {
                selectedDigits.Add(i);
                digitButtons[i].GetComponent<Image>().color = Color.green;
            }
        }
        else
        {
            // Deselect ALL
            selectedDigits.Clear();
            for (int i = 0; i < digitButtons.Length; i++)
            {
                digitButtons[i].GetComponent<Image>().color = Color.white;
            }
        }
    }

    private void OnMinInputChanged(string val)
    {
        int requiredLength = (allToggle.isOn || selectedDigits.Count > 0) ? 2 : 3;

        if (val.Length >= requiredLength)
        {
            // Auto shift focus to Max field
            maxInput.Select();
            maxInput.ActivateInputField();
        }
    }

    private void ProcessRange()
    {
        if (outputNumGenerator.straightToggle.isOn || outputNumGenerator.boxToggle.isOn)
        {
            if (!int.TryParse(minInput.text, out int minVal) ||
                !int.TryParse(maxInput.text, out int maxVal))
            {
                Debug.LogError("Invalid range inputs!");
                return;
            }

            if (minVal > maxVal)
            {
                Debug.LogError("Min cannot be greater than Max!");
                return;
            }

            List<string> results = new List<string>();

            if (allToggle.isOn)
            {
                // All digits = 0–9
                for (int d = 0; d <= 9; d++)
                {
                    for (int n = minVal; n <= maxVal; n++)
                    {
                        results.Add($"{d}{n:D2}");
                    }
                }
            }
            else if (selectedDigits.Count > 0)
            {
                // Only selected digits
                foreach (int d in selectedDigits)
                {
                    for (int n = minVal; n <= maxVal; n++)
                    {
                        results.Add($"{d}{n:D2}");
                    }
                }
            }
            else
            {
                // Only selected digits

                for (int n = minVal; n <= maxVal; n++)
                {
                    results.Add($"{n:D2}");
                }

                //  Debug.LogWarning("No digits selected and 'All' is OFF.");
            }

            // Optional: preview in UI
            if (resultText != null)
            {
                resultText.text = string.Join(", ", results);
            }

            // Send results for spawning
            GenerateBulkResults(results);
            minInput.text = "";
            maxInput.text = "";
        }
        else
        {
            ToastManager.Instance.ShowToast("Select Straight Box");
            minInput.text = "";
            maxInput.text = "";
        }
    }

    public void GenerateBulkResults(List<string> numbers)
    {
        foreach (string num in numbers)
        {
            outputNumGenerator.GenerateResults(num);
        }
    }

    public void ResetFields()
    {
        // Clear inputs
        minInput.text = "";
        maxInput.text = "";

        // Reset digit selections
        selectedDigits.Clear();
        foreach (var btn in digitButtons)
        {
            btn.GetComponent<Image>().color = Color.white;
        }

        // Reset toggle
        if (allToggle != null)
            allToggle.isOn = false;

        // Clear preview
        if (resultText != null)
            resultText.text = "";
    }


}
