using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class LipsyncSimpleController : MonoBehaviour
{
    public Animator anim;
    private AudioSource audioSource;
    public float minActivationTime = 0.1f;
    public float maxActivationTime = 0.3f;
    private float nextActivationTime = 0f;
    private float[] audioClipSamples = new float[1024];
    private float currentAverageVolume = 0f;
    private float sensitivity = 10000f;

    private void Start()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
    }

    private void Update()
    {
        AnalyzeSound();

        if (currentAverageVolume != 0)
        {
            if (Time.time >= nextActivationTime)
            {
                UpdatelipsyncBlendShape();
                nextActivationTime = Time.time + Random.Range(minActivationTime, maxActivationTime);
            }
        }
    }

    private void AnalyzeSound()
    {
        audioSource.GetSpectrumData(audioClipSamples, 0, FFTWindow.BlackmanHarris);
        currentAverageVolume = 0f;
        foreach (var sample in audioClipSamples)
        {
            currentAverageVolume += Mathf.Abs(sample);
        }
        currentAverageVolume /= 1024;
    }

    private void UpdatelipsyncBlendShape()
    {
        float weight = Mathf.Clamp(currentAverageVolume * sensitivity, 0, 1);

        anim.SetLayerWeight(anim.GetLayerIndex("Lipsync"), weight);
    }
}
