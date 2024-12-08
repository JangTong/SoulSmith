using UnityEngine;

public class ForgeFire : MonoBehaviour
{
    private Renderer targetRenderer; // 오브젝트의 Renderer
    private Material fireMaterial;   // Instance로 생성된 Material
    private float currentIntensity;  // 현재 Emission Intensity
    private float targetIntensity;   // 목표 Emission Intensity

    public bool OnFire = false;          // Emission 활성화 여부
    public Color baseEmissionColor = Color.red; // 기본 Emission 색상
    public float maxIntensity = 5f;      // 최대 Emission Intensity
    public float transitionSpeed = 2f;   // Emission 전환 속도 (1초에 변화량)

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
        currentIntensity = 0f; // 초기 밝기를 0으로 설정
    }

    private void Update()
    {
        // Emission 상태에 따라 목표 Intensity 설정
        targetIntensity = OnFire ? maxIntensity : 0f;

        // 현재 Intensity를 목표 Intensity로 천천히 전환
        SmoothTransition();
    }

    private void EnableEmission()
    {
        if (fireMaterial != null)
        {
            fireMaterial.EnableKeyword("_EMISSION");
        }
    }

    private void SmoothTransition()
    {
        if (fireMaterial != null)
        {
            // 현재 Intensity를 목표 Intensity로 선형적으로 전환
            currentIntensity = Mathf.MoveTowards(currentIntensity, targetIntensity, Time.deltaTime * transitionSpeed);

            // Emission Color에 현재 Intensity 적용
            fireMaterial.SetColor("_EmissionColor", baseEmissionColor * currentIntensity);

            // DynamicGI 업데이트
            DynamicGI.SetEmissive(targetRenderer, baseEmissionColor * currentIntensity);
        }
    }
}
