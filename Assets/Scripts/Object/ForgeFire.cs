using UnityEngine;

public class ForgeFire : MonoBehaviour
{
    private Renderer targetRenderer; // 오브젝트의 Renderer
    private Material fireMaterial;   // Instance로 생성된 Material
    private float currentIntensity;  // 현재 Emission Intensity
    private float targetIntensity;   // 목표 Emission Intensity

    public bool OnFire = false;          // Emission 활성화 여부
    public Color baseEmissionColor = Color.red; // 기본 Emission 색상
    public float minIntensity = 0.5f;   // 최소 Emission Intensity
    public float maxIntensity = 2f;    // 최대 Emission Intensity
    public float transitionSpeed = 2f; // Emission 전환 속도
    public float flickerSpeed = 0.1f;  // Flicker 속도 (시간 간격)

    private void Start()
    {
        // Renderer 가져오기
        targetRenderer = GetComponent<Renderer>();

        if (targetRenderer == null)
        {
            Debug.LogError("Renderer가 이 오브젝트에 없습니다.");
            enabled = false; // 스크립트 비활성화
            return;
        }

        // Material Instance 생성
        fireMaterial = targetRenderer.material; // 인스턴스화
        if (fireMaterial == null)
        {
            Debug.LogError("Renderer에 Material이 없습니다.");
            enabled = false; // 스크립트 비활성화
            return;
        }

        EnableEmission();
    }

    private void Update()
    {
        if (OnFire)
        {
            // Emission을 활성화 상태로 목표 Intensity를 최대값으로 설정
            targetIntensity = maxIntensity;
            FlickerEmission(); // Flicker 효과 적용
        }
        else
        {
            // Emission을 비활성화 상태로 목표 Intensity를 최소값으로 설정
            targetIntensity = 0;
        }

        SmoothTransition(); // Intensity 부드럽게 전환
    }

    private void EnableEmission()
    {
        if (fireMaterial != null)
        {
            fireMaterial.EnableKeyword("_EMISSION");
        }
    }

    private void DisableEmission()
    {
        if (fireMaterial != null)
        {
            // Emission 비활성화
            fireMaterial.SetColor("_EmissionColor", Color.black);
            DynamicGI.SetEmissive(targetRenderer, Color.black);
        }
    }

    private void FlickerEmission()
    {
        if (fireMaterial != null)
        {
            // Perlin Noise를 사용한 Intensity 변동
            float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0);
            float flickerIntensity = Mathf.Lerp(minIntensity, maxIntensity, noise);

            // Emission Color에 Flicker Intensity 적용
            fireMaterial.SetColor("_EmissionColor", baseEmissionColor * flickerIntensity);
            DynamicGI.SetEmissive(targetRenderer, baseEmissionColor * flickerIntensity);
        }
    }

    private void SmoothTransition()
    {
        if (fireMaterial != null)
        {
            // 현재 Intensity를 목표 Intensity로 부드럽게 전환
            currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * transitionSpeed);

            // Emission Color에 현재 Intensity 적용
            fireMaterial.SetColor("_EmissionColor", baseEmissionColor * currentIntensity);

            // DynamicGI 업데이트
            DynamicGI.SetEmissive(targetRenderer, baseEmissionColor * currentIntensity);
        }
    }
}
