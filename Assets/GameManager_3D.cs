using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using SFB;
using UnityEngine.SceneManagement;
public class GameManager_3D : MonoBehaviour
{
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(System.IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern System.IntPtr GetActiveWindow();

    const int SW_MINIMIZE = 6;


    public static GameManager_3D instance;
  
    public QuantityPointsManager qntypointsMgr;
    public TimeIncrementer incrementer;

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

       // ToastManager.Instance.transform.SetParent(canvas.transform);
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F2))
        {
            //seriesMgr.ClearAllSeriesAndRange();
            //gridMgr.ClearAll();
            qntypointsMgr.ClearData();
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

    //public void BuyBtn()
    //{
    //    SoundManager.Instance.PlaySound(SoundManager.Instance.commonSound);

    //    foreach (int series in seriesMgr.currentSeriesSelected)
    //    {
    //        foreach (int range in seriesMgr.currentRangeSelected)
    //        {
    //            gridMgr.SaveCurrentGridData(series, range);
    //        }
    //    }
    //    StartCoroutine(SubmitDictionary(seriesMgr.betNumbers, PlayerPrefs.GetInt("UserId"), int.Parse(qntypointsMgr.PointsTotalTxt.text), ""));
    //}

    public void GetTimer()
    {
        StartCoroutine(FetchTimers());
    }

    private IEnumerator FetchTimers()
    {
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

                        StartCoroutine(RunTimer(minutes, seconds));
                    }
                }
            }
        }
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

        // Wrap dictionary + userId + points
        BetPayload<int, int> payload = new BetPayload<int, int>(userid, betNumbers, points, draw_time);
        string json = JsonUtility.ToJson(payload);
        loadingObj.SetActive(true);
        // Send as raw JSON
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

            // Parse JSON into SubmitResponse
            SubmitResponse response = JsonUtility.FromJson<SubmitResponse>(www.downloadHandler.text);

            if (response != null && response.status == "success")
            {
                ToastManager.Instance.ShowToast("Bet Placed Successfully");
                Debug.Log(" Bets submitted successfully!");
                Debug.Log("Message: " + response.message);
                Debug.Log("Wallet Balance: " + response.wallet);
               // Debug.Log("PDF URL: " + response.pdf_url);

               // gridMgr.ClearAll();
              //  StartCoroutine(DownloadPDF(response.pdf_url));
            }
        }
        else
        {
            ToastManager.Instance.ShowToast("Unexpected Error Occured");
            Debug.LogError("Error: " + www.error);
            loadingObj.SetActive(false);
          //  gridMgr.ClearAll();
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
                    Application.OpenURL(path);
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

    public void ClaimPoints()
    {

    }
}



