using System;
using System.IO;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;
using TMPro;

public class FeedbackHandler : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI ResponseTMP;
    public TextMeshProUGUI NextStepInt;
    public TextMeshProUGUI ScoreUpdate;

    [Header("Server Settings")]
    [SerializeField] private string emotionApiUrl = "http://127.0.0.1:5000/get_emotion";
    [SerializeField] private string feedbackApiUrl = "http://127.0.0.1:5000/submit_feedback";
    [SerializeField] private OllamaHandler ollamaHandler;

    [Header("Timing and Factors")]
    [SerializeField] private int FaceFactor = 1;
    [SerializeField] private int VoiceFactor = 1;
    [SerializeField] private double ThresholdFactor = 9;

    [Header("Current static id's")] //Make this later dynamic
    private int UserID = 1;
    private int LevelID = 2;
    private string Feedback = "";

    private enum Emotions { Angry = 1, Fear = 2, Disgust = 3,  Sad = 4, Surprise = 5, Happy = 6, Neutral = 7 }

    [Header("Video loader")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private int ScenarioID = 1;
    [SerializeField] private int CurrentStep = 1;
    [SerializeField] private int CurrentProgression = 1;
    [SerializeField] private int LastVideoStep = 7;

    void Start()
    {
        LoadVideo();
        StartCoroutine(PeriodicFeedback());
    }

    private IEnumerator PeriodicFeedback()
    {
        while (true)
        {
            yield return StartCoroutine(FetchEmotionsAndGenerateFeedback());
            yield return new WaitUntil(() => !videoPlayer.isPlaying);
        }
    }

    private IEnumerator FetchEmotionsAndGenerateFeedback()
    {
        // STEP 1: Fetch emotions from API (can start immediately)
        string faceRaw = "none";
        string voiceRaw = "none";
        string handRaw = "none";
        string fingerRaw = "none";

        using (UnityWebRequest www = UnityWebRequest.Get(emotionApiUrl))
        {
            www.timeout = 2;
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    EmotionData data = JsonUtility.FromJson<EmotionData>(www.downloadHandler.text);
                    faceRaw = SafeText(data.face_emotion);
                    voiceRaw = SafeText(data.voice_emotion);
                    handRaw = SafeText(data.hand_sign);
                    fingerRaw = SafeText(data.finger_gesture);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"⚠️ Failed to parse emotion API: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ Emotion API request failed: {www.error}");
            }
        }

        Debug.Log($"🎤 Fetched Emotions | Face: '{faceRaw}', Voice: '{voiceRaw}', Hand: '{handRaw}', Finger: '{fingerRaw}'");

        // STEP 2: Normalize face/voice only for calculation
        string faceNorm = NormalizeEmotion(faceRaw);
        string voiceNorm = NormalizeEmotion(voiceRaw);
        int endResult = 0;

        if (Enum.TryParse(faceNorm, true, out Emotions faceVal) && Enum.TryParse(voiceNorm, true, out Emotions voiceVal))
        {
            double total = ((int)faceVal * FaceFactor) + ((int)voiceVal * VoiceFactor);

            // Step 1: Determine endResult based on threshold
            endResult = total >= ThresholdFactor ? -1 : 1;

            int newProgression = CurrentProgression + endResult;

            // Clamp progression to valid range (1-4)
            newProgression = Mathf.Clamp(newProgression, 1, 4);

            // Update endResult based on clamped progression
            endResult = newProgression - CurrentProgression;
            CurrentProgression = newProgression;

            // Update score display
            if (ScoreUpdate != null)
            {
                ScoreUpdate.text = total.ToString();   
            }

            if (NextStepInt != null)
            {
                NextStepInt.text = endResult.ToString();   
            }
        }

        // STEP 3: Load and play video immediately
        if(CurrentStep != LastVideoStep)
        {
            string videoTitle = $"Scenario_0{ScenarioID}_0{CurrentStep}_0{CurrentProgression}";

            if(CurrentStep == 1)
            {
                videoTitle = $"Scenario_0{ScenarioID}_0{CurrentStep}_01";
            }

            VideoClip clip = Resources.Load<VideoClip>($"Videos/{videoTitle}");
            if (clip == null)
            {
                Debug.LogError($"❌ Could not load video: Resources/Videos/{videoTitle}");
            }
            else
            {
                videoPlayer.clip = clip;
                videoPlayer.Prepare();
                yield return new WaitUntil(() => videoPlayer.isPrepared);
                videoPlayer.Play();
                CurrentStep++;
            }
        }

        // STEP 4: Start Ollama asynchronously, but DON'T yield return it
        yield return StartCoroutine(
            ollamaHandler.StartOllama(
                faceRaw,
                faceNorm,
                voiceRaw,
                voiceNorm,
                handRaw,
                fingerRaw,
                endResult.ToString(),
                (full, face, voice, gesture) =>
                {
                    // Update Feedback and UI when done
                    Feedback = full;
                    if (ResponseTMP != null)
                        ResponseTMP.text = Feedback.Replace("\n", " ");

                    // Send to server
                    if (!string.IsNullOrEmpty(Feedback))
                    {
                        StartCoroutine(
                            SendFeedbackToServer(UserID, LevelID, Feedback.Replace("\n", " "))
                        );
                    }
                }
            )
        );

        // STEP 5: Update UI asynchronously
        if (ResponseTMP != null) ResponseTMP.text = Feedback.Replace("\n", " ");

        // STEP 6: Send feedback to server asynchronously
        StartCoroutine(SendFeedbackToServer(UserID, LevelID, Feedback.Replace("\n", " ")));
    }

    private string SafeText(string value)
    {
        return string.IsNullOrEmpty(value) ? "none" : value.ToLower().Trim();
    }

    private string NormalizeEmotion(string emotion)
    {
        if (string.IsNullOrEmpty(emotion)) return "Neutral";
        emotion = emotion.ToLower().Trim();

        switch (emotion)
        {
            case "01":
            case "02": return "Neutral";
            case "03": return "Happy";
            case "04": return "Sad";
            case "05": return "Angry";
            case "06": return "Fear";
            case "07": return "Disgust";
            case "08": return "Surprise";
        }

        if (emotion.Contains("fear")) return "Fear";
        if (emotion.Contains("angry")) return "Angry";
        if (emotion.Contains("disgust")) return "Disgust";
        if (emotion.Contains("sad")) return "Sad";
        if (emotion.Contains("happy")) return "Happy";
        if (emotion.Contains("surprise")) return "Surprise";
        if (emotion.Contains("neutral") || emotion.Contains("calm") || emotion.Contains("none")) return "Neutral";

        return "Neutral";
    }

    private string ExtractSection(string text, string section)
    {
        if (string.IsNullOrEmpty(text)) return "";

        foreach (string line in text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.StartsWith(section + ":", StringComparison.OrdinalIgnoreCase))
            {
                string content = line.Substring(section.Length + 1).Trim();
                string firstWord = content.Split(new[] { ' ', '.', ',' }, StringSplitOptions.RemoveEmptyEntries)[0];
                return NormalizeEmotion(firstWord);
            }
        }
        return "";
    }

    private IEnumerator SendFeedbackToServer(int userId, int level, string feedback)
    {
        // Build JSON payload
        var payload = new FeedbackData
        {
            User_ID = userId,
            Level = level,
            Feedback = feedback
        };
        string jsonData = JsonUtility.ToJson(payload);

        // Create request
        using (UnityWebRequest www = new UnityWebRequest(feedbackApiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            Debug.Log($"➡️ Sending feedback JSON: {jsonData}");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ Feedback submission failed: {www.error}\n{www.downloadHandler.text}");
            }
            else
            {
                Debug.Log($"✅ Feedback successfully sent: {www.downloadHandler.text}");
            }
        }
    }

    private void LoadVideo()
    {
        if (videoPlayer == null)
        {
            Debug.LogError("❌ No VideoPlayer assigned in the inspector!");
            return;
        }

        string videoTitle = $"Scenario_0{ScenarioID}_0{CurrentStep}_0{CurrentProgression}";
        VideoClip clip = Resources.Load<VideoClip>($"Videos/{videoTitle}");

        if (clip == null)
        {
            Debug.LogError($"❌ Could not load video: Resources/Videos/{videoTitle}");
            return;
        }

        videoPlayer.clip = clip;
        videoPlayer.Play();
    }

    [Serializable] public class OllamaRequest { public string model; public string prompt; }
    [Serializable] private class OllamaResponse { public string response; }

    
    [Serializable]
    private class FeedbackData
    {
        public int User_ID;
        public int Level;
        public string Feedback;
    }

    [Serializable]
    private class EmotionData
    {
        public string face_emotion;
        public string voice_emotion;
        public string hand_sign;
        public string finger_gesture;
    }
}