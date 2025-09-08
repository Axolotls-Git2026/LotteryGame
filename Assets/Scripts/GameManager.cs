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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
public class GameManager : MonoBehaviour
{
    [DllImport("user32.dll")]
    private static extern bool  ShowWindow(System.IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern System.IntPtr GetActiveWindow();

    const int SW_MINIMIZE = 6;


    public static GameManager instance;
    public SeriesManager seriesMgr;
    public GridManager gridMgr;
    public QuantityPointsManager qntypointsMgr;
    public TimeIncrementer incrementer;
    public AdvanceTime advnceTime;

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


    public Canvas canvas;
    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        StartCoroutine(FetchUserData());
        StartCoroutine(FetchResultsOnStart());
        GetTimer();
       FindAnyObjectByType<LoginManager>().gameObject.SetActive(false);
        ToastManager.Instance.transform.SetParent(canvas.transform);
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            seriesMgr.ClearAllSeriesAndRange();
            gridMgr.ClearAll();
            qntypointsMgr.ClearData();
            gridMgr.ClearPopup();
          
        }
    }

    public void LoadScene(int _index)
    {
        SceneManager.LoadScene(_index);
    }

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
                    advnceTime.selectedTimes.Add(data.current_slot);
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
        gridMgr.ClearAll();
        StartCoroutine(FetchUserData());
    }

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

    public void BuyBtn()
    {
        SoundManager.Instance.PlaySound(SoundManager.Instance.commonSound);

        foreach (int series in seriesMgr.currentSeriesSelected)
        {
            foreach (int range in seriesMgr.currentRangeSelected)
            {
                gridMgr.SaveCurrentGridData(series, range);
            }
        }
        //string dictLog = "Current betNumbers: ";
        //foreach (var kvp in seriesMgr.betNumbers)
        //{
        //    dictLog += $"[{kvp.Key} : {kvp.Value}] ";
        //}
        //Debug.Log("Current bet nums : " + dictLog);

        Dictionary<int, int> sortedDicByKey = seriesMgr.betNumbers
    .OrderBy(kvp => kvp.Key)
    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        StartCoroutine(SubmitDictionary(sortedDicByKey, PlayerPrefs.GetInt("UserId"), int.Parse(qntypointsMgr.PointsTotalTxt.text), ""));
    }
    private Coroutine timerCoroutine;
    public void GetTimer()
    {
        // Stop any previously running timer coroutine
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
        // Start the new coroutine and store its reference
        timerCoroutine = StartCoroutine(FetchTimers());
    }

    private IEnumerator FetchTimers()
    {
        int maxRetries = 5; // Set a maximum number of retries
        int retries = 0;

        // Outer loop to retry the request
        while (retries < maxRetries)
        {
            WWWForm form = new WWWForm();
            form.AddField("id", PlayerPrefs.GetInt("UserId"));

            using (UnityWebRequest www = UnityWebRequest.Post(GameAPIs.getTimerAPi, form))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("Timer Response: " + www.downloadHandler.text);

                    TimerData timerData = JsonUtility.FromJson<TimerData>(www.downloadHandler.text);

                    if (timerData != null && timerData.status == "success")
                    {
                        string[] parts = timerData.time_remaining.Split(':');
                        if (parts.Length == 2)
                        {
                            int minutes = int.Parse(parts[0]);
                            int seconds = int.Parse(parts[1]);
                            timerCoroutine = StartCoroutine(RunTimer(minutes, seconds));
                            yield break; // Exit the coroutine on success
                        }
                    }
                }
                else
                {
                    Debug.LogError($"Error fetching timer (Attempt {retries + 1}/{maxRetries}): " + www.error);
                }
            }
            retries++;
            yield return new WaitForSeconds(1f); // Wait before retrying
        }

        Debug.LogError("Failed to fetch timer after multiple attempts.");
    }

    private IEnumerator RunTimer(int minutes, int seconds)
    {
        float timer = (minutes * 60) + seconds;
        float oneSecondCounter = 0f;

        while (timer > 0)
        {
            // Accumulate time on a per-frame basis
            oneSecondCounter += Time.deltaTime;

            // Only update the timer every full second
            if (oneSecondCounter >= 1f)
            {
                timer--;
                oneSecondCounter = 0f;

                // Calculate new minutes and seconds
                int currentMinutes = Mathf.FloorToInt(timer / 60);
                int currentSeconds = Mathf.FloorToInt(timer % 60);

                // Format and display the time
                string mm = currentMinutes.ToString("00");
                string ss = currentSeconds.ToString("00");
                string digits = mm + ss;

                for (int i = 0; i < timerObjs.Length && i < digits.Length; i++)
                {
                    TMP_Text txt = timerObjs[i].transform.GetChild(0).GetComponent<TMP_Text>();
                    txt.text = digits[i].ToString();
                }

                // Your existing logic for sound and other events
                if (currentMinutes == 0 && currentSeconds == 10)
                {
                    Debug.Log("Only 10 seconds left!");
                    StartCoroutine(SoundDelay());
                }

                // SoundManager.Instance.PlaySound(SoundManager.Instance.tickTimer);
            }

            yield return null; // Wait for the next frame
        }

        // Timer end logic
        for (int i = 0; i < timerObjs.Length; i++)
        {
            TMP_Text txt = timerObjs[i].transform.GetChild(0).GetComponent<TMP_Text>();
            txt.text = "0";
        }
        randomNumObj.SetActive(true);
        randomNumObj.GetComponent<ShowRandomNums>().animCoroutine = StartCoroutine(randomNumObj.GetComponent<ShowRandomNums>().AnimateNumbers());
        gridMgr.familyToggle.isOn = false;
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

        // Create the payload with the list of selected times
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

            if (response != null && response.status == "success")
            {
                ToastManager.Instance.ShowToast("Bet Placed Successfully");
                Debug.Log(" Bets submitted successfully!");
                Debug.Log("Message: " + response.message);
                Debug.Log("Wallet Balance: " + response.wallet);

                // Loop through the list of PDF URLs and call the download coroutine for each
                foreach (string pdfUrl in response.pdf_urls)
                {
                    Debug.Log("PDF URL: " + pdfUrl);
                    StartCoroutine(DownloadPDF(pdfUrl));
                }

                gridMgr.ClearAll();
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
    public IEnumerator DownloadPDF(string pdfUrl)
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
                var path = StandaloneFileBrowser.SaveFilePanel("Save PDF", "", "Lottery", "pdf");

                if (!string.IsNullOrEmpty(path))
                {
                    File.WriteAllBytes(path, pdfData);
                    Debug.Log("PDF saved at: " + path);

                    // Open the file after saving (optional)
                //    Application.OpenURL(path);
                    StartCoroutine(FetchUserData());
                    GetTimer();
                }
                else
                {
                    Debug.Log("Save cancelled.");


                }
            }
        }
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

            if (www.result == UnityWebRequest.Result.Success)
            {
                // Deserialize the JSON response
                CancelResponse response = JsonUtility.FromJson<CancelResponse>(www.downloadHandler.text);

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
                        ToastManager.Instance.ShowToast("Error canceling bet");
                        Debug.LogError("Error canceling bet: " + response.message);
                      //  ToastManager.Instance.ShowToast("Unexpected error occured.Try Again");

                    }
                    else
                    {
                        // Handle other unexpected statuses
                        ToastManager.Instance.ShowToast("Unexpected response from server.");
                        Debug.LogError("Unexpected status: " + response.status);
                    }
                }
            }
            else
            {
                // Handle a network or server error
                ToastManager.Instance.ShowToast("Unexpected response from server.");
                Debug.LogError("UnityWebRequest Error: " + www.error);
            }
        }
    }

    public void ClaimPoints()
    {

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
    public string set_name;
    // Update this from a single string to a list of strings
    public List<string> pdf_urls;
}

