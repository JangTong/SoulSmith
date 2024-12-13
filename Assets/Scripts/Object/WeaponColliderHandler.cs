using UnityEngine;
using System.Collections;

public class WeaponColliderHandler : MonoBehaviour
{
    private WeaponBase parentWeaponBase; // 부모 WeaponBase 참조
    private Renderer weaponRenderer;     // 머티리얼 접근을 위한 Renderer
    private Material weaponMaterial;     // 머티리얼 참조
    private string colliderName;         // Collider 이름
    public static bool canDetect = false; // Trigger 감지 가능 여부
    private float currentEmissionIntensity = 2f; // 현재 Emission Intensity
    private readonly float maxEmissionIntensity = 10f; // 최대 Emission Intensity

    public void Initialize(WeaponBase weaponBase, string name)
    {
        parentWeaponBase = weaponBase;
        colliderName = name;

        // Renderer 및 Material 초기화
        weaponRenderer = GetComponent<Renderer>();
        if (weaponRenderer != null)
        {
            weaponMaterial = weaponRenderer.material;
        }  
    }

    private void OnTriggerEnter(Collider other)
    {
        // 감지가 가능한 상태에서만 Trigger 처리
        if (canDetect && parentWeaponBase != null && other.name == "HammerHead" && ItemPickup.Instance.isSwinging)
        {
            parentWeaponBase.IncrementCollisionCount(colliderName);

            // Emission Intensity 증가
            IncreaseEmissionIntensity(1f);

            // Trigger 감지를 잠시 비활성화
            StartCoroutine(DelayTriggerDetection(0.6f)); // 0.7초 딜레이
        }
    }

    /// Emission Intensity를 증가시키는 메서드
    private void IncreaseEmissionIntensity(float increment)
    {
        if (weaponMaterial != null)
        {
            // Emission Intensity 증가
            currentEmissionIntensity = Mathf.Clamp(currentEmissionIntensity + increment, 0, maxEmissionIntensity);
            Debug.Log($"Current Emission Intensity: {currentEmissionIntensity}");

            // 기존 EmissionColor 가져오기
            Color baseEmissionColor = weaponMaterial.GetColor("_EmissionColor");
            
            // HDR Intensity 조정
            Color newEmissionColor = baseEmissionColor * currentEmissionIntensity;
            
            // Emission Color 적용
            weaponMaterial.SetColor("_EmissionColor", newEmissionColor);

            // Emission이 활성화된 상태로 적용
            DynamicGI.SetEmissive(weaponRenderer, newEmissionColor);
        }
        else
        {
            Debug.LogError("Weapon Material is null!");
        }
    }

    /// Trigger 감지 비활성화를 위한 딜레이 코루틴
    private IEnumerator DelayTriggerDetection(float delayTime)
    {
        canDetect = false; // Trigger 감지 비활성화
        yield return new WaitForSeconds(delayTime); // 딜레이
        canDetect = true; // Trigger 감지 재활성화
    }
}


