using System.Collections;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class AccountManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text label of the login/create button (used to detect mode).")]
    [SerializeField]
    private TMP_Text loginButtonLabel;

    [Header("Input Button Texts")]
    [Tooltip("Text shown on the username button (set by InputHandler).")]
    [SerializeField]
    private TMP_Text usernameButtonLabel;

    [Tooltip("Text shown on the password button (set by InputHandler).")]
    [SerializeField]
    private TMP_Text passwordButtonLabel;

    [Header("Server Settings")]
    [SerializeField]
    private string serverUrl = "http://127.0.0.1:5000/AccountInformation";

    public void SubmitAccount()
    {
        string username = usernameButtonLabel != null ? usernameButtonLabel.text.Trim() : string.Empty;
        string password = passwordButtonLabel != null ? passwordButtonLabel.text.Trim() : string.Empty;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            Debug.LogWarning("Username or password is empty.");
            return;
        }

        string hashedPassword = ComputeSha256Hash(password);

        string requestType = loginButtonLabel != null &&
                             loginButtonLabel.text.ToLower().Contains("login") ? "login" : "create";

        Debug.Log($"Submitting {requestType} request for user '{username}'");

        StartCoroutine(PostToServer(requestType, username, hashedPassword));
    }

    private string ComputeSha256Hash(string rawData)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            StringBuilder builder = new StringBuilder();
            foreach (byte b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }

            return builder.ToString();
        }
    }

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
                Debug.Log($"Server response: {request.downloadHandler.text}");
            }
            else
            {
                Debug.LogError($"Server error: {request.error}");
            }
        }
    }
}
