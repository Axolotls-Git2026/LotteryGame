using NUnit.Framework.Constraints;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI.Table;

public class GridManager : MonoBehaviour
{
    [Header("Grid Inputs (fixed layout)")]
    public GridLayoutGroup layoutGrp;
    public int rows = 10;
    public int cols = 10;
    public bool reserveFirstRowLastCol = false;

    public GameObject[,] gridInputs;
    public GameObject[] allFInputs;
    public GameObject[] allBInputs;

    public Toggle familyToggle;

    public Toggle allNum;
    public Toggle evenNum;
    public Toggle oddNum;
    private Toggle lastActive;

    public SeriesManager seriesManager;
    // Top level dictionary keyed by (series, range)
    private Dictionary<(int series, int range), Dictionary<(int row, int col), string>> allGridData =
        new Dictionary<(int, int), Dictionary<(int, int), string>>();

    public bool isLoading;

    // CRITICAL: A flag to prevent circular updates when setting input.text
    private bool isUpdatingInputs = false;

    // Track separate contributions
    private int?[,] rowValues;
    private int?[,] colValues;

    private int currentRow = 0;
    private int currentCol = 0;
    private int currentIndexF, currentIndexB;
    private void Awake()
    {
        gridInputs = new GameObject[rows, cols];

        int index = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Transform child = layoutGrp.transform.GetChild(index);

                // Skip All_F_Input_Object
                if (child.name.Contains("All_F_Input_Object"))
                {
                    index++;
                    continue;
                }

                gridInputs[r, c] = child.gameObject;
                index++;
            }
        }
    }


    private TMP_InputField[,] cachedFields;

public void InitGrid()
{
    cachedFields = new TMP_InputField[rows, cols];

    for (int r = 0; r < rows; r++)
    {
        for (int c = 0; c < cols; c++)
        {
            if (gridInputs[r, c] == null) continue; // skip null slots

            cachedFields[r, c] = gridInputs[r, c].transform
                .GetChild(1)
                .GetComponent<TMP_InputField>();
        }
    }
}



    private void Start()
    {
        InitGridTracking();
        InitGrid();
        allNum.isOn = true;
        evenNum.isOn = false;
        oddNum.isOn = false;
        lastActive = allNum;

        for (int r = 0; r < allFInputs.Length; r++)
        {
            int rowIndex = r;
            // Capture the TMP_InputField instance
            TMP_InputField input = allFInputs[r].transform.GetChild(1).GetComponent<TMP_InputField>();

            // Pass the input instance as an additional parameter
            input.onValueChanged.AddListener(val =>
            {
                FillRow(rowIndex, val);
                ValueValidation(val, input);
            });
        }

        for (int c = 0; c < allBInputs.Length; c++)
        {
            int colIndex = c;
            // Capture the TMP_InputField instance
            TMP_InputField input = allBInputs[c].transform.GetChild(1).GetComponent<TMP_InputField>();

            // Pass the input instance as an additional parameter
            input.onValueChanged.AddListener(val =>
            {
                FillColumn(colIndex, val);
                ValueValidation(val, input);
            });
        }

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (gridInputs[r, c] == null)
                    continue;

                // Skip unwanted objects by name
                if (gridInputs[r, c].name.Contains("All_F_Input_Object"))
                    continue;

                Transform child = gridInputs[r, c].transform;
                if (child.childCount < 2)
                    continue;

                var inputField = child.GetChild(1).GetComponent<TMP_InputField>();
                if (inputField == null)
                    continue;

                int capturedRow = r;
                int capturedCol = c;

                inputField.onValueChanged.AddListener(newValue =>
                    OnSingleInputChanged(capturedRow, capturedCol, newValue));

                inputField.onSubmit.AddListener(newValue =>
                    OnSingleInputChanged(capturedRow, capturedCol, newValue));
            }
        }


        allNum.onValueChanged.AddListener((isOn) => HandleToggle(allNum, isOn));
        evenNum.onValueChanged.AddListener((isOn) => HandleToggle(evenNum, isOn));
        oddNum.onValueChanged.AddListener((isOn) => HandleToggle(oddNum, isOn));
    }

    private Coroutine debounceCoroutine;

    private void OnSingleInputChanged(int r, int c, string newValue)
    {
        if (isUpdatingInputs) return;
        Debug.Log($"? Validation triggered for [{r},{c}] ? {newValue}");
        if (debounceCoroutine != null)
            StopCoroutine(debounceCoroutine);

        debounceCoroutine = StartCoroutine(DebounceSave(r, c, newValue));
    }

    private IEnumerator DebounceSave(int r, int c, string newValue)
    {
        // Capture the context at the time of typing
        var seriesSnapshot = new List<int>(seriesManager.currentSeriesSelected);
        var rangeSnapshot = new List<int>(seriesManager.currentRangeSelected);

        yield return new WaitForSeconds(0.1f);

        SaveCell(r, c, newValue, seriesSnapshot, rangeSnapshot);

        // ONLY recalculate totals - don't save grid data again
        RecalculateTotals();
        debounceCoroutine = null;
    }


    // =========================
    // SAVE CELL HELPER
    // =========================
    private void SaveCell(int r, int c, string newValue, List<int> seriesSnapshot, List<int> rangeSnapshot)
    {
        if (r < 0 || c < 0 || r >= rows || c >= cols) return;

        int value = 0;
        if (int.TryParse(newValue, out int parsedValue))
        {
            value = Mathf.Clamp(parsedValue, 0, 999);
            Debug.Log("Clamping value to : " + value);
        }
        Debug.Log($"[SaveCell] Saving value '{value}' at ({r},{c}) for {seriesSnapshot.Count} series and {rangeSnapshot.Count} ranges");

        // Save to all selected series/range combinations
        foreach (int series in seriesSnapshot)
        {
            foreach (int range in rangeSnapshot)
            {
                UpdateSingleCellData(series, range, r, c, value);
                Debug.Log($"[SaveCell] Saved to series:{series}, range:{range}");
            }
        }

        // Family logic (if enabled)
        if (familyToggle != null && familyToggle.isOn)
        {
            foreach (int series in seriesSnapshot)
            {
                foreach (int range in rangeSnapshot)
                {
                    int bettedNum = CalculateNumbers(series, range, r, c);
                    ApplyFamilyToBettedNumber(bettedNum, value, seriesSnapshot, rangeSnapshot);
                }
            }
        }

        Debug.Log($"[SaveCell] r:{r} c:{c} value:{newValue}");
        // RecalculateTotals() is called by DebounceSave, so don't call it here
    }

    private void ApplyFamilyToBettedNumber(int baseNumber, int value, List<int> seriesSnapshot, List<int> rangeSnapshot)
    {
        var family = GenerateFamily(baseNumber);
        if (family == null || family.Count == 0) return;

        bool prevUpdating = isUpdatingInputs;
        isUpdatingInputs = true;

        string valueStr = value > 0 ? value.ToString() : "";

        // 1) Update UI directly for family numbers
        foreach (int familyNum in family)
            UpdateInputForNumber(familyNum, valueStr);

        // 2) Update only the **cells that correspond to these family numbers**
        foreach (int series in seriesSnapshot)
        {
            foreach (int range in rangeSnapshot)
            {
                foreach (var cellPos in GetCellsForFamily(series, range, family.ToList()))
                {
                    UpdateSingleCellData(series, range, cellPos.row, cellPos.col, value);
                }
            }
        }

        isUpdatingInputs = prevUpdating;
    }

    // Helper: returns only the positions of cells that match the family numbers
    private IEnumerable<(int row, int col)> GetCellsForFamily(int series, int range, List<int> family)
    {
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int bet = CalculateNumbers(series, range, r, c);
                if (family.Contains(bet))
                    yield return (r, c);
            }
        }
    }



    // =========================
    // UPDATE SINGLE CELL
    // =========================
    public void UpdateSingleCellData(int series, int range, int row, int col, int value)
    {
        var key = (series, range);

        if (!allGridData.TryGetValue(key, out var gridInputData))
        {
            gridInputData = new Dictionary<(int, int), string>();
            allGridData[key] = gridInputData;
        }

        if (value > 0)
        {
            gridInputData[(row, col)] = value.ToString();

            int bettedNum = CalculateNumbers(series, range, row, col);
            seriesManager.betNumbers[bettedNum] = value;

            Debug.Log($"Saved {value} for bet {bettedNum} at ({series},{range}) [{row},{col}]");
        }
        else
        {
            gridInputData.Remove((row, col));

            int bettedNum = CalculateNumbers(series, range, row, col);
            seriesManager.betNumbers.Remove(bettedNum);

            Debug.Log($"Removed bet {bettedNum} at ({series},{range}) [{row},{col}]");
        }
    }


    private void UpdateFamily(int baseNumber, string value)
    {
        isUpdatingInputs = true;

        HashSet<int> family = GenerateFamily(baseNumber);

        foreach (int num in family)
        {
            UpdateInputForNumber(num, value);
        }

        isUpdatingInputs = false;
    }

    private void UpdateInputForNumber(int number, string value)
    {
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                GameObject cell = gridInputs[r, c];
                if (cell == null) continue;

                TMP_Text numberText = cell.transform.GetChild(0).GetComponent<TMP_Text>();
                TMP_InputField input = cell.transform.GetChild(1).GetComponent<TMP_InputField>();

                if (numberText == null || input == null) continue;

                if (int.TryParse(numberText.text, out int num) && num == number)
                {
                    input.text = value;
                }
            }
        }
    }

    // --- FAMILY LOGIC ---
    private static readonly Dictionary<int, int> SwapMap = new Dictionary<int, int>
    {
        { 0, 5 }, { 5, 0 }, { 1, 6 }, { 6, 1 }, { 2, 7 },
        { 7, 2 }, { 3, 8 }, { 8, 3 }, { 4, 9 }, { 9, 4 }
    };

    public static HashSet<int> GenerateFamily(int baseNumber)
    {
        HashSet<int> family = new HashSet<int>();
        int prefix = baseNumber / 100;
        int tens = (baseNumber / 10) % 10;
        int units = baseNumber % 10;
        family.Add(baseNumber);

        TryAddSwap(family, prefix, tens, units, swapTens: false, swapUnits: true);
        TryAddSwap(family, prefix, tens, units, swapTens: true, swapUnits: false);
        TryAddSwap(family, prefix, tens, units, swapTens: true, swapUnits: true);

        int invTens = units;
        int invUnits = tens;
        int inverse = prefix * 100 + invTens * 10 + invUnits;
        family.Add(inverse);

        TryAddSwap(family, prefix, invTens, invUnits, swapTens: false, swapUnits: true);
        TryAddSwap(family, prefix, invTens, invUnits, swapTens: true, swapUnits: false);
        TryAddSwap(family, prefix, invTens, invUnits, swapTens: true, swapUnits: true);

        return family;
    }

    private static void TryAddSwap(HashSet<int> family, int prefix, int tens, int units, bool swapTens, bool swapUnits)
    {
        int newTens = tens;
        int newUnits = units;

        if (swapTens && SwapMap.ContainsKey(tens))
            newTens = SwapMap[tens];

        if (swapUnits && SwapMap.ContainsKey(units))
            newUnits = SwapMap[units];

        int newNumber = prefix * 100 + newTens * 10 + newUnits;
        family.Add(newNumber);
    }

    //------------------------------------------------------------------------------------------------------------------

    #region YOUR EXISTING METHODS

    void Update()
    {
        if (gridInputs == null && allFInputs == null && allBInputs == null) return;

        var selectedObj = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
        if (selectedObj != null)
        {
            TMP_InputField selected = selectedObj.GetComponent<TMP_InputField>();
            if (selected != null)
            {
                // --- GRID INPUTS ---
                bool found = false;
                for (int r = 0; r < (gridInputs?.GetLength(0) ?? 0); r++)
                {
                    for (int c = 0; c < (gridInputs?.GetLength(1) ?? 0); c++)
                    {
                        if (gridInputs[r, c] != null && gridInputs[r, c].transform.GetChild(1).GetComponent<TMP_InputField>() == selected)
                        {
                            currentRow = r;
                            currentCol = c;
                            found = true;
                            break;
                        }
                    }
                    if (found) break;
                }

                if (found)
                {
                    if (Input.GetKeyDown(KeyCode.UpArrow))
                        MoveTo(currentRow - 1, currentCol);
                    else if (Input.GetKeyDown(KeyCode.DownArrow))
                        MoveTo(currentRow + 1, currentCol);
                    else if (Input.GetKeyDown(KeyCode.LeftArrow))
                        MoveLeft();
                    else if (Input.GetKeyDown(KeyCode.RightArrow))
                        MoveRight();
                }

                // --- ALL F INPUTS ---
                for (int i = 0; i < (allFInputs?.Length ?? 0); i++)
                {
                    if (allFInputs[i] != null && allFInputs[i].transform.GetChild(1).GetComponent<TMP_InputField>() == selected)
                    {
                        currentIndexF = i;
                        if (Input.GetKeyDown(KeyCode.UpArrow))
                            MoveF(currentIndexF - 1);
                        else if (Input.GetKeyDown(KeyCode.DownArrow))
                            MoveF(currentIndexF + 1);
                        break;
                    }
                }

                // --- ALL B INPUTS ---
                for (int i = 0; i < (allBInputs?.Length ?? 0); i++)
                {
                    if (allBInputs[i] != null && allBInputs[i].transform.GetChild(1).GetComponent<TMP_InputField>() == selected)
                    {
                        currentIndexB = i;
                        if (Input.GetKeyDown(KeyCode.LeftArrow))
                            MoveB(currentIndexB - 1);
                        else if (Input.GetKeyDown(KeyCode.RightArrow))
                            MoveB(currentIndexB + 1);
                        break;
                    }
                }
            }
        }
    }

    void MoveTo(int r, int c)
    {
        if (r >= 0 && r < gridInputs.GetLength(0) && c >= 0 && c < gridInputs.GetLength(1) && gridInputs[r, c] != null)
        {
            gridInputs[r, c].transform.GetChild(1).GetComponent<TMP_InputField>().Select();
        }
    }
    void MoveLeft()
    {
        int newRow = currentRow;
        int newCol = currentCol - 1;

        // if at first column, go to previous row’s last column
        if (newCol < 0)
        {
            newRow--;
            if (newRow >= 0)
                newCol = gridInputs.GetLength(1) - 1;
        }

        MoveTo(newRow, newCol);
    }

    void MoveRight()
    {
        int newRow = currentRow;
        int newCol = currentCol + 1;

        // if at last column, go to next row’s first column
        if (newCol >= gridInputs.GetLength(1))
        {
            newRow++;
            if (newRow < gridInputs.GetLength(0))
                newCol = 0;
        }

        MoveTo(newRow, newCol);
    }
    void MoveF(int index)
    {
        if (index >= 0 && index < allFInputs.Length && allFInputs[index] != null)
        {
            allFInputs[index].transform.GetChild(1).GetComponent<TMP_InputField>().Select();
        }
    }

    void MoveB(int index)
    {
        if (index >= 0 && index < allBInputs.Length && allBInputs[index] != null)
        {
            allBInputs[index].transform.GetChild(1).GetComponent<TMP_InputField>().Select();
        }
    }

    public void UpdateGridNumbers(int seriesBase, int rangeIndex)
    {
        int bandStart = (seriesBase * 100) + (rangeIndex * 100);
        int bandEnd = bandStart + 99;

        int n = bandStart;

        for (int r = 0; r < rows; r++)
        {
            int maxColsThisRow = cols;
            maxColsThisRow = Mathf.Max(0, cols);

            for (int c = 0; c < maxColsThisRow; c++)
            {
                var label = GetCellLabel(r, c);
                if (label == null) continue;

                if (n <= bandEnd)
                {
                    label.text = n.ToString();
                    n++;
                }
                else
                {
                    label.text = "";
                }
            }

            for (int c = maxColsThisRow; c < cols; c++)
            {
                var label = GetCellLabel(r, c);
                if (label != null) label.text = "";
            }
        }
    }

    private TMP_Text GetCellLabel(int r, int c)
    {
        var go = gridInputs[r, c];
        if (go == null) return null;

        TMP_Text tmp = null;
        if (go.transform.childCount > 0)
            tmp = go.transform.GetChild(0).GetComponent<TMP_Text>();
        if (tmp == null)
            tmp = go.GetComponentInChildren<TMP_Text>(true);

        return tmp;
    }

    void InitGridTracking()
    {
        rowValues = new int?[rows, cols];
        colValues = new int?[rows, cols];
        CacheGridInputs();
    }

    private TMP_InputField[,] cachedInputs;

    private void CacheGridInputs()
    {
        cachedInputs = new TMP_InputField[rows, cols];
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (gridInputs[r, c] == null) // <-- prevent null access
                    continue;

                if (gridInputs[r, c].name.Contains("All_F_Input_Object"))
                    continue;

                cachedInputs[r, c] = gridInputs[r, c].transform.GetChild(1).GetComponent<TMP_InputField>();
            }
        }
    }




    void UpdateCell(int r, int c)
    {
        int rowVal = rowValues[r, c] ?? 0;
        int colVal = colValues[r, c] ?? 0;
        int sum = rowVal + colVal;

        GameObject go = gridInputs[r, c];
        if (go == null) return;

        TMP_InputField input = go.GetComponent<TMP_InputField>() ?? go.GetComponentInChildren<TMP_InputField>();
        if (input == null) return;

        isUpdatingInputs = true;
        input.text = sum == 0 ? "" : sum.ToString();
        isUpdatingInputs = false;

        foreach (int series in seriesManager.currentSeriesSelected)
        {
            foreach (int range in seriesManager.currentRangeSelected)
            {
                SaveCurrentGridData(series, range);
            }
        }

        // ? NEW: Add this call to ensure totals are updated after a row/column fill
        RecalculateTotals();
    }

    //public void ValueValidation(string value, out int amount)
    //{
    //    if (int.TryParse(value, out amount))
    //    {
    //        if (amount < 0 || amount > 999)
    //        {
    //            // Invalid range, set amount to a safe default.
    //            amount = 0;
    //            // You could also provide user feedback here, like a toast message.
    //            if (amount < 0 || amount > 999)
    //            {
    //                field.text = "999";
    //                return;
    //            }
    //        }
    //    }
    //    else
    //    {
    //        // Not a valid integer, set amount to a safe default.
    //        amount = 0;
    //    }
    //}

    private Coroutine saveCoroutine;

    public void FillRow(int rowIndex, string value)
    {
        if (isUpdatingInputs) return;

        isUpdatingInputs = true;

        // Validate
        if (rowIndex < 0 || rowIndex >= rows || gridInputs == null)
        {
            isUpdatingInputs = false;
            return;
        }

        bool hasValue = int.TryParse(value, out int amount);
        int parsedValue = hasValue ? Mathf.Clamp(amount, 0, 999) : 0;

        // Capture selection context at the start
        var seriesSnapshot = new List<int>(seriesManager.currentSeriesSelected);
        var rangeSnapshot = new List<int>(seriesManager.currentRangeSelected);

        // ? Use a list to track saved cells for debugging
        int savedCellsCount = 0;

        for (int c = 0; c < cols; c++)
        {
            if (gridInputs[rowIndex, c] == null) continue;

            bool shouldFill = false;

            if (hasValue)
            {
                if (allNum != null && allNum.isOn) shouldFill = true;
                else if (evenNum != null && evenNum.isOn && c % 2 == 0) shouldFill = true;
                else if (oddNum != null && oddNum.isOn && c % 2 != 0) shouldFill = true;
            }

            string newValue = shouldFill ? parsedValue.ToString() : "";

            // Update UI
            TMP_InputField input = gridInputs[rowIndex, c].GetComponentInChildren<TMP_InputField>();
            if (input != null)
            {
                input.SetTextWithoutNotify(newValue);
            }

            // Update internal values
            if (rowValues != null && rowIndex < rowValues.GetLength(0) && c < rowValues.GetLength(1))
            {
                rowValues[rowIndex, c] = shouldFill ? parsedValue : (int?)null;
            }

            // Save the cell
            if (shouldFill)
            {
                SaveCellDirect(rowIndex, c, parsedValue, seriesSnapshot, rangeSnapshot);
                savedCellsCount++;
            }
            else
            {
                SaveCellDirect(rowIndex, c, 0, seriesSnapshot, rangeSnapshot);
            }
        }

        // ? Force a small delay to ensure all saves are processed
        StartCoroutine(DelayedRecalculateWithDebug(savedCellsCount));

        isUpdatingInputs = false;

        Debug.Log($"[FillRow] Filled row {rowIndex}, saved {savedCellsCount} cells");
    }

    private IEnumerator DelayedRecalculateWithDebug(int savedCellsCount)
    {
        yield return new WaitForSeconds(0.05f); // Small delay to ensure saves complete

        Debug.Log($"[FillRow] Delayed recalculate after saving {savedCellsCount} cells");
        RecalculateTotals();
    }

    // ? Enhanced debug version of SaveCellDirect
    private void SaveCellDirect(int r, int c, int value, List<int> seriesSnapshot, List<int> rangeSnapshot)
    {
        if (r < 0 || c < 0 || r >= rows || c >= cols) return;

        Debug.Log($"[SaveCellDirect] Saving value {value} at ({r},{c}) for {seriesSnapshot.Count} series, {rangeSnapshot.Count} ranges");

        // Save to all selected series/range combinations
        foreach (int series in seriesSnapshot)
        {
            foreach (int range in rangeSnapshot)
            {
                Debug.Log($"[SaveCellDirect] Updating series:{series}, range:{range}");
                UpdateSingleCellData(series, range, r, c, value);
            }
        }
    }

    private IEnumerator DelayedRecalculateTotals()
    {
        yield return new WaitForSeconds(0.1f);
        RecalculateTotals();
    }

    private IEnumerator DelayedSaveAndTotals()
    {
        yield return new WaitForSeconds(0.25f); // 0.2–0.3 sec delay

        if (seriesManager.currentSeriesSelected.Count > 0 &&
            seriesManager.currentRangeSelected.Count > 0)
        {
            foreach (int series in seriesManager.currentSeriesSelected)
            {
                foreach (int range in seriesManager.currentRangeSelected)
                {
                    SaveCurrentGridData(series, range);
                }
            }
        }

        RecalculateTotals();

        saveCoroutine = null;
    }




    void FillColumn(int colIndex, string value)
    {
        isUpdatingInputs = true;

        bool hasValue = int.TryParse(value, out int amount);
        int parsedValue = hasValue ? Mathf.Clamp(amount, 0, 999) : 0;

        for (int r = 0; r < rows; r++)
        {
            colValues[r, colIndex] = hasValue ? (int?)parsedValue : null;

            UpdateCell(r, colIndex); // this will recalc sum with rowValues
        }

        isUpdatingInputs = false;
    }


    public void HandleAllToggle()
    {
        allNum.isOn = true;
        evenNum.isOn = false;
        oddNum.isOn = false;
    }

    public void HandleEvenToggle()
    {
        allNum.isOn = false;
        evenNum.isOn = true;
        oddNum.isOn = false;
    }

    public void HandleOddToggle()
    {
        allNum.isOn = false;
        evenNum.isOn = false;
        oddNum.isOn = true;
    }

    private void HandleToggle(Toggle changedToggle, bool isOn)
    {
        if (isOn)
        {
            RemoveAllListeners();
            if (changedToggle != allNum) allNum.isOn = false;
            if (changedToggle != evenNum) evenNum.isOn = false;
            if (changedToggle != oddNum) oddNum.isOn = false;

            lastActive = changedToggle;
            AddAllListeners();
        }
        else
        {
            if (changedToggle == lastActive)
            {
                RemoveAllListeners();
                changedToggle.isOn = true;
                AddAllListeners();
            }
        }
    }

    private void RemoveAllListeners()
    {
        allNum.onValueChanged.RemoveAllListeners();
        evenNum.onValueChanged.RemoveAllListeners();
        oddNum.onValueChanged.RemoveAllListeners();
    }

    private void AddAllListeners()
    {
        allNum.onValueChanged.AddListener((isOn) => HandleToggle(allNum, isOn));
        evenNum.onValueChanged.AddListener((isOn) => HandleToggle(evenNum, isOn));
        oddNum.onValueChanged.AddListener((isOn) => HandleToggle(oddNum, isOn));
    }

    public void ValueValidation(string value, TMP_InputField field)
    {
        int parsedValue;

        if (field.text == "")
        {
            SeriesManager.OnQuantityAdded?.Invoke(0, seriesManager.currentSeriesSelected, seriesManager.currentRangeSelected);
        }
        if (value == "")
        {
            TMP_Text label = field.transform.parent.GetChild(0).GetComponent<TMP_Text>();
            if (int.TryParse(label.text, out int key))
            {
                seriesManager.betNumbers.Remove(int.Parse(field.transform.parent.GetChild(0).GetComponent<TMP_Text>().text));
                string dictLog = "Current betNumbers: ";
                foreach (var kvp in seriesManager.betNumbers)
                {
                    dictLog += $"[{kvp.Key} : {kvp.Value}] ";
                }
                Debug.Log(dictLog);
            }
        }
        if (int.TryParse(value, out parsedValue))
        {
            if (parsedValue != 0 || parsedValue.ToString() == "")
            {
                seriesManager.isSingleRangeSelected = false;
            }
        }

        if (parsedValue < 0 || parsedValue > 999)
        {
            field.text = "999";
            return;
        }

        TrySaveGridData(parsedValue);
    }
    private float lastSaveTime = 0f;
    private const float SaveDelay = 0.1f; // half a second

    public void TrySaveGridData(int parsedValue)
    {
        if (parsedValue <= 0 || parsedValue > 999) return;

        if (Time.time - lastSaveTime >= SaveDelay)
        {
            lastSaveTime = Time.time;
            SaveAllCurrentGrids();
        }
    }

    public IEnumerator DelayCall(int parsedValue)
    {
        yield return new WaitForSeconds(1f);
        if (parsedValue > 0 && parsedValue < 999)
        {
            foreach (int series in seriesManager.currentSeriesSelected)
            {
                foreach (int range in seriesManager.currentRangeSelected)
                {
                    SaveCurrentGridData(series, range);
                    //   Debug.Log("Saving");
                }
            }
        }
    }

    Dictionary<(int series, int range), Dictionary<(int row, int col), string>> lastGridValues = new Dictionary<(int series, int range), Dictionary<(int row, int col), string>>();


    public void SaveCurrentGridData(int series, int range)
    {
        var key = (series, range);

        // outer dictionary: all grids
        if (!allGridData.TryGetValue(key, out var gridInputData))
        {
            gridInputData = new Dictionary<(int, int), string>();
            allGridData[key] = gridInputData;
        }

        // ensure last values dictionary exists for this series+range
        if (!lastGridValues.TryGetValue(key, out var lastValuesForGrid))
        {
            lastValuesForGrid = new Dictionary<(int, int), string>();
            lastGridValues[key] = lastValuesForGrid;
        }

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                string newValue = cachedFields[row, col].text;

                // skip if unchanged (but now per-series+range)
                if (lastValuesForGrid.TryGetValue((row, col), out var oldValue) && oldValue == newValue)
                    continue;

                // update cache
                lastValuesForGrid[(row, col)] = newValue;

                if (!string.IsNullOrEmpty(newValue) &&
                    int.TryParse(newValue, out int val) && val >= 0 && val <= 999)
                {
                    gridInputData[(row, col)] = newValue;

                    int bettedNum = CalculateNumbers(series, range, row, col);
                    seriesManager.betNumbers[bettedNum] = val;

                    Debug.Log($"Saving: {bettedNum} at series:{series}, range:{range}, row:{row}, col:{col}");
                }
                else
                {
                    gridInputData.Remove((row, col));
                }
            }
        }
        // RecalculateTotals();
    }

    private void SaveAllCurrentGrids()
    {
        foreach (var series in seriesManager.currentSeriesSelected)
        {
            foreach (var range in seriesManager.currentRangeSelected)
            {
                SaveCurrentGridData(series, range);
            }
        }
    }

    public void SaveCellData(int series, int range, int row, int col, int val)
    {
        var key = (series, range);

        if (!allGridData.ContainsKey(key))
        {
            allGridData[key] = new Dictionary<(int, int), string>();
        }

        allGridData[key][(row, col)] = val.ToString();

        int bettedNum = CalculateNumbers(series, range, row, col);
        seriesManager.betNumbers[bettedNum] = val;
    }


    public int CalculateNumbers(int series, int range, int row, int col)
    {
        string preBetNum = (series + range).ToString() + row.ToString() + col.ToString();
        string finalNum = preBetNum.ToString();

        return int.Parse(finalNum);
    }

    public void LoadGridData(int series, int range)
    {
        var key = (series, range);
        if (!allGridData.TryGetValue(key, out var gridInputsData))
            return;

        isUpdatingInputs = true; // ?? Prevent OnValueChanged firing

        // Clear grid first
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                var textField = gridInputs[row, col].transform.GetChild(1).GetComponent<TMP_InputField>();
                textField.SetTextWithoutNotify(""); // ? Important!
            }
        }

        // Fill from saved data
        foreach (var kvp in gridInputsData)
        {
            int row = kvp.Key.Item1;
            int col = kvp.Key.Item2;
            string value = kvp.Value;

            var textField = gridInputs[row, col].transform.GetChild(1).GetComponent<TMP_InputField>();
            textField.SetTextWithoutNotify(value); // ? No event triggered
        }

        isUpdatingInputs = false; // ?? Re-enable input change

         RecalculateTotals();
    }



    void CalculationLogic(int value)
    {
    }

    public void RecalculateTotals()
    {
        if (isUpdatingInputs) return;

        // Always calculate from ALL saved data, regardless of current selection
        Dictionary<int, int> quantitiesPerRange = new Dictionary<int, int>();
        Dictionary<int, int> pointsPerRange = new Dictionary<int, int>();

        // Initialize all ranges to 0
        for (int range = 0; range < 10; range++)
        {
            quantitiesPerRange[range] = 0;
            pointsPerRange[range] = 0;
        }

        // ? CRITICAL: Iterate through ALL saved data, not just current selection
        foreach (var kvp in allGridData)
        {
            int series = kvp.Key.series;
            int range = kvp.Key.range;
            var gridData = kvp.Value;

            foreach (var cell in gridData)
            {
                if (int.TryParse(cell.Value, out int value))
                {
                    quantitiesPerRange[range] += value;
                    // pointsPerRange[range] += CalculatePoints(value);
                }
            }
        }

        // Notify with quantities for EACH range (across all series)
        foreach (var rangeQuantity in quantitiesPerRange)
        {
            int range = rangeQuantity.Key;
            int quantity = rangeQuantity.Value;
            int points = pointsPerRange[range];

            SeriesManager.OnQuantityAdded?.Invoke(quantity, new List<int>(), new List<int> { range });
        }

        Debug.Log($"[RecalculateTotals] Calculated from ALL saved data");
    }
    public void ResetGame()
    {

        // 4. Reset quantity/points for all series & ranges
        if (SeriesManager.OnQuantityAdded != null)
        {
            // Assuming you know the total series and ranges possible
            int totalSeries = 10; // replace with actual total series count
            int totalRanges = 10; // replace with actual total ranges count

            for (int s = 0; s < totalSeries; s++)
            {
                for (int r = 0; r < totalRanges; r++)
                {
                    SeriesManager.OnQuantityAdded.Invoke(0, new List<int> { s }, new List<int> { r });
                }
            }
        }


        Debug.Log("Game reset complete.");
    }


    public void ClearAll()
    {
       // var toastManager = FindAnyObjectByType<ToastManager>();
        //if (toastManager != null && toastManager.transform.parent == null)
        //{
          //  toastManager.transform.SetParent(GameManager.instance.toasterHolderObj.transform);
            // Only mark once, otherwise Unity will complain if it's already in DontDestroyOnLoad
           // DontDestroyOnLoad(toastManager.gameObject);

       // }

        ToastManager.Instance.ShowToast("Cleared");

        StartCoroutine(LoadSceneDelay());
    }

    IEnumerator LoadSceneDelay()
    {
        yield return new WaitForSeconds(0.5f);

        SceneManager.LoadSceneAsync(1);
    }


    public void ResetGameCompletely()
    {
        // 1. Stop all coroutines first
        StopAllCoroutines();

        // 2. Set loading/clearing flags
        isLoading = true;

        // 3. Clear all data storage
        allGridData.Clear();
        seriesManager.betNumbers.Clear();

        // 4. Clear all UI inputs
        ClearMainInputs();
        ClearBandF();

        // 5. Reset series and range selections safely
        seriesManager.currentSeriesSelected.Clear();
        seriesManager.currentRangeSelected.Clear();

        //// Remove toggle listeners temporarily to prevent events
        //foreach (var toggle in seriesManager.seriesToggles)
        //{
        //    if (toggle != null) toggle.onValueChanged.RemoveAllListeners();
        //}
        //foreach (var rangeGroup in seriesManager.rangeGroups)
        //{
        //    if (rangeGroup.toggle != null) rangeGroup.toggle.onValueChanged.RemoveAllListeners();
        //}

        // Set all toggles to false
        foreach (var toggle in seriesManager.seriesToggles)
        {
            if (toggle != null) toggle.isOn = false;
        }
        foreach (var rangeGroup in seriesManager.rangeGroups)
        {
            if (rangeGroup.toggle != null) rangeGroup.toggle.isOn = false;
        }

        // Set default selections
        seriesManager.currentSeriesSelected.Add(10);
        seriesManager.currentRangeSelected.Add(0);
        seriesManager.currentSeriesBase = 10;
        seriesManager.currentRangeIndx = 0;

        // Set default toggles on
        if (seriesManager.seriesToggles.Count > 0 && seriesManager.seriesToggles[0] != null)
            seriesManager.seriesToggles[0].isOn = true;
        if (seriesManager.rangeGroups.Count > 0 && seriesManager.rangeGroups[0].toggle != null)
            seriesManager.rangeGroups[0].toggle.isOn = true;

        // 6. Reset grid numbers
        UpdateGridNumbers(10, 0);

        // 7. Reset toggles and UI states
        HandleAllToggle();
        if (familyToggle != null) familyToggle.isOn = false;

        // 8. Clear external managers
        if (GameManager.instance != null)
        {
            if (GameManager.instance.qntypointsMgr != null)
                GameManager.instance.qntypointsMgr.ClearData();
            if (GameManager.instance.advnceTime != null)
                GameManager.instance.advnceTime.ClearAdvanceTimeData();
        }

        // 9. Reset quantities and points to zero for all ranges
        for (int range = 0; range < 10; range++)
        {
            SeriesManager.OnQuantityAdded?.Invoke(0, new List<int>(), new List<int> { range });
        }

        // 10. Reload empty grid data
        LoadGridData(10, 0);

        // 11. Reset flags
        isLoading = false;

        // 12. Play sound
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySound(SoundManager.Instance.commonSound);

        // 13. Show confirmation
        //if (ToastManager.Instance != null)
        //    ToastManager.Instance.ShowToast("Game Reset Complete");
        seriesManager.OnRangeBtnSelected(0);
        seriesManager.OnSeriesBtnClicked(10, 0);
        Debug.Log("Game completely reset");
    }

    private IEnumerator ReAddToggleListenersAfterFrame()
    {
        yield return new WaitForEndOfFrame();

        // Re-add series toggle listeners
        for (int i = 0; i < seriesManager.seriesToggles.Count; i++)
        {
            if (seriesManager.seriesToggles[i] != null)
            {
                int index = i; // ? Capture the current value
                int seriesValue = seriesManager.seriesValues[index]; // ? Get the value now

                seriesManager.seriesToggles[i].onValueChanged.RemoveAllListeners();
                seriesManager.seriesToggles[i].onValueChanged.AddListener((isOn) =>
                {
                    if (!isLoading) seriesManager.SetSeries(seriesValue); // ? Use captured value
                });
            }
        }

        // Re-add range toggle listeners
        for (int i = 0; i < seriesManager.rangeGroups.Count; i++)
        {
            if (seriesManager.rangeGroups[i].toggle != null)
            {
                int index = i;
                seriesManager.rangeGroups[i].toggle.onValueChanged.RemoveAllListeners();
                seriesManager.rangeGroups[i].toggle.onValueChanged.AddListener((isOn) =>
                {
                    if (!isLoading) seriesManager.OnRangeSelected(index);
                });
            }
        }
    }
    public void ClearPopup()
    {
        ToastManager.Instance.ShowToast("Cleared");
    }


    public void ClearMainInputs()
    {
        foreach (var obj in gridInputs)
        {
            var input = obj.transform.GetChild(1).GetComponent<TMP_InputField>();
            input.SetTextWithoutNotify("");   // ? clears without firing OnValueChanged
        }
    }


    public void ClearSeries()
    {
        seriesManager.currentSeriesSelected.Clear();
    }

    public void ClearRange()
    {
        seriesManager.currentRangeSelected.Clear();
    }

    public void ClearStoredDataFromDictionary()
    {
        allGridData.Clear();
        seriesManager.betNumbers.Clear();
        //  ToastManager.Instance.ShowToast("Cleared");
    }
    public void OnValueAddedInGridInputs()
    {
        int value;
        int finalValue = 0;
        foreach (var obj in gridInputs)
        {
            if (int.TryParse(obj.transform.GetChild(1).GetComponent<TMP_InputField>().text, out value))
            {
                finalValue += value;
            }
            if (finalValue == 0)
            {
                obj.transform.GetChild(1).GetComponent<TMP_InputField>().text = "";
            }
        }
        SeriesManager.OnQuantityAdded?.Invoke(finalValue, seriesManager.currentSeriesSelected, seriesManager.currentRangeSelected);
    }
    public void ClearBandF()
    {
        foreach (var go in allFInputs)
        {
            if (go == null) continue;
            var tmpInput = go.transform.GetChild(1).GetComponent<TMP_InputField>();
            if (tmpInput != null) tmpInput.text = "";
        }

        foreach (var go in allBInputs)
        {
            if (go == null) continue;
            var tmpInput = go.transform.GetChild(1).GetComponent<TMP_InputField>();
            if (tmpInput != null) tmpInput.text = "";
        }
        Debug.Log("All F and B inputs cleared.");
    }

    #endregion
}

[System.Serializable]
public class GridData
{
    public Dictionary<(int row, int col), string> inputs = new Dictionary<(int, int), string>();
}