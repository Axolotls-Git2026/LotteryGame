using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

[System.Serializable]
public class SlotsResponse
{
    public string status;
    public List<string> slots;
}

public class AdvanceTime : MonoBehaviour
{
    public GameObject prefab;
    public Transform content;
    public TMP_Text drawTime;
    public TMP_Text previousTime;
    public TMP_InputField quantityToSelect;

    // Changed to a list to handle multiple selections
    public List<Toggle> toggleList;
    // New list to store the text from selected toggles
    public List<string> selectedTimes = new List<string>();
    public int totalCount;
    private void OnToggleChanged(bool isOn, string _txt, Toggle currentToggle)
    {
        // Add or remove the text based on the toggle's state
        if (isOn)
        {
            if (!selectedTimes.Contains(_txt))
            {
                selectedTimes.Add(_txt);
            }
        }
        else
        {
            if (selectedTimes.Contains(_txt))
            {
                selectedTimes.Remove(_txt);
            }
        }

        // Update the drawTime text to reflect all selected times
        UpdateDrawTimeText();
    }

    private void UpdateDrawTimeText()
    {
        if (selectedTimes.Count > 0)
        {
          //  drawTime.text = string.Join(", ", selectedTimes);
        }
        else
        {
             drawTime.text = previousTime.text;
        }
    }

    // New method to select toggles based on quantity input
    public void SelectTogglesByQuantity(string quantityText)
    {
        // First, try to parse the integer value safely.
        if (int.TryParse(quantityText, out int quantity) && quantity > 0)
        {
            // If the parsed quantity is greater than the total count,
            // set the input field's text to the total count.
            if (quantity > toggleList.Count)
            {
                quantity = toggleList.Count;
                // This line updates the visual input field.
                quantityToSelect.text = quantity.ToString();
            }

            // Clear all existing selections.
            foreach (var toggle in toggleList)
            {
                toggle.isOn = false;
            }
            selectedTimes.Clear();

            // Select toggles in sequence up to the entered quantity.
            for (int i = 0; i < quantity && i < toggleList.Count; i++)
            {
                // This will trigger the OnToggleChanged event.
                toggleList[i].isOn = true;
            }
        }
        else
        {
            // If the input is invalid, clear selections and reset drawTime.
            foreach (var toggle in toggleList)
            {
                toggle.isOn = false;
            }
            selectedTimes.Clear();
            drawTime.text = previousTime.text;
        }
    }

    void Start()
    {
        // Add an onValueChanged listener to the input field
        quantityToSelect.onValueChanged.AddListener(SelectTogglesByQuantity);
        totalCount = 0;
      //  drawTime.text = previousTime.text;
        StartCoroutine(FetchSlotsFromAPI());
    }

    private IEnumerator FetchSlotsFromAPI()
    {
        using (UnityWebRequest www = UnityWebRequest.Get(GameAPIs.advanceTimeAPi))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("API Error: " + www.error);
            }
            else
            {
                string json = www.downloadHandler.text;
                Debug.Log("API Response: " + json);
                SlotsResponse response = JsonUtility.FromJson<SlotsResponse>(json);

                if (response != null && response.status == "success")
                {
                    PopulateSlots(response.slots);
                }
                else
                {
                    Debug.LogWarning("Invalid response from API");
                }
            }
        }
    }

    private void PopulateSlots(List<string> slots)
    {
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }
        toggleList.Clear();

        foreach (string slot in slots)
        {
            GameObject obj = Instantiate(prefab, content);
            // Get the toggle and TMP_Text components
            Toggle toggle = obj.transform.GetChild(0).GetComponent<Toggle>();
            TMP_Text txt = obj.transform.GetChild(1).GetComponent<TMP_Text>();

            // Set the text
            txt.text = slot;

            // Add the listener
            toggle.onValueChanged.AddListener((isOn) => OnToggleChanged(isOn, txt.text, toggle));

            // Add the toggle to your list
            toggleList.Add(toggle);
        }
        totalCount = toggleList.Count - 1;
    }


    public void ClearAdvanceTimeData()
    {
        totalCount = 0;
        selectedTimes.Clear();
        foreach (var toggle in toggleList)
        {
            {
                toggle.isOn = false;
            }

        }
        toggleList.Clear();
        this.gameObject.SetActive(false);
    }
}