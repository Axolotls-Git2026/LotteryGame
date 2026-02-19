using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class GlobalInternetChecker : MonoBehaviour
{
    public static GlobalInternetChecker Instance;

    [Header("UI To Show When Offline")]
    public GameObject noInternetObject;

    [Header("Check Settings")]
    public float checkInterval = 5f; // seconds between checks
    private bool isOffline = false;

    private void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        StartCoroutine(CheckInternetRoutine());
    }

    IEnumerator CheckInternetRoutine()
    {
        while (true)
        {
            yield return CheckInternetConnection();
            yield return new WaitForSeconds(checkInterval);
        }
    }

    IEnumerator CheckInternetConnection()
    {
        UnityWebRequest request = UnityWebRequest.Get("https://www.google.com");
        request.timeout = 5;
        yield return request.SendWebRequest();

        bool hasInternet = request.result == UnityWebRequest.Result.Success;

        if (!hasInternet && !isOffline)
        {
            isOffline = true;
            if (noInternetObject != null)
                noInternetObject.SetActive(true);
        }
        else if (hasInternet && isOffline)
        {
            isOffline = false;
            if (noInternetObject != null)
                noInternetObject.SetActive(false);
        }
    }
}
