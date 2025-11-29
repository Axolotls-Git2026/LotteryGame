using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SeriesManager : MonoBehaviour
{
    [Header("Series Data (toggles)")]
    public List<Toggle> seriesToggles;   // Assign in Inspector
    public List<int> seriesValues = new List<int> { 10, 30, 50 }; // Match order with toggles
    public List<SeriesButton> seriesButtons = new List<SeriesButton>();
    public List<int> currentSeriesSelected = new List<int>();
    public int currentSeriesBase = 10; // 10 => 10xx, 30 => 30xx, etc.


    [Header("Range Buttons (10 items)")]
    public GameObject[] RangeGrp;   // each has Button + Text (TMP_Text or Text)
    public List<int> currentRangeSelected = new List<int>();
    public List<RangeData> rangeGroups; // Assign in Inspector


    [Header("Grid")]
    public GridManager gridManager;

    [Header("Quantity&Points")]
    //public List<GameObject> quantity;
    //public List<GameObject> points;
    public static Action<int, List<int>, List<int>> OnQuantityAdded;


    private bool rangeListenersHooked = false;

    public Dictionary<int, int> betNumbers = new Dictionary<int, int>();


    public List<string> rangeGrpColorHex;
    public GameObject mainGridBG;
    public int currentRangeIndx;
    void Start()
    {
        for (int i = 0; i < seriesToggles.Count; i++)
        {
            int seriesValue = seriesValues[i]; // capture value for closure
            seriesToggles[i].onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                {
                    SetSeries(seriesValue); //  Pass 10, 30, 50 instead of index
                }
                else
                {
                    OnSeriesDeselected(seriesValue);
                }
            });
        }
        foreach (var sb in seriesButtons)
        {
            int value = sb.seriesValue; // capture the value
            sb.button.onClick.AddListener(() =>
            {
                //  isSingleRangeSelected = true;
                OnSeriesBtnClicked(value, sb.index);
            });

        }



        // Default series
        SetSeries(10);
        rangeGroups[0].toggle.isOn = true;
        // Hook range buttons once

        foreach (var range in rangeGroups)
        {
            // Handle Toggle
            if (range.toggle != null)
            {
                range.toggle.onValueChanged.AddListener(isOn =>
                {
                    if (isOn)
                        OnRangeSelected(range.rangeValue);
                    else
                        OnRangeDeselected(range.rangeValue);
                });
            }

            // Handle Button
            if (range.button != null)
            {
                range.button.onClick.AddListener(() =>
                {
                    OnRangeBtnSelected(range.rangeValue);
                });
            }
        }




        // Default range (first 100-block)
        OnRangeSelected(0);
    }

    private void Update()
    {
        // Check for keyboard input to toggle ranges
        if (Input.GetKeyDown(KeyCode.PageDown))
        {
            //// Move to the next index, wrapping around if necessary
            currentRangeIndx++;
            if (currentRangeIndx >= rangeGroups.Count - 1)
            {
                currentRangeIndx = 0;
            }
            OnRangeBtnSelected(rangeGroups[currentRangeIndx].rangeValue);
        }
        else if (Input.GetKeyDown(KeyCode.PageUp))
        {
            // Move to the previous index, wrapping around if necessary
            currentRangeIndx--;
            if (currentRangeIndx < 0)
            {
                currentRangeIndx = rangeGroups.Count - 1;
            }
            OnRangeBtnSelected(rangeGroups[currentRangeIndx].rangeValue);
        }
    }

    private void ToggleRangeButton(int index)
    {
        if (index >= 0 && index < RangeGrp.Length)
        {
            // Get the Toggle component from the GameObject at the given index
            var toggleComponent = rangeGroups[index].toggle;

            if (toggleComponent != null)
            {
                // Set the toggle to be "on"
                toggleComponent.isOn = true;

                // ? Call the OnRangeBtnSelected function with the correct range index
                OnRangeBtnSelected(index);
            }
        }
    }

    public void SetSeries(int seriesBase)
    {
        currentSeriesBase = seriesBase;
        if (!currentSeriesSelected.Contains(currentSeriesBase))
        {
            currentSeriesSelected.Add(currentSeriesBase);
        }
        // Update RangeGrp labels: (seriesBase + i) * 100 .. +99
        for (int i = 0; i < RangeGrp.Length; i++)
        {
            int start = (currentSeriesBase + i) * 100;
            int end = start + 99;

            var tmp = RangeGrp[i].GetComponentInChildren<TMP_Text>();
            if (tmp != null) tmp.text = $"{start}-{end}";
            else
            {
                var ugui = RangeGrp[i].GetComponentInChildren<TMP_Text>();
                if (ugui != null) ugui.text = $"{start}-{end}";
            }
        }




        // Also refresh grid for currently selected range (assume 0 if none)
        OnRangeSelected(currentRangeIndx);
    }

    public void OnSeriesBtnClicked(int seriesBase, int index)
    {
        foreach (var toggle in seriesToggles)
            toggle.isOn = false;

        // ? CORRECT: Update current series for DISPLAY only, but keep multi-selection for data
        currentSeriesBase = seriesBase; // For UI display

        // Don't clear currentSeriesSelected - it should contain ALL selected series for data purposes
        // Only ensure the clicked series is selected
        if (!currentSeriesSelected.Contains(seriesBase))
        {
            currentSeriesSelected.Add(seriesBase);
        }

        gridManager.ClearMainInputs();
       // gridManager.ClearBandF();

        if (currentRangeIndx < 0 || currentRangeIndx >= RangeGrp.Length)
            currentRangeIndx = 0;

        // Load only for UI display purposes
        gridManager.LoadGridData(currentSeriesBase, currentRangeIndx);
        gridManager.UpdateGridNumbers(currentSeriesBase, currentRangeIndx);

        seriesToggles[index].isOn = true;

        // Update RangeGrp labels
        for (int i = 0; i < RangeGrp.Length; i++)
        {
            int start = (currentSeriesBase + i) * 100;
            int end = start + 99;
            var tmp = RangeGrp[i].GetComponentInChildren<TMP_Text>();
            if (tmp != null) tmp.text = $"{start}-{end}";
        }

        OnRangeSelected(currentRangeIndx);

        // ? CRITICAL: Recalculate from ALL data, not just current series
        gridManager.RecalculateTotals();
    }





    void OnSeriesDeselected(int seriesBase)
    {
        currentSeriesSelected.Remove(seriesBase);

        if (currentSeriesBase == seriesBase)
        {
            if (currentSeriesSelected.Count > 0)
            {
                // pick the first other series from the list
                currentSeriesBase = currentSeriesSelected[0];
            }
            else
            {
                // fallback default
                currentSeriesBase = 10;
            }
        }

        Debug.Log("series removed for " + seriesBase);

    }

    public bool isRangeBtnClicked;
    public void OnRangeSelected(int rangeIndex)
    {
        if (!currentRangeSelected.Contains(rangeIndex))
        {
            currentRangeSelected.Add(rangeIndex);
        }

        if (currentRangeSelected.Count > 1)
        {
            foreach (int series in currentSeriesSelected)
            {
                foreach (int range in currentRangeSelected)
                {
                    gridManager.SaveCurrentGridData(series, range);
                }
            }
        }

        currentRangeIndx = rangeIndex;

        gridManager.isLoading = true;
        gridManager.LoadGridData(currentSeriesBase, rangeIndex); // UI display only
        gridManager.UpdateGridNumbers(currentSeriesBase, rangeIndex);
        gridManager.isLoading = false;

        // ? FIX: Use RecalculateTotals instead of OnValueAddedInGridInputs
        gridManager.RecalculateTotals(); // This uses ALL saved data
    }

    public bool isSingleRangeSelected;
    public void OnRangeBtnSelected(int rangeIndex)
    {
        foreach (var grp in rangeGroups)
        {
            grp.toggle.isOn = false;
        }

        gridManager.ClearMainInputs();

        // ? Only clear if you want single range selection, otherwise modify logic
        // For single range selection (when clicking individual range buttons):
        currentRangeSelected.Clear();

        if (!currentRangeSelected.Contains(rangeIndex))
        {
            currentRangeSelected.Add(rangeIndex);
        }

        currentRangeIndx = rangeIndex;
        rangeGroups[rangeIndex].toggle.isOn = true;

        gridManager.isLoading = true;
        gridManager.LoadGridData(currentSeriesBase, rangeIndex); // UI display only
        gridManager.UpdateGridNumbers(currentSeriesBase, rangeIndex);
        gridManager.isLoading = false;

        gridManager.RecalculateTotals(); // Use ALL data
    }


    private void OnRangeDeselected(int rangeIndex)
    {
        rangeGroups[rangeIndex].toggle.isOn = false;
        currentRangeSelected.Remove(rangeIndex);
    }

    void OnBuyBtnClicked()
    {

    }


    public void SelectAllSeries()
    {
        // Check if all toggles are currently ON
        bool allOn = true;
        for (int i = 0; i < seriesToggles.Count; i++)
        {
            if (seriesToggles[i] != null && !seriesToggles[i].isOn)
            {
                allOn = false;
                break;
            }
        }

        // If all were ON ? turn OFF all
        if (allOn)
        {
            for (int i = 0; i < seriesToggles.Count; i++)
            {
                if (seriesToggles[i] != null)
                {
                    seriesToggles[i].isOn = false;
                    currentSeriesSelected.Remove(seriesValues[i]);
                    currentSeriesBase = 10;
                    Debug.Log($"Series {i + 1} deselected with value: {seriesValues[i]}");
                }
            }
        }
        // Otherwise ? turn ON all
        else
        {
            for (int i = 0; i < seriesToggles.Count; i++)
            {
                if (seriesToggles[i] != null)
                {
                    seriesToggles[i].isOn = true;
                    if (!currentSeriesSelected.Contains(seriesValues[i]))
                    {
                        currentSeriesSelected.Add(seriesValues[i]);
                    }
                    Debug.Log($"Series {i + 1} selected with value: {seriesValues[i]}");
                }
            }
        }
    }

    public void SelectAllRange()
    {
        bool allSelected = true;

        // Check if all are already selected
        for (int i = 0; i < rangeGroups.Count; i++)
        {
            if (rangeGroups[i].toggle != null && !rangeGroups[i].toggle.isOn)
            {
                allSelected = false;
                break;
            }
        }

        if (allSelected)
        {
            // Deselect all
            for (int i = 0; i < rangeGroups.Count; i++)
            {
                if (rangeGroups[i].toggle != null)
                {
                    rangeGroups[i].toggle.isOn = false;
                }
            }
            currentRangeSelected.Clear();
            Debug.Log("All ranges deselected");
        }
        else
        {
            // Select all
            for (int i = 0; i < rangeGroups.Count; i++)
            {
                if (rangeGroups[i].toggle != null)
                {
                    rangeGroups[i].toggle.isOn = true;

                    if (!currentRangeSelected.Contains(rangeGroups[i].rangeValue))
                    {
                        currentRangeSelected.Add(rangeGroups[i].rangeValue);
                    }

                    Debug.Log($"Series {i + 1} selected with value: {rangeGroups[i].rangeValue}");
                }
            }
        }
    }



    public void ClearAllSeriesAndRange()
    {
        currentRangeSelected.Clear();
        currentSeriesSelected.Clear();
        for (int i = 0; i < seriesToggles.Count; i++)
        {
            if (seriesToggles[i] != null)
            {
                seriesToggles[i].isOn = false;
            }
        }

        for (int i = 0; i < rangeGroups.Count; i++)
        {
            if (rangeGroups[i].toggle != null)
            {
                rangeGroups[i].toggle.isOn = false;
            }
        }
        if (!currentSeriesSelected.Contains(10))
        {
            currentSeriesSelected.Add(10);
        }
        seriesToggles[0].isOn = true;
        rangeGroups[0].toggle.isOn = true;
    }


}
[System.Serializable]
public class SeriesButton
{
    public Button button;
    public int seriesValue;
    public int index;
}

[System.Serializable]
public class RangeData
{
    public Toggle toggle;
    public Button button;
    public int rangeValue; // e.g., 10, 30, 50
}