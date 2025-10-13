using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class OpenCVLoader : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI FaceTMP;
    public TextMeshProUGUI VoiceTMP;
    public TextMeshProUGUI GestureTMP;

    // Flask API endpoint
    private string apiUrl = "http://127.0.0.1:5000/get_emotion";

    // Internal state
    private bool isConnected = false;

    void Start()
    {
        StartCoroutine(GetEmotionLoop());
    }

    IEnumerator GetEmotionLoop()
    {
        while (true)
        {
            // We move the web request out of the try/catch so yield is valid
            UnityWebRequest www = null;
            bool success = false;
            string errorMessage = "";

            // Perform the request safely
            www = UnityWebRequest.Get(apiUrl);
            www.timeout = 2; // seconds

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                success = true;
                string json = www.downloadHandler.text;

                try
                {
                    EmotionData data = JsonUtility.FromJson<EmotionData>(json);

                    isConnected = true;
                    Debug.Log($"🎭 Face: {data.face_emotion} | 🎤 Voice: {data.voice_emotion} | ✋ Hand: {data.hand_sign} | 👉 Gesture: {data.finger_gesture}");

                    // --- Update UI elements ---
                    if (FaceTMP != null)
                        FaceTMP.text = "Face Emotion: " + SafeText(data.face_emotion);

                    if (VoiceTMP != null)
                        VoiceTMP.text = "Voice Emotion: " + SafeText(data.voice_emotion);

                    if (GestureTMP != null)
                        GestureTMP.text = $"Hand Sign: {SafeText(data.hand_sign)}\nFinger Gesture: {SafeText(data.finger_gesture)}";
                }
                catch (System.Exception e)
                {
                    errorMessage = e.Message;
                    success = false;
                }
            }
            else
            {
                errorMessage = www.error;
            }

            // Handle network or parse errors
            if (!success)
            {
                HandleConnectionError(errorMessage);
            }

            yield return new WaitForSeconds(1f);
        }
    }

    private void HandleConnectionError(string error)
    {
        if (isConnected)
        {
            Debug.LogWarning($"⚠️ Lost connection to Flask server: {error}");
            isConnected = false;
        }
        else
        {
            Debug.Log($"🕐 Waiting for Flask server... ({error})");
        }

        if (FaceTMP != null)
            FaceTMP.text = "Face Emotion: (disconnected)";
        if (VoiceTMP != null)
            VoiceTMP.text = "Voice Emotion: (disconnected)";
        if (GestureTMP != null)
            GestureTMP.text = "Hand Sign: (disconnected)\nFinger Gesture: (disconnected)";
    }

    private string SafeText(string value)
    {
        return string.IsNullOrEmpty(value) ? "none" : value;
    }
}

[System.Serializable]
public class EmotionData
{
    public string face_emotion;
    public string voice_emotion;
    public string hand_sign;
    public string finger_gesture;
}