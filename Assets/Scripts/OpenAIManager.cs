using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class OpenAIManager : MonoBehaviour
{
    [Header("OpenAI 설정")]
    public string apiKey = "YOUR_API_KEY";
    public string geminiApiKey = "YOUR_API_KEY";

    public static OpenAIManager Instance;

    public StringEvent onResponseOpenAI;

    [System.Serializable]
    public class Part
    {
        public string text;
    }

    [System.Serializable]
    public class Content
    {
        public Part[] parts;
        public string role;
    }

    [System.Serializable]
    public class Candidate
    {
        public Content content;
        public string finishReason;
        public float avgLogprobs;
    }

    [System.Serializable]
    public class ResponseData
    {
        public Candidate[] candidates;
    }

    protected virtual void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    protected virtual void Start()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.onInputFieldSubmit.AddListener(OnInputFieldCompleted);
        }
        else
        {
            Debug.LogError("UIManager 인스턴스가 없습니다.");
        }

        if (WhisperManager.Instance != null)
        {
            WhisperManager.Instance.OnReceivedWhisper += OnReceivedWhisperMessage;
        }
        else
        {
            Debug.LogError("WhisperManager 인스턴스가 없습니다.");
        }
    }

    protected virtual void OnReceivedWhisperMessage(string message)
    {
        StartCoroutine(SendRequestToOpenAI(message));
    }

    protected virtual void OnInputFieldCompleted(string message)
    {
        StartCoroutine(SendRequestToOpenAI(message));
    }

    protected virtual IEnumerator SendRequestToOpenAI(string message)
    {
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={geminiApiKey}";

        string jsonPayload = "{\"contents\": [{\"parts\": [{\"text\": \"" + message + "\"}]}]}";

        using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error: " + request.error);
            }
            else
            {
                string jsonResponse = request.downloadHandler.text;
                ResponseData responseData = JsonUtility.FromJson<ResponseData>(jsonResponse);

                if (responseData != null && responseData.candidates != null && responseData.candidates.Length > 0)
                {
                    Candidate candidate = responseData.candidates[0];
                    if (candidate.content != null && candidate.content.parts != null && candidate.content.parts.Length > 0)
                    {
                        string text = candidate.content.parts[0].text.Trim();
                        ProcessOpenAIResponse(text);
                        Debug.Log("Response text: " + text);
                    }
                    else
                    {
                        Debug.Log("응답 내 content parts를 찾을 수 없습니다.");
                    }
                }
                else
                {
                    Debug.Log("응답 내 candidates를 찾을 수 없습니다.");
                }
            }
        }
    }

    protected virtual void ProcessOpenAIResponse(string responseMessage)
    {
        onResponseOpenAI.Invoke(responseMessage);
    }

    protected string EscapeJson(string text)
    {
        if (string.IsNullOrEmpty(text))
            return "";
        return text.Replace("\"", "\\\"").Replace("\n", "\\n");
    }
}
