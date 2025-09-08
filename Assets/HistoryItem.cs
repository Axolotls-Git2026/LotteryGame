using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HistoryItem : MonoBehaviour
{
    public Button detailsButton;
    public string orderId; // To store the orderid for the API call

     HistoryFetcher historyFetcher;

    public void Setup(string orderId, HistoryFetcher fetcher)
    {
        this.orderId = orderId;
        this.historyFetcher = fetcher;
        detailsButton.onClick.AddListener(OnDetailsButtonClicked);
    }

    private void OnDetailsButtonClicked()
    {
        historyFetcher.StartCoroutine(historyFetcher.FetchDetailsAPI(orderId));
    }
}