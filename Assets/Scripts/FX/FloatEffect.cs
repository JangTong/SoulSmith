using UnityEngine;
using DG.Tweening;

public class FloatEffect : MonoBehaviour
{
    [Header("Floating Movement")]
    public float floatHeight = 0.5f;
    public float floatDuration = 1.5f;

    [Header("Wobble Noise")]
    public bool useNoiseWobble = true;
    public float noiseIntensity = 0.1f;
    public float noiseSpeed = 1f;

    [Header("Optional Rotation")]
    public bool rotate = true;
    public float rotationSpeed = 45f;

    private Vector3 initialPos;

    void Start()
    {
        initialPos = transform.localPosition;

        // Y축 부유 애니메이션
        transform.DOLocalMoveY(initialPos.y + floatHeight, floatDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        // 회전 애니메이션
        if (rotate)
        {
            transform.DORotate(new Vector3(0f, 360f, 0f), 360f / rotationSpeed, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart);
        }
    }

    void Update()
    {
        if (!useNoiseWobble) return;

        float time = Time.time * noiseSpeed;
        float offsetX = (Mathf.PerlinNoise(time, 0f) - 0.5f) * 2f * noiseIntensity;
        float offsetZ = (Mathf.PerlinNoise(0f, time) - 0.5f) * 2f * noiseIntensity;

        // Y축은 DOTween이 처리하므로 그대로 유지
        float yOffset = transform.localPosition.y - initialPos.y;
        Vector3 target = initialPos + new Vector3(offsetX, yOffset, offsetZ);

        transform.localPosition = Vector3.Lerp(transform.localPosition, target, Time.deltaTime * 5f);
    }
}