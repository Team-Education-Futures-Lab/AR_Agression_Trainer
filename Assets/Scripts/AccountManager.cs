using System.Collections;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI; // Required for Text component
using TMPro;

public class AccountManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text LoginButtonText;

    [Header("Login Fields")]
    [SerializeField] private string UsernameInput;
    [SerializeField] private string PasswordInput;

    [Header("Server Settings")]
    [SerializeField] private string serverUrl = "http://127.0.0.1:5000/AccountInformation"; // Flask server URL

    // --------------------------
    // Login / Create Account
    // --------------------------
    public void SubmitAccount()
    {
        if (string.IsNullOrWhiteSpace(UsernameInput) || string.IsNullOrWhiteSpace(PasswordInput))
        {
            Debug.LogWarning("Username or password is empty.");
            return;
        }

        string hashedPassword = ComputeSHA256Hash(PasswordInput);

        // Determine request type based on button text
        string requestType = LoginButtonText.text.ToLower() == "login" ? "login" : "create";

        StartCoroutine(PostToServer(requestType, UsernameInput, hashedPassword));
    }

    // --------------------------
    // Hashing helper
    // --------------------------
    private string ComputeSHA256Hash(string rawData)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            StringBuilder builder = new StringBuilder();
            foreach (byte b in bytes)
                builder.Append(b.ToString("x2"));
            return builder.ToString();
        }
    }

    // --------------------------
    // Coroutine to send POST request
    // --------------------------
    private IEnumerator PostToServer(string requestType, string username, string hashedPassword)
    {
        WWWForm form = new WWWForm();
        form.AddField("type", requestType); // lowercase, no spaces
        form.AddField("username", username);
        form.AddField("password", hashedPassword);

        if(requestType == "create")
        {
            form.AddField("email", username + "@example.com");
            form.AddField("phonenumber", "0000000000");
        }

        using (UnityWebRequest request = UnityWebRequest.Post(serverUrl, form))
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