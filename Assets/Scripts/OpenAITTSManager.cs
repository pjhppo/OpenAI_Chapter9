using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using System.Text;

public class OpenAITextManager : MonoBehaviour
{
    [Header("OpenAI Settings")]
    [SerializeField] private string openAIApiKey = "YOUR_API_KEY_HERE";
    [SerializeField] private string voiceName = "alloy";
    [SerializeField] private string testMessage = "Today is a wonderful day to build something people love!";
    [SerializeField] private string model = "tts-1";

    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        StartCoroutine(GetAndPlayAudio(testMessage));
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
        request.SetRequestHeader("Authorization", "Bearer " + openAIApiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            byte[] audioData = request.downloadHandler.data;

            yield return StartCoroutine(LoadAudioClipFromMp3(audioData));
        }
        else
        {
            Debug.LogError("TTS 요청 실패: " + request.error);
        }
    }

    private IEnumerator LoadAudioClipFromMp3(byte[] mp3Data)
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
                audioSource.Play();
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
}
