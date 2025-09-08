using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

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
    public string status;
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
}

public class HistoryFetcher : MonoBehaviour
{
    [Header("Prefab and Content Holder")]
    public GameObject historyitemPrefab;
    public Transform content;

    [Header("Details Prefab and Content Holder")]
    public GameObject detailItemPrefab;
    public Transform detailContent;

    public GameObject detailsObj;

    [System.Serializable]
    public class HistoryRequest
    {
        public string userid;
    }

    void Start()
    {
        StartCoroutine(FetchHistory());
    }

    IEnumerator FetchHistory()
    {
        WWWForm form = new WWWForm();
        form.AddField("id", PlayerPrefs.GetInt("UserId").ToString());
        GameManager.instance.loadingObj.gameObject.SetActive(true);

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

                if (response != null && response.data != null)
                {
                    foreach (var entry in response.data)
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
                        if (cancelBtn != null)
                        {
                            // This line sets up the button click event
                            cancelBtn.onClick.AddListener(() => { 
                                StartCoroutine(GameManager.instance.CancelBetAPI(entry.barcode));
                                StartCoroutine(FetchHistory());
                            });
                        } 
                        Button printBtn = item.transform.GetChild(8).GetComponent<Button>();
                        if (printBtn != null)
                        {
                            // This line sets up the button click event
                            printBtn.onClick.AddListener(() => StartCoroutine(GameManager.instance.DownloadPDF(entry.pdf_url)));
                        }
                        // Assuming your button is at index 9
                        Button detailsButton = item.transform.GetChild(9).GetComponent<Button>();
                        if (detailsButton != null)
                        {
                            // This line sets up the button click event
                            detailsButton.onClick.AddListener(() => StartCoroutine(FetchDetailsAPI(entry.barcode)));
                        }
                    }
                    GameManager.instance.loadingObj.gameObject.SetActive(false);

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
                        // Instantiate the detail item prefab
                        GameObject detailItem = Instantiate(detailItemPrefab, detailContent);

                        // Set text on the children of the detail prefab
                        SetText(detailItem.transform.GetChild(0), detail.id);
                        SetText(detailItem.transform.GetChild(1), (int.Parse(detail.amount) * 2).ToString() );
                        SetText(detailItem.transform.GetChild(2), detail.amount);
                        SetText(detailItem.transform.GetChild(3), detail.orderid);
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