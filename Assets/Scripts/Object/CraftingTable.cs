using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class CraftingTable : MonoBehaviour
{
    // 디버그 로그 접두어
    private const string LOG_PREFIX = "[CraftingTable]";

    [Header("References")]
    public Transform snapPoint;
    public Transform partsHolder; // 파츠를 임시로 보관할 Transform

    [Header("Camera Positions")]
    public Transform cameraCraftingViewPoint;
    public float cameraMoveDuration = 0.5f;
    
    [Header("Orthographic Settings")]
    public bool useOrthographicInEditMode = true; // 편집 모드에서 직교 투영 사용 여부
    public float orthographicSize = 3f; // 직교 투영 크기

    [Header("Movement Settings")]
    public float moveSpeed = 1f;
    public float moveRangeX = 0.4f; // X축 이동 범위 (-moveRangeX ~ +moveRangeX)
    public float moveRangeZ = 1f;   // Z축 이동 범위 (-moveRangeZ ~ +moveRangeZ)
    
    [Header("Position Settings")]
    public float partYOffset = 0.02f; // 파츠가 블레이드 위에 떠있는 높이
    public float bladeDetectionDistance = 5f; // 블레이드 이탈 감지 거리
    
    [Header("Animation Settings")]
    public float bladeAnimationDuration = 0.1f; // 블레이드 배치 애니메이션 시간
    public float partAnimationDuration = 0.2f;  // 파츠 배치 애니메이션 시간
    public float combineAnimationDuration = 0.05f; // 합성 애니메이션 시간
    
    [Header("Current State")]
    public ItemComponent currentBlade;
    public ItemComponent currentPart;
    private bool isEditing = false;
    
    // 블레이드 배치 상태 추적
    private bool isBladePositioning = false;
    
    [Header("Forging Settings")]
    public int requiredHitsPerPart = 3; // 각 파츠당 필요한 타격 횟수
    
    [Header("Combine Effects")]
    public ParticleSystem combineParticleEffect; // 합성 완료 파티클
    public AudioClip[] combineSuccessSounds; // 합성 성공 사운드 배열
    public float combineEffectDuration = 1f; // 이펙트 지속 시간
    public Color combineGlowColor = Color.yellow; // 합성 시 발광 색상
    public Color combineEmissionColor = Color.white; // 합성 시 Emission 색상
    public float emissionIntensity = 3f; // Emission 강도 (HDR)
    
    // 간단한 최적화 변수들
    private float lastBladeCheckTime = 0f;
    private Vector3 tempPos = Vector3.zero; // Vector3 재사용
    
    // 파츠별 타격 횟수 추적
    private Dictionary<Transform, int> partHitCounts = new Dictionary<Transform, int>();
    
    // UI 상태 관리
    private bool wasDetectorActive = true;

    private void Awake()
    {
        // partsHolder가 없으면 생성
        if (partsHolder == null)
        {
            partsHolder = new GameObject("PartsHolder").transform;
            partsHolder.SetParent(transform);
            partsHolder.localPosition = Vector3.zero;
            Debug.Log($"{LOG_PREFIX} Awake: PartsHolder 생성됨");
        }
    }

    private void SwitchToTableCamera()
    {
        if (cameraCraftingViewPoint == null)
        {
            Debug.LogError($"{LOG_PREFIX} SwitchToTableCamera: 카메라 작업대 위치가 설정되지 않았습니다!");
            return;
        }
        
        Debug.Log($"{LOG_PREFIX} SwitchToTableCamera: 카메라를 작업대 시점으로 이동 (duration={cameraMoveDuration}s)");
        
        // UI 감지 비활성화
        DisableUIDetection();
        
        // 직교 투영 모드 활성화 (편집 모드 전용)
        if (useOrthographicInEditMode)
        {
            PlayerController.Instance.cam.EnableOrthographicMode(orthographicSize);
        }
        
        // 기존 PlayerCameraController 사용
        PlayerController.Instance.cam.MoveTo(cameraCraftingViewPoint, cameraMoveDuration);
    }

    private void SwitchToMainCamera()
    {
        Debug.Log($"{LOG_PREFIX} SwitchToMainCamera: 카메라를 기본 시점으로 복귀 (duration={cameraMoveDuration}s)");
        
        // 직교 투영 모드 비활성화
        if (useOrthographicInEditMode)
        {
            PlayerController.Instance.cam.DisableOrthographicMode();
        }
        
        // 기존 PlayerCameraController 사용
        PlayerController.Instance.cam.ResetToDefault(cameraMoveDuration);
        
        // UI 감지 복원
        EnableUIDetection();
    }
    
    // UI 감지 비활성화 (파츠 이동 중 ItemName, Focus UI 숨김)
    private void DisableUIDetection()
    {
        if (ItemInteractionDetector.Instance != null)
        {
            wasDetectorActive = ItemInteractionDetector.Instance.enabled;
            ItemInteractionDetector.Instance.enabled = false;
            
            // 현재 표시된 UI 숨김
            UIManager.Instance.HideItemName();
            UIManager.Instance.SetFocusActive(false);
            
            Debug.Log($"{LOG_PREFIX} DisableUIDetection: UI 감지 비활성화됨");
        }
    }
    
    // UI 감지 활성화 복원
    private void EnableUIDetection()
    {
        if (ItemInteractionDetector.Instance != null && wasDetectorActive)
        {
            ItemInteractionDetector.Instance.enabled = true;
            UIManager.Instance.SetFocusActive(true);
            
            Debug.Log($"{LOG_PREFIX} EnableUIDetection: UI 감지 활성화됨");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 2) 플레이어가 들고 있거나 장착한 아이템(UI/Detector에서 camera 자식으로 붙임)은 무시
        var ctrl = ItemInteractionController.Instance;
        if (ctrl != null && other.transform.IsChildOf(ctrl.playerCamera))
            return;

        // 3) 나머지 아이템 인식 기존 로직
        ItemComponent item = other.GetComponent<ItemComponent>();
        if (item == null || item.partsType == PartsType.None || item.canCombine == false)
            return;

        // 이미 처리된 아이템은 무시 (중복 처리 방지)
        if (item == currentBlade || item == currentPart)
        {
            Debug.Log($"{LOG_PREFIX} OnTriggerEnter: 이미 처리된 아이템 '{item.name}' 무시");
            return;
        }

        if (currentBlade == null && item.partsType != PartsType.Blade)
        {
            Debug.LogWarning($"{LOG_PREFIX} OnTriggerEnter: Blade가 없는 상태에서 부품을 추가할 수 없습니다!");
            return;
        }

        Debug.Log($"{LOG_PREFIX} OnTriggerEnter: 아이템 '{item.name}' 감지됨 (파츠 타입: {item.partsType})");
        AttachItem(item);
    }
    
    private void AttachItem(ItemComponent item)
    {
        bool hasBlade = false;
        foreach (Transform child in this.transform)
        {
            if (child.TryGetComponent<ItemComponent>(out ItemComponent part) && part.partsType == PartsType.Blade)
            {
                hasBlade = true;
                break;
            }
        }

        if (!hasBlade)
        {
            Debug.LogWarning($"{LOG_PREFIX} AttachItem: Blade 부품이 없으므로 초기화합니다.");
            currentBlade = null;
        }

        if (currentBlade == null && item.partsType == PartsType.Blade && item.canCombine)
        {
            Debug.Log($"{LOG_PREFIX} AttachItem: 블레이드 '{item.name}' 장착");
            currentBlade = item;

            currentBlade.transform.SetParent(snapPoint, worldPositionStays: true); // 월드 위치 유지

            // 블레이드 배치 중 상태 설정
            isBladePositioning = true;
            
            // 현재 로컬 위치에서 XZ는 유지하고 Y만 0으로 설정
            Vector3 currentLocalPos = currentBlade.transform.localPosition;
            Vector3 targetLocalPos = new Vector3(currentLocalPos.x, 0f, currentLocalPos.z);
            
            // 배치 상태 디버그 로그
            Debug.Log($"{LOG_PREFIX} AttachItem: 블레이드 DOTween 시작 - 현재 LocalPos: {currentLocalPos}, 목표: {targetLocalPos}");

            currentBlade.transform
                .DOLocalMove(targetLocalPos, bladeAnimationDuration)
                .SetEase(Ease.OutSine)
                .OnComplete(() => {
                    isBladePositioning = false; // 애니메이션 완료 후 상태 해제
                    Debug.Log($"{LOG_PREFIX} AttachItem: 블레이드 배치 완료");
                });
            currentBlade.transform
                .DOLocalRotate(Vector3.zero, bladeAnimationDuration)
                .SetEase(Ease.OutSine);

            // Rigidbody 설정은 그대로
            if (!currentBlade.TryGetComponent<Rigidbody>(out Rigidbody bladeRb))
            {
                bladeRb = currentBlade.gameObject.AddComponent<Rigidbody>();
                bladeRb.mass = 5f;
                bladeRb.interpolation = RigidbodyInterpolation.Interpolate;
                bladeRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
            bladeRb.isKinematic = true;

            currentPart = null;
            isEditing = true;
        }
        else if (currentBlade != null && item.partsType == PartsType.Blade && item.weaponType == currentBlade.weaponType && item.canCombine)
        {
            // 이미 블레이드가 있는 상태에서 같은 무기 타입의 블레이드가 들어오면 파츠로 취급
            Debug.Log($"{LOG_PREFIX} AttachItem: 블레이드 '{item.name}'를 파츠로 취급하여 배치 시작");
            SetupPartItem(item, "블레이드(파츠)");
        }
        else if (currentBlade != null && item.partsType != PartsType.None && item.weaponType == currentBlade.weaponType && item.canCombine)
        {
            Debug.Log($"{LOG_PREFIX} AttachItem: 파츠 '{item.name}' 배치 시작 (타입: {item.partsType})");
            SetupPartItem(item, "파츠");
        }
        else return;

        SwitchToTableCamera();
    }
    
    // 파츠 설정 공통 함수
    private void SetupPartItem(ItemComponent item, string itemTypeStr)
    {
        currentPart = item;

        // XZ 위치는 유지하되 Y만 조정
        Vector3 worldPos = item.transform.position;
        float bladeYPos = currentBlade.transform.position.y;
        
        // partsHolder의 자식으로 배치
        currentPart.transform.SetParent(partsHolder, worldPositionStays: true);
        Debug.Log($"{LOG_PREFIX} AttachItem: {itemTypeStr}가 partsHolder의 자식으로 설정됨");
        
        // XZ위치는 유지하고 Y위치만 조정
        Vector3 targetPos = new Vector3(worldPos.x, bladeYPos + partYOffset, worldPos.z);
        currentPart.transform.DOMove(targetPos, partAnimationDuration).SetEase(Ease.OutSine);
        currentPart.transform.DOLocalRotate(Vector3.zero, partAnimationDuration).SetEase(Ease.OutSine);
        
        Debug.Log($"{LOG_PREFIX} AttachItem: {itemTypeStr} 위치 설정 - 원래={worldPos}, 목표={targetPos}");

        // Rigidbody 설정
        if (currentPart.TryGetComponent<Rigidbody>(out Rigidbody partRb))
        {
            partRb.isKinematic = true;
        }

        // 콜라이더 충돌 무시
        Collider partCollider = currentPart.GetComponent<Collider>();
        Collider bladeCollider = currentBlade.GetComponent<Collider>();
        if (partCollider != null && bladeCollider != null)
        {
            Physics.IgnoreCollision(partCollider, bladeCollider, true);
            Debug.Log($"{LOG_PREFIX} AttachItem: {itemTypeStr}-블레이드 콜라이더 충돌 무시됨");
        }
        
        // 파츠 타격 횟수 초기화
        partHitCounts[currentPart.transform] = 0;

        isEditing = true;
        Debug.Log($"{LOG_PREFIX} AttachItem: {itemTypeStr} 배치 완료, 이동 가능 상태");
    }
    
    // Raycast 기반 망치 타격 처리 함수 - Hammer.cs에서 호출됨
    public void HandleHammerHit(Transform hitTransform, Vector3 hitPoint)
    {
        Debug.Log($"{LOG_PREFIX} HandleHammerHit: 타격됨 - {hitTransform.name}, 위치={hitPoint}");
        
        if (currentBlade == null)
        {
            Debug.LogWarning($"{LOG_PREFIX} HandleHammerHit: currentBlade가 NULL입니다!");
            return;
        }
        
        // 파츠 컴포넌트 확인 - 직접 또는 부모에서 검색
        ItemComponent partItem = hitTransform.GetComponent<ItemComponent>();
        if (partItem == null)
        {
            partItem = hitTransform.GetComponentInParent<ItemComponent>();
            if (partItem != null)
            {
                Debug.Log($"{LOG_PREFIX} HandleHammerHit: 부모에서 파츠 '{partItem.name}' 발견");
            }
        }
        
        if (partItem == null)
        {
            Debug.LogWarning($"{LOG_PREFIX} HandleHammerHit: {hitTransform.name}에 ItemComponent가 없습니다!");
            return;
        }
        
        // 블레이드는 타격 대상이 아님 (단, currentBlade가 아닌 경우는 파츠로 취급)
        if (partItem.partsType == PartsType.Blade && partItem == currentBlade)
        {
            Debug.LogWarning($"{LOG_PREFIX} HandleHammerHit: 메인 블레이드는 타격 대상이 아닙니다!");
            return;
        }
        
        // 이미 합성된 파츠는 무시
        if (partItem.isPolished)
        {
            Debug.Log($"{LOG_PREFIX} HandleHammerHit: 이미 합성된 파츠입니다: {partItem.name}");
            return;
        }
        
        // 현재 파츠가 블레이드에 적합한지 확인
        if (partItem.weaponType != currentBlade.weaponType)
        {
            Debug.LogWarning($"{LOG_PREFIX} HandleHammerHit: {partItem.name}은(는) {currentBlade.name}에 적합하지 않은 무기 타입입니다!");
            return;
        }
        
        // 타격 횟수 증가
        if (!partHitCounts.ContainsKey(hitTransform))
        {
            partHitCounts[hitTransform] = 0;
            Debug.Log($"{LOG_PREFIX} HandleHammerHit: 새로운 파츠 타격 카운트 초기화 - {hitTransform.name}");
        }
        
        partHitCounts[hitTransform]++;
        int currentHits = partHitCounts[hitTransform];
        
        Debug.Log($"{LOG_PREFIX} HandleHammerHit: 파츠 '{partItem.name}' 타격됨 ({currentHits}/{requiredHitsPerPart})");
        
        // 필요한 타격 횟수에 도달하면 마지막 이동 후 합성
        if (currentHits >= requiredHitsPerPart)
        {
            Debug.Log($"{LOG_PREFIX} HandleHammerHit: 파츠 '{partItem.name}' 단조 완료! 최종 이동 후 합성 진행");
            
            // 마지막 Y축 이동 후 합성 진행
            ProcessProgressiveHammerHit(partItem, currentHits, () => {
                // Y축 이동 완료 후 콜백에서 합성 실행
                CombineSinglePart(partItem);
                partHitCounts.Remove(hitTransform);
            });
        }
        else
        {
            // 일반 타격 시에는 Y축 이동만
            ProcessProgressiveHammerHit(partItem, currentHits);
        }
    }
    
    // 점진적 타격 처리 - 각 타격마다 Y축 이동
    private void ProcessProgressiveHammerHit(ItemComponent partItem, int currentHits, System.Action onComplete = null)
    {
        if (partItem == null || currentBlade == null) return;
        
        // 현재 블레이드의 Y 위치
        float bladeYPos = currentBlade.transform.position.y;
        
        // 각 타격당 이동할 거리 계산
        float movePerHit = partYOffset / requiredHitsPerPart;
        
        // 목표 Y 위치 계산 (블레이드 Y + 남은 오프셋)
        float remainingOffset = partYOffset - (movePerHit * currentHits);
        float targetY = bladeYPos + remainingOffset;
        
        // 현재 월드 위치 가져오기
        Vector3 currentWorldPos = partItem.transform.position;
        Vector3 targetWorldPos = new Vector3(currentWorldPos.x, targetY, currentWorldPos.z);
        
        Debug.Log($"{LOG_PREFIX} ProcessProgressiveHammerHit: 파츠 '{partItem.name}' 타격 {currentHits}회 - Y이동: {currentWorldPos.y:F3} → {targetY:F3} (이동거리: {movePerHit:F3})");
        
        // DOTween으로 부드럽게 이동
        partItem.transform.DOMove(targetWorldPos, combineAnimationDuration * 2f)
            .SetEase(Ease.OutBounce)
            .OnComplete(() => {
                Debug.Log($"{LOG_PREFIX} ProcessProgressiveHammerHit: 파츠 '{partItem.name}' Y축 이동 완료 - 현재 Y: {partItem.transform.position.y:F3}");
                
                // 완료 콜백이 있으면 실행 (마지막 타격일 때 합성 진행)
                onComplete?.Invoke();
            });
    }
    
    // 단일 파츠 합성
    private void CombineSinglePart(ItemComponent part)
    {
        if (currentBlade == null || part == null) return;
        
        Debug.Log($"{LOG_PREFIX} CombineSinglePart: 파츠 '{part.name}' 합성 시작, 현재 위치: {part.transform.position}");
        
        // 합성 이펙트 실행
        PlayCombineEffects(part.transform.position);
        
        // 월드 위치와 회전 저장 (이후 복원)
        Vector3 worldPosition = part.transform.position;
        Quaternion worldRotation = part.transform.rotation;
        
        // 블레이드에 파츠 스탯 추가
        currentBlade.AddStatsFrom(part);
        
        // 파츠를 블레이드의 자식으로 이동 (합성 시점에 부모-자식 관계 설정)
        part.transform.SetParent(currentBlade.transform);
        Debug.Log($"{LOG_PREFIX} CombineSinglePart: 파츠가 블레이드의 자식으로 설정됨");
        
        // 파츠 위치와 회전을 현재 상태로 유지 + 합성 이펙트 애니메이션
        part.transform.DOMove(worldPosition, combineAnimationDuration).SetEase(Ease.OutQuad);
        part.transform.DORotateQuaternion(worldRotation, combineAnimationDuration).SetEase(Ease.OutQuad);
        
        // 합성 시 파츠가 빛나는 효과
        StartCombineGlowEffect(part);
        
        // 파츠의 Rigidbody 제거 (콜라이더는 유지)
        if (part.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            Destroy(rb);
            Debug.Log($"{LOG_PREFIX} CombineSinglePart: Rigidbody 제거됨");
        }
        
        // 합성 완료 표시
        part.isPolished = true;
        part.canCombine = false;
        
        Debug.Log($"{LOG_PREFIX} CombineSinglePart: 파츠 '{part.name}'가 '{currentBlade.name}'에 합성되었습니다.");
    }
    
    // 합성 이펙트 재생
    private void PlayCombineEffects(Vector3 effectPosition)
    {
        Debug.Log($"{LOG_PREFIX} PlayCombineEffects: 합성 이펙트 재생 시작 at {effectPosition}");
        
        // 1. 파티클 이펙트
        if (combineParticleEffect != null)
        {
            combineParticleEffect.transform.position = effectPosition;
            combineParticleEffect.Play();
            Debug.Log($"{LOG_PREFIX} PlayCombineEffects: 파티클 이펙트 재생됨");
        }
        
        // 2. 사운드 이펙트
        if (combineSuccessSounds != null && combineSuccessSounds.Length > 0)
        {
            int randomIndex = Random.Range(0, combineSuccessSounds.Length);
            AudioClip selectedSound = combineSuccessSounds[randomIndex];
            
            if (selectedSound != null && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySoundAtPosition(selectedSound.name, effectPosition);
                Debug.Log($"{LOG_PREFIX} PlayCombineEffects: 사운드 재생됨 - {selectedSound.name}");
            }
        }
    }
    
    // 파츠 발광 효과
    private void StartCombineGlowEffect(ItemComponent part)
    {
        if (part == null) return;
        
        // 파츠의 모든 렌더러 찾기
        Renderer[] renderers = part.GetComponentsInChildren<Renderer>();
        
        foreach (Renderer renderer in renderers)
        {
            if (renderer.material != null)
            {
                // 발광 효과를 위한 머티리얼 속성 변경
                StartCoroutine(GlowEffect(renderer));
            }
        }
    }
    
    // 발광 효과 코루틴
    private System.Collections.IEnumerator GlowEffect(Renderer targetRenderer)
    {
        if (targetRenderer == null || targetRenderer.material == null) yield break;
        
        Material material = targetRenderer.material;
        Color originalColor = material.color;
        
        // Emission 관련 설정
        bool hasEmission = material.HasProperty("_EmissionColor");
        Color originalEmission = Color.black;
        bool wasEmissionEnabled = false;
        
        if (hasEmission)
        {
            originalEmission = material.GetColor("_EmissionColor");
            wasEmissionEnabled = material.IsKeywordEnabled("_EMISSION");
            
            // Emission 강제 활성화
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            
            Debug.Log($"{LOG_PREFIX} GlowEffect: Emission 활성화됨 - {targetRenderer.name}");
        }
        else
        {
            Debug.LogWarning($"{LOG_PREFIX} GlowEffect: '{targetRenderer.name}' 머티리얼이 Emission을 지원하지 않습니다.");
        }
        
        float elapsed = 0f;
        float glowDuration = combineEffectDuration;
        
        while (elapsed < glowDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.PingPong(elapsed * 4f, 1f); // 4배 속도로 깜빡임
            
            // 기본 색상 효과 (subtle하게)
            Color currentColor = Color.Lerp(originalColor, combineGlowColor, t * 0.3f);
            material.color = currentColor;
            
            // Emission 효과 (강렬하게)
            if (hasEmission)
            {
                // HDR 색상으로 강렬한 발광 효과
                Color targetEmission = combineEmissionColor * emissionIntensity;
                Color currentEmission = Color.Lerp(originalEmission, targetEmission, t);
                material.SetColor("_EmissionColor", currentEmission);
                
                // 실시간 GI 업데이트
                RendererExtensions.UpdateGIMaterials(targetRenderer);
            }
            
            yield return null;
        }
        
        // 원래 상태로 복원
        material.color = originalColor;
        if (hasEmission)
        {
            material.SetColor("_EmissionColor", originalEmission);
            
            // 원래 Emission 상태로 복원
            if (wasEmissionEnabled)
                material.EnableKeyword("_EMISSION");
            else
                material.DisableKeyword("_EMISSION");
        }
        
        Debug.Log($"{LOG_PREFIX} GlowEffect: 발광 효과 완료 - {targetRenderer.name}");
    }

    private void Update()
    {
        // 블레이드 상태 체크 - 0.1초마다만 체크 (간단한 최적화)
        if (Time.time - lastBladeCheckTime > 0.1f)
        {
            CheckBladeStatus();
            lastBladeCheckTime = Time.time;
        }
        
        // 편집 모드일 때 블레이드나 파츠 이동 처리
        if (isEditing && (currentBlade != null || currentPart != null))
        {
            float moveX = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;
            float moveZ = -Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime;

            // 이동 입력이 있을 때 처리
            if (Mathf.Abs(moveX) > 0.001f || Mathf.Abs(moveZ) > 0.001f)
            {
                // 현재 파츠가 있으면 파츠 이동, 없으면 블레이드 이동
                Transform targetTransform = currentPart != null ? currentPart.transform : currentBlade.transform;
                string targetName = currentPart != null ? currentPart.name : currentBlade.name;
                
                // 이동 전 위치
                Vector3 oldPos = targetTransform.localPosition;
                
                // Y값 유지하면서 이동 (tempPos 재사용으로 GC 줄이기)
                tempPos.Set(
                    Mathf.Clamp(oldPos.x + moveX, -moveRangeX, moveRangeX),
                    oldPos.y,
                    Mathf.Clamp(oldPos.z + moveZ, -moveRangeZ, moveRangeZ)
                );
                targetTransform.localPosition = tempPos;
                
                Debug.Log($"{LOG_PREFIX} Update: {targetName} 이동 - {oldPos} → {tempPos}");
            }
        }

        // 키 입력 처리 (편집 모드일 때만)
        if (isEditing)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log($"{LOG_PREFIX} Update: Space 키 감지됨 - 위치 확정");
                FinalizeAttachment();
            }
            
            // ESC 키로 작업 취소
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Debug.Log($"{LOG_PREFIX} Update: ESC 키 감지됨 - 작업 취소");
                CancelEditing();
            }
        }
    }
    
    // 블레이드 상태 체크 함수
    private void CheckBladeStatus()
    {
        if (currentBlade == null || isBladePositioning) return; // 블레이드 배치 중에는 체크하지 않음
        
        // 블레이드가 snapPoint의 자식인지 체크
        bool isChildOfSnapPoint = currentBlade.transform.IsChildOf(snapPoint);
        
        // 거리 체크 최적화 (sqrMagnitude 사용으로 sqrt 계산 제거)
        float sqrDistance = (currentBlade.transform.position - snapPoint.position).sqrMagnitude;
        float sqrDetectionDistance = bladeDetectionDistance * bladeDetectionDistance;
        bool isTooFar = sqrDistance > sqrDetectionDistance;
        
        if (!isChildOfSnapPoint || isTooFar)
        {
            Debug.LogWarning($"{LOG_PREFIX} CheckBladeStatus: 블레이드가 작업대를 벗어났습니다. 초기화합니다.");
            ResetCraftingTable();
        }
    }
    
    // 작업대 초기화 함수
    private void ResetCraftingTable()
    {
        Debug.Log($"{LOG_PREFIX} ResetCraftingTable: 작업대 상태 초기화");
        
        // 현재 파츠가 있다면 partsHolder에서 해제
        if (currentPart != null)
        {
            currentPart.transform.SetParent(null);
            if (currentPart.TryGetComponent<Rigidbody>(out Rigidbody partRb))
            {
                partRb.isKinematic = false;
            }
            Debug.Log($"{LOG_PREFIX} ResetCraftingTable: 파츠 '{currentPart.name}' 해제됨");
        }
        
        // partsHolder의 모든 자식 파츠들 해제 (합쳐지지 않은 모든 파츠들)
        if (partsHolder != null)
        {
            for (int i = partsHolder.childCount - 1; i >= 0; i--)
            {
                Transform child = partsHolder.GetChild(i);
                ItemComponent childItem = child.GetComponent<ItemComponent>();
                
                if (childItem != null && !childItem.isPolished)
                {
                    // 파츠를 partsHolder에서 해제
                    child.SetParent(null);
                    
                    // Rigidbody 활성화하여 물리 상호작용 가능하게 함
                    if (child.TryGetComponent<Rigidbody>(out Rigidbody childRb))
                    {
                        childRb.isKinematic = false;
                    }
                    
                    Debug.Log($"{LOG_PREFIX} ResetCraftingTable: 미완성 파츠 '{childItem.name}' 해제됨");
                }
            }
        }
        
        // 모든 상태 초기화
        currentBlade = null;
        currentPart = null;
        partHitCounts.Clear();
        isEditing = false;
        isBladePositioning = false; // 블레이드 배치 상태도 초기화
        
        Debug.Log($"{LOG_PREFIX} ResetCraftingTable: 작업대 초기화 완료");
    }
    
    // 작업 취소
    private void CancelEditing()
    {
        if (!isEditing) return;
        
        Debug.Log($"{LOG_PREFIX} CancelEditing: 작업 취소");
        isEditing = false;
        SwitchToMainCamera();
    }

    private void FinalizeAttachment()
    {
        Debug.Log($"{LOG_PREFIX} FinalizeAttachment: 함수 시작 - currentBlade: {currentBlade != null}, currentPart: {currentPart != null}, isEditing: {isEditing}");
        
        if (currentBlade == null)
        {
            Debug.LogError($"{LOG_PREFIX} FinalizeAttachment: currentBlade가 NULL입니다! 블레이드를 먼저 배치하세요.");
            return;
        }

        // 현재 파츠가 있으면 파츠 위치 확정, 없으면 블레이드 위치 확정
        if (currentPart != null)
        {
            // 파츠 위치 확정
            Vector3 finalLocalPos = currentPart.transform.localPosition;
            Vector3 finalWorldPos = currentPart.transform.position;
            Debug.Log($"{LOG_PREFIX} FinalizeAttachment: 파츠 위치 확정 - local={finalLocalPos}, world={finalWorldPos}");
            
            currentPart = null;
            Debug.Log($"{LOG_PREFIX} FinalizeAttachment: 파츠 위치 확정 완료, 망치로 타격하여 합성하세요");
        }
        else
        {
            // 블레이드 위치 확정
            Vector3 finalLocalPos = currentBlade.transform.localPosition;
            Vector3 finalWorldPos = currentBlade.transform.position;
            Debug.Log($"{LOG_PREFIX} FinalizeAttachment: 블레이드 위치 확정 - local={finalLocalPos}, world={finalWorldPos}");
            Debug.Log($"{LOG_PREFIX} FinalizeAttachment: 블레이드 위치 확정 완료, 파츠를 추가하세요");
        }
        
        // 편집 모드 종료
        Debug.Log($"{LOG_PREFIX} FinalizeAttachment: 편집 모드 종료 중...");
        isEditing = false;
        SwitchToMainCamera();
        Debug.Log($"{LOG_PREFIX} FinalizeAttachment: 완료");
    }
}