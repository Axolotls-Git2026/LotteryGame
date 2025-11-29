using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using SFB;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Threading.Tasks;
using static GameManager_3D;
using UnityEngine.UI;
using System;
using System.Linq.Expressions;

public class GameManager : MonoBehaviour
{
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(System.IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern System.IntPtr GetActiveWindow();
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
    const int SW_MINIMIZE = 6;


    public static GameManager instance;
    public SeriesManager seriesMgr;
    public GridManager gridMgr;
    public QuantityPointsManager qntypointsMgr;
    public TimeIncrementer incrementer;
    public AdvanceTime advnceTime;
    public Button buybtn;

    [Header("Game Data")]
    public GameObject[] dataObjs;
    public GameObject[] resultObjs;

    [Header("Timer Data")]
    //public TMP_Text mins;
    //public TMP_Text secs;
    public bool isTimeCompleted;
    public GameObject[] timerObjs;   // 4 parent objects

    public GameObject loadingObj;
    public GameObject toastPopup;
    public GameObject randomNumObj;

    public TMP_Text drawTime;
    public TMP_Text drawId;
    public TMP_Text lastResultMin;
    public TMP_Text lastResultSec;
    public TMP_InputField barcodeTxt;

    public GameObject resetPassPnl;
    public GameObject toasterHolderObj;
    public Canvas canvas;
    private void Awake()
    {
        instance = this;
    }
    private void OnEnable()
    {
        StartCoroutine(FetchUserData());

    }
    private void Start()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        if (PlayerPrefs.GetString("pass_count") == "0")
        {
            resetPassPnl.gameObject.SetActive(true);
        }
        //  StartCoroutine(FetchUserData());
        StartCoroutine(FetchResultsOnStart());
        CheckSession();
        UpdatePlayerStatus(PlayerPrefs.GetInt("UserId"));
        GetTimer();
        FindAnyObjectByType<LoginManager>()?.gameObject.SetActive(false);
        //  toasterHolderObj = GameObject.FindGameObjectWithTag("LoginCanvas");
        //  ToastManager.Instance.transform.SetParent(canvas.transform);
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            seriesMgr.ClearAllSeriesAndRange();
            gridMgr.ClearAll();
            qntypointsMgr.ClearData();
            //  gridMgr.ClearPopup();

        }
    }

    public void LoadScene(int _index)
    {
        SceneManager.LoadScene(_index);
    }

    #region Logout API
    public void LogOUT()
    {
        UpdatePlayerStatus(PlayerPrefs.GetInt("UserId"));
        PlayerPrefs.DeleteAll();
        LoadScene(0);

    }

    #endregion
    public IEnumerator FetchUserData()
    {
        WWWForm form = new WWWForm();
        form.AddField("id", PlayerPrefs.GetInt("UserId"));

        using (UnityWebRequest www = UnityWebRequest.Post(GameAPIs.getUserDataAPi, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error fetching user data: " + www.error);
            }
            else
            {
                Debug.Log("Response: " + www.downloadHandler.text);

                UserDataResponse data = JsonUtility.FromJson<UserDataResponse>(www.downloadHandler.text);

                if (data != null)
                {
                    // Define which fields to map
                    Dictionary<string, string> kv = new Dictionary<string, string>
                    {
                        { "Agent Id", PlayerPrefs.GetInt("UserId").ToString() },
                        { "Limit", data.limit },
                        { "Last Transaction", data.last_trn },
                        { "Transaction Points", data.tran_pt },
                        { "Date Time", data.datetime },
                        { "Current Slot", data.current_slot }
                    };
                    drawTime.text = data.current_slot;
                    drawId.text = data.draw_id;
                    if (!advnceTime.selectedTimes.Contains(data.current_slot))
                    {
                        advnceTime.selectedTimes.Add(data.current_slot);
                    }
                    advnceTime.RecalculationForAdvTime();


                    PlayerPrefs.SetInt("selectedTimes", advnceTime.selectedTimes.Count);
                    string[] previousSlotParts = data.previous_slot.Split(':');
                    if (previousSlotParts.Length == 2)
                    {
                        lastResultMin.text = previousSlotParts[0];
                        lastResultSec.text = previousSlotParts[1].Split(' ')[0]; // Splits "15 PM" to get "15"
                    }
                    int index = 0;
                    foreach (var entry in kv)
                    {
                        if (index < dataObjs.Length)
                        {
                            TMP_Text label = dataObjs[index].transform.GetChild(0).GetComponent<TMP_Text>();
                            TMP_Text value = dataObjs[index].transform.GetChild(1).GetComponent<TMP_Text>();

                            label.text = entry.Key;   // static label
                            value.text = entry.Value; // API value
                        }
                        index++;
                    }
                }
                incrementer.StartHeaderCurrentTime();
            }
        }
    }

    public IEnumerator FetchResults(GameObject[] list1, GameObject[] list2, GameObject[] list3)
    {
        CheckSession();

        WWWForm form = new WWWForm();
        form.AddField("id", PlayerPrefs.GetInt("UserId"));

        using (UnityWebRequest www = UnityWebRequest.Post(GameAPIs.getResultsAPi, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error fetching results: " + www.error);
            }
            else
            {
                Debug.Log("Results Response: " + www.downloadHandler.text);

                ResultsResponse res = JsonUtility.FromJson<ResultsResponse>(www.downloadHandler.text);

                if (res != null && res.status == "success" && res.data != null)
                {
                    // Reverse the list
                    res.data.Reverse();

                    // --- Existing resultObjs assignment ---
                    for (int i = 0; i < resultObjs.Length && i < res.data.Count; i++)
                    {
                        TMP_Text txt = resultObjs[i].transform.GetChild(0).GetComponent<TMP_Text>();
                        txt.text = res.data[i].number;
                    }

                    // --- New logic: fill list1, list2, list3 sequentially ---
                    int index = 0;

                    // Fill list1
                    for (int i = 0; i < list1.Length && index < res.data.Count; i++, index++)
                    {
                        TMP_Text txt = list1[i].transform.GetChild(0).GetComponent<TMP_Text>();
                        txt.text = res.data[index].number;
                    }

                    // Fill list2
                    for (int i = 0; i < list2.Length && index < res.data.Count; i++, index++)
                    {
                        TMP_Text txt = list2[i].transform.GetChild(0).GetComponent<TMP_Text>();
                        txt.text = res.data[index].number;
                    }

                    // Fill list3
                    for (int i = 0; i < list3.Length && index < res.data.Count; i++, index++)
                    {
                        TMP_Text txt = list3[i].transform.GetChild(0).GetComponent<TMP_Text>();
                        txt.text = res.data[index].number;
                    }
                }
            }
        }
        // gridMgr.ClearAll();
        SceneManager.LoadSceneAsync(1);
        // yield return StartCoroutine(ClearAndReinitialize());
        yield return StartCoroutine(FetchUserData());
    }

    private IEnumerator ClearAndReinitialize()
    {
        // Clear everything
        gridMgr.ClearAll();

        // Wait for clear to complete
        yield return new WaitForSeconds(0.1f);

        // ? CRITICAL: Re-initialize the game state properly
        gridMgr.ResetGameCompletely(); // Use the comprehensive reset method we created earlier

        // Wait a bit more for re-initialization
        yield return new WaitForSeconds(0.1f);

        Debug.Log("Game re-initialized after fetch results");
    }

    #region PlayerStatusAPi

    public void UpdatePlayerStatus(int userId)
    {
        StartCoroutine(UpdatePlayerStatusCoroutine(userId));
    }

    private IEnumerator UpdatePlayerStatusCoroutine(int userId)
    {
        // ? Prepare Form Data
        WWWForm form = new WWWForm();
        form.AddField("id", userId);

        using (UnityWebRequest www = UnityWebRequest.Post(GameAPIs.playerStatusAPi, form))
        {
            Debug.Log("?? Sending ID: " + userId);

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("? Player Status Response: " + www.downloadHandler.text);

                // ? Parse JSON response
                PlayerStatusResponse response = JsonUtility.FromJson<PlayerStatusResponse>(www.downloadHandler.text);
                if (response != null && response.status == "success")
                {
                    Debug.Log("?? Message: " + response.message);
                    Debug.Log("?? New Status: " + response.new_status);
                }
                else
                {
                    Debug.LogWarning("?? Unexpected response: " + www.downloadHandler.text);
                }
            }
            else
            {
                Debug.LogError("? API Error: " + www.error);
            }
        }
    }

    [System.Serializable]
    private class PlayerStatusResponse
    {
        public string status;
        public string message;
        public int new_status;
    }

    #endregion

    #region SessionMatchAPI

    [System.Serializable]
    public class SessionResponse
    {
        public string status;
        public string message;
    }




    public void CheckSession()
    {
        StartCoroutine(CheckSession(PlayerPrefs.GetString("Sessionid")));
    }

    public IEnumerator CheckSession(string sessionId)
    {
        // ?? Prepare Form Data
        WWWForm form = new WWWForm();
        form.AddField("sessionid", sessionId);

        // ?? Send Request
        using (UnityWebRequest www = UnityWebRequest.Post(GameAPIs.checkSessionAPi, form))
        {
            yield return www.SendWebRequest();

            // ?? Handle response
            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("API Response: " + www.downloadHandler.text);

                // Parse JSON response
                SessionResponse response = JsonUtility.FromJson<SessionResponse>(www.downloadHandler.text);

                if (response.status == "success")
                {

                }
                else
                {
                    ToastManager.Instance.ShowToast(response.message);
                    LogOUT();
                }
            }
            else
            {
                Debug.LogError($"Request failed: {www.error}\nResponse: {www.downloadHandler.text}");

            }
        }
    }
    #endregion


    public IEnumerator FetchResultsOnStart()
    {
        WWWForm form = new WWWForm();
        form.AddField("id", PlayerPrefs.GetInt("UserId"));

        using (UnityWebRequest www = UnityWebRequest.Post(GameAPIs.getResultsAPi, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error fetching results: " + www.error);
            }
            else
            {
                Debug.Log("Results Response: " + www.downloadHandler.text);

                ResultsResponse res = JsonUtility.FromJson<ResultsResponse>(www.downloadHandler.text);

                if (res != null && res.status == "success" && res.data != null)
                {
                    // Reverse the list
                    res.data.Reverse();

                    for (int i = 0; i < resultObjs.Length && i < res.data.Count; i++)
                    {
                        TMP_Text txt = resultObjs[i].transform.GetChild(0).GetComponent<TMP_Text>();
                        txt.text = res.data[i].number;
                    }
                }
            }
        }
    }
    private bool isBuying = false; // Add this flag to your class

    public IEnumerator SaveAllSelectedCoroutine()
    {
        // Check the flag at the very beginning
        if (isBuying)
        {
            Debug.LogWarning("Buy process already in progress. Ignoring duplicate call.");
            yield break; // Exit the coroutine immediately
        }

        isBuying = true; // Set the flag to true to lock the process

        if (loadingObj != null)
            loadingObj.SetActive(true);

        // Step 1: Collect and save data
        foreach (int series in seriesMgr.currentSeriesSelected)
        {
            foreach (int range in seriesMgr.currentRangeSelected)
            {
                gridMgr.SaveCurrentGridData(series, range);
                yield return null;
            }
        }

        if (loadingObj != null)
            loadingObj.SetActive(false);

        // Step 2: Prepare final dictionary
        Dictionary<int, int> sortedDicByKey = seriesMgr.betNumbers
            .OrderBy(kvp => kvp.Key)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        // Step 3: Submit the dictionary
        yield return StartCoroutine(SubmitDictionary(
            sortedDicByKey,
            PlayerPrefs.GetInt("UserId"),
            (int.Parse(qntypointsMgr.PointsTotalTxt.text) / advnceTime.selectedTimes.Count),
            ""
        ));

        isBuying = false; // Reset the flag once the process is complete
    }





    public void BuyBtn()
    {
        CheckSession();
        SoundManager.Instance.PlaySound(SoundManager.Instance.commonSound);
        StartCoroutine(SaveAllSelectedCoroutine());
    }


    private Coroutine fetchCoroutine;
    private Coroutine timerCoroutine;

    public void GetTimer()
    {
        // Stop any previously running fetch coroutine
        if (fetchCoroutine != null)
            StopCoroutine(fetchCoroutine);

        fetchCoroutine = StartCoroutine(FetchTimers());
    }

    private IEnumerator FetchTimers()
    {
        //int maxRetries = 5;
        //int retries = 0;

        //while (retries < maxRetries)
        //{
        WWWForm form = new WWWForm();
        form.AddField("id", PlayerPrefs.GetInt("UserId"));

        using (UnityWebRequest www = UnityWebRequest.Post(GameAPIs.getTimerAPi, form))
        {
            yield return www.SendWebRequest();

            bool success = false;

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Timer Response: " + www.downloadHandler.text);

                TimerData timerData = JsonUtility.FromJson<TimerData>(www.downloadHandler.text);

                if (timerData != null && timerData.status == "success")
                {
                    string[] parts = timerData.time_remaining.Split(':');
                    if (parts.Length == 2 &&
                        int.TryParse(parts[0], out int minutes) &&
                        int.TryParse(parts[1], out int seconds))
                    {
                        // Stop any existing timer coroutine
                        if (timerCoroutine != null)
                        {
                            StopCoroutine(timerCoroutine);
                            timerCoroutine = null; // ensure it's cleared
                        }

                        randomNumActivated = false;
                        timerCoroutine = StartCoroutine(RunTimer(minutes, seconds));

                        success = true;
                        yield break;
                    }
                }
            }

            if (!success)
            {
                if (fetchCoroutine != null)
                    StopCoroutine(fetchCoroutine);

                fetchCoroutine = StartCoroutine(FetchTimers());
                yield return new WaitForSeconds(1f);
            }
        }


        Debug.LogError("Failed to fetch timer after multiple attempts.");
    }

    private bool randomNumActivated = false; // class level

    private IEnumerator RunTimer(int minutes, int seconds)
    {
        float totalTime = minutes * 60 + seconds;
        float endTime = Time.time + totalTime;

        int lastSecond = -1; // track last second to play sound only once per second
        bool noMoreBetsPlayed = false;

        while (Time.time < endTime)
        {
            float remaining = Mathf.Max(0, endTime - Time.time);
            int currentMinutes = Mathf.FloorToInt(remaining / 60);
            int currentSeconds = Mathf.FloorToInt(remaining % 60);

            string digits = currentMinutes.ToString("00") + currentSeconds.ToString("00");

            for (int i = 0; i < timerObjs.Length && i < digits.Length; i++)
            {
                TMP_Text txt = timerObjs[i].transform.GetChild(0).GetComponent<TMP_Text>();
                txt.text = digits[i].ToString();
            }

            // Play noMoreBets once at 10 seconds
            if (currentMinutes == 0 && currentSeconds == 10 && !noMoreBetsPlayed)
            {
                buybtn.interactable = false;
                SoundManager.Instance.PlaySound(SoundManager.Instance.noMoreBets);
                noMoreBetsPlayed = true;
            }

            // Play tickTimer every second below 10 seconds
            if (currentMinutes == 0 && currentSeconds < 10 && currentSeconds != lastSecond)
            {
                SoundManager.Instance.PlaySound(SoundManager.Instance.tickTimer);
                lastSecond = currentSeconds;
            }

            yield return null;
        }

        // Set UI to 0
        for (int i = 0; i < timerObjs.Length; i++)
            timerObjs[i].transform.GetChild(0).GetComponent<TMP_Text>().text = "0";

        // Activate randomNumObj only once
        if (!randomNumActivated)
        {
            randomNumActivated = true; // set immediately to block others

            randomNumObj.SetActive(true);

            ShowRandomNums s = randomNumObj.GetComponent<ShowRandomNums>();
            if (s.animCoroutine != null)
                StopCoroutine(s.animCoroutine);

            s.animCoroutine = StartCoroutine(s.AnimateNumbers());
        }

        gridMgr.familyToggle.isOn = false;
        timerCoroutine = null;
    }




    IEnumerator SoundDelay()
    {
        SoundManager.Instance.PlaySound(SoundManager.Instance.noMoreBets);
        yield return new WaitForSeconds(SoundManager.Instance._source.clip.length);
        SoundManager.Instance.PlaySound(SoundManager.Instance.tickTimer);

    }




    public void MinimizeBtn()
    {
#if UNITY_STANDALONE_WIN
        ShowWindow(GetActiveWindow(), SW_MINIMIZE);
#endif
    }

    public void ExitBtn()
    {
        Application.Quit();
    }
    IEnumerator SubmitDictionary(Dictionary<int, int> betNumbers, int userid, int points, string draw_time)
    {
        string url = GameAPIs.submitBetAPi;
        Debug.Log("Submit called");
        // Create the payload with the list of selected times
        foreach (var time in advnceTime.selectedTimes)
        {
            Debug.Log("selected time... " + time);

        }
        BetPayload<int, int> payload = new BetPayload<int, int>(userid, betNumbers, points, advnceTime.selectedTimes);
        string json = JsonUtility.ToJson(payload);

        loadingObj.SetActive(true);

        UnityWebRequest www = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            loadingObj.SetActive(false);
            Debug.Log("Response: " + www.downloadHandler.text);
            // Parse JSON into the updated SubmitResponse class
            SubmitResponse response = JsonUtility.FromJson<SubmitResponse>(www.downloadHandler.text);

            ToastManager.Instance.ShowToast(response.message);

            if (response.message.Contains("Invalid data"))
            {
                ToastManager.Instance.ShowToast("Place Bets first");
            }

            if (response != null && response.status == "success")
            {
                ToastManager.Instance.ShowToast("Bet Placed Successfully");
                Debug.Log(" Bets submitted successfully!");
                Debug.Log("Message: " + response.message);
                Debug.Log("Wallet Balance: " + response.wallet);
                StartCoroutine(ClearDelay());
                // Loop through the list of PDF URLs and call the download coroutine for each
                for (int i = 0; i < response.pdf_urls.Count; i++)
                {
                    string pdfUrl = response.pdf_urls[i];
                    string setName = response.set_name[i];

                    Debug.Log($"Downloading PDF: {setName} from {pdfUrl}");
                  //  StartCoroutine(DownloadPDF(pdfUrl, setName));
                }


                // gridMgr.ClearAll();

            }
        }
        else
        {
            ToastManager.Instance.ShowToast("Unexpected Error Occured");
            Debug.LogError("Error: " + www.error);
            loadingObj.SetActive(false);
            gridMgr.ClearAll();
        }
    }
    IEnumerator ClearDelay()
    {
        yield return new WaitForSeconds(2f);
        gridMgr.ClearAll();
    }

    public IEnumerator DownloadPDF(string pdfUrl, string defName)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(pdfUrl))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("PDF Download failed: " + www.error);
            }
            else
            {
                byte[] pdfData = www.downloadHandler.data;

                // Open Save File Dialog
                var path = StandaloneFileBrowser.SaveFilePanel("Save PDF", "", defName, "pdf");

                if (!string.IsNullOrEmpty(path))
                {
                    File.WriteAllBytes(path, pdfData);
                    Debug.Log("PDF saved at: " + path);
                    ForceFocus();
                    SceneManager.LoadSceneAsync(1);
                    // Open the file after saving (optional)
                    //    Application.OpenURL(path);
                    // StartCoroutine(FetchUserData());
                    //   GetTimer();
                }
                else
                {
                    Debug.Log("Save cancelled.");
                    ForceFocus();
                    SceneManager.LoadSceneAsync(1);

                }
            }
        }
    }

    public void RedirectToUrl()
    {

        Application.OpenURL(GameAPIs.baseUrl + "Auth/check/" + PlayerPrefs.GetInt("UserId"));
    }

    //public GameObject loadingObject; // Assuming you have a loading indicator

    public void CancelBetBtn()
    {
        if (string.IsNullOrEmpty(barcodeTxt.text))
        {
            ToastManager.Instance.ShowToast("Enter valid Barcode ID");
            return;
        }

        // Start the coroutine to send the API request
        StartCoroutine(CancelBetAPI(barcodeTxt.text));
    }
    public void ShowComingSoon()
    {
        ToastManager.Instance.ShowToast("Coming Soon");
    }
    public IEnumerator CancelBetAPI(string barcode)
    {
        // Replace with your actual API endpoint for canceling bets
        string cancelBetUrl = GameAPIs.cancelBetAPi;

        WWWForm form = new WWWForm();
        form.AddField("id", PlayerPrefs.GetInt("UserId"));
        form.AddField("barcode", barcode);

        loadingObj.SetActive(true);

        using (UnityWebRequest www = UnityWebRequest.Post(cancelBetUrl, form))
        {
            yield return www.SendWebRequest();

            loadingObj.SetActive(false);

            CancelResponse response = JsonUtility.FromJson<CancelResponse>(www.downloadHandler.text);
            if (www.result == UnityWebRequest.Result.Success)
            {
                // Deserialize the JSON response

                if (response != null)
                {
                    if (response.status == "success")
                    {
                        // Handle a successful cancellation
                        ToastManager.Instance.ShowToast(response.message);
                        Debug.Log("Bet canceled successfully: " + response.message);
                        // Optional: Clear the input field and other UI elements
                        barcodeTxt.text = "";
                        ToastManager.Instance.ShowToast("Bet cancelled successfully");
                    }
                    else if (response.status == "error")
                    {
                        // Handle an unsuccessful cancellation (e.g., "No active bets")
                        ToastManager.Instance.ShowToast(response.message);
                        Debug.LogError("Error canceling bet: " + response.message);
                        //  ToastManager.Instance.ShowToast("Unexpected error occured.Try Again");

                    }
                    else
                    {
                        // Handle other unexpected statuses
                        ToastManager.Instance.ShowToast(response.message);
                        Debug.LogError("Unexpected status: " + response.status);
                    }
                }
            }
            else
            {
                // Handle a network or server error
                ToastManager.Instance.ShowToast(response.message);
                Debug.LogError("UnityWebRequest Error: " + www.error);
            }
        }
    }

    public void OnClaimPointsButtonClicked()
    {
        string barcode = barcodeTxt.text.Trim();

        if (string.IsNullOrEmpty(barcode))
        {
            Debug.LogWarning("Please enter a barcode.");
            ToastManager.Instance.ShowToast("Please enter a barcode");

            return;
        }

        StartCoroutine(ClaimPointsCoroutine(barcode));
    }
    public IEnumerator ClaimPointsCoroutine(string barcode)
    {
        WWWForm form = new WWWForm();
        form.AddField("id", PlayerPrefs.GetInt("UserId"));  // Assuming you store user ID in PlayerPrefs
        form.AddField("orderid", barcode);

        using (UnityWebRequest www = UnityWebRequest.Post(GameAPIs.claimPointsAPi, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error claiming points: " + www.error);
                ToastManager.Instance.ShowToast("Error claiming points");


            }
            else
            {
                Debug.Log("Claim Points Response: " + www.downloadHandler.text);


                // Optionally parse JSON if you want to check status
                var res = JsonUtility.FromJson<ClaimPointsResponse>(www.downloadHandler.text);
                if (res != null && res.status == "success")
                {
                    Debug.Log("Points claimed successfully!");
                    ToastManager.Instance.ShowToast(res.message);
                }
                else
                {
                    Debug.LogWarning("Failed to claim points.");
                    ToastManager.Instance.ShowToast(res.message);

                }
            }
        }
    }

    [System.Serializable]
    public class BetPayload<TKey, TValue>
    {
        public int userid;
        public int points;

        // Change this to a list to match the JSON array
        public List<string> draw_time;

        public List<Entry<TKey, TValue>> items = new List<Entry<TKey, TValue>>();

        public BetPayload(int userId, Dictionary<TKey, TValue> dict, int points, List<string> draw_times)
        {
            this.userid = userId;
            this.points = points;
            this.draw_time = draw_times;

            foreach (var kv in dict)
            {
                items.Add(new Entry<TKey, TValue> { key = kv.Key, value = kv.Value });
            }
        }
    }

    private void ForceFocus()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        IntPtr handle = GetActiveWindow();
        if (handle != IntPtr.Zero)
        {
            SetForegroundWindow(handle);
        }
