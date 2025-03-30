using UnityEngine;

public class UniversalLipsyncController : MonoBehaviour
{
    public SkinnedMeshRenderer faceMesh;
    public AudioSource audioSource;
    public Vector2 randomPeriodRange = new Vector2(0.5f, 1.5f);

    // ARKit 블렌드셰이프 인덱스
    private int jawOpen, mouthSmileL, mouthSmileR, mouthFrownL, mouthFrownR;
    private int mouthFunnel, mouthPucker, mouthDimpleL, mouthDimpleR;
    private int squintL, squintR, mouthLeft, mouthRight;

    private float timer = 0;
    private float currentDuration = 0;
    private string currentVowel = "";
    private float[] targetWeights;
    private float[] currentWeights;

    private void Start()
    {
        // faceMesh의 blendShape 개수만큼 배열 크기 동적 할당
        int blendShapeCount = faceMesh.sharedMesh.blendShapeCount;
        targetWeights = new float[blendShapeCount];
        currentWeights = new float[blendShapeCount];

        InitializeBlendShapeIndices();
        SetNewRandomVowel();
    }

    private void InitializeBlendShapeIndices()
    {
        jawOpen = GetIndex("b1.jawOpen1");
        mouthSmileL = GetIndex("b1.mouthSmileLeft");
        mouthSmileR = GetIndex("b1.mouthSmileRight");
        mouthFrownL = GetIndex("b1.mouthFrownLeft");
        mouthFrownR = GetIndex("b1.mouthFrownRight");
        mouthFunnel = GetIndex("b1.mouthFunnel");
        mouthPucker = GetIndex("b1.mouthPucker");
        mouthDimpleL = GetIndex("b1.mouthDimpleLeft");
        mouthDimpleR = GetIndex("b1.mouthDimpleRight");
        squintL = GetIndex("b1.cheekSquintLeft");
        squintR = GetIndex("b1.cheekSquintRight");
        mouthLeft = GetIndex("b1.mouthLeft");
        mouthRight = GetIndex("b1.mouthRight");
    }

    private int GetIndex(string name)
    {
        return faceMesh.sharedMesh.GetBlendShapeIndex(name);
    }

    private void Update()
    {
        UpdateAudioDetection();
        HandleAnimationState();
        ApplyBlendShapeWeights();
    }

    private void UpdateAudioDetection()
    {
        // 실제 음성 입력 감지 로직 구현 (기존 AnalyzeSound() 활용)
    }

    private void HandleAnimationState()
    {
        if (audioSource.isPlaying && audioSource.volume > 0.1f)
        {
            timer += Time.deltaTime;
            
            if (timer >= currentDuration)
            {
                SetNewRandomVowel();
                timer = 0;
                currentDuration = Random.Range(randomPeriodRange.x, randomPeriodRange.y);
            }

            SmoothWeightTransition();
        }
        else
        {
            ResetAllWeights();
        }
    }

    private void SetNewRandomVowel()
    {
        currentVowel = GetRandomVowel();
        ConfigureVowelShape(currentVowel);
    }

    private string GetRandomVowel()
    {
        string[] vowels = { "A", "E", "I", "O", "U" };
        return vowels[Random.Range(0, vowels.Length)];
    }

    private void ConfigureVowelShape(string vowel)
    {
        ResetTargetWeights();

        switch (vowel)
        {
            case "A": // 입 크게 열기
                targetWeights[jawOpen] = 100f;
                targetWeights[mouthDimpleL] = 30f;
                targetWeights[mouthDimpleR] = 30f;
                break;

            case "E": // 옆으로 넓게 벌림
                targetWeights[mouthSmileL] = 80f;
                targetWeights[mouthSmileR] = 80f;
                targetWeights[mouthLeft] = 20f;
                targetWeights[mouthRight] = 20f;
                break;

            case "I": // 좁게 벌리고 미소
                targetWeights[mouthSmileL] = 60f;
                targetWeights[mouthSmileR] = 60f;
                targetWeights[mouthFunnel] = 40f;
                targetWeights[squintL] = 20f;
                targetWeights[squintR] = 20f;
                break;

            case "O": // 둥글게 오므림
                targetWeights[mouthFunnel] = 100f;
                targetWeights[mouthPucker] = 70f;
                targetWeights[mouthDimpleL] = 40f;
                targetWeights[mouthDimpleR] = 40f;
                break;

            case "U": // 앞으로 내민 입술
                targetWeights[mouthPucker] = 100f;
                targetWeights[mouthFrownL] = 30f;
                targetWeights[mouthFrownR] = 30f;
                targetWeights[jawOpen] = 20f;
                break;
        }
    }

    private void SmoothWeightTransition()
    {
        for (int i = 0; i < currentWeights.Length; i++)
        {
            currentWeights[i] = Mathf.Lerp(currentWeights[i], targetWeights[i], Time.deltaTime * 5f);
        }
    }

    private void ApplyBlendShapeWeights()
    {
        faceMesh.SetBlendShapeWeight(jawOpen, currentWeights[jawOpen]);
        faceMesh.SetBlendShapeWeight(mouthSmileL, currentWeights[mouthSmileL]);
        faceMesh.SetBlendShapeWeight(mouthSmileR, currentWeights[mouthSmileR]);
        faceMesh.SetBlendShapeWeight(mouthFrownL, currentWeights[mouthFrownL]);
        faceMesh.SetBlendShapeWeight(mouthFrownR, currentWeights[mouthFrownR]);
        faceMesh.SetBlendShapeWeight(mouthFunnel, currentWeights[mouthFunnel]);
        faceMesh.SetBlendShapeWeight(mouthPucker, currentWeights[mouthPucker]);
        faceMesh.SetBlendShapeWeight(mouthDimpleL, currentWeights[mouthDimpleL]);
        faceMesh.SetBlendShapeWeight(mouthDimpleR, currentWeights[mouthDimpleR]);
        faceMesh.SetBlendShapeWeight(squintL, currentWeights[squintL]);
        faceMesh.SetBlendShapeWeight(squintR, currentWeights[squintR]);
        faceMesh.SetBlendShapeWeight(mouthLeft, currentWeights[mouthLeft]);
        faceMesh.SetBlendShapeWeight(mouthRight, currentWeights[mouthRight]);
    }

    private void ResetTargetWeights()
    {
        for (int i = 0; i < targetWeights.Length; i++)
        {
            targetWeights[i] = 0f;
        }
    }

    private void ResetAllWeights()
    {
        ResetTargetWeights();
        for (int i = 0; i < currentWeights.Length; i++)
        {
            currentWeights[i] = 0f;
        }
        ApplyBlendShapeWeights();
    }
}
