using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.SceneManagement;

public class ResultFetcher : MonoBehaviour
{
    [System.Serializable]
    public class ApiResponse
    {
        public string status;
        public string date;
        public ResultData[] data; // <-- FIX: now it's an array
    }

    [System.Serializable]
    public class ResultData
    {
        public string time;
        public string[] numbers; // <-- FIX: match "numbers" key in JSON
    }

    [Header("Prefab & Parent")]
    public GameObject resultPrefab;   // Assign in Inspector
    public GameObject resultPrefab_3D;   // Assign in Inspector
    public Transform parent;          // Where to spawn results
    public GameObject parent_3d;          // Where to spawn results

    private void OnEnable()
    {
        if (SceneManager.GetActiveScene().name.Contains("3D"))
        {
            Debug.Log("Fetch 3d called");
            Fetch3DResult();
        }
        else
        { 
            FetchAndInstantiate();
        }
    }
    private void OnDisable()
    {
        if (parent.childCount > 0)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }
    }

    private void Start()
    {

     //   FetchAndInstantiate();
    }

    public void FetchAndInstantiate()
    {
        if (parent.childCount > 0)
        {
            for (int i = 0; i <= parent.childCount; i++)
            {

                Destroy(parent.GetChild(i));
            }
        }

        StartCoroutine(FetchResults());
    }

    private IEnumerator FetchResults()
    {
        GameManager.instance.loadingObj.gameObject.SetActive(true);
        using (UnityWebRequest www = UnityWebRequest.Get(GameAPIs.fetchResultAPi))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("API Error: " + www.error);
            }
            else
            {
                string json = www.downloadHandler.text;
                Debug.Log("Response: " + json);

                ApiResponse response = JsonUtility.FromJson<ApiResponse>(json);
                if (response.status != "success")
                {
                    GameManager.instance.loadingObj.gameObject.SetActive(false);
                    ToastManager.Instance.ShowToast("No data found");
                }

                if (response != null && response.data != null && response.data.Length > 0)
                {
                    foreach (var entry in response.data)
                    {
                        // Instantiate prefab
                        GameObject resultObj = Instantiate(resultPrefab, parent);

                        // --- Timer Text (0th child) ---
                        TMP_Text timerText = resultObj.transform.GetChild(0).GetChild(0).GetComponent<TMP_Text>();
                        timerText.text = entry.time;

                        // --- Result Items (1st child’s children) ---
                        Transform resultItemsParent = resultObj.transform.GetChild(1);

                        for (int i = 0; i < entry.numbers.Length && i < resultItemsParent.childCount; i++)
                        {
                            TMP_Text numText = resultItemsParent.GetChild(i).GetChild(0).GetComponent<TMP_Text>();
                            numText.text = entry.numbers[i];
                        }
                    }
                    GameManager.instance.loadingObj.gameObject.SetActive(false);

                }
                else
                {
                    Debug.LogWarning("No data found in response!");
                }
            }
        }
    }

    public void Fetch3DResult()
    {
     StartCoroutine(GameManager_3D.instance.FetchAndSpawn(resultPrefab_3D, parent_3d));
    }

}
