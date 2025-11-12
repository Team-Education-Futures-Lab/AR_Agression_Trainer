using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Newtonsoft.Json;

public class FeedbackHandler : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI ResponseTMP;
    public TextMeshProUGUI NextStepInt;

    [Header("Server Settings")]
    [SerializeField] private string emotionApiUrl = "http://127.0.0.1:5000/get_emotion";
    [SerializeField] private string ollamaUrl = "http://127.0.0.1:11434/api/generate";
    [SerializeField] private string ollamaModel = "llama3";

    [Header("Timing and Factors")]
    private float feedbackInterval = 120f;
    private int FaceFactor = 1;
    private int VoiceFactor = 2;
    private double ThresholdFactor = 10.5;

    private string Feedback = "";

    private enum Emotions { Angry = 1, Disgust = 2, Fear = 3, Sad = 4, Surprise = 5, Happy = 6, Neutral = 7 }

    void Start()
    {
        StartCoroutine(PeriodicFeedback());
    }

    private IEnumerator PeriodicFeedback()
    {
        while (true)
        {
            yield return StartCoroutine(FetchEmotionsAndGenerateFeedback());
            yield return new WaitForSeconds(feedbackInterval);
        }
    }

    private IEnumerator FetchEmotionsAndGenerateFeedback()
    {
        // STEP 1: Fetch emotions directly from API
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

        Debug.Log($"🧩 Normalized values | Face: '{faceNorm}', Voice: '{voiceNorm}'");

        // STEP 3: Threshold calculation
        int endResult = 0;
        if (Enum.TryParse(faceNorm, true, out Emotions faceVal) &&
            Enum.TryParse(voiceNorm, true, out Emotions voiceVal))
        {
            double total = ((int)faceVal * FaceFactor) + ((int)voiceVal * VoiceFactor);
            endResult = total >= ThresholdFactor ? 1 : 0;
        }

        if (NextStepInt != null) NextStepInt.text = endResult.ToString();

        // STEP 4: Build LLM prompt using raw values
        string prompt =
            $"You are analyzing a VR training session where a trainee is interacting with an angry customer.\n" +
            $"Based on the following measurements, determine whether the aggression is likely to escalate and provide actionable feedback to the trainee.\n\n" +
            $"User Measurements:\n" +
            $"- Face Emotion: {faceRaw} (normalized: {faceNorm})\n" +
            $"- Voice Emotion: {voiceRaw} (normalized: {voiceNorm})\n" +
            $"- Hand Sign: {handRaw}\n" +
            $"- Finger Gesture: {fingerRaw}\n" +
            $"- Threshold Calculation Result: {endResult} (1 = aggression likely escalates, 0 = aggression under control)\n\n" +
            $"Provide constructive feedback in the following format:\n" +
            $"FACE: Comment on how the trainee's facial expressions impact escalation.\n" +
            $"VOICE: Comment on how the trainee's tone, volume, and emotion impact escalation.\n" +
            $"GESTURE: Comment on how the trainee's hand/finger gestures impact escalation.\n" +
            $"GENERAL: Provide an overall recommendation to reduce escalation risk.";

        Debug.Log("📌 Prompt sent to LLM:\n" + prompt);

        // STEP 5: Query LLM
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

        Debug.Log($"📝 LLM Feedback | Face={faceFeedback}, Voice={voiceFeedback}, Gesture={gestureFeedback}");

        // STEP 6: Update UI
        if (ResponseTMP != null) ResponseTMP.text = Feedback.Replace("\n", " ");
    }

    private string SafeText(string value)
    {
        return string.IsNullOrEmpty(value) ? "none" : value.ToLower().Trim();
    }

    private string NormalizeEmotion(string emotion)
    {
        if (string.IsNullOrEmpty(emotion)) return "Neutral";
        emotion = emotion.ToLower().Trim();

        // Map voice numeric codes to string
        switch (emotion)
        {
            case "01":
            case "02": return "Neutral"; // neutral / calm
            case "03": return "Happy";
            case "04": return "Sad";
            case "05": return "Angry";
            case "06": return "Fear";
            case "07": return "Disgust";
            case "08": return "Surprise"; // added
        }

        // Face string mapping
        if (emotion.Contains("fear")) return "Fear";
        if (emotion.Contains("angry")) return "Angry";
        if (emotion.Contains("disgust")) return "Disgust";
        if (emotion.Contains("sad")) return "Sad";
        if (emotion.Contains("happy")) return "Happy";
        if (emotion.Contains("surprise")) return "Surprise"; // ensure lowercase match
        if (emotion.Contains("neutral") || emotion.Contains("calm") || emotion.Contains("none")) return "Neutral";

        return "Neutral";
    }

    private IEnumerator QueryOllama(string prompt, Action<string, string, string> onComplete)
    {
        var requestData = new OllamaRequest { model = ollamaModel, prompt = prompt };
        string jsonData = JsonConvert.SerializeObject(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(ollamaUrl, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");

            yield return request.SendWebRequest();

            string responseText = request.downloadHandler.text;

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ Ollama request failed: {request.error}\nResponse: {responseText}");
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
        foreach (string line in rawJson.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                var chunk = JsonConvert.DeserializeObject<OllamaResponse>(line);
                if (!string.IsNullOrEmpty(chunk?.response))
                    sb.Append(chunk.response);
            }
            catch { sb.Append(line); }
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

    [Serializable] public class OllamaRequest { public string model; public string prompt; }
    [Serializable] private class OllamaResponse { public string response; }

    [Serializable]
    private class EmotionData
    {
        public string face_emotion;
        public string voice_emotion;
        public string hand_sign;
        public string finger_gesture;
    }
}