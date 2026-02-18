using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using static OutputNumGenerator;
using System.Collections;
using System.Xml;
using UnityEngine.Networking;
using Unity.VisualScripting;
using Unity.Burst.Intrinsics;

public class OutputNumGenerator : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField numberInput;
    public Toggle selectAllToggle;
    public Toggle straightToggle;
    public Toggle boxToggle;
    public Toggle frontPairToggle;
    public Toggle backPairToggle;
    public Toggle splitPairToggle;
    public Toggle anyPairToggle;

    public List<Toggle> allToggles;
    public Dictionary<string, List<BetEntry>> AResults = new Dictionary<string, List<BetEntry>>();
    public Dictionary<string, List<BetEntry>> BResults = new Dictionary<string, List<BetEntry>>();


    // ? Currently active payout rate
    public List<Toggle> rates = new List<Toggle>();
    public int activeRate = 10;

    [Header("Results")]
    public Toggle AllToggle;
    public Toggle AToggle;
    public Toggle BToggle;

    [Header("Prefab Setup")]
    public GameObject resultPrefab;  // Prefab with 2 children: 0 = PlayType, 1 = Number
    public Transform resultsParent;  // Parent where prefabs will spawn

    [Header("Combination")]
    public TMP_InputField combinationsField;
    public Toggle singleToggle;
    public Toggle doubleToggle;
    public Toggle tripleToggle;

    public BetData betData;
    public QntyPointsManager ptsManager;

    public int resultSelectedCount = 1;


    private void Start()
    {
        numberInput.Select();
        // Default: A toggle ON
        AToggle.isOn = true;
        BToggle.isOn = false;
        AllToggle.isOn = false;

        // Setup listeners
        AllToggle.onValueChanged.AddListener(OnAllToggleChanged);
        AToggle.onValueChanged.AddListener((bool isOn) =>
        {
            OnIndividualToggleChanged(isOn, "A");
        });
        BToggle.onValueChanged.AddListener((bool isOn) =>
        {
            OnIndividualToggleChanged(isOn, "B");
        });
        AllToggle.onValueChanged.AddListener(delegate { ResultCounter(); });
        AToggle.onValueChanged.AddListener(delegate { ResultCounter(); });
        BToggle.onValueChanged.AddListener(delegate { ResultCounter(); });

        selectAllToggle.onValueChanged.AddListener(isOn => SelectDeselectAllToggles(isOn));
        rates[0].isOn = true;
        // Setup toggle listeners
        foreach (var toggle in rates)
        {
            toggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    // Turn OFF all others
                    foreach (var t in rates)
                    {
                        if (t != toggle) t.isOn = false;
                    }

                    // ? Parse rate from toggle’s child text
                    TMP_Text txt = toggle.GetComponentInChildren<TMP_Text>();
                    if (txt && int.TryParse(txt.text, out int parsedRate))
                    {
                        activeRate = parsedRate;
                    }
                }
            });
        }

        // Initialize active rate (first toggle ON if available)
        if (rates.Count > 0 && rates[0].isOn)
        {
            TMP_Text txt = rates[0].GetComponentInChildren<TMP_Text>();
            if (txt && int.TryParse(txt.text, out int parsedRate))
                activeRate = parsedRate;
        }

    }

    #region Normal Generate with input
    // ? Overload 1: Accepts TMP_InputField directly (manual input)
    public void GenerateResults(TMP_InputField inputField)
    {
        if (inputField == null) return;

        string input = inputField.text.Trim();

        // Numeric check
        if (!int.TryParse(input, out _))
        {
            Debug.LogWarning("Please enter a numeric value.");
            return;
        }

        bool fpOn = frontPairToggle != null && frontPairToggle.isOn;
        bool bpOn = backPairToggle != null && backPairToggle.isOn;
        bool spOn = splitPairToggle != null && splitPairToggle.isOn;
        bool apOn = anyPairToggle != null && anyPairToggle.isOn;

        bool anyPairOn = fpOn || bpOn || spOn || apOn;

        // Any toggle OTHER than FP / BP
        bool otherToggleOn =
            (straightToggle != null && straightToggle.isOn) ||
            (boxToggle != null && boxToggle.isOn);

        // -----------------------------
        // ?? Case 1: 2-digit input
        // -----------------------------
        if (input.Length == 2)
        {
            // Must be ONLY FP / BP
            if (!anyPairOn)
            {
                Debug.LogWarning("2-digit input is allowed only for Front Pair or Back Pair.");
                return;
            }

            if (otherToggleOn)
            {
                Debug.LogWarning("3-digit input is required when other play types are selected.");
                return;
            }

            inputField.text = "";
            GenerateResults(input);
            return;
        }

        // -----------------------------
        // ?? Case 2: 3-digit input
        // -----------------------------
        if (input.Length == 3)
        {
            inputField.text = "";
            GenerateResults(input);
            return;
        }

        // -----------------------------
        // ?? Invalid length
        // -----------------------------
        Debug.LogWarning("Please enter a valid 2 or 3 digit number.");
    }




    //  Overload 2: Accepts string directly (for range generator)
    public void GenerateResults(string txt)
    {
        string input = txt.Trim();

        bool isNumeric = int.TryParse(input, out _);

        // -------------------------------
        // ?? CASE 1: 2-digit FP / BP only
        // -------------------------------
        if (input.Length == 2 && isNumeric)
        {


            // Only allowed if FP or BP is selected
            if ((frontPairToggle != null && frontPairToggle.isOn) ||
                (backPairToggle != null && backPairToggle.isOn) || (splitPairToggle != null && splitPairToggle.isOn) ||
                (anyPairToggle != null && anyPairToggle.isOn))
            {
                var results = new List<(string type, string number)>();

                void Add2DigitResult(Toggle toggle, string type, string number)
                {
                    if (toggle != null && toggle.isOn)
                    {
                        results.Add((type, number));
                        ptsManager.AddBet(type, activeRate, 1);

                        if (AToggle != null && AToggle.isOn)
                            AddToList("A", type, number, activeRate);

                        if (BToggle != null && BToggle.isOn)
                            AddToList("B", type, number, activeRate);

                        if ((AToggle != null && !AToggle.isOn) &&
                            (BToggle != null && !BToggle.isOn))
                        {
                            AddToList("A", type, number, activeRate);
                        }
                    }
                }

                // ?? Generate FP & BP from same 2-digit number
                Add2DigitResult(frontPairToggle, "FP", input);
                Add2DigitResult(backPairToggle, "BP", input);
                Add2DigitResult(splitPairToggle, "SP", input);
                Add2DigitResult(anyPairToggle, "AP", input);

                // Spawn prefabs
                foreach (var (type, number) in results)
                {
                    GameObject obj = Instantiate(resultPrefab, resultsParent);
                    obj.transform.GetChild(1).GetComponent<TMP_Text>().text = type;
                    obj.transform.GetChild(2).GetComponent<TMP_Text>().text = number;

                    obj.GetComponent<ResultPrefab>().number = number;
                    obj.GetComponent<ResultPrefab>().type = type;
                    obj.GetComponent<ResultPrefab>().amount = activeRate;
                }

                return; // ? stop here, don't run 3-digit logic
            }

            Debug.LogWarning("2-digit input allowed only for Front Pair or Back Pair.");
            return;
        }

        // -------------------------------
        // ?? CASE 2: Normal 3-digit logic
        // -------------------------------
        if (input.Length != 3 || !isNumeric)
        {
            Debug.LogWarning("Please enter a valid 3-digit number.");
            return;
        }

        string d1 = input[0].ToString();
        string d2 = input[1].ToString();
        string d3 = input[2].ToString();

        var results3 = new List<(string type, string number)>();

        void AddResult(Toggle toggle, string type, string number)
        {
            if (toggle != null && toggle.isOn)
            {
                results3.Add((type, number));
                ptsManager.AddBet(type, activeRate, 1);

                if (AToggle != null && AToggle.isOn)
                    AddToList("A", type, number, activeRate);

                if (BToggle != null && BToggle.isOn)
                    AddToList("B", type, number, activeRate);

                if ((AToggle != null && !AToggle.isOn) &&
                    (BToggle != null && !BToggle.isOn))
                {
                    AddToList("A", type, number, activeRate);
                }
            }
        }

        // Single plays
        AddResult(straightToggle, "STR", input);
        AddResult(boxToggle, "BOX", input);
        AddResult(frontPairToggle, "FP", d1 + d2);
        AddResult(backPairToggle, "BP", d2 + d3);
        AddResult(splitPairToggle, "SP", d1 + d3);

        // Any Pair
        if (anyPairToggle != null && anyPairToggle.isOn)
        {
            results3.Add(("AP", d1 + d2));
            results3.Add(("AP", d2 + d3));
            results3.Add(("AP", d1 + d3));
            ptsManager.AddBet("AP", activeRate, 3);
        }

        // Spawn prefabs
        foreach (var (type, number) in results3)
        {
            GameObject obj = Instantiate(resultPrefab, resultsParent);
            obj.transform.GetChild(1).GetComponent<TMP_Text>().text = type;
            obj.transform.GetChild(2).GetComponent<TMP_Text>().text = number;

            obj.GetComponent<ResultPrefab>().number = number;
            obj.GetComponent<ResultPrefab>().type = type;
            obj.GetComponent<ResultPrefab>().amount = activeRate;
        }
    }

    public void GenerateResultsForCombination(string txt)
    {
        string input = txt.Trim();

        if (input.Length != 3 || !int.TryParse(input, out _))
        {
            Debug.LogWarning("Please enter a valid 3-digit number.");
            return;
        }


        string d1 = input[0].ToString();
        string d2 = input[1].ToString();
        string d3 = input[2].ToString();

        var results = new List<(string type, string number)>();

        void AddResult(Toggle toggle, string type, string number)
        {
            if (toggle != null && toggle.isOn)
            {
                results.Add((type, number));
                ptsManager.AddBet(type, activeRate, 1);

                // --- Store bets into A or B ---
                if (AToggle != null && AToggle.isOn)
                {
                    AddToList("A", type, number, activeRate);
                }

                if (BToggle != null && BToggle.isOn)
                {
                    AddToList("B", type, number, activeRate);
                }

                // Optional: if neither A nor B is selected, default to A
                if ((AToggle != null && !AToggle.isOn) && (BToggle != null && !BToggle.isOn))
                {
                    AddToList("A", type, number, activeRate);
                }
            }
        }



        // Single plays
        AddResult(straightToggle, "STR", input);

        // Spawn prefabs
        foreach (var (type, number) in results)
        {
            GameObject obj = Instantiate(resultPrefab, resultsParent);
            obj.transform.GetChild(1).GetComponent<TMP_Text>().text = type;
            obj.transform.GetChild(2).GetComponent<TMP_Text>().text = number;

            string capturedType = type;
            string capturedNumber = number;
            int capturedAmount = activeRate;
            obj.GetComponent<ResultPrefab>().number = number;
            obj.GetComponent<ResultPrefab>().type = type;
            obj.GetComponent<ResultPrefab>().amount = capturedAmount;
            Button btn = obj.transform.GetChild(0).GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                // Remove from betData.A
                var entryA = betData.A.Find(e => e.Type == capturedType && e.Number == capturedNumber && e.Amount == capturedAmount);
                if (entryA != null) betData.A.Remove(entryA);

                // Remove from betData.B
                var entryB = betData.B.Find(e => e.Type == capturedType && e.Number == capturedNumber && e.Amount == capturedAmount);
                if (entryB != null) betData.B.Remove(entryB);

                // Optional: also update QntyPointsManager (subtract points)
                ptsManager.totalPoints -= capturedAmount;
                ptsManager.totalPointsTxt.text = ptsManager.totalPoints.ToString();

                ptsManager.RemoveBet(capturedType, capturedAmount);
                Destroy(obj);

            });
        }
    }
    private void OnAllToggleChanged(bool isOn)
    {
        if (isOn)
        {
            AToggle.isOn = true;
            BToggle.isOn = true;
        }
        else
        {
            AToggle.isOn = true;
            BToggle.isOn = false;
        }
    }

    private void OnIndividualToggleChanged(bool isOn, string result)
    {

        Debug.Log($"Toggle {result} changed to {isOn}");
        // Keep AllToggle synced
        AllToggle.SetIsOnWithoutNotify(AToggle.isOn && BToggle.isOn);

        // Only handle toggle turned ON
        if (isOn)
        {
            
            foreach (Transform child in resultsParent)
            {
                ResultPrefab data = child.GetComponent<ResultPrefab>();
                if (data == null) continue;

                if (result == "A")
                {
                    AddToList("A", data.type, data.number, data.amount);
                   ptsManager.AddBet(data.type, data.amount, 1);
                }
                else if (result == "B")
                {
                    AddToList("B", data.type, data.number, data.amount);
                    ptsManager.AddBet(data.type, data.amount, 1);
                }
            }
        }
        //else
        //{
        //    // Optional: clear this toggle’s dictionary when turned off
        //    if (result == "A")
        //    {
        //        RemoveFromList("A"); // implement your clear logic
        //    }
        //    else if (result == "B")
        //    {
        //        RemoveFromList("B");
        //    }
        //}
    }



    //private void AddToDictionary(Dictionary<string, List<BetEntry>> dict, string type, string number, int amount)
    //{
    //    if (!dict.ContainsKey(type))
    //        dict[type] = new List<BetEntry>();

    //    dict[type].Add(new BetEntry(type,number, amount));
    //    DebugDictionary(dict, "AResults");
    //}

    //public void DebugDictionary(Dictionary<string, List<BetEntry>> dict, string dictName = "Dictionary")
    //{
    //    if (dict == null || dict.Count == 0)
    //    {
    //        Debug.Log($"{dictName} is empty.");
    //        return;
    //    }

    //    Debug.Log($"---- {dictName} Contents ----");
    //    foreach (var kvp in dict)
    //    {
    //        string type = kvp.Key;
    //        List<BetEntry> entries = kvp.Value;

    //        Debug.Log($"Type: {type} ? Count: {entries.Count}");
    //        foreach (var entry in entries)
    //        {
    //            Debug.Log($"    Number: {entry.Number}, Amount: {entry.Amount}");
    //        }
    //    }
    //    Debug.Log($"---- End of {dictName} ----");
    //}

    public void LoadResultsFromLists(BetData betData)
    {
        ClearResultsView();

        if ((betData.A == null || betData.A.Count == 0) &&
            (betData.B == null || betData.B.Count == 0))
        {
            Debug.Log("No results to load from betData.");
            return;
        }

        // Load A entries
        foreach (var entry in betData.A)
        {
            SpawnResult(entry);
        }

        // Load B entries
        foreach (var entry in betData.B)
        {
            SpawnResult(entry);
        }
    }

    private void SpawnResult(BetEntry entry)
    {
        //  GenerateResults(entry.Number);

        GameObject obj = Instantiate(resultPrefab, resultsParent);
        obj.transform.GetChild(1).GetComponent<TMPro.TMP_Text>().text = entry.Type;
        obj.transform.GetChild(2).GetComponent<TMPro.TMP_Text>().text = entry.Number;
        string capturedType = entry.Type;
        string capturedNumber = entry.Number;
        int capturedAmount = activeRate;
        obj.GetComponent<ResultPrefab>().number = capturedNumber;
        obj.GetComponent<ResultPrefab>().type = capturedType;
        obj.GetComponent<ResultPrefab>().amount = capturedAmount;
        Button btn = obj.transform.GetChild(0).GetComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            // Remove from betData.A
            var entryA = betData.A.Find(e => e.Type == capturedType && e.Number == capturedNumber && e.Amount == capturedAmount);
            if (entryA != null) betData.A.Remove(entryA);

            // Remove from betData.B
            var entryB = betData.B.Find(e => e.Type == capturedType && e.Number == capturedNumber && e.Amount == capturedAmount);
            if (entryB != null) betData.B.Remove(entryB);

            // Optional: also update QntyPointsManager (subtract points)
            ptsManager.totalPoints -= capturedAmount;
            ptsManager.totalPointsTxt.text = ptsManager.totalPoints.ToString();

            ptsManager.RemoveBet(capturedType, capturedAmount);
            Destroy(obj);

        });
        // obj.transform.GetChild(3).GetComponent<TMPro.TMP_Text>().text = entry.Amount.ToString();

        // Optional: update points manager
        // ptsManager.AddBet(entry.Type, entry.Amount);
    }



    public void OnResultABtnClicked()
    {
        ClearResultsView();
        foreach (var entry in betData.A)
        {
            SpawnResult(entry);
        }
    }

    public void OnResultBBtnClicked()
    {
        ClearResultsView();

        // Make a copy so removing later doesn’t break iteration
        var copy = new List<BetEntry>(betData.B);

        foreach (var entry in copy)
        {
            SpawnResult(entry);
        }
    }



    public void SelectDeselectAllToggles(bool state)
    {
        foreach (var obj in allToggles)
        {
            obj.isOn = state;
        }

        if (!state)
        {
            allToggles[1].isOn = true;
        }
    }
    #endregion

    void ResultCounter()
    {
        resultSelectedCount = 0;

        if (AllToggle != null && AllToggle.isOn)
        {
            resultSelectedCount = 2; // All = always 2
        }
        else
        {
            // A = 1, B = 1
            if (AToggle != null && AToggle.isOn) resultSelectedCount += 1;
            if (BToggle != null && BToggle.isOn) resultSelectedCount += 1;

            // Cap at 2 max
            if (resultSelectedCount > 2)
                resultSelectedCount = 2;
        }

        Debug.Log("Result Count = " + resultSelectedCount);
    }

    public void ValidateUniqueDigits()
    {
        string input = combinationsField.text;
        string result = "";

        foreach (char c in input)
        {
            if (char.IsDigit(c) && !result.Contains(c))
            {
                result += c; // only add if not already present
            }
        }

        // Update the input field with validated string
        combinationsField.SetTextWithoutNotify(result);
    }

    public void GenerateCombinations(TMP_InputField field)
    {
        if (string.IsNullOrEmpty(field.text) || field.text.Length < 3)
            return;

        if (!straightToggle.isOn)
        {
            ToastManager.Instance.ShowToast("Please check straight check box");
            return;
        }

        char[] digits = field.text.ToCharArray();
        HashSet<string> generated = new HashSet<string>(); // avoid duplicates

        //  SINGLE (Straight = all permutations of 3 unique digits)
        if (singleToggle != null && singleToggle.isOn)
        {
            foreach (var perm in GetPermutations(digits, 3))
            {
                string combo = new string(perm.ToArray());
                if (generated.Add(combo)) // only new combos
                    GenerateResultsForCombination(combo); // prefab spawn
            }
        }
        //  DOUBLE (exactly two digits same, one different)
        if (doubleToggle != null && doubleToggle.isOn)
        {
            var distinct = new HashSet<char>(digits);

            foreach (char same in distinct) // digit that repeats
            {
                foreach (char diff in distinct)
                {
                    if (same == diff) continue; // skip same (prevents triple)

                    string baseCombo = $"{same}{same}{diff}";

                    // now allow permutations WITH repeats
                    foreach (var perm in GetAllPermutations(baseCombo.ToCharArray(), 3))
                    {
                        string c = new string(perm.ToArray());

                        // ensure it's exactly "double" (two same, one different)
                        if (c.Distinct().Count() == 2 &&
                            (c.Count(x => x == same) == 2 || c.Count(x => x == diff) == 2))
                        {
                            if (generated.Add(c))
                                GenerateResultsForCombination(c); // prefab spawn
                        }
                    }
                }
            }
        }

        //  TRIPLE (all three digits same)
        if (tripleToggle != null && tripleToggle.isOn)
        {
            foreach (char d in digits)
            {
                string triple = new string(d, 3); // "111", "222", etc.
                if (generated.Add(triple))
                    GenerateResultsForCombination(triple); // prefab spawn
            }
        }
        combinationsField.text = "";
    }


    // Add an entry to the respective list
    public void AddToList(string dictName, string type, string number, int amount)
    {
        BetEntry entry = new BetEntry(type, number, amount);

        if (dictName == "A")
            betData.A.Add(entry);
        else if (dictName == "B")
            betData.B.Add(entry);
    }


    void ClearResultsView()
    {
        for (int i = resultsParent.childCount - 1; i >= 0; i--)
        {
            Destroy(resultsParent.GetChild(i).gameObject);
        }
    }


    public static IEnumerable<IEnumerable<T>> GetPermutations<T>(IEnumerable<T> list, int length)
    {
        if (length == 1) return list.Select(t => new T[] { t });

        return GetPermutations(list, length - 1)
            .SelectMany(t => list.Where(e => !t.Contains(e)),
                        (t1, t2) => t1.Concat(new T[] { t2 }));
    }


    public static IEnumerable<IEnumerable<T>> GetAllPermutations<T>(IEnumerable<T> list, int length)
    {
        if (length == 1) return list.Select(t => new T[] { t });

        return GetAllPermutations(list, length - 1)
            .SelectMany(t => list,
                        (t1, t2) => t1.Concat(new T[] { t2 }));
    }

    public void ResetCombinations()
    {

        if (combinationsField != null)
            combinationsField.text = "";

        // Reset digit selections



        if (singleToggle != null) singleToggle.isOn = true;
        if (doubleToggle != null) doubleToggle.isOn = false;
        if (tripleToggle != null) tripleToggle.isOn = false;
    }


    public void ResetAllData()
    {
        ClearResultsView();
        foreach (var obj in rates)
        {
            obj.isOn = false;
        }
        rates[0].isOn = true;
        betData.A.Clear();
        betData.B.Clear();
        SelectDeselectAllToggles(false);
        selectAllToggle.isOn = false;
        AllToggle.isOn = false;
        AToggle.isOn = true;
        BToggle.isOn = false;
        GameManager_3D.instance.qntyMgr.ResetSpots();
        GameManager_3D.instance.rangeMgr.ResetFields();
        ResetCombinations();
        StartCoroutine(GameManager_3D.instance.advnceTime.FetchSlotsFromAPI());
        ToastManager.Instance.ShowToast("Cleared");
    }



    [System.Serializable]
    public class BetEntry
    {
        public string Type;
        public string Number;
        public int Amount;

        public BetEntry(string type, string number, int amount)
        {
            Type = type;
            Number = number;
            Amount = amount;
        }
    }

    [System.Serializable]
    public class BetData
    {
        public int userid;
        public List<string> draw_time = new List<string>();
        public List<BetEntry> A = new List<BetEntry>();
        public List<BetEntry> B = new List<BetEntry>();

        public BetData(int userId, string drawTime)
        {
            userid = userId;
            draw_time.Add(drawTime);
        }
    }






}
