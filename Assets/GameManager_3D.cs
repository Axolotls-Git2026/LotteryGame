using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using SFB;
using UnityEngine.SceneManagement;
using static OutputNumGenerator;
using System;
using UnityEngine.UI;
using System.Linq;
public class GameManager_3D : MonoBehaviour
{
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(System.IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern System.IntPtr GetActiveWindow();

    const int SW_MINIMIZE = 6;


    public static GameManager_3D instance;


    public TimeIncrementer incrementer;
    public OutputNumGenerator outputNumGenerator;
    public QntyPointsManager qntyMgr;
    public RangeFilterManager rangeMgr;
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
    public TMP_Text lastResultTime;
    public TMP_InputField barcodeTxt;
    public TMP_Text currentTimeTxt;

    [Header("3D game Result")]
    public TMP_Text[] a_Results;
    public TMP_Text[] b_Results;
    public AdvanceTime advnceTime;

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

       // StartCoroutine(FetchUserData());
        CheckSession();
        UpdatePlayerStatus(PlayerPrefs.GetInt("UserId"));
        StartCoroutine(FetchResults(a_Results, b_Results));

        GetTimer();
        // ToastManager.Instance.transform.SetParent(canvas.transform);
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F2))
        {
            outputNumGenerator.ResetAllData();
            ToastManager.Instance.ShowToast("Cleared");

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
                    // drawId.text = data.draw_id;
                    if (!advnceTime.selectedTimes.Contains(data.current_slot))
                    {
                        advnceTime.selectedTimes.Add(data.current_slot);
                    }
                    PlayerPrefs.SetInt("selectedTimes",advnceTime.selectedTimes.Count);
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
                    outputNumGenerator.betData = new BetData(PlayerPrefs.GetInt("UserId"), drawTime.text);
                }
                // incrementer.StartHeaderCurrentTime();
            }
        }
    }

    public IEnumerator FetchResults(TMP_Text[] list1, TMP_Text[] list2)
    {
        // Start shuffling visuals
        Coroutine shuffle = StartCoroutine(ShuffleTexts(list1, list2, 5f)); // 5 seconds shuffle

        WWWForm form = new WWWForm();
        form.AddField("id", PlayerPrefs.GetInt("UserId"));

        using (UnityWebRequest www = UnityWebRequest.Post(GameAPIs.fetch3DResultAPi, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error fetching results: " + www.error);
            }
            else
            {
                Debug.Log("Results Response: " + www.downloadHandler.text);

                Bet3DResponse res = JsonUtility.FromJson<Bet3DResponse>(www.downloadHandler.text);

                if (res != null && res.status == "success" && res.data != null && res.data.Count > 0)
                {
                    // Stop shuffling
                    StopCoroutine(shuffle);

                    // First number -> list1
                    if (res.data.Count > 0)
                    {
                        string number1 = res.data[0].number;
                        for (int i = 0; i < list1.Length && i < number1.Length; i++)
                            list1[i].text = number1[i].ToString();
                    }

                    // Second number -> list2
                    if (res.data.Count > 1)
                    {
                        string number2 = res.data[1].number;
                        for (int i = 0; i < list2.Length && i < number2.Length; i++)
                            list2[i].text = number2[i].ToString();
                    }

                    if (lastResultTime != null)
                        lastResultTime.text = res.data[0].draw_time;
                }
                buybtn.interactable = true;
            }
        }

        GetTimer();
        StartCoroutine(FetchUserData());

    }

    [System.Serializable]
    public class Numbers
    {
        public List<string> A;
        public List<string> B;
    }

    [System.Serializable]
    public class ResultData
    {
        public string draw_time;
        public Numbers numbers;
    }

    [System.Serializable]
    public class AllResultsResponse
    {
        public string status;
        public List<ResultData> data;
    }

    public IEnumerator FetchAndSpawn(GameObject prefab, GameObject parent)
    {
        WWWForm form = new WWWForm();
        form.AddField("id", PlayerPrefs.GetInt("UserId"));

        using (UnityWebRequest www = UnityWebRequest.Post(GameAPIs.fetchAll3DResultAPi, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error fetching results: " + www.error);
            }
            else
            {
                Debug.Log("API Response: " + www.downloadHandler.text);

                AllResultsResponse res = JsonUtility.FromJson<AllResultsResponse>(www.downloadHandler.text);

                if (res != null && res.status == "success" && res.data != null && res.data.Count > 0)
                {
                    // Clear old prefabs
                    foreach (Transform child in parent.transform)
                        Destroy(child.gameObject);

                    // Instantiate for each draw result
                    foreach (var item in res.data)
                    {
                        GameObject obj = Instantiate(prefab, parent.transform);

                        // Set Draw Time (if Child 0 has text)
                        TMP_Text timeText = obj.transform.GetChild(0).GetComponentInChildren<TMP_Text>();
                        if (timeText != null)
                            timeText.text = item.draw_time;

                        // --- A Result ---
                        if (item.numbers.A != null && item.numbers.A.Count > 0)
                        {
                            string numberA = item.numbers.A[0];
                            Transform aParent = obj.transform.GetChild(3);
                            for (int i = 0; i < aParent.childCount && i < numberA.Length; i++)
                            {
                                TMP_Text txt = aParent.GetChild(i).GetComponentInChildren<TMP_Text>();
                                if (txt != null)
                                    txt.text = numberA[i].ToString();
                            }
                        }

                        // --- B Result ---
                        if (item.numbers.B != null && item.numbers.B.Count > 0)
                        {
                            string numberB = item.numbers.B[0];
                            Transform bParent = obj.transform.GetChild(4);
                            for (int i = 0; i < bParent.childCount && i < numberB.Length; i++)
                            {
                                TMP_Text txt = bParent.GetChild(i).GetComponentInChildren<TMP_Text>();
                                if (txt != null)
                                    txt.text = numberB[i].ToString();
                            }
                        }
                    }
                }
            }
        }
    }


    private IEnumerator ShuffleTexts(TMP_Text[] list, float duration)
    {
        float endTime = Time.time + duration;
        string digits = "0123456789";

        while (Time.time < endTime)
        {
            for (int i = 0; i < list.Length; i++)
            {
                list[i].text = digits[UnityEngine.Random.Range(0, digits.Length)].ToString();
            }
            yield return new WaitForSeconds(0.05f);
        }
    }

    private void OnApplicationFocus(bool focus)
    {
        if (focus)
        {
            GetTimer();
            StartCoroutine(FetchUserData());

        }
    }
    // Shuffle coroutine
    private IEnumerator ShuffleTexts(TMP_Text[] list1, TMP_Text[] list2, float duration)
    {
        float timer = 0f;
        System.Random rand = new System.Random();

        while (timer < duration)
        {
            foreach (var txt in list1)
                txt.text = rand.Next(0, 10).ToString();

            foreach (var txt in list2)
                txt.text = rand.Next(0, 10).ToString();

            timer += 0.05f; // update every 50ms
            yield return new WaitForSeconds(0.05f);
        }
    }


    // Fill first list (list1) with digits of number1

    public IEnumerator FetchResultsOnStart()
    {
        WWWForm form = new WWWForm();
        form.AddField("id", PlayerPrefs.GetInt("UserId"));

        using (UnityWebRequest www = UnityWebRequest.Post(GameAPIs.fetch3DResultAPi, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error fetching results: " + www.error);
                yield break;
            }

            ResultsResponse res = JsonUtility.FromJson<ResultsResponse>(www.downloadHandler.text);

            if (res != null && res.status == "success" && res.data != null)
            {
                // Reverse the list if needed
                res.data.Reverse();

                int digitIndex = 0;

                foreach (var entry in res.data)
                {
                    string num = entry.number;
                    if (num.Length < 3) continue; // skip invalid numbers

                    for (int i = 0; i < num.Length && digitIndex < resultObjs.Length; i++, digitIndex++)
                    {
                        TMP_Text txt = resultObjs[digitIndex].transform.GetChild(0).GetComponent<TMP_Text>();
                        txt.text = num[i].ToString();
                    }
                }

            }
        }
    }
    public void BuyBtn()
    {
        CheckSession();
        if (outputNumGenerator.betData.A.Count == 0 && outputNumGenerator.betData.B.Count == 0)
        {
            ToastManager.Instance.ShowToast("Place Bet First");

        }
        else
        {
            // SoundManager.Instance.PlaySound(SoundManager.Instance.commonSound);
            StartCoroutine(BuyCoroutine());
        }
    }

    public void GetTimer()
    {
        StartCoroutine(FetchTimers());
    }
    private Coroutine timerRoutine;
    private IEnumerator FetchTimers()
    {
        Debug.Log("Fetch called");
        WWWForm form = new WWWForm();
        form.AddField("id", PlayerPrefs.GetInt("UserId"));

        using (UnityWebRequest www = UnityWebRequest.Post(GameAPIs.getTimerAPi, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error fetching timer: " + www.error);
            }
            else
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

                        if (timerRoutine != null)
                        {
                            StopCoroutine(timerRoutine);
                            timerRoutine = null;
                        }

                        // Start new timer
                        timerRoutine = StartCoroutine(RunTimer(minutes, seconds));
                    }
                }
            }
        }
    }

    private Coroutine timerCoroutine;
    bool isDataFetchedCalled;
    private IEnumerator RunTimer(int minutes, int seconds)
    {
        Debug.Log("RunTimer started!");   // ?? check this
        int totalSeconds = (minutes * 60) + seconds;

        bool noMoreBetsPlayed = false;

        while (totalSeconds > 0)
        {
            int currentHours = totalSeconds / 3600;
            int currentMinutes = (totalSeconds % 3600) / 60;
            int currentSeconds = totalSeconds % 60;

            // Format hh:mm:ss
            string timeString = $"{currentHours:00}:{currentMinutes:00}:{currentSeconds:00}";
            currentTimeTxt.text = timeString;
            isDataFetchedCalled = false;

            // --- Sounds ---
            if (currentMinutes == 0 && currentSeconds == 10 && !noMoreBetsPlayed)
            {
                buybtn.interactable = false;
                SoundManager.Instance.PlaySound(SoundManager.Instance.noMoreBets);
                noMoreBetsPlayed = true;
            }
            else if (currentMinutes == 0 && currentSeconds < 10)
            {
                SoundManager.Instance.PlaySound(SoundManager.Instance.tickTimer);
            }

            // --- Update digit UI ---
            string digits = currentMinutes.ToString("00") + currentSeconds.ToString("00");
            for (int i = 0; i < timerObjs.Length && i < digits.Length; i++)
            {
                TMP_Text txt = timerObjs[i].transform.GetChild(0).GetComponent<TMP_Text>();
                txt.text = digits[i].ToString();
            }

            // Wait exactly one second before looping
            yield return new WaitForSeconds(1f);

            totalSeconds--;
        }

        // --- Timer end ---
        foreach (var obj in timerObjs)
        {
            TMP_Text txt = obj.transform.GetChild(0).GetComponent<TMP_Text>();
            txt.text = "0";
        }

        outputNumGenerator.ResetAllData();
        if (!isDataFetchedCalled)
        {
            CheckSession();
            StartCoroutine(FetchResults(a_Results, b_Results));
            isDataFetchedCalled = true;
        }
    }






    public void RedirectToUrl()
    {

        Application.OpenURL(GameAPIs.baseUrl + "Auth/check/" + PlayerPrefs.GetInt("UserId"));
    }

    IEnumerator SoundDelay()
    {
        SoundManager.Instance.PlaySound(SoundManager.Instance.noMoreBets);
        yield return new WaitForSeconds(SoundManager.Instance._source.clip.length);
        SoundManager.Instance.PlaySound(SoundManager.Instance.tickTimer);

    }

    #region Logout API
    public void LogOUT()
    {
        UpdatePlayerStatus(PlayerPrefs.GetInt("UserId"));
        PlayerPrefs.DeleteAll();
        LoadScene(0);

    }

    #endregion

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

    public void ShowComingSoon()
    {
        ToastManager.Instance.ShowToast("Coming Soon");
    }

    [System.Serializable]
    public class BetResponse
    {
        public string status;
        public string[] set_name;
        public string[] pdf_urls;
    }

    private IEnumerator BuyCoroutine()
    {


        // Convert bet data to JSON
        string json = JsonUtility.ToJson(outputNumGenerator.betData); // no pretty print
        Debug.Log("Sending JSON: " + json);

        byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);

        // Setup UnityWebRequest
        UnityWebRequest request = new UnityWebRequest(GameAPIs.submit3DBetAPi, "POST");
        request.uploadHandler = new UploadHandlerRaw(jsonBytes);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

      


        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error sending data: " + request.error);
            ToastManager.Instance.ShowToast("Failed to send data");
        }
        else
        {
            string resultJson = request.downloadHandler.text;
            Debug.Log("Data sent successfully: " + resultJson);

            // Parse server response
            BetResponse response = JsonUtility.FromJson<BetResponse>(resultJson);

            if (response != null && response.pdf_urls != null && response.pdf_urls.Length > 0)
            {
                ToastManager.Instance.ShowToast("Data sent successfully");
                StartCoroutine(ClearDelay());

                for (int i = 0; i < response.pdf_urls.Count(); i++)
                {
                    string pdfUrl = response.pdf_urls[i];
                    string setName = response.set_name[i];

                    Debug.Log($"Downloading PDF: {setName} from {pdfUrl}");
                 //   StartCoroutine(DownloadPDF(pdfUrl, setName));
                }
            }
            else
            {
                Debug.LogWarning("Response JSON did not contain expected fields.");
                ToastManager.Instance.ShowToast("Invalid server response");
            }
        }
    }
    IEnumerator ClearDelay()
    {
        yield return new WaitForSeconds(2f);
        outputNumGenerator.ResetAllData();
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
                    outputNumGenerator.ResetAllData();
                    // Open the file after saving (optional)
                    //    Application.OpenURL(path);
                    StartCoroutine(FetchUserData());
                    GetTimer();
                }
                else
                {
                    Debug.Log("Save cancelled.");
                    outputNumGenerator.ResetAllData();


                }
            }
        }
    }
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
    public class Bet3DEntry
    {
        public string number;     // e.g. "349"
        public string type;       // "A" or "B"
        public string draw_time;  // e.g. "04:00"
    }

    [System.Serializable]
    public class Bet3DResponse
    {
        public string status;             // "success" / "error"
        public List<Bet3DEntry> data;
    }
    [System.Serializable]
    public class ClaimPointsResponse
    {
        public string status;
        public string message;
        public string orderid;
        public string userid;
    }


}



