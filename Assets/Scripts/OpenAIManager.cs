using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class OpenAIManager : MonoBehaviour
{
    [Header("OpenAI 설정")]
    public string apiKey = "YOUR_API_KEY";
    public string geminiApiKey = "YOUR_API_KEY";


    // 싱글톤 인스턴스 (OpenAIActionManager가 상속받아 Instance에 할당됩니다)
    public static OpenAIManager Instance;

    // UI에 응답 텍스트를 전달하기 위한 이벤트 (예: UnityEvent<string>를 상속받은 클래스)
    public StringEvent onResponseOpenAI;

    // JSON 응답을 파싱하기 위한 클래스들
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
        // usageMetadata 및 modelVersion은 필요에 따라 추가할 수 있습니다.
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

    // 기본 시스템 프롬프트만 사용하는 OpenAI API 호출
    protected virtual IEnumerator SendRequestToOpenAI(string message)
    {
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={geminiApiKey}";

        // JSON 페이로드에 prompt는 고정 문자열, text는 전달된 message 값을 사용합니다.
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
                // 응답 JSON 파싱
                string jsonResponse = request.downloadHandler.text;
                ResponseData responseData = JsonUtility.FromJson<ResponseData>(jsonResponse);

                if (responseData != null && responseData.candidates != null && responseData.candidates.Length > 0)
                {
                    Candidate candidate = responseData.candidates[0];
                    if (candidate.content != null && candidate.content.parts != null && candidate.content.parts.Length > 0)
                    {
                        // "안녕하세요!\n"에서 Trim()을 사용하여 공백 및 개행문자 제거
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

    // 응답 처리 (하위 클래스에서 재정의 가능)
    protected virtual void ProcessOpenAIResponse(string responseMessage)
    {
        onResponseOpenAI.Invoke(responseMessage);
    }

    // JSON 문자열 내의 특수문자(예: "와 줄바꿈)를 이스케이프 처리하는 헬퍼 메서드
    protected string EscapeJson(string text)
    {
        if (string.IsNullOrEmpty(text))
            return "";
        return text.Replace("\"", "\\\"").Replace("\n", "\\n");
    }
}
