using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class GrindingWheel : MonoBehaviour
{
    private const string LOG_PREFIX = "[GrindingWheel]";
    
    [Header("연마 설정")]
    public Transform grindingPosition;  // 무기가 고정될 위치
    public float interactionRange = 3f;
    public ParticleSystem grindingEffect;
    
    [Header("파티클 효과 설정")]
    [SerializeField] private float perfectParticleMultiplier = 3f;  // Perfect일 때 파티클 강도
    [SerializeField] private float goodParticleMultiplier = 2f;     // Good일 때 파티클 강도  
    [SerializeField] private float particleDuration = 0.5f;         // 파티클 강화 지속 시간
    [SerializeField] private float burstInterval = 0.2f;            // Burst 발생 간격 (초)
    
    [Header("카메라 설정")]
    public Transform cameraGrindingViewpoint;  // 연마 시 카메라 위치
    public float cameraMoveDuration = 0.5f;    // 카메라 전환 시간
    
    [Header("판정 기준")]
    [SerializeField] private float perfectRange = 0.04f;      // 4%
    [SerializeField] private float goodRange = 0.20f;        // 20%
    
    [Header("Smooth 증가량")]
    [SerializeField] private float perfectIncrease = 0.07f;
    [SerializeField] private float goodIncrease = 0.04f;
    [SerializeField] private float failIncrease = 0.00f;
    
    [Header("난이도 조절 (공격력 기반)")]
    [SerializeField] private float baseCursorSpeed = 1f;      // 기본 속도
    [SerializeField] private float speedMultiplier = 0.1f;   // 공격력당 속도 증가
    [SerializeField] private float maxSpeedMultiplier = 3f;   // 최대 속도 제한
    
    [Header("회전 설정")]
    public Transform wheelTransform;         // 회전할 바퀴 Transform (없으면 자기 자신)
    public float rotationSpeed = 100f;       // 회전 속도 (도/초)
    public Vector3 rotationAxis = Vector3.right; // 회전축 (기본: X축)
    [SerializeField] private float currentRotationSpeed = 0f; // 현재 회전 속도 (보간용)
    
    // 파티클 관련 - 원본 설정값들을 저장
    private float originalParticleBurstCount = 50f;
    private float originalEmissionRate = 10f;
    
    private GameObject weaponOnGrinder;
    private bool isGrinding = false;
    private bool isFixingWeapon = false; // 무기 고정 중 플래그
    public GrindingMiniGame miniGame;
    
    // UI 상태 관리
    private bool wasDetectorActive = true;
    
    // 접근자 프로퍼티들
    public float PerfectRange => perfectRange;
    public float GoodRange => goodRange;
    public float PerfectIncrease => perfectIncrease;
    public float GoodIncrease => goodIncrease;
    public float FailIncrease => failIncrease;

    private void Awake()
    {
        miniGame = new GrindingMiniGame(this);
        
        // grindingPosition이 설정되지 않았으면 자동으로 찾기
        if (grindingPosition == null)
        {
            grindingPosition = transform.Find("Grinding_Wheel");
            if (grindingPosition == null)
            {
                Debug.LogWarning($"{LOG_PREFIX} grindingPosition을 찾을 수 없습니다. 수동으로 설정해주세요.");
            }
        }
        
        // wheelTransform이 설정되지 않았으면 자기 자신을 사용
        if (wheelTransform == null)
        {
            wheelTransform = transform;
            Debug.Log($"{LOG_PREFIX} wheelTransform이 설정되지 않아 자기 자신을 사용합니다.");
        }
        
        // 원본 파티클 설정 저장
        if (grindingEffect != null)
        {
            var emission = grindingEffect.emission;
            var main = grindingEffect.main;
            
            if (emission.burstCount > 0)
            {
                var burst = emission.GetBurst(0);
                originalParticleBurstCount = burst.count.constant;
            }
            
            originalEmissionRate = emission.rateOverTime.constant;
            
            // **Burst 간격을 Inspector에서 설정 가능하게**
            if (emission.burstCount > 0)
            {
                var burst = emission.GetBurst(0);
                burst.repeatInterval = burstInterval; // Inspector에서 조절 가능한 간격
                emission.SetBurst(0, burst);
                Debug.Log($"{LOG_PREFIX} Burst 간격을 {burstInterval}초로 설정");
            }
            
            Debug.Log($"{LOG_PREFIX} 원본 파티클 설정 저장 완료");
        }
        
        Debug.Log($"{LOG_PREFIX} 초기화 완료");
    }

    private void Update()
    {
        // **회전 속도 부드러운 보간**
        if (isGrinding)
        {
            currentRotationSpeed = Mathf.Lerp(currentRotationSpeed, rotationSpeed, Time.deltaTime * 2f);
        }
        else
        {
            currentRotationSpeed = Mathf.Lerp(currentRotationSpeed, 0f, Time.deltaTime * 3f);
        }
        
        // **바퀴 회전 적용**
        if (wheelTransform != null && currentRotationSpeed > 0.1f)
        {
            wheelTransform.Rotate(rotationAxis * currentRotationSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Items")) return;
        if (isGrinding) return;
        
        // 플레이어가 들고 있는 아이템은 무시 (Anvil, CraftingTable과 동일한 방식)
        var ctrl = ItemInteractionController.Instance;
        if (ctrl != null && other.transform.IsChildOf(ctrl.playerCamera))
        {
            Debug.Log($"{LOG_PREFIX} 플레이어 카메라 자식 오브젝트 무시: {other.name}");
            return;
        }
        
        var itemComp = other.GetComponent<ItemComponent>();
        if (itemComp == null) return;
        
        // 이미 연마된 무기는 아예 무시 (가장 먼저 체크)
        if (itemComp.isPolished)
        {
            Debug.LogWarning($"{LOG_PREFIX} {itemComp.itemName}은(는) 이미 연마가 완료된 무기입니다! 고정되지 않습니다.");
            return;
        }
        
        // 연마 가능한 무기인지 확인
        if (!itemComp.IsSharpenable)
        {
            Debug.Log($"{LOG_PREFIX} {itemComp.itemName}은(는) 연마할 수 없는 아이템입니다.");
            return;
        }
        
        // 이미 다른 무기가 올라가 있는지 확인
        if (weaponOnGrinder != null)
        {
            Debug.LogWarning($"{LOG_PREFIX} 이미 다른 무기가 연마기에 올라가 있습니다.");
            return;
        }
        
        Debug.Log($"{LOG_PREFIX} 연마 가능한 무기 감지: {itemComp.itemName}");
        weaponOnGrinder = other.gameObject;
        FixWeaponOnGrinder(weaponOnGrinder);
        
        // **자동으로 연마 시작**
        StartCoroutine(DelayedGrindingStart());
    }

    /// <summary>
    /// 무기 고정 후 잠시 대기하고 연마 시작
    /// </summary>
    private IEnumerator DelayedGrindingStart()
    {
        yield return new WaitForSeconds(0.5f); // 무기 고정 애니메이션 대기
        if (weaponOnGrinder != null && !isGrinding)
        {
            StartGrinding();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (weaponOnGrinder == other.gameObject && !isGrinding && !isFixingWeapon)
        {
            // 무기가 이미 grindingPosition의 자식으로 설정된 경우는 제거하지 않음
            if (other.transform.parent == grindingPosition)
            {
                Debug.Log($"{LOG_PREFIX} 무기가 고정된 상태라 제거하지 않음: {other.name}");
                return;
            }
            
            Debug.Log($"{LOG_PREFIX} 무기가 연마기에서 제거됨: {other.name}");
            weaponOnGrinder = null;
        }
    }

    private void FixWeaponOnGrinder(GameObject weapon)
    {
        if (weapon == null || grindingPosition == null) return;
        
        isFixingWeapon = true; // 고정 시작
        
        Rigidbody rb = weapon.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        
        weapon.transform.SetParent(grindingPosition);
        
        // DOTween 시퀀스로 완료 후 플래그 해제
        var sequence = DOTween.Sequence();
        sequence.Append(weapon.transform.DOLocalMove(Vector3.zero, 0.5f).SetEase(Ease.OutSine));
        sequence.Join(weapon.transform.DOLocalRotate(new Vector3(0, -90, 0), 0.5f).SetEase(Ease.OutSine));
        sequence.OnComplete(() => {
            isFixingWeapon = false; // 고정 완료
            Debug.Log($"{LOG_PREFIX} 무기 고정 애니메이션 완료: {weapon.name}");
        });
        
        Debug.Log($"{LOG_PREFIX} 무기가 연마기에 고정됨: {weapon.name}");
    }
    
    /// <summary>
    /// 연마 카메라로 이동
    /// </summary>
    private void MoveToGrindingCamera()
    {
        if (cameraGrindingViewpoint != null && PlayerController.Instance?.cam != null)
        {
            // **UI 감지 비활성화 (연마 중 ItemName, Focus UI 숨김)**
            DisableUIDetection();
            
            PlayerController.Instance.cam.MoveTo(cameraGrindingViewpoint, cameraMoveDuration);
            PlayerController.Instance.ToggleUI(true);
            Debug.Log($"{LOG_PREFIX} 연마 카메라로 이동");
        }
    }
    
    /// <summary>
    /// 기본 카메라로 복귀
    /// </summary>
    private void ResetToDefaultCamera()
    {
        if (PlayerController.Instance?.cam != null)
        {
            PlayerController.Instance.cam.ResetToDefault(cameraMoveDuration, true);
            Debug.Log($"{LOG_PREFIX} 기본 카메라로 복귀");
        }
        
        // **UI 감지 복원**
        EnableUIDetection();
    }
    
    /// <summary>
    /// UI 감지 비활성화 (연마 중 ItemName, Focus UI 숨김)
    /// </summary>
    private void DisableUIDetection()
    {
        if (ItemInteractionDetector.Instance != null)
        {
            wasDetectorActive = ItemInteractionDetector.Instance.enabled;
            ItemInteractionDetector.Instance.enabled = false;
            
            // 현재 표시된 UI 숨김
            UIManager.Instance.HideItemName();
            UIManager.Instance.SetFocusActive(false);
            
            Debug.Log($"{LOG_PREFIX} UI 감지 비활성화됨");
        }
    }
    
    /// <summary>
    /// UI 감지 활성화 복원
    /// </summary>
    private void EnableUIDetection()
    {
        if (ItemInteractionDetector.Instance != null && wasDetectorActive)
        {
            ItemInteractionDetector.Instance.enabled = true;
            UIManager.Instance.SetFocusActive(true);
            
            Debug.Log($"{LOG_PREFIX} UI 감지 활성화됨");
        }
    }

    // 외부에서 호출 (상호작용 키 또는 UI 버튼)
    public void StartGrinding()
    {
        if (weaponOnGrinder == null)
        {
            Debug.LogWarning($"{LOG_PREFIX} 연마할 무기가 없습니다.");
            return;
        }
        
        if (isGrinding)
        {
            Debug.LogWarning($"{LOG_PREFIX} 이미 연마 중입니다.");
            return;
        }
        
        StartCoroutine(GrindingSequence());
    }

    /// <summary>
    /// 연마기 바퀴 회전 시작
    /// </summary>
    private void StartWheelRotation()
    {
        Debug.Log($"{LOG_PREFIX} 연마기 바퀴 회전 시작 (속도: {rotationSpeed}도/초)");
        
        // 연마 파티클 효과 시작
        if (grindingEffect != null)
        {
            grindingEffect.Play();
        }
    }
    
    /// <summary>
    /// 연마기 바퀴 회전 정지
    /// </summary>
    private void StopWheelRotation()
    {
        Debug.Log($"{LOG_PREFIX} 연마기 바퀴 회전 정지");
        
        // 연마 파티클 효과 정지
        if (grindingEffect != null)
        {
            grindingEffect.Stop();
        }
    }
    
    /// <summary>
    /// 판정 결과에 따른 파티클 강도 조절 (기본 파티클은 계속 재생 중)
    /// </summary>
    public void PlayJudgmentParticle(GrindingMiniGame.JudgmentType judgment)
    {
        if (grindingEffect == null) return;
        
        switch (judgment)
        {
            case GrindingMiniGame.JudgmentType.Perfect:
                // **Perfect: 파티클 강도 3배**
                SetParticleIntensity(perfectParticleMultiplier);
                Debug.Log($"{LOG_PREFIX} Perfect 파티클 효과 - 강도 {perfectParticleMultiplier}배");
                break;
                
            case GrindingMiniGame.JudgmentType.Good:
                // **Good: 파티클 강도 2배**
                SetParticleIntensity(goodParticleMultiplier);
                Debug.Log($"{LOG_PREFIX} Good 파티클 효과 - 강도 {goodParticleMultiplier}배");
                break;
                
            case GrindingMiniGame.JudgmentType.Fail:
                // **Fail: 기본 설정 유지 (강도 변화 없음)**
                Debug.Log($"{LOG_PREFIX} Fail - 기본 파티클 강도 유지");
                return; // 강도 변화 없이 바로 리턴
        }
        
        // **추가 Burst 효과로 즉시 강화된 파티클 방출**
        int burstCount = Mathf.RoundToInt(originalParticleBurstCount * (judgment == GrindingMiniGame.JudgmentType.Perfect ? perfectParticleMultiplier : goodParticleMultiplier));
        grindingEffect.Emit(burstCount);
        
        // 설정된 시간 후 파티클 강도를 원래대로 복구
        StartCoroutine(RestoreParticleIntensity());
    }
    
    /// <summary>
    /// 파티클 강도 설정 (Burst Count만 조절, Loop 파티클용)
    /// </summary>
    private void SetParticleIntensity(float multiplier)
    {
        if (grindingEffect == null) return;
        
        var emission = grindingEffect.emission;
        
        // **Burst Count만 조절 (Emission Rate는 Loop에서 계속 적용되므로 건드리지 않음)**
        if (emission.burstCount > 0)
        {
            var burst = emission.GetBurst(0);
            burst.count = originalParticleBurstCount * multiplier;
            emission.SetBurst(0, burst);
            Debug.Log($"{LOG_PREFIX} Burst Count 설정: {burst.count.constant} (배수: {multiplier})");
        }
    }
    

    
    /// <summary>
    /// 파티클 Burst Count를 원래대로 복구
    /// </summary>
    private IEnumerator RestoreParticleIntensity()
    {
        yield return new WaitForSeconds(particleDuration);
        
        if (grindingEffect != null)
        {
            var emission = grindingEffect.emission;
            
            // **원본 Burst Count로만 복구**
            if (emission.burstCount > 0)
            {
                var burst = emission.GetBurst(0);
                burst.count = originalParticleBurstCount;
                emission.SetBurst(0, burst);
                Debug.Log($"{LOG_PREFIX} Burst Count 원상복구: {originalParticleBurstCount}");
            }
        }
    }

    private IEnumerator GrindingSequence()
    {
        isGrinding = true;
        StartWheelRotation(); // **회전 시작**
        
        ItemComponent weaponItem = weaponOnGrinder.GetComponent<ItemComponent>();
        float weaponAttack = weaponItem.atkPower;
        
        Debug.Log($"{LOG_PREFIX} 연마 시작 - 무기: {weaponItem.itemName}, 공격력: {weaponAttack}, 난이도: {miniGame.GetDifficultyText(weaponAttack)}");
        
        // 1. 카메라 이동 및 UI 설정
        MoveToGrindingCamera();
        
        // 2. UI 오픈
        UIManager uiManager = UIManager.Instance;
        if (uiManager != null && uiManager.grindingUI != null)
        {
            uiManager.OpenGrindingUI(weaponOnGrinder, weaponAttack);
        }
        else
        {
            Debug.LogError($"{LOG_PREFIX} UIManager 또는 grindingUI가 null입니다! UIManager: {uiManager != null}, grindingUI: {uiManager?.grindingUI != null}");
        }
        
        yield return new WaitForSeconds(0.5f);
        
        // 2. 3회 미니게임
        List<GrindingMiniGame.GrindingResult> results = new List<GrindingMiniGame.GrindingResult>();
        
        for (int round = 0; round < 3; round++)
        {
            Debug.Log($"{LOG_PREFIX} {round + 1}라운드 시작");
            
            // 공격력에 따른 커서 속도 적용
            float cursorSpeed = CalculateCursorSpeed(weaponAttack);
            yield return StartCoroutine(PlayMiniGameRound(cursorSpeed, weaponAttack, round, results));
            
            Debug.Log($"{LOG_PREFIX} {round + 1}라운드 결과: {results[round].judgment} (정확도: {results[round].accuracy:F2})");
            
            yield return new WaitForSeconds(0.3f); // 라운드 간 대기
        }
        
        // 3. 최종 결과 적용
        float finalSmoothIncrease = miniGame.CalculateFinalSmooth(results);
        ApplySmoothToWeapon(weaponItem, finalSmoothIncrease);
        
        // 4. 결과 표시
        ShowFinalResult(results, finalSmoothIncrease);
        yield return new WaitForSeconds(0.5f);
        
        // 5. UI 닫기 및 카메라 복귀
        if (uiManager != null && uiManager.grindingUI != null)
        {
            uiManager.CloseGrindingUI();
        }
        else
        {
            Debug.LogWarning($"{LOG_PREFIX} UI 닫기 실패 - UIManager 또는 grindingUI가 null");
        }
        
        // **6. 연마 완료 처리**
        CompleteGrinding(weaponItem);
        
        StopWheelRotation(); // **회전 정지**
        
        // **7. 자동 카메라 복귀 (연마 완료 후)**
        yield return new WaitForSeconds(0.2f); // 잠시 대기 후 카메라 복귀
        ResetToDefaultCamera();
        
        isGrinding = false;
        Debug.Log($"{LOG_PREFIX} 연마 완료 - 카메라 자동 복귀됨");
    }

    private IEnumerator PlayMiniGameRound(float cursorSpeed, float weaponAttack, int round, List<GrindingMiniGame.GrindingResult> results)
    {
        UIManager uiManager = UIManager.Instance;
        if (uiManager != null && uiManager.grindingUI != null)
        {
            // UI에서 라운드 플레이
            yield return StartCoroutine(uiManager.grindingUI.PlayRound(round, weaponAttack));
            
            // **실제 결과 받아오기**
            var actualResult = uiManager.grindingUI.GetLastResult();
            if (actualResult != null)
            {
                results.Add(actualResult);
                Debug.Log($"{LOG_PREFIX} {round + 1}라운드 결과: {actualResult.judgment} (+{actualResult.smoothIncrease:F3})");
            }
            else
            {
                // 폴백: 랜덤 결과
                float randomPos = Random.Range(0f, 1f);
                var result = miniGame.CalculateResult(randomPos, weaponAttack);
                results.Add(result);
                Debug.LogWarning($"{LOG_PREFIX} UI 결과를 받지 못해 랜덤 결과 사용");
            }
        }
        else
        {
            Debug.LogError($"{LOG_PREFIX} UIManager 또는 grindingUI가 null입니다!");
            // 폴백: 1초 대기 후 랜덤 결과
            yield return new WaitForSeconds(1f);
            float randomPos = Random.Range(0f, 1f);
            var result = miniGame.CalculateResult(randomPos, weaponAttack);
            results.Add(result);
        }
    }

    private void ApplySmoothToWeapon(ItemComponent weapon, float increase)
    {
        float oldSmooth = weapon.Smoothness;
        weapon.Smoothness += increase;
        
        // **공격력 증가 - 연마도에 비례하여 증가**
        float atkIncrease = increase * 10f; // 연마도 0.1당 공격력 1 증가
        float oldAttack = weapon.atkPower;
        weapon.atkPower += atkIncrease;
        
        Debug.Log($"{LOG_PREFIX} 연마 적용:");
        Debug.Log($"{LOG_PREFIX}   연마도: {oldSmooth:F3} → {weapon.Smoothness:F3} (+{increase:F3})");
        Debug.Log($"{LOG_PREFIX}   공격력: {oldAttack:F1} → {weapon.atkPower:F1} (+{atkIncrease:F1})");
        
        // 시각 효과 적용
        UpdateWeaponAppearance(weapon);
        
        // 파티클 효과는 StartWheelRotation에서 이미 시작됨
    }

    private void UpdateWeaponAppearance(ItemComponent weapon)
    {
        // 무기의 Material Smoothness 값 업데이트
        Renderer renderer = weapon.GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            Material mat = renderer.material;
            if (mat.HasProperty("_Smoothness"))
            {
                mat.SetFloat("_Smoothness", weapon.Smoothness);
            }
            else if (mat.HasProperty("_Glossiness"))
            {
                mat.SetFloat("_Glossiness", weapon.Smoothness);
            }
        }
    }

    private void ShowFinalResult(List<GrindingMiniGame.GrindingResult> results, float finalIncrease)
    {
        Debug.Log($"{LOG_PREFIX} === 연마 결과 ===");
        for (int i = 0; i < results.Count; i++)
        {
            var result = results[i];
            Debug.Log($"{LOG_PREFIX} {i + 1}라운드: {result.judgment} (+{result.smoothIncrease:F3})");
        }
        Debug.Log($"{LOG_PREFIX} 최종 Smooth 증가: +{finalIncrease:F3}");
    }

    // 무기 공격력에 따른 커서 속도 계산
    public float CalculateCursorSpeed(float weaponAttack)
    {
        float multiplier = 1f + (weaponAttack * speedMultiplier);
        multiplier = Mathf.Min(multiplier, maxSpeedMultiplier);
        return baseCursorSpeed * multiplier;
    }

    // 공격력에 따른 판정 범위 조정 (더 어렵게)
    public float GetAdjustedRange(float baseRange, float weaponAttack)
    {
        float difficulty = 1f + (weaponAttack * 0.01f); // 공격력 1당 1% 어려워짐
        return baseRange / Mathf.Min(difficulty, 2f); // 최대 50%까지만 줄어듦
    }

    /// <summary>
    /// 현재 연마기 위에 있는 무기 반환
    /// </summary>
    public GameObject GetWeaponOnGrinder()
    {
        return weaponOnGrinder;
    }
    
    /// <summary>
    /// 연마 강제 중단 (비상시 사용)
    /// </summary>
    public void ForceStopGrinding()
    {
        if (!isGrinding) return;
        
        Debug.Log($"{LOG_PREFIX} 연마 강제 중단");
        
        // 현재 실행 중인 코루틴 모두 중지
        StopAllCoroutines();
        
        // **회전 정지**
        StopWheelRotation();
        
        // 무기를 연마기에서 해제
        if (weaponOnGrinder != null)
        {
            weaponOnGrinder.transform.SetParent(null);
            Rigidbody rb = weaponOnGrinder.GetComponent<Rigidbody>();
            if (rb != null) 
            {
                rb.isKinematic = false;
                // 무기를 약간 위로 이동하여 연마기에서 떨어지도록 함
                weaponOnGrinder.transform.position += Vector3.up * 0.5f;
            }
        }
        
        // UI 닫기 및 카메라 복귀
        UIManager uiManager = UIManager.Instance;
        if (uiManager != null && uiManager.grindingUI != null)
        {
            uiManager.CloseGrindingUI();
        }
        
        ResetToDefaultCamera();
        
        // 상태 초기화
        isGrinding = false;
        weaponOnGrinder = null;
        
        Debug.Log($"{LOG_PREFIX} 연마 강제 중단 완료");
    }

    // Context Menu for testing
    [ContextMenu("연마 테스트")]
    public void TestGrinding()
    {
        if (Application.isPlaying)
        {
            Debug.Log($"{LOG_PREFIX} 테스트 시작 - wheelTransform: {(wheelTransform != null ? wheelTransform.name : "null")}");
            StartGrinding();
        }
    }

    /// <summary>
    /// 연마 완료 처리 - 무기 해제 및 상태 업데이트
    /// </summary>
    private void CompleteGrinding(ItemComponent weaponItem)
    {
        if (weaponOnGrinder == null) return;
        
        // 연마 완료 플래그 설정
        weaponItem.isPolished = true;
        
        // 무기를 연마기에서 해제
        weaponOnGrinder.transform.SetParent(null);
        Rigidbody rb = weaponOnGrinder.GetComponent<Rigidbody>();
        if (rb != null) 
        {
            rb.isKinematic = false;
            // 무기를 약간 위로 이동하여 연마기에서 떨어지도록 함
            weaponOnGrinder.transform.position += Vector3.up * 0.5f;
        }
        
        Debug.Log($"{LOG_PREFIX} {weaponItem.itemName} 연마 완료 - 무기 해제됨");
        weaponOnGrinder = null;
    }
} 