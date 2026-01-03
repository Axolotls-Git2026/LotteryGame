using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.SceneManagement;

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
    public GridManager gridMgr;
    public List<Toggle> toggleList = new List<Toggle>();
    public List<string> selectedTimes = new List<string>();

    private void OnToggleChanged(bool isOn, string _txt, Toggle currentToggle)
    {
        if (isOn)
        {
            if (!selectedTimes.Contains(_txt))
            {
                selectedTimes.Add(_txt);
                PlayerPrefs.SetInt("selectedTimes", selectedTimes.Count);
                RecalculationForAdvTime();


            }
        }
        else
        {
            selectedTimes.Remove(_txt);
            PlayerPrefs.SetInt("selectedTimes", selectedTimes.Count);
            RecalculationForAdvTime();



        }

        UpdateDrawTimeText();
    }

    private void UpdateDrawTimeText()
    {
        if (selectedTimes.Count > 0)
        {
            // drawTime.text = string.Join(", ", selectedTimes);
        }
        else
        {
            drawTime.text = previousTime.text;
        }
    }

    // This method now handles input changes more reliably
    public void SelectTogglesByQuantity(string quantityText)
    {
        // Safely parse the input
        if (int.TryParse(quantityText, out int quantity) && quantity >= 0)
        {
            // Limit the quantity to the total number of available toggles
            int countToSelect = Mathf.Min(quantity, toggleList.Count);

            // First, deselect all toggles to start fresh
            // Use a for loop to avoid modifying the list during iteration
            for (int i = 0; i < toggleList.Count; i++)
            {
                toggleList[i].isOn = false;
            }

            // Clear the list of selected times
            selectedTimes.Clear();
            selectedTimes.Add(drawTime.text);
            PlayerPrefs.SetInt("selectedTimes", selectedTimes.Count);
                RecalculationForAdvTime();


            // Select the first 'countToSelect' number of toggles
            for (int i = 0; i < countToSelect; i++)
            {
                // Setting isOn will trigger the OnToggleChanged callback
                toggleList[i].isOn = true;
            }
        }
        else
        {
            // If the input is not a number, deselect all toggles
            for (int i = 0; i < toggleList.Count; i++)
            {
                toggleList[i].isOn = false;
            }
            selectedTimes.Clear();
            selectedTimes.Add(drawTime.text);
            RecalculationForAdvTime();

            PlayerPrefs.SetInt("selectedTimes", selectedTimes.Count);
          
            UpdateDrawTimeText();
        }
    }

    public void RecalculationForAdvTime()
    {
        if (SceneManager.GetActiveScene().name.Contains("4D"))
        {
            gridMgr.RecalculateTotals();
        }
    }

    // Use OnEnable to fetch data whenever the GameObject becomes active
    private void OnEnable()
    {
        StartCoroutine(FetchSlotsFromAPI());
    }

    void Start()
    {
        StartCoroutine(FetchSlotsFromAPI());
        // Add listener here, but only once
        quantityToSelect.onValueChanged.AddListener(SelectTogglesByQuantity);
        //  drawTime.text = previousTime.text;
    }

    public IEnumerator FetchSlotsFromAPI()
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
        // Clear all old UI objects
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }
        toggleList.Clear();
        selectedTimes.Clear();
        selectedTimes.Add(slots[0]);
        RecalculationForAdvTime();

        PlayerPrefs.SetInt("selectedTimes", selectedTimes.Count);
        // Instantiate and set up new toggles
        foreach (string slot in slots)
        {
            GameObject obj = Instantiate(prefab, content);
            Toggle toggle = obj.transform.GetChild(0).GetComponent<Toggle>();
            TMP_Text txt = obj.transform.GetChild(1).GetComponent<TMP_Text>();

            txt.text = slot;
            toggle.onValueChanged.AddListener((isOn) => OnToggleChanged(isOn, txt.text, toggle));
            toggleList.Add(toggle);
        }
    }

    // A separate method to clear all data and UI
    public void ClearAdvanceTimeData()
    {
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        toggleList.Clear();
        selectedTimes.Clear();


        quantityToSelect.text = "";
        drawTime.text = previousTime.text;

        this.gameObject.SetActive(false);
    }

    // Refactored SelectAll method
    public void SelectAll()
    {
        bool allSelected = toggleList.All(t => t.isOn);

        // This prevents the SelectedAll logic from overwriting manual selections
        if (allSelected)
        {
            // Deselect all
            foreach (var toggle in toggleList)
            {
                toggle.isOn = false;
            }
        }
        else
        {
            // Select all
            foreach (var toggle in toggleList)
            {
                toggle.isOn = true;
            }
        }
    }
}