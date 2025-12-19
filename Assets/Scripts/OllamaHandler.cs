using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;


public class OllamaHandler : MonoBehaviour
{
    [Header("Server Settings")]
    [SerializeField] private string ollamaUrl = "http://127.0.0.1:11434/api/generate";
    [SerializeField] private string ollamaModel = "llama3";

    public IEnumerator StartOllama(
        string faceRaw,
        string faceNorm,
        string voiceRaw,
        string voiceNorm,
        string handRaw,
        string fingerRaw,
        string decision,
        Action<string, string, string, string> onComplete // full feedback + parts
    )
    {
        string promptTemplate = File.ReadAllText("Assets/Prompts/ARTrainingAnalysis.prompt");

        string prompt = promptTemplate
            .Replace("{faceRaw}", faceRaw)
            .Replace("{faceNorm}", faceNorm)
            .Replace("{voiceRaw}", voiceRaw)
            .Replace("{voiceNorm}", voiceNorm)
            .Replace("{handRaw}", handRaw)
            .Replace("{fingerRaw}", fingerRaw)
            .Replace("{endResult}", decision);

        yield return StartCoroutine(QueryOllama(prompt, onComplete));
    }

    private IEnumerator QueryOllama(
        string prompt,
        Action<string, string, string, string> onComplete
    )
    {
        var requestData = new OllamaRequest
        {
            model = ollamaModel,
            prompt = prompt
        };

        string jsonData = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(ollamaUrl, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ Ollama failed: {request.error}");
                onComplete?.Invoke("", "", "", "");
                yield break;
            }

            string feedback = FlattenOllamaStreamingResponse(request.downloadHandler.text);

            string face = ExtractSection(feedback, "FACE");
            string voice = ExtractSection(feedback, "VOICE");
            string gesture = ExtractSection(feedback, "GESTURE");

            onComplete?.Invoke(feedback, face, voice, gesture);
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
                return line.Substring(section.Length + 1).Trim();
            }
        }

        return "";
    }

    [Serializable]
    private class OllamaRequest
    {
        public string model;
        public string prompt;
    }
}