using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class AccountManager : MonoBehaviour
{
    [Header("Login Fields")]
    [SerializeField] private string UsernameInput;
    [SerializeField] private string PasswordInput;

    [Header("Server Settings")]
    [SerializeField] private string serverUrl = "http://127.0.0.1:5000"; // Flask server URL

    // --------------------------
    // Login
    // --------------------------
    public void Login()
    {
        if (string.IsNullOrWhiteSpace(UsernameInput) || string.IsNullOrWhiteSpace(PasswordInput))
        {
            Debug.LogWarning("Username or password is empty.");
            return;
        }

        StartCoroutine(PostToServer("/login", UsernameInput, PasswordInput));
    }

    // --------------------------
    // Create Account
    // --------------------------
    public void CreateAccount()
    {
        if (string.IsNullOrWhiteSpace(UsernameInput) || string.IsNullOrWhiteSpace(PasswordInput))
        {
            Debug.LogWarning("Username or password is empty.");
            return;
        }

        StartCoroutine(PostToServer("/create_account", UsernameInput, PasswordInput));
    }

    // --------------------------
    // Coroutine to send POST request
    // --------------------------
    private IEnumerator PostToServer(string endpoint, string username, string password)
    {
        string url = serverUrl + endpoint;

        WWWForm form = new WWWForm();
        form.AddField("username", username);
        form.AddField("password", password);

        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"✅ Server Response: {request.downloadHandler.text}");
            }
            else
            {
                Debug.LogError($"❌ Server Error: {request.error}");
            }
        }
    }
}