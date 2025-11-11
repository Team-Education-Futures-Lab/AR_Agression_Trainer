using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Newtonsoft.Json;

public class FeedbackHandler : MonoBehaviour
{
    // UI Text fields
    public TextMeshProUGUI FaceTMP;
    public TextMeshProUGUI VoiceTMP;
    public TextMeshProUGUI GestureTMP;
    public TextMeshProUGUI ResponseTMP;
    public TextMeshProUGUI NextStepInt;

    // Feedback data
    private int UserID = 1;
    private int Level = 2;
    private string Feedback = "";

    // Flask server URL
    [SerializeField] private string serverUrl = "http://127.0.0.1:5000/FeedbackPoster";

    // Ollama server URL
    [SerializeField] private string ollamaUrl = "http://127.0.0.1:11434/api/generate";
    [SerializeField] private string ollamaModel = "llama3";

    // Interval in seconds (2 minutes)
    private float feedbackInterval = 120f;

    void Start()
    {
        StartCoroutine(PeriodicFeedback());
    }

    // ==========================
    // PERIODIC FEEDBACK
    // ==========================
    private IEnumerator PeriodicFeedback()
    {
        while (true)
        {
            GeneratingFeedback();
            yield return new WaitForSeconds(feedbackInterval);
        }
    }

    // ==========================
    // GENERATE FEEDBACK
    // ==========================
    public void GeneratingFeedback()
    {
        string faceResult = GetAfterColon(FaceTMP.text);
        string voiceResult = GetAfterColon(VoiceTMP.text);
        string gestureResult = GetAfterColon(GestureTMP.text);

        Debug.Log($"Generating feedback:\nFace: {faceResult}\nVoice: {voiceResult}\nGesture: {gestureResult}");

        string prompt =
            $"Generate constructive feedback for a VR training session.\n" +
            $"Provide three parts: FACE feedback, VOICE feedback, and GESTURE feedback.\n" +
            $"Using the following results:\n" +
            $"FACE expression: {faceResult}\n" +
            $"VOICE tone: {voiceResult}\n" +
            $"GESTURE movement: {gestureResult}\n" +
            $"Format your answer as:\nFACE: ...\nVOICE: ...\nGESTURE: ...";

        StartCoroutine(QueryOllama(prompt));
    }

    private string GetAfterColon(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        string[] parts = input.Split(':');
        return parts.Length > 1 ? parts[1].Trim() : parts[0].Trim();
    }

    // ==========================
    // OLLAMA QUERY
    // ==========================
    private IEnumerator QueryOllama(string prompt)
    {
        var requestData = new OllamaRequest
        {
            model = ollamaModel,
            prompt = prompt
        };

        string jsonData = JsonConvert.SerializeObject(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(ollamaUrl, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json"); // important for Ollama API

            yield return request.SendWebRequest();

            string responseText = request.downloadHandler.text;

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ Ollama request failed: {request.error}\nResponse: {responseText}");
            }
            else
            {
                // Flatten all chunked responses into one single sentence
                string feedbackText = FlattenOllamaStreamingResponse(responseText);

                FaceTMP.text = ExtractSection(feedbackText, "FACE");
                VoiceTMP.text = ExtractSection(feedbackText, "VOICE");
                GestureTMP.text = ExtractSection(feedbackText, "GESTURE");
                ResponseTMP.text = feedbackText.Replace("\n", " "); // single sentence in ResponseTMP

                Feedback = feedbackText;

                Debug.Log("✅ Ollama feedback generated:\n" + feedbackText);
            }
        }
    }

    // Flatten streaming JSON lines from Ollama into one string
    private string FlattenOllamaStreamingResponse(string rawJson)
    {
        if (string.IsNullOrEmpty(rawJson)) return "";

        // Split lines, parse each JSON chunk
        StringBuilder fullResponse = new StringBuilder();
        string[] lines = rawJson.Split('\n');
        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var chunk = JsonConvert.DeserializeObject<OllamaResponse>(line);
                if (!string.IsNullOrEmpty(chunk?.response))
                {
                    fullResponse.Append(chunk.response);
                }
            }
            catch
            {
                fullResponse.Append(line); // fallback
            }
        }

        // Clean multiple spaces and newlines
        string result = fullResponse.ToString().Replace("\n", " ").Replace("\r", " ").Trim();
        return System.Text.RegularExpressions.Regex.Replace(result, @"\s+", " ");
    }

    [System.Serializable]
    public class OllamaRequest
    {
        public string model;
        public string prompt;
    }

    private string ParseOllamaResponse(string rawJson)
    {
        try
        {
            var resp = JsonConvert.DeserializeObject<OllamaResponse>(rawJson);
            return resp?.response ?? rawJson ?? "";
        }
        catch
        {
            Debug.LogWarning("Failed to parse Ollama response, returning raw text.");
            return rawJson ?? "";
        }
    }

    [System.Serializable]
    private class OllamaResponse
    {
        public string response;
    }

    private string ExtractSection(string text, string section)
    {
        if (string.IsNullOrEmpty(text)) return "";
        string[] lines = text.Split('\n');
        foreach (string line in lines)
        {
            if (line.StartsWith(section + ":"))
            {
                string[] parts = line.Split(new[] { ':' }, 2);
                return parts.Length > 1 ? parts[1].Trim() : "";
            }
        }
        return "";
    }

    // ==========================
    // POST FEEDBACK TO FLASK
    // ==========================
    public void SubmitFeedback(string userFeedback)
    {
        StartCoroutine(PostToServer("Feedback push", userFeedback));
    }

    private IEnumerator PostToServer(string requestType, string userFeedback)
    {
        WWWForm form = new WWWForm();
        form.AddField("type", requestType);
        form.AddField("User_ID", UserID);
        form.AddField("Level", Level);
        form.AddField("Feedback", userFeedback);

        using (UnityWebRequest request = UnityWebRequest.Post(serverUrl, form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"✅ Server Response: {request.downloadHandler.text}");
                ResponseTMP.text = request.downloadHandler.text;
            } else
            {
                Debug.LogError($"❌ Server Error: {request.error}");   
            }
        }
    }
}