using NUnit.Framework.Constraints;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
                gridInputs[r, c] = child.gameObject;
                index++;
            }
        }
    }

    private void Start()
    {
        InitGridTracking();

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
                var inputField = gridInputs[r, c].transform.GetChild(1).GetComponent<TMP_InputField>();
                int capturedRow = r;
                int capturedCol = c;

                inputField.onValueChanged.AddListener(newValue => OnSingleInputChanged(capturedRow, capturedCol, newValue));
                inputField.onSubmit.AddListener(newValue => OnSingleInputChanged(capturedRow, capturedCol, newValue));
            }
        }

        allNum.onValueChanged.AddListener((isOn) => HandleToggle(allNum, isOn));
        evenNum.onValueChanged.AddListener((isOn) => HandleToggle(evenNum, isOn));
        oddNum.onValueChanged.AddListener((isOn) => HandleToggle(oddNum, isOn));
    }

    private Coroutine debounceCoroutine;

    private void OnSingleInputChanged(int r, int c, string newValue)
    {
        if (isUpdatingInputs)
        {
            return;
        }

        // Stop any previous debounce coroutine to prevent redundant saves
        if (debounceCoroutine != null)
        {
            StopCoroutine(debounceCoroutine);
        }

        // Start a new coroutine that will wait before saving the data
        debounceCoroutine = StartCoroutine(DebounceSave(r, c, newValue));
    }

    private IEnumerator DebounceSave(int r, int c, string newValue)
    {
        // Wait for a short duration after the last input
        // 0.2f is a good starting point, adjust as needed
        yield return new WaitForSeconds(0.2f);

        TMP_InputField field = gridInputs[r, c].transform.GetChild(1).GetComponent<TMP_InputField>();

        int value = 0;
        if (int.TryParse(newValue, out int parsedValue))
        {
            value = Mathf.Clamp(parsedValue, 0, 999);
            field.text = value.ToString();
        }
        else
        {
            field.text = "";
        }

        if (familyToggle.isOn)
        {
            if (int.TryParse(gridInputs[r, c].transform.GetChild(0).GetComponent<TMP_Text>().text, out int baseNumber))
            {
                UpdateFamily(baseNumber, field.text);
            }
        }

        // Only now, after the debounce, do we perform the expensive operation
        // The foreach loops are here as per your logic
        foreach (int series in seriesManager.currentSeriesSelected)
        {
            foreach (int range in seriesManager.currentRangeSelected)
            {
                UpdateSingleCellData(series, range, r, c, value);
            }
        }

        RecalculateTotals();

        debounceCoroutine = null;
    }

    // A new, more efficient helper function to update a single cell
    public void UpdateSingleCellData(int series, int range, int row, int col, int value)
    {
        var key = (series, range);
        if (!allGridData.ContainsKey(key))
        {
            allGridData[key] = new Dictionary<(int, int), string>();
        }

        var gridInputdata = allGridData[key];
        gridInputdata[(row, col)] = value.ToString();

        int bettedNum = CalculateNumbers(series, range, row, col);

        if (value > 0)
        {
            // Add or update the bet number
            seriesManager.betNumbers[bettedNum] = value;
            Debug.Log($"Added/Updated bet number: {bettedNum} with value: {value}.");
        }
        else
        {
            // Remove the bet number
            seriesManager.betNumbers.Remove(bettedNum);
            Debug.Log($"Removed bet number: {bettedNum}.");
        }

        // Always debug the current state of the dictionary
        string dictLog = "Current betNumbers: ";
        foreach (var kvp in seriesManager.betNumbers)
        {
            dictLog += $"[{kvp.Key}:{kvp.Value}] ";
        }
        Debug.Log(dictLog);
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

    void FillRow(int rowIndex, string value)
    {
        Debug.Log("Fill row called");
        isUpdatingInputs = true;

        for (int c = 0; c < cols; c++)
        {
            // --- If no value entered ---
            if (string.IsNullOrEmpty(value))
            {
                rowValues[rowIndex, c] = null;
            }
            else if (int.TryParse(value, out int amount))
            {
                // --- If "All" toggle is ON ---
                if (allNum != null && allNum.isOn)
                {
                    rowValues[rowIndex, c] = amount;
                }
                // --- If "Even" toggle is ON (apply only to even columns) ---
                else if (evenNum != null && evenNum.isOn && c % 2 == 0)
                {
                    rowValues[rowIndex, c] = amount;
                }
                // --- If "Odd" toggle is ON (apply only to odd columns) ---
                else if (oddNum!= null && oddNum.isOn && c % 2 != 0)
                {
                    rowValues[rowIndex, c] = amount;
                }
                // --- If no toggle is ON, clear cells ---
                else
                {
                    rowValues[rowIndex, c] = null;
                }
            }

            UpdateCell(rowIndex, c);
        }

        isUpdatingInputs = false;
    }




    void FillColumn(int colIndex, string value)

    {

        isUpdatingInputs = true;

        for (int r = 0; r < rows; r++)
        {

            if (string.IsNullOrEmpty(value))

                colValues[r, colIndex] = null;

            else if (int.TryParse(value, out int amount))

                colValues[r, colIndex] = amount;



            UpdateCell(r, colIndex);

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

        StartCoroutine(DelayCall(parsedValue));
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

    public void SaveCurrentGridData(int series, int range)
    {
        var key = (series, range);
        Debug.Log("Saved key : " + key + " For series : " + series + " range : " + range );
        // ? FIX: Instead of clearing the whole dictionary,
        // we manage the data for this specific key.
        if (!allGridData.ContainsKey(key))
        {
            allGridData[key] = new Dictionary<(int, int), string>();
        }

        // Clear only the specific dictionary for the current series/range
        var gridInputdata = allGridData[key];
      //  gridInputdata.Clear();

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                var textField = gridInputs[row, col].transform.GetChild(1).GetComponent<TMP_InputField>();
                string text = textField.text;

                if (!string.IsNullOrEmpty(text))
                {
                    if (int.TryParse(text, out int val) && val >= 0 && val <= 999)
                    {
                        gridInputdata[(row, col)] = text;
                        int bettedNum = CalculateNumbers(series, range, row, col);

                        if (seriesManager.betNumbers.ContainsKey(bettedNum))
                        {
                            seriesManager.betNumbers[bettedNum] = val;
                        }
                        else
                        {
                            seriesManager.betNumbers.Add(bettedNum, val);
                          //  Debug.Log("Number : " + bettedNum + " Added to dictionary : " + "with value : " + val);

                        }

                    }
                    // Debug the dictionary contents
                    string dictLog = "Current betNumbers: ";
                    foreach (var kvp in seriesManager.betNumbers)
                    {
                        dictLog += $"[{kvp.Key} : {kvp.Value}] ";
                    }
                   // Debug.Log("Current bet nums : " + dictLog);
                }
            }
        }
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
        Debug.Log("Loading data for key : " + key + " For series : " + series + " range : " + range);
        if (!allGridData.ContainsKey(key))
        {
            return;
        }
        Debug.Log("Return for loading grid data not found");

        isUpdatingInputs = true;
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                var textField = gridInputs[row, col].transform.GetChild(1).GetComponent<TMP_InputField>();
                textField.text = "";
            }
        }

        var gridInputsdata = allGridData[key];
        if(gridInputsdata.Keys.Count == 0)
        {
            Debug.Log("Grid Input data is null");
        }
        foreach (var kvp in gridInputsdata)
        {
            int row = kvp.Key.Item1;
            int col = kvp.Key.Item2;
            string value = kvp.Value;

            var textField = gridInputs[row, col].transform.GetChild(1).GetComponent<TMP_InputField>();
            textField.text = value;
            Debug.Log($"Loaded series range :  ({series},{range}) at {row},{col} with value : {value}");
        }
        isUpdatingInputs = false;

        // ? NEW: Add this call to ensure totals are updated after loading a saved grid
        RecalculateTotals();
    }

    void CalculationLogic(int value)
    {
    }

    // ? REFACTORED: Renamed from OnValueAddedInGridInputs to a more descriptive name
    // and made it public so other methods can call it.
    public void RecalculateTotals()
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

    public void ClearAll()
    {
        seriesManager.ClearAllSeriesAndRange();
        ClearMainInputs();
        ClearStoredDataFromDictionary();
        // Assuming GameManager and ToastManager exist
        GameManager.instance.qntypointsMgr.ClearData();
        ClearBandF();
        HandleAllToggle();
        GameManager.instance.advnceTime.ClearAdvanceTimeData();
        familyToggle.isOn = false;
        SoundManager.Instance.PlaySound(SoundManager.Instance.commonSound);
    }
    public void ClearPopup()
    {
        ToastManager.Instance.ShowToast("Cleared");
    }


    public void ClearMainInputs()
    {
        foreach (var obj in gridInputs)
        {
            obj.transform.GetChild(1).GetComponent<TMP_InputField>().text = "";
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