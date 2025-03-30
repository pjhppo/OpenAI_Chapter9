using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine.Events;

public class OpenAITTS : MonoBehaviour
{
    [Header("OpenAI 설정")]
    private string apiKey;
    [SerializeField] private string voiceName = "alloy";
    [SerializeField] private string model = "tts-1";

    [Header("오디오 설정")]
    private AudioSource audioSource;

    public static OpenAITTS Instance;

    // 이벤트 정의
    public StringEvent onResponseTTS;  // TTS 재생 시작 이벤트
    public UnityEvent OnStopAudio;    // TTS 재생 종료 이벤트

    // 싱글톤 선언
    private void Awake()
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

    private void Start()
    {
        // 싱글톤 인스턴스를 통해 이벤트 구독
        if (OpenAIManager.Instance != null)
        {
            apiKey = OpenAIManager.Instance.apiKey;
            OpenAIManager.Instance.onResponseOpenAI.AddListener(OnResponseOpenAI);
        }
        else
        {
            Debug.LogError("UIManager 인스턴스가 없습니다.");
        }
        audioSource = gameObject.GetComponent<AudioSource>();
    }

    private void OnResponseOpenAI(string message)
    {
        StartCoroutine(GetAndPlayAudio(message));
    }

    private IEnumerator GetAndPlayAudio(string textToSynthesize)
    {
        string url = "https://api.openai.com/v1/audio/speech";

        string jsonPayload = $@"{{
            ""model"": ""{model}"",
            ""input"": ""{textToSynthesize}"",
            ""voice"": ""{voiceName}""
        }}";

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonPayload));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            byte[] audioData = request.downloadHandler.data;
            yield return StartCoroutine(LoadAudioClipFromMp3(audioData, textToSynthesize));
        }
        else
        {
            Debug.LogError("TTS 요청 실패: " + request.error);
        }
    }

    private IEnumerator LoadAudioClipFromMp3(byte[] mp3Data, string message)
    {
        string tempFilePath = Path.Combine(Application.persistentDataPath, "temp_audio.mp3");
        File.WriteAllBytes(tempFilePath, mp3Data);

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + tempFilePath, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                audioSource.clip = clip;

                // 재생 시작 시 이벤트 호출
                audioSource.Play();
                onResponseTTS.Invoke(message);
                StartCoroutine(WaitForAudioEnd());
            }
            else
            {
                Debug.LogError("MP3 로드 실패: " + www.error);
            }
        }

        if (File.Exists(tempFilePath))
        {
            File.Delete(tempFilePath);
        }
    }

    // 오디오 재생 종료 감지 코루틴
    private IEnumerator WaitForAudioEnd()
    {
        // 오디오가 재생 중인 동안 대기
        while (audioSource.isPlaying)
        {
            yield return null;
        }

        // 재생이 멈추면 이벤트 호출
        OnStopAudio.Invoke();
    }
}