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

    [Header("API Settings")]
    [SerializeField] private string apiUrl = "http://127.0.0.1:5000/get_emotion";
    private bool isConnected = false;

    // Shared state accessible from FeedbackHandler
    public static string CurrentFaceEmotion = "none";
    public static string CurrentVoiceEmotion = "none";
    public static string CurrentHandSign = "none";
    public static string CurrentFingerGesture = "none";

    void Start()
    {
        StartCoroutine(GetEmotionLoop());
    }

    private IEnumerator GetEmotionLoop()
    {
        while (true)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(apiUrl))
            {
                www.timeout = 2;
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    string json = www.downloadHandler.text;

                    try
                    {
                        EmotionData data = JsonUtility.FromJson<EmotionData>(json);
                        isConnected = true;

                        // Update static values
                        CurrentFaceEmotion = SafeText(data.face_emotion);
                        CurrentVoiceEmotion = SafeText(data.voice_emotion);
                        CurrentHandSign = SafeText(data.hand_sign);
                        CurrentFingerGesture = SafeText(data.finger_gesture);

                        // Debug the actual raw readings
                        Debug.Log($"🎭 [OpenCVLoader] Raw Emotions | Face: {CurrentFaceEmotion}, Voice: {CurrentVoiceEmotion}, Hand: {CurrentHandSign}, Finger: {CurrentFingerGesture}");

                        // Update UI for visibility (optional)
                        if (FaceTMP) FaceTMP.text = $"Face Emotion: {CurrentFaceEmotion}";
                        if (VoiceTMP) VoiceTMP.text = $"Voice Emotion: {CurrentVoiceEmotion}";
                        if (GestureTMP) GestureTMP.text = $"Hand: {CurrentHandSign} | Finger: {CurrentFingerGesture}";
                    }
                    catch (System.Exception e)
                    {
                        HandleConnectionError($"JSON parse error: {e.Message}");
                    }
                }
                else
                {
                    HandleConnectionError(www.error);
                }
            }

            // Poll every 1 second (can be reduced if needed)
            yield return new WaitForSeconds(1f);
        }
    }

    private void HandleConnectionError(string error)
    {
        if (isConnected)
        {
            Debug.LogWarning($"⚠️ Lost connection: {error}");
            isConnected = false;
        }

        // Update UI to show disconnected
        if (FaceTMP) FaceTMP.text = "Face Emotion: (disconnected)";
        if (VoiceTMP) VoiceTMP.text = "Voice Emotion: (disconnected)";
        if (GestureTMP) GestureTMP.text = "Hand: (disconnected) | Finger: (disconnected)";

        // Reset static values
        CurrentFaceEmotion = "none";
        CurrentVoiceEmotion = "none";
        CurrentHandSign = "none";
        CurrentFingerGesture = "none";
    }

    private string SafeText(string value)
    {
        if (string.IsNullOrEmpty(value)) return "none";
        return value.ToLower().Trim();
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