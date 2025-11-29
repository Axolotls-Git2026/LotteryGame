using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;

public class ResetPass : MonoBehaviour
{
    public TMP_InputField EnterPass;
    public TMP_InputField ConfirmPass;
    public Button resetPass;

   

    void Start()
    {
        resetPass.onClick.AddListener(ResetBtn);
    }

    public void ResetBtn()
    {
        string pass = EnterPass.text;
        string confirm = ConfirmPass.text;

        if (string.IsNullOrEmpty(pass) || string.IsNullOrEmpty(confirm))
        {
            Debug.LogWarning("Password fields cannot be empty!");
            ToastManager.Instance.ShowToast("Password fields cannot be empty!");

            return;
        }

        if (pass != confirm)
        {
            Debug.LogWarning("Passwords do not match!");
            ToastManager.Instance.ShowToast("Password do not match");
            return;
        }

        StartCoroutine(ResetPasswordRoutine(PlayerPrefs.GetInt("UserId"), confirm));
    }

    private IEnumerator ResetPasswordRoutine(int id, string password)
    {
        WWWForm form = new WWWForm();
        form.AddField("id", id);
        form.AddField("password", password);

        using (UnityWebRequest www = UnityWebRequest.Post(GameAPIs.resetPassAPi, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error: " + www.error);
            }
            else
            {
                string response = www.downloadHandler.text;
                Debug.Log("Response: " + response);

                // Optionally parse JSON
                ResetResponse res = JsonUtility.FromJson<ResetResponse>(response);
                if (res != null && res.status == "success")
                {
                    Debug.Log("? Password updated successfully!");
                }
                else
                {
                    Debug.LogWarning("? Password update failed: " + res?.message);
                }
            }
        }
    }
}

[System.Serializable]
public class ResetResponse
{
    public string status;
    public string message;
}