#endif
    }


    [System.Serializable]
    public class Entry<TKey, TValue>
    {
        public TKey key;
        public TValue value;
    }


}
// Unity's JsonUtility does not serialize Dictionary directly, so we wrap it
[System.Serializable]
public class Wrapper<TKey, TValue>
{
    public List<Entry<TKey, TValue>> items = new List<Entry<TKey, TValue>>();

    public Wrapper(Dictionary<TKey, TValue> dict)
    {
        foreach (var kv in dict)
        {
            items.Add(new Entry<TKey, TValue> { key = kv.Key, value = kv.Value });
        }
    }
}
[System.Serializable]
public class Entry<TKey, TValue>
{
    public TKey key;
    public TValue value;
}
[System.Serializable]
public class BetPayload<TKey, TValue>
{
    public int userId;
    public int totalPoints;
    public string draw_time;
    public List<Entry<TKey, TValue>> items = new List<Entry<TKey, TValue>>();

    public BetPayload(int userId, Dictionary<TKey, TValue> dict, int totalPoints, string draw_time)
    {
        this.userId = userId;
        this.totalPoints = totalPoints;
        this.draw_time = draw_time;
        foreach (var kv in dict)
        {
            items.Add(new Entry<TKey, TValue> { key = kv.Key, value = kv.Value });
        }
    }
}
[System.Serializable]
public class UserDataResponse
{
    public string status;
    public string limit;
    public string last_trn;
    public string tran_pt;
    public string datetime;
    public string current_slot;
    public string previous_slot;
    public string draw_id;
}

[System.Serializable]
public class NumberData
{
    public string number;
}
// Define a serializable class to match the JSON response structure
[System.Serializable]
public class CancelResponse
{
    public string status;
    public string message;
}
[System.Serializable]
public class ResultsResponse
{
    public string status;
    public List<NumberData> data;
}

[System.Serializable]
public class TimerData
{
    public string status;
    public string time_remaining;
}


[System.Serializable]
public class SubmitResponse
{
    public string status;
    public string message;
    public int wallet;
    public string[] set_name;
    // Update this from a single string to a list of strings
    public List<string> pdf_urls;
}

