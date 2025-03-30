using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SetMicrophone : MonoBehaviour
{
    public Dropdown dropdown;
    public string currentMicrophone;

    void Start()
    {
        SetDeviceMicrophone();
    }

    void SetDeviceMicrophone()
    {
        // 디바이스에 연결된 마이크 목록을 가져옵니다.
        string[] microphones = Microphone.devices;

        // 기존 Dropdown 옵션을 초기화합니다.
        dropdown.ClearOptions();

        // 마이크 목록을 Dropdown 옵션 형식으로 변환합니다.
        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();

        foreach (string device in microphones)
        {
            options.Add(new Dropdown.OptionData(device));
        }

        // 변환된 옵션을 Dropdown에 추가합니다.
        dropdown.AddOptions(options);

        // currentMicrophone에 첫 번째 마이크를 저장합니다.
        if (microphones.Length > 0)
        {
            currentMicrophone = microphones[0];
            dropdown.value = 0; // 첫 번째 옵션으로 설정
        }
        else
        {
            currentMicrophone = null;
        }

        // Dropdown의 값이 변경될 때마다 호출되는 이벤트를 추가합니다.
        dropdown.onValueChanged.AddListener(delegate { DropdownValueChanged(dropdown); });
    }

    void DropdownValueChanged(Dropdown change)
    {
        // 현재 선택된 옵션의 텍스트를 currentMicrophone에 저장합니다.
        currentMicrophone = change.options[change.value].text;
        Debug.Log(currentMicrophone);
    }
}
