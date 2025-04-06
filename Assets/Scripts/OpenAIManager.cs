using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class OpenAIManager : MonoBehaviour
{
    [Header("OpenAI 설정")]
    public string apiKey = "YOUR_API_KEY";
    [SerializeField] protected string model = "gpt-4o-mini";

    // 싱글톤 인스턴스 (OpenAIActionManager가 상속받아 Instance에 할당됩니다)
    public static OpenAIManager Instance;

    // UI에 응답 텍스트를 전달하기 위한 이벤트 (예: UnityEvent<string>를 상속받은 클래스)
    public StringEvent onResponseOpenAI;

    [System.Serializable]
    protected class SimpleResponse
    {
        [System.Serializable]
        public class Choice
        {
            [System.Serializable]
            public class Message
            {
                public string content;
            }
            public Message message;
        }
        public Choice[] choices;
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
        string url = "https://api.openai.com/v1/chat/completions";

        string systemPrompt = "You are a helpful assistant. Answer questions concisely using only standard alphanumeric characters and basic punctuation.";
        string jsonPayload = @"{
            ""model"": """ + model + @""",
            ""messages"": [
                { ""role"": ""system"", ""content"": """ + EscapeJson(systemPrompt) + @""" },
                { ""role"": ""user"", ""content"": """ + EscapeJson(message) + @""" }
            ],
            ""store"": false
        }";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                SimpleResponse response = JsonUtility.FromJson<SimpleResponse>(request.downloadHandler.text);
                string responseMessage = response.choices[0].message.content;
                ProcessOpenAIResponse(responseMessage);
                Debug.Log("응답: " + responseMessage);
            }
            else
            {
                Debug.LogError("요청 실패: " + request.error);
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