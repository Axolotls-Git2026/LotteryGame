using System;
using System.Collections;
using System.Runtime.InteropServices;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class LoginManager : MonoBehaviour
{

    public static bool isLogin = false;
    public TMP_InputField user_id;
    public TMP_InputField user_password;

    public Toggle rememberMe;

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(System.IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern System.IntPtr GetActiveWindow();

    const int SW_MINIMIZE = 6;




    [Header("Logout data")]
    public GameObject loginPanel;
    public GameObject settingPanel;





    private void Start()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;

        if (PlayerPrefs.GetInt("RememberMe") == 1)
        {
            WWWForm form = new WWWForm();
            string id = PlayerPrefs.GetString(GameAPIs.user_id);
            string password = PlayerPrefs.GetString(GameAPIs.user_Password);


            form.AddField("id", PlayerPrefs.GetInt("UserId", 0));
            form.AddField("password", password);
            user_id.text = id;
            user_password.text = password;

            StartCoroutine(Authenticate(GameAPIs.loginAPi, form, id));
        }


        user_id.contentType = TMP_InputField.ContentType.IntegerNumber;


        if (PlayerPrefs.GetInt(GameAPIs.login_Done) != 1) return;


    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            BTN_Login();
        }
    }
    public void OnRememberMe()
    {
        PlayerPrefs.SetInt("RememberMe", rememberMe.isOn ? 1 : 0);
    }

    public void BTN_Login()
    {

        if (string.IsNullOrEmpty(user_id.text) || string.IsNullOrEmpty(user_password.text))
        {
            ToastManager.Instance.ShowToast("Invalid username or Password");
            return;
        }

        WWWForm form = new WWWForm();
        string phone = user_id.text;
        string password = user_password.text;

        Debug.Log($"BTN_Login - Phone: {phone}, Password: {password}");

        form.AddField("id", phone);
        form.AddField("password", password);

        StartCoroutine(Authenticate(GameAPIs.loginAPi, form, phone));
    }

    public static string GetDeviceID()
    {
        if (Application.platform != RuntimePlatform.Android) return "";

        // Get Android ID
        AndroidJavaClass clsUnity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject objActivity = clsUnity.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaObject objResolver = objActivity.Call<AndroidJavaObject>("getContentResolver");
        AndroidJavaClass clsSecure = new AndroidJavaClass("android.provider.Settings$Secure");

        string android_id = clsSecure.CallStatic<string>("getString", objResolver, "android_id");

        // Get bytes of Android ID
        System.Text.UTF8Encoding ue = new System.Text.UTF8Encoding();
        byte[] bytes = ue.GetBytes(android_id);

        // Encrypt bytes with md5
        System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
        byte[] hashBytes = md5.ComputeHash(bytes);

        // Convert the encrypted bytes back to a string (base 16)
        string hashString = "";

        for (int i = 0; i < hashBytes.Length; i++)
        {
            hashString += System.Convert.ToString(hashBytes[i], 16).PadLeft(2, '0');
        }

        string device_id = hashString.PadLeft(32, '0');

        return device_id;
    }

    IEnumerator Authenticate(string apiUrl, WWWForm form, string phone)
    {
        DateTime currTime = DateTime.Now;

        using (UnityWebRequest request = UnityWebRequest.Post(apiUrl, form))
        {
            // Log form data
            Debug.Log($"Authenticate - API URL: {apiUrl}");
            foreach (var field in form.data)
            {
                //Debug.Log($"Authenticate - Field: {field}");
            }

            // Set Basic Authentication header
            string auth = "Basic " + Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes("phone:password"));
            request.SetRequestHeader("Authorization", auth);


            yield return request.SendWebRequest();


            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error: " + request.error);
                ToastManager.Instance.ShowToast("Unexpected Error Occured Please try again");


            }
            else
            {

                DateTime finishTime = DateTime.Now;

                Debug.Log("Request successful!");
                Debug.Log(request.downloadHandler.text);
                string jsonData = request.downloadHandler.text;
                UserData userData = JsonUtility.FromJson<UserData>(jsonData);



                if (userData.status == "success")
                {

                    PlayerPrefs.SetInt(GameAPIs.login_Done, 1);


                    PlayerPrefs.SetString("MobileNumber", phone);

                    if (!string.IsNullOrEmpty(user_id.text))
                    {
                        PlayerPrefs.SetInt("UserId", int.Parse(userData.data.id));
                        PlayerPrefs.SetInt(GameAPIs.user_id, int.Parse(userData.data.id));

                    }
                    if (!string.IsNullOrEmpty(user_password.text))
                        PlayerPrefs.SetString(GameAPIs.user_Password, user_password.text);
                        PlayerPrefs.SetString("Sessionid", userData.data.session);
                    Debug.Log("Session Id : " + userData.data.session);
                    GameAPIs.SetUserName(userData.data.name);
                    PlayerPrefs.SetString("pass_count", userData.data.pass_count);
                    StartCoroutine(LoadSceneDelay());


                }
                else
                {
                    ToastManager.Instance.ShowToast("Invalid username or Password");
                }
            }
        }
    }

    IEnumerator LoadSceneDelay()
    {
        ToastManager.Instance.ShowToast("Login Successful");
        SoundManager.Instance.PlaySound(SoundManager.Instance.commonSound);


        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene(1);

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

    #region Logout API
    public void LogOUT()
    {


        string id = PlayerPrefs.GetString(GameAPIs.user_id);

        Debug.Log($"LogOUT - Phone: {id}");

        WWWForm form = new WWWForm();
        form.AddField("phone", id);
        //  StartCoroutine(LogoutCall(GameAPIs.logOutAPi, form));
    }

    IEnumerator LogoutCall(string apiURL, WWWForm form)
    {
        using (UnityWebRequest request = UnityWebRequest.Post(apiURL, form))
        {
            // Log form data
            Debug.Log($"LogoutCall - API URL: {apiURL}");
            foreach (var field in form.data)
            {
                Debug.Log($"LogoutCall - Field: {field}");
            }

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error: " + request.error);
            }
            else
            {
                Debug.Log("Request successful!" + request.downloadHandler.text);


                loginPanel.SetActive(true);



                settingPanel.SetActive(false);


                PlayerPrefs.DeleteAll();
            }

            request.Dispose();
        }
    }
    #endregion

    [System.Serializable]
    public class UserData
    {
        public string status;
        public string message;
        public Data data;
    }

    [System.Serializable]
    public class Data
    {
        public string id;
        public string name;
        public string session;
        public int wallet;
        public string pass_count;

    }
}