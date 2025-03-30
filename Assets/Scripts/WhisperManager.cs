using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections;
using UnityEngine.Networking;
using System;

public class WhisperManager : MonoBehaviour
{
    public Toggle recordToggle;
    private AudioClip clip;
    private SetMicrophone setMicrophoneScript;
    private bool isRecording = false;
    private int duration = 300; // 최대 녹음 시간 (초)
    private string filename = "recordedAudio.wav";
    private string url = "https://api.openai.com/v1/audio/transcriptions";
    public string apiKey;
    public event Action<string> OnReceivedWhisper;
    public event Action OnStartRecording;
    public event Action OnStopRecording;

    [Serializable]
    public class WhisperResponse
    {
        public string text;
    }

    public static WhisperManager Instance;
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

    void Start()
    {
        // 씬에서 SetMicrophone 스크립트를 찾습니다.
        setMicrophoneScript = FindObjectOfType<SetMicrophone>();
        if (setMicrophoneScript == null)
        {
            Debug.LogError("SetMicrophone 스크립트를 찾을 수 없습니다.");
        }

        // Toggle에 리스너를 추가합니다.
        recordToggle.onValueChanged.AddListener(OnToggleValueChanged);
    }

    void OnToggleValueChanged(bool isOn)
    {
        if (isOn)
        {
            StartRecording();
        }
        else
        {
            StopRecording();
        }
    }

    void StartRecording()
    {
        if (setMicrophoneScript == null || string.IsNullOrEmpty(setMicrophoneScript.currentMicrophone))
        {
            Debug.LogError("선택된 마이크가 없습니다.");
            return;
        }

        if (!isRecording)
        {
            // 현재 마이크에서 녹음을 시작합니다.
            clip = Microphone.Start(setMicrophoneScript.currentMicrophone, false, duration, 44100);
            isRecording = true;
            Debug.Log($"녹음을 시작합니다: {setMicrophoneScript.currentMicrophone}");
            OnStartRecording?.Invoke();
        }
    }

    void StopRecording()
    {
        if (isRecording)
        {
            // 녹음을 중지합니다.
            Microphone.End(setMicrophoneScript.currentMicrophone);
            isRecording = false;
            Debug.Log("녹음을 중지했습니다.");

            // 오디오 클립을 .wav 파일로 저장합니다.
            SaveClip();
            OnStopRecording?.Invoke();
        }
    }

    void SaveClip()
    {
        if (clip != null)
        {
            var filepath = Path.Combine(Application.persistentDataPath, filename);
            SaveWav.Save(filepath, clip);
            Debug.Log($"녹음이 저장되었습니다: {filepath}");

            // Whisper API에 오디오 파일을 전송하여 텍스트를 받습니다.
            StartCoroutine(SendWhisperRequest(filepath));
        }
        else
        {
            Debug.LogError("저장할 오디오 클립이 없습니다.");
        }
    }

    IEnumerator SendWhisperRequest(string filepath)
    {
        // 오디오 파일을 바이트 배열로 읽어옵니다.
        byte[] audioData = File.ReadAllBytes(filepath);

        // Multipart form 데이터를 생성합니다.
        WWWForm form = new WWWForm();
        form.AddField("model", "whisper-1");
        form.AddBinaryData("file", audioData, "audio.wav", "audio/wav");

        // 요청을 생성합니다.
        UnityWebRequest request = UnityWebRequest.Post(url, form);
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        // 요청을 보내고 응답을 기다립니다.
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // 응답을 처리합니다.
            string responseText = request.downloadHandler.text;
            Debug.Log("Whisper API 응답: " + responseText);

            // JSON 파싱을 통해 텍스트를 추출합니다.
            try
            {
                var jsonResponse = JsonUtility.FromJson<WhisperResponse>(responseText);
                string transcribedText = jsonResponse.text;
                Debug.Log("인식된 텍스트: " + transcribedText);
                OnReceivedWhisper?.Invoke(transcribedText);
            }
            catch (Exception e)
            {
                Debug.LogError("JSON 파싱 오류: " + e.Message);
            }
        }
        else
        {
            Debug.LogError("Whisper API 요청 실패: " + request.error);
        }
    }


}
