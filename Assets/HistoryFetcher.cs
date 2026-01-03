using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

[System.Serializable]
public class HistoryResponse
{
    public string status;
    public HistoryData[] data;
}

[System.Serializable]
public class HistoryData
{
    public string id;
    public string userid;
    public string barcode;
    public string game_name;
    public string ticket_time;
    public string game_no;
    public string draw_time;
    public string play_point;
    public string claim_point;
    public string status;          // e.g., "loss", "active"
    public string set_name;        // e.g., "A", "B"
    public string print_count;
    public string claim_status;
    public string commision;
    public string cancel_charge;
    public string refund_amount;
    public string pdf_url;
}

// New classes for the details API response
[System.Serializable]
public class DetailsResponse
{
    public string status;
    public DetailData[] data;
}

[System.Serializable]
public class DetailData
{
    public string id;
    public string number;
    public string amount;
    public string orderid;
    public string bet_type;
    public string game;
    public string group_key;
    public string status;
}


public class HistoryFetcher : MonoBehaviour
{
    [Header("Prefab and Content Holder")]
    public GameObject historyitemPrefab;
    public GameObject historyitemAndroid_Prefab;
    public Transform content;

    [Header("Details Prefab and Content Holder")]
    public GameObject detailItemPrefab;
    public GameObject detailItemPrefab_3D;
    public Transform detailContent;

    public GameObject detailsObj;
    public Color colorActive;
    public Color colorCancelled;
    public Color colorOther;

    [System.Serializable]
    public class HistoryRequest
    {
        public string userid;
    }
    private void OnEnable()
    {
       // StartCoroutine(FetchHistory());
    }
    void Start()
    {
        StartCoroutine(FetchHistory());
    }

    public IEnumerator FetchHistory()
    {
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }
        WWWForm form = new WWWForm();
        form.AddField("id", PlayerPrefs.GetInt("UserId").ToString());

        if (SceneManager.GetActiveScene().name.Contains("3D"))
        {
            form.AddField("game_type", "3D");
            GameManager_3D.instance.loadingObj.gameObject.SetActive(true);
        }
        else
        {
            form.AddField("game_type", "4D");
            GameManager.instance.loadingObj.gameObject.SetActive(true);

        }

        using (UnityWebRequest www = UnityWebRequest.Post(GameAPIs.fetchHistoryAPi, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(www.error);
            }
            else
            {
                string json = www.downloadHandler.text;
                Debug.Log("Response: " + json);
                HistoryResponse response = JsonUtility.FromJson<HistoryResponse>(json);
                if(response.status == "error")
                {
                    GameManager_3D.instance.loadingObj.gameObject.SetActive(false);
                }

                if (response != null && response.data != null)
                {
                    foreach (var entry in response.data)
                    {
                        if (SceneManager.GetActiveScene().name.Contains("Android"))
                        {
                            GameObject item = Instantiate(historyitemAndroid_Prefab, content);

                            if (SceneManager.GetActiveScene().name.Contains("3D"))
                            {
                                Button claimBtn = item.transform.GetChild(8).GetComponent<Button>();
                                if (claimBtn != null)
                                {
                                    // This line sets up the button click event
                                    claimBtn.onClick.AddListener(() => StartCoroutine(GameManager_3D.instance.ClaimPointsCoroutine(entry.barcode)));
                                }
                                Button cancelBtn = item.transform.GetChild(7).GetComponent<Button>();

                                if (entry.status == "active")
                                {
                                    cancelBtn.GetComponent<Image>().color = colorActive;
                                }
                                else if (entry.status == "cancelled")
                                {
                                    cancelBtn.GetComponent<Image>().color = colorCancelled;
                                }
                                else
                                {
                                    cancelBtn.GetComponent<Image>().color = colorOther;

                                }

                                if (cancelBtn != null)
                                {
                                    // This line sets up the button click event
                                    cancelBtn.onClick.AddListener(() =>
                                    {

                                        if (entry.status == "active")
                                        {
                                            StartCoroutine(GameManager_3D.instance.CancelBetAPI(entry.barcode));
                                        }
                                        else if (entry.status == "cancelled")
                                        {
                                            ToastManager.Instance.ShowToast("Bet already cancelled");

                                        }
                                        else
                                        {
                                            ToastManager.Instance.ShowToast("The bet time is already completed");
                                        }

                                        StartCoroutine(FetchHistory());
                                    });
                                }
                            }
                            else
                            {
                                Button claimBtn = item.transform.GetChild(8).GetComponent<Button>();
                                if (claimBtn != null)
                                {
                                    // This line sets up the button click event
                                    claimBtn.onClick.AddListener(() => StartCoroutine(GameManager.instance.ClaimPointsCoroutine(entry.barcode)));
                                }
                                Button cancelBtn = item.transform.GetChild(7).GetComponent<Button>();

                                if (entry.status == "active")
                                {
                                    cancelBtn.GetComponent<Image>().color = colorActive;
                                }
                                else if (entry.status == "cancelled")
                                {
                                    cancelBtn.GetComponent<Image>().color = colorCancelled;
                                }
                                else
                                {
                                    cancelBtn.GetComponent<Image>().color = colorOther;

                                }

                                if (cancelBtn != null)
                                {
                                    // This line sets up the button click event
                                    cancelBtn.onClick.AddListener(() =>
                                    {

                                        if (entry.status == "active")
                                        {
                                            StartCoroutine(GameManager.instance.CancelBetAPI(entry.barcode));
                                        }
                                        else if (entry.status == "cancelled")
                                        {
                                            ToastManager.Instance.ShowToast("Bet already cancelled");

                                        }
                                        else
                                        {
                                            ToastManager.Instance.ShowToast("The bet time is already completed");
                                        }

                                        StartCoroutine(FetchHistory());
                                    });
                                }
                            }

                            // Set text on the prefab's children
                            SetText(item.transform.GetChild(0), entry.barcode);
                            SetText(item.transform.GetChild(1), entry.game_name); // This is index 1
                            SetText(item.transform.GetChild(2), entry.ticket_time); // This is index 2
                            SetText(item.transform.GetChild(3), entry.game_no); // etc.
                            SetText(item.transform.GetChild(4), entry.draw_time);
                            SetText(item.transform.GetChild(5), entry.play_point);
                            SetText(item.transform.GetChild(6), entry.claim_point);

                            // Assuming your button is at index 9
                            Button detailsButton = item.transform.GetChild(9).GetComponent<Button>();
                            if (detailsButton != null)
                            {
                                // This line sets up the button click event
                                detailsButton.onClick.AddListener(() => StartCoroutine(FetchDetailsAPI(entry.barcode)));
                            }



                        }
                        else
                        {
                            GameObject item = Instantiate(historyitemPrefab, content);
                            // Set text on the prefab's children
                            SetText(item.transform.GetChild(0), entry.barcode);
                            SetText(item.transform.GetChild(1), entry.game_name); // This is index 1
                            SetText(item.transform.GetChild(2), entry.ticket_time); // This is index 2
                            SetText(item.transform.GetChild(3), entry.game_no); // etc.
                            SetText(item.transform.GetChild(4), entry.draw_time);
                            SetText(item.transform.GetChild(5), entry.play_point);
                            SetText(item.transform.GetChild(6), entry.claim_point);

                            // Assuming your button is at index 9
                            Button cancelBtn = item.transform.GetChild(7).GetComponent<Button>();

                            if (entry.status == "active")
                            {
                                cancelBtn.GetComponent<Image>().color = colorActive;
                            }
                            else if (entry.status == "cancelled")
                            {
                                cancelBtn.GetComponent<Image>().color = colorCancelled;
                            }
                            else
                            {
                                cancelBtn.GetComponent<Image>().color = colorOther;

                            }

                            if (cancelBtn != null)
                            {
                                // This line sets up the button click event
                                cancelBtn.onClick.AddListener(() =>
                                {

                                    if (entry.status == "active")
                                    {
                                        if (SceneManager.GetActiveScene().name.Contains("3D"))
                                        {
                                            StartCoroutine(GameManager_3D.instance.CancelBetAPI(entry.barcode));
                                        }
                                        else
                                        {
                                            StartCoroutine(GameManager.instance.CancelBetAPI(entry.barcode));

                                        }
                                    }
                                    else if (entry.status == "cancelled")
                                    {
                                        ToastManager.Instance.ShowToast("Bet already cancelled");

                                    }
                                    else
                                    {
                                        ToastManager.Instance.ShowToast("The bet time is already completed");
                                    }

                                    StartCoroutine(FetchHistory());
                                });
                            }
                            // Assuming your button is at index 9
                            Button detailsButton = item.transform.GetChild(9).GetComponent<Button>();
                            if (detailsButton != null)
                            {
                                // This line sets up the button click event
                                detailsButton.onClick.AddListener(() => StartCoroutine(FetchDetailsAPI(entry.barcode)));
                            }
                            if (SceneManager.GetActiveScene().name.Contains("3D"))
                            {
                                Button printBtn = item.transform.GetChild(8).GetComponent<Button>();
                                if (printBtn != null)
                                {
                                    // This line sets up the button click event
                                    printBtn.onClick.AddListener(() => StartCoroutine(GameManager_3D.instance.DownloadPDF(entry.pdf_url, entry.game_no)));
                                }
                            }
                            else
                            {
                                Button printBtn = item.transform.GetChild(8).GetComponent<Button>();
                                if (printBtn != null)
                                {
                                    // This line sets up the button click event
                                    printBtn.onClick.AddListener(() => StartCoroutine(GameManager.instance.DownloadPDF(entry.pdf_url, entry.game_no)));
                                }
                            }

                        }

                    }
                    if (SceneManager.GetActiveScene().name.Contains("3D"))
                    {

                        GameManager_3D.instance.loadingObj.gameObject.SetActive(false);
                    }
                    else
                    {

                        GameManager.instance.loadingObj.gameObject.SetActive(false);

                    }

                }
                else
                {
                    Debug.LogWarning("No data found in response");
                }
            }
        }
    }

    // New coroutine to fetch details
    public IEnumerator FetchDetailsAPI(string orderId)
    {
        // Clear previous detail items
        foreach (Transform child in detailContent)
        {
            Destroy(child.gameObject);
        }
        detailsObj.gameObject.SetActive(true);

        WWWForm form = new WWWForm();
        form.AddField("orderid", orderId);

        using (UnityWebRequest www = UnityWebRequest.Post(GameAPIs.fetchHistoryDetailsAPi, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Details API Error: " + www.error);
            }
            else
            {
                string json = www.downloadHandler.text;
                Debug.Log("Details Response: " + json);

                DetailsResponse response = JsonUtility.FromJson<DetailsResponse>(json);

                if (response != null && response.data != null)
                {
                    foreach (var detail in response.data)
                    {
                        if (SceneManager.GetActiveScene().name.Contains("3D"))
                        {
                            GameObject detailItem3d = Instantiate(detailItemPrefab_3D, detailContent);
                            SetText(detailItem3d.transform.GetChild(0), detail.number);
                            SetText(detailItem3d.transform.GetChild(1), (int.Parse(detail.amount) * 2).ToString());
                            SetText(detailItem3d.transform.GetChild(2), detail.amount);
                            SetText(detailItem3d.transform.GetChild(3), detail.bet_type);
                            SetText(detailItem3d.transform.GetChild(4), detail.group_key);
                            SetText(detailItem3d.transform.GetChild(5), detail.status);
                        }
                        else
                        {


                            // Instantiate the detail item prefab
                            GameObject detailItem = Instantiate(detailItemPrefab, detailContent);

                            // Set text on the children of the detail prefab
                            SetText(detailItem.transform.GetChild(0), detail.id);
                            SetText(detailItem.transform.GetChild(1), (int.Parse(detail.amount) * 2).ToString());
                            SetText(detailItem.transform.GetChild(2), detail.amount);
                            SetText(detailItem.transform.GetChild(3), detail.orderid);
                            SetText(detailItem.transform.GetChild(4), detail.status);
                        }
                    }

                }
            }
        }
    }

    private void SetText(Transform target, string value)
    {
        TMP_Text text = target.GetComponent<TMP_Text>();
        if (text != null)
        {
            text.text = value;
        }
    }
}