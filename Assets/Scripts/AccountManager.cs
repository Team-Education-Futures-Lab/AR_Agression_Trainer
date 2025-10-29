using System.Collections;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class AccountManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The text label of the login/create button (used to detect mode).")]
    public TMP_Text LoginButtonText;

    [Header("Input Button Texts")]
    [Tooltip("The text shown on the username button (set by InputHandler).")]
    [SerializeField] private TMP_Text UsernameButtonText;

    [Tooltip("The text shown on the password button (set by InputHandler).")]
    [SerializeField] private TMP_Text PasswordButtonText;

    [Header("Server Settings")]
    [SerializeField] 
    private string serverUrl = "http://127.0.0.1:5000/AccountInformation"; // Flask server URL

    // --------------------------
    // Login / Create Account
    // --------------------------
    public void SubmitAccount()
    {
        string username = UsernameButtonText != null ? UsernameButtonText.text.Trim() : "";
        string password = PasswordButtonText != null ? PasswordButtonText.text.Trim() : "";

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            Debug.LogWarning("⚠️ Username or password is empty.");
            return;
        }

        string hashedPassword = ComputeSHA256Hash(password);

        // Determine request type based on button text
        string requestType = LoginButtonText != null && 
                             LoginButtonText.text.ToLower().Contains("login") ? "login" : "create";

        Debug.Log($"Submitting {requestType} request for user '{username}'");

        StartCoroutine(PostToServer(requestType, username, hashedPassword));
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
        form.AddField("type", requestType);
        form.AddField("username", username);
        form.AddField("password", hashedPassword);

        if (requestType == "create")
        {
            form.AddField("email", $"{username}@example.com");
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
