using System;
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
    [SerializeField] private string ollamaUrl = "http://127.0.0.1:11434/api/generate";
    [SerializeField] private string ollamaModel = "llama3";

    [Header("Timing and Factors")]
    [SerializeField] private int FaceFactor = 1;
    [SerializeField] private int VoiceFactor = 1;
    [SerializeField] private double ThresholdFactor = 15;

    [Header("Current static id's")]
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
        StartCoroutine(PeriodicFeedback());
        LoadVideo();
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

        // STEP 3: Load next video and wait until it's prepared
        if(CurrentStep != LastVideoStep)
        {
            string videoTitle = $"Scenario_0{ScenarioID}_0{CurrentStep}_0{CurrentProgression}";
            VideoClip clip = Resources.Load<VideoClip>($"Videos/{videoTitle}");
            if (clip == null)
            {
                Debug.LogError($"❌ Could not load video: Resources/Videos/{videoTitle}");
                yield break;
            }

            videoPlayer.clip = clip;
            videoPlayer.Prepare();

            // Wait until the video is fully prepared
            yield return new WaitUntil(() => videoPlayer.isPrepared);

            // Start playing the video after preparation
            videoPlayer.Play();
            CurrentStep++;   
        }

        // STEP 4: Build LLM prompt
        string prompt =
            $"You are analyzing a VR training session where a trainee interacts with an angry customer.\n" +
            $"Your task is to evaluate whether the situation is escalating (0) or de-escalating (1) based on the trainee's behavior and biometric indicators.\n\n" +
            $"Interpretation rule:\n" +
            $"- 1 means the trainee is successfully de-escalating the situation.\n" +
            $"- 0 means the situation is escalating and corrective action is necessary.\n\n" +
            $"User Measurements:\n" +
            $"- Face Emotion: {faceRaw} (normalized: {faceNorm})\n" +
            $"- Voice Emotion: {voiceRaw} (normalized: {voiceNorm})\n" +
            $"- Hand Sign: {handRaw}\n" +
            $"- Finger Gesture: {fingerRaw}\n" +
            $"- Threshold Calculation Result (0=escalation, 1=de-escalation): {endResult}\n\n" +
            $"Using these values, provide clear and actionable feedback to help the trainee improve.\n" +
            $"Use the following structured format:\n" +
            $"FACE: ...\n" +
            $"VOICE: ...\n" +
            $"GESTURE: ...\n" +
            $"GENERAL: ...";

        // STEP 5: Query Ollama asynchronously while video is playing
        bool completed = false;
        string faceFeedback = faceNorm;
        string voiceFeedback = voiceNorm;
        string gestureFeedback = $"{handRaw} / {fingerRaw}";

        yield return StartCoroutine(QueryOllama(prompt, (f, v, g) =>
        {
            faceFeedback = string.IsNullOrEmpty(f) ? faceNorm : f;
            voiceFeedback = string.IsNullOrEmpty(v) ? voiceNorm : v;
            gestureFeedback = string.IsNullOrEmpty(g) ? $"{handRaw} / {fingerRaw}" : g;
            completed = true;
        }));

        yield return new WaitUntil(() => completed);

        // STEP 6: Update UI asynchronously
        if (ResponseTMP != null) ResponseTMP.text = Feedback.Replace("\n", " ");

        // STEP 7: Send feedback to server asynchronously
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

    private IEnumerator QueryOllama(string prompt, Action<string, string, string> onComplete)
    {
        var requestData = new OllamaRequest { model = ollamaModel, prompt = prompt };
        string jsonData = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(ollamaUrl, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            string responseText = request.downloadHandler.text;

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ Ollama request failed: {request.error}\n{responseText}");
                onComplete?.Invoke("", "", "");
                yield break;
            }

            Feedback = FlattenOllamaStreamingResponse(responseText);

            string face = ExtractSection(Feedback, "FACE");
            string voice = ExtractSection(Feedback, "VOICE");
            string gesture = ExtractSection(Feedback, "GESTURE");

            onComplete?.Invoke(face, voice, gesture);
        }
    }

    private string FlattenOllamaStreamingResponse(string rawJson)
    {
        if (string.IsNullOrEmpty(rawJson)) return "";

        StringBuilder sb = new StringBuilder();

        string[] lines = rawJson.Split('\n');

        foreach (string line in lines)
        {
            if (line.Contains("\"response\""))
            {
                int idx = line.IndexOf("\"response\"");
                int colon = line.IndexOf(':', idx);
                int firstQuote = line.IndexOf('"', colon + 1);
                int secondQuote = line.IndexOf('"', firstQuote + 1);

                if (firstQuote != -1 && secondQuote != -1)
                {
                    string extracted = line.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
                    sb.Append(extracted);
                }
            }
        }

        return System.Text.RegularExpressions.Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
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

        CurrentStep++;
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