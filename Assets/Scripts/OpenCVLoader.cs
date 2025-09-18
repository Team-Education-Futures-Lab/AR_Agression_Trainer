using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class OpenCVLoader : MonoBehaviour
{
    public TextMeshProUGUI emotionTMP; // Assign in Inspector
    string apiUrl = "http://127.0.0.1:5000/get_emotion";

    void Start()
    {
        StartCoroutine(GetEmotionLoop());
    }

    IEnumerator GetEmotionLoop()
    {
        while (true)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(apiUrl))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    string json = www.downloadHandler.text;
                    EmotionData data = JsonUtility.FromJson<EmotionData>(json);

                    Debug.Log("🎭 Emotion: " + data.emotion);

                    if (emotionTMP != null)
                        emotionTMP.text = "Emotion: " + data.emotion;

                    /* if (data.emotion == "happy")
                        GetComponent<Animator>().SetTrigger("Smile");
                    */
                }
                else
                {
                    Debug.LogError("❌ API Error: " + www.error);
                }
            }

            yield return new WaitForSeconds(1f); // poll every 0.5 sec
        }
    }
}

[System.Serializable]
public class EmotionData
{
    public string emotion;
}