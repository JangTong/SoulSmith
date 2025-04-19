using UnityEngine;
using DG.Tweening;

public class LightFlicker : MonoBehaviour
{
    public Light targetLight;
    public float intensityRange = 0.3f;     // 기본값에서 ± 범위
    public float flickerDurationMin = 0.1f; // 깜빡임 최소 간격
    public float flickerDurationMax = 0.3f; // 깜빡임 최대 간격

    private float baseIntensity;

    private void Start()
    {
        if (targetLight == null)
            targetLight = GetComponent<Light>();

        baseIntensity = targetLight.intensity;

        StartFlickerLoop();
    }

    private void StartFlickerLoop()
    {
        float randomOffset = Random.Range(-intensityRange, intensityRange);
        float newIntensity = baseIntensity + randomOffset;
        float duration = Random.Range(flickerDurationMin, flickerDurationMax);

        targetLight.DOIntensity(newIntensity, duration)
            .SetEase(Ease.InOutSine)
            .OnComplete(StartFlickerLoop); // 재귀 호출로 무한 반복
    }
}
