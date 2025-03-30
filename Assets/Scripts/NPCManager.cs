using UnityEngine;

public class NPCManager : MonoBehaviour
{
    public static NPCManager Instance; // 싱글톤 인스턴스
    public Animator anim; // NPC의 애니메이터

    public GameObject balloon; //말풍선 프리팹

    private bool isListening = false;

    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시에도 객체 유지
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // UInputField 이벤트 리스너 설정 - 입력이 발생했을 때 실행되는 이벤트
        UIManager.Instance.inputField.onValueChanged.AddListener(OnInputFieldChanged);

        // InputField 이벤트 리스너 설정 - 입력 완료 시 실행되는 이벤트
        UIManager.Instance.inputField.onSubmit.AddListener(OnInputFieldSubmit);

        OpenAITTS.Instance.onResponseTTS.AddListener(StartTalking);

        OpenAITTS.Instance.OnStopAudio.AddListener(StopTalking);

        WhisperManager.Instance.OnStartRecording += StartListening;
        WhisperManager.Instance.OnStopRecording += StopListening;
    }

    private void StartListening()
    {
        anim.SetTrigger("listen");
    }

    private void StopListening()
    {
        anim.SetTrigger("idle");
    }

    // InputField에서 입력이 발생했을 때 호출되는 메서드
    private void OnInputFieldChanged(string inputText)
    {
        if (!isListening)
        {
            // 애니메이터에 "listening" 트리거 설정
            anim.SetTrigger("listen");
            isListening = true; // 플래그 설정
            Debug.Log("Animator Triggered: listen");
        }
    }

    // InputField에서 입력이 완료되었을 때 호출되는 메서드    
    private void OnInputFieldSubmit(string inputText)
    {
        balloon.SetActive(true);
        isListening = false;
    }

    private void StartTalking(string message)
    {
        UIManager.Instance.resultText.gameObject.SetActive(true);
        balloon.SetActive(false);
        anim.SetTrigger("talk");
    }

    private void StopTalking()
    {
        anim.SetTrigger("idle");
        UIManager.Instance.resultText.gameObject.SetActive(false);
    }

}
