using UnityEngine;
using DG.Tweening;
using System.Collections;

/// <summary>
/// 개별 오브젝트에 붙여서 자기 자신을 애니메이션할 수 있는 헬퍼 컴포넌트
/// UnityEvent에서 바로 호출 가능
/// </summary>
public class DOTweenHelper : MonoBehaviour
{
    [Header("애니메이션 설정")]
    public float duration = 1f;
    public Ease ease = Ease.OutQuad;
    
    [Header("플로팅 효과 설정")]
    public float floatHeight = 0.3f;  // 떠다니는 높이
    public float floatDuration = 1.5f;  // 한 번 위아래 움직이는 시간
    
    [Header("스피닝 회전 설정")]
    [SerializeField] private float defaultSpinSpeed = 180f;  // 기본 회전 속도 (도/초)
    
    /// <summary>
    /// 회전 속도를 기반으로 DOTween 지속시간 계산 (360도 기준)
    /// </summary>
    private float CalculateSpinDuration(float speed)
    {
        return 360f / Mathf.Max(1f, speed); // 최소 1도/초
    }
    
    // 각각의 애니메이션용 Tween들
    private Tween currentTween; // 기존 호환성을 위해 유지
    private Tween floatingTween; // 둥둥 효과 전용
    private Tween spinningTween; // 회전 효과 전용
    
    // 캐시된 Transform
    private Transform cachedTransform;
    
    private Vector3 originalPosition;
    private bool isFloating = false;
    
    private void Awake()
    {
        cachedTransform = transform;
        originalPosition = cachedTransform.position;
    }

    #region 기본 애니메이션 메서드들 (매개변수 없음)

    /// <summary>
    /// 오브젝트 사라지기 (스케일을 0으로)
    /// </summary>
    public void FadeOut()
    {
        StopCurrentTween();
        currentTween = cachedTransform.DOScale(Vector3.zero, duration).SetEase(Ease.InBack);
        Debug.Log($"[DOTweenHelper] {name}: 사라지기 시작");
    }

    /// <summary>
    /// 오브젝트 나타나기 (스케일을 1로)
    /// </summary>
    public void FadeIn()
    {
        StopCurrentTween();
        currentTween = cachedTransform.DOScale(Vector3.one, duration).SetEase(Ease.OutBack);
        Debug.Log($"[DOTweenHelper] {name}: 나타나기 시작");
    }

    /// <summary>
    /// 펀치 스케일 효과
    /// </summary>
    public void PunchScale()
    {
        StopCurrentTween();
        currentTween = cachedTransform.DOPunchScale(Vector3.one * 0.2f, duration, 10, 1);
        Debug.Log($"[DOTweenHelper] {name}: 펀치 스케일 효과 시작");
    }

    /// <summary>
    /// 현재 애니메이션 중지
    /// </summary>
    public void StopAnimation()
    {
        StopCurrentTween();
        Debug.Log($"[DOTweenHelper] {name}: 애니메이션 중지");
    }

    /// <summary>
    /// 둥둥 떠다니는 효과 시작
    /// </summary>
    public void StartFloating()
    {
        if (isFloating) return;
        
        // 기존 애니메이션이 있으면 완료될 때까지 기다림
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.OnComplete(() => StartFloatingInternal());
        }
        else
        {
            StartFloatingInternal();
        }
    }

    /// <summary>
    /// 둥둥 효과 내부 구현
    /// </summary>
    private void StartFloatingInternal()
    {
        if (isFloating) return;
        
        originalPosition = cachedTransform.position;
        isFloating = true;
        
        Vector3 targetPosition = originalPosition + Vector3.up * floatHeight;
        
        // 기존 둥둥 효과가 있으면 중지
        if (floatingTween != null && floatingTween.IsActive())
        {
            floatingTween.Kill();
        }
        
        floatingTween = cachedTransform.DOMove(targetPosition, floatDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
            
        Debug.Log($"[DOTweenHelper] {name}: 둥둥 효과 시작 (높이: {floatHeight}, 주기: {floatDuration}초) - 회전과 동시 실행 가능");
    }

    /// <summary>
    /// 둥둥 떠다니는 효과 중지
    /// </summary>
    public void StopFloating()
    {
        if (!isFloating) return;
        
        // 둥둥 효과만 중지 (회전은 그대로 유지)
        if (floatingTween != null && floatingTween.IsActive())
        {
            floatingTween.Kill();
        }
        
        isFloating = false;
        
        // 원래 위치로 부드럽게 복귀
        floatingTween = cachedTransform.DOMove(originalPosition, 0.5f).SetEase(Ease.OutQuad);
        
        Debug.Log($"[DOTweenHelper] {name}: 둥둥 효과 중지 (원래 위치로 복귀) - 회전은 계속 유지");
    }

    /// <summary>
    /// 둥둥 효과 토글 (켜기/끄기)
    /// </summary>
    public void ToggleFloating()
    {
        if (isFloating)
        {
            StopFloating();
        }
        else
        {
            StartFloating();
        }
    }

    /// <summary>
    /// 둥둥 효과 높이 설정
    /// </summary>
    public void SetFloatHeight(float height)
    {
        floatHeight = height;
        Debug.Log($"[DOTweenHelper] {name}: 둥둥 높이 {height}로 설정");
        
        // 현재 둥둥 중이면 다시 시작
        if (isFloating)
        {
            StartFloating();
        }
    }

    /// <summary>
    /// 둥둥 효과 주기 설정
    /// </summary>
    public void SetFloatDuration(float newFloatDuration)
    {
        floatDuration = newFloatDuration;
        Debug.Log($"[DOTweenHelper] {name}: 둥둥 주기 {newFloatDuration}초로 설정");
        
        // 현재 둥둥 중이면 다시 시작
        if (isFloating)
        {
            StartFloating();
        }
    }

    #endregion

    #region Float 매개변수 메서드들

    /// <summary>
    /// X 위치로 이동
    /// </summary>
    public void MoveToX(float x)
    {
        Vector3 newPosition = new Vector3(x, cachedTransform.position.y, cachedTransform.position.z);
        StopCurrentTween();
        currentTween = cachedTransform.DOMove(newPosition, duration).SetEase(ease);
        Debug.Log($"[DOTweenHelper] {name}: X={x}로 이동 시작");
    }

    /// <summary>
    /// Y 위치로 이동
    /// </summary>
    public void MoveToY(float y)
    {
        Vector3 newPosition = new Vector3(cachedTransform.position.x, y, cachedTransform.position.z);
        StopCurrentTween();
        currentTween = cachedTransform.DOMove(newPosition, duration).SetEase(ease);
        Debug.Log($"[DOTweenHelper] {name}: Y={y}로 이동 시작");
    }

    /// <summary>
    /// Z 위치로 이동
    /// </summary>
    public void MoveToZ(float z)
    {
        Vector3 newPosition = new Vector3(cachedTransform.position.x, cachedTransform.position.y, z);
        StopCurrentTween();
        currentTween = cachedTransform.DOMove(newPosition, duration).SetEase(ease);
        Debug.Log($"[DOTweenHelper] {name}: Z={z}로 이동 시작");
    }

    /// <summary>
    /// Y 회전 (로컬 좌표계)
    /// </summary>
    public void RotateToY(float y)
    {
        Vector3 newRotation = new Vector3(cachedTransform.localEulerAngles.x, y, cachedTransform.localEulerAngles.z);
        StopCurrentTween();
        currentTween = cachedTransform.DOLocalRotate(newRotation, duration).SetEase(ease);
        Debug.Log($"[DOTweenHelper] {name}: Y 회전={y}로 로컬 회전 시작");
    }

    /// <summary>
    /// X 회전 (로컬 좌표계)
    /// </summary>
    public void RotateToX(float x)
    {
        Vector3 newRotation = new Vector3(x, cachedTransform.localEulerAngles.y, cachedTransform.localEulerAngles.z);
        StopCurrentTween();
        currentTween = cachedTransform.DOLocalRotate(newRotation, duration).SetEase(ease);
        Debug.Log($"[DOTweenHelper] {name}: X 회전={x}로 로컬 회전 시작");
    }

    /// <summary>
    /// Z 회전 (로컬 좌표계)
    /// </summary>
    public void RotateToZ(float z)
    {
        Vector3 newRotation = new Vector3(cachedTransform.localEulerAngles.x, cachedTransform.localEulerAngles.y, z);
        StopCurrentTween();
        currentTween = cachedTransform.DOLocalRotate(newRotation, duration).SetEase(ease);
        Debug.Log($"[DOTweenHelper] {name}: Z 회전={z}로 로컬 회전 시작");
    }

    /// <summary>
    /// 균등 스케일로 크기 변경
    /// </summary>
    public void ScaleToUniform(float scale)
    {
        Vector3 scaleVector = Vector3.one * scale;
        StopCurrentTween();
        currentTween = cachedTransform.DOScale(scaleVector, duration).SetEase(ease);
        Debug.Log($"[DOTweenHelper] {name}: 균등 스케일={scale}로 크기 변경 시작");
    }

    /// <summary>
    /// 지속시간 설정
    /// </summary>
    public void SetDuration(float newDuration)
    {
        duration = newDuration;
        Debug.Log($"[DOTweenHelper] {name}: 지속시간 {newDuration}초로 설정");
    }

    /// <summary>
    /// X 위치에 값 더하기 (상대적 이동)
    /// </summary>
    public void AddToX(float deltaX)
    {
        Vector3 newPosition = cachedTransform.position + new Vector3(deltaX, 0, 0);
        StopCurrentTween();
        currentTween = cachedTransform.DOMove(newPosition, duration).SetEase(ease);
        Debug.Log($"[DOTweenHelper] {name}: X에 {deltaX} 더해서 이동 시작");
    }

    /// <summary>
    /// Y 위치에 값 더하기 (상대적 이동)
    /// </summary>
    public void AddToY(float deltaY)
    {
        Vector3 newPosition = cachedTransform.position + new Vector3(0, deltaY, 0);
        StopCurrentTween();
        currentTween = cachedTransform.DOMove(newPosition, duration).SetEase(ease);
        Debug.Log($"[DOTweenHelper] {name}: Y에 {deltaY} 더해서 이동 시작");
    }

    /// <summary>
    /// Z 위치에 값 더하기 (상대적 이동)
    /// </summary>
    public void AddToZ(float deltaZ)
    {
        Vector3 newPosition = cachedTransform.position + new Vector3(0, 0, deltaZ);
        StopCurrentTween();
        currentTween = cachedTransform.DOMove(newPosition, duration).SetEase(ease);
        Debug.Log($"[DOTweenHelper] {name}: Z에 {deltaZ} 더해서 이동 시작");
    }

    /// <summary>
    /// 로컬 X 위치에 값 더하기 (로컬 좌표계 상대적 이동)
    /// </summary>
    public void AddToLocalX(float deltaX)
    {
        Vector3 newLocalPosition = cachedTransform.localPosition + new Vector3(deltaX, 0, 0);
        StopCurrentTween();
        currentTween = cachedTransform.DOLocalMove(newLocalPosition, duration).SetEase(ease);
        Debug.Log($"[DOTweenHelper] {name}: 로컬 X에 {deltaX} 더해서 이동 시작");
    }

    /// <summary>
    /// 로컬 Y 위치에 값 더하기 (로컬 좌표계 상대적 이동)
    /// </summary>
    public void AddToLocalY(float deltaY)
    {
        Vector3 newLocalPosition = cachedTransform.localPosition + new Vector3(0, deltaY, 0);
        StopCurrentTween();
        currentTween = cachedTransform.DOLocalMove(newLocalPosition, duration).SetEase(ease);
        Debug.Log($"[DOTweenHelper] {name}: 로컬 Y에 {deltaY} 더해서 이동 시작");
    }

    /// <summary>
    /// 로컬 Z 위치에 값 더하기 (로컬 좌표계 상대적 이동)
    /// </summary>
    public void AddToLocalZ(float deltaZ)
    {
        Vector3 newLocalPosition = cachedTransform.localPosition + new Vector3(0, 0, deltaZ);
        StopCurrentTween();
        currentTween = cachedTransform.DOLocalMove(newLocalPosition, duration).SetEase(ease);
        Debug.Log($"[DOTweenHelper] {name}: 로컬 Z에 {deltaZ} 더해서 이동 시작");
    }

    /// <summary>
    /// X축에 회전값 더하기 (상대적 로컬 회전)
    /// </summary>
    public void AddRotationX(float deltaX)
    {
        Vector3 deltaRotation = new Vector3(deltaX, 0, 0);
        StopCurrentTween();
        currentTween = cachedTransform.DOLocalRotate(deltaRotation, duration, RotateMode.LocalAxisAdd).SetEase(ease);
        Debug.Log($"[DOTweenHelper] {name}: X축에 {deltaX}도 상대 회전 시작");
    }

    /// <summary>
    /// Y축에 회전값 더하기 (상대적 로컬 회전)
    /// </summary>
    public void AddRotationY(float deltaY)
    {
        Vector3 deltaRotation = new Vector3(0, deltaY, 0);
        StopCurrentTween();
        currentTween = cachedTransform.DOLocalRotate(deltaRotation, duration, RotateMode.LocalAxisAdd).SetEase(ease);
        Debug.Log($"[DOTweenHelper] {name}: Y축에 {deltaY}도 상대 회전 시작");
    }

    /// <summary>
    /// Z축에 회전값 더하기 (상대적 로컬 회전)
    /// </summary>
    public void AddRotationZ(float deltaZ)
    {
        Vector3 deltaRotation = new Vector3(0, 0, deltaZ);
        StopCurrentTween();
        currentTween = cachedTransform.DOLocalRotate(deltaRotation, duration, RotateMode.LocalAxisAdd).SetEase(ease);
        Debug.Log($"[DOTweenHelper] {name}: Z축에 {deltaZ}도 상대 회전 시작");
    }

    /// <summary>
    /// Y축 연속 회전 시작 (기본 속도)
    /// </summary>
    public void StartSpinningY()
    {
        StartSpinningY(defaultSpinSpeed);
    }
    
    /// <summary>
    /// Y축 연속 회전 시작 (속도 지정) - 음수 값으로 역방향 회전 가능
    /// </summary>
    public void StartSpinningY(float speed)
    {
        // 기존 회전 효과가 있으면 중지 (둥둥 효과는 그대로 유지)
        if (spinningTween != null && spinningTween.IsActive())
        {
            spinningTween.Kill();
        }
        
        // 음수면 역방향 회전
        float rotationAmount = speed > 0 ? 360f : -360f;
        Vector3 targetRotation = new Vector3(0, rotationAmount, 0);
        
        spinningTween = cachedTransform.DOLocalRotate(targetRotation, CalculateSpinDuration(Mathf.Abs(speed)), RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart);
            
        // 호환성을 위해 currentTween도 설정
        currentTween = spinningTween;
        
        string direction = speed > 0 ? "시계방향" : "반시계방향";
        Debug.Log($"[DOTweenHelper] {name}: Y축 {direction} 스피닝 시작 ({Mathf.Abs(speed)}도/초)");
    }

    /// <summary>
    /// X축 연속 회전 시작 (기본 속도)
    /// </summary>
    public void StartSpinningX()
    {
        StartSpinningX(defaultSpinSpeed);
    }
    
    /// <summary>
    /// X축 연속 회전 시작 (속도 지정) - 음수 값으로 역방향 회전 가능
    /// </summary>
    public void StartSpinningX(float speed)
    {
        // 기존 회전 효과가 있으면 중지 (둥둥 효과는 그대로 유지)
        if (spinningTween != null && spinningTween.IsActive())
        {
            spinningTween.Kill();
        }
        
        // 음수면 역방향 회전
        float rotationAmount = speed > 0 ? 360f : -360f;
        Vector3 targetRotation = new Vector3(rotationAmount, 0, 0);
        
        spinningTween = cachedTransform.DOLocalRotate(targetRotation, CalculateSpinDuration(Mathf.Abs(speed)), RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart);
            
        currentTween = spinningTween;
        string direction = speed > 0 ? "시계방향" : "반시계방향";
        Debug.Log($"[DOTweenHelper] {name}: X축 {direction} 스피닝 시작 ({Mathf.Abs(speed)}도/초)");
    }

    /// <summary>
    /// Z축 연속 회전 시작 (기본 속도)
    /// </summary>
    public void StartSpinningZ()
    {
        StartSpinningZ(defaultSpinSpeed);
    }
    
    /// <summary>
    /// Z축 연속 회전 시작 (속도 지정) - 음수 값으로 역방향 회전 가능
    /// </summary>
    public void StartSpinningZ(float speed)
    {
        // 기존 회전 효과가 있으면 중지 (둥둥 효과는 그대로 유지)
        if (spinningTween != null && spinningTween.IsActive())
        {
            spinningTween.Kill();
        }
        
        // 음수면 역방향 회전
        float rotationAmount = speed > 0 ? 360f : -360f;
        Vector3 targetRotation = new Vector3(0, 0, rotationAmount);
        
        spinningTween = cachedTransform.DOLocalRotate(targetRotation, CalculateSpinDuration(Mathf.Abs(speed)), RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart);
            
        currentTween = spinningTween;
        string direction = speed > 0 ? "시계방향" : "반시계방향";
        Debug.Log($"[DOTweenHelper] {name}: Z축 {direction} 스피닝 시작 ({Mathf.Abs(speed)}도/초)");
    }



    /// <summary>
    /// 스피닝 중지
    /// </summary>
    public void StopSpinning()
    {
        // 회전 효과만 중지 (둥둥 효과는 그대로 유지)
        if (spinningTween != null && spinningTween.IsActive())
        {
            spinningTween.Kill();
            spinningTween = null;
        }
        
        // currentTween이 spinningTween과 같았다면 null로 설정
        if (currentTween == spinningTween)
        {
            currentTween = null;
        }
        
        Debug.Log($"[DOTweenHelper] {name}: 스피닝 중지 - 둥둥 효과는 계속 유지");
    }

    /// <summary>
    /// 펀치 회전 효과 (Y축)
    /// </summary>
    public void PunchRotationY()
    {
        StopCurrentTween();
        currentTween = cachedTransform.DOPunchRotation(new Vector3(0, 30, 0), duration, 10, 1);
        Debug.Log($"[DOTweenHelper] {name}: Y축 펀치 회전 효과 시작");
    }

    /// <summary>
    /// 펀치 회전 효과 (Z축)
    /// </summary>
    public void PunchRotationZ()
    {
        StopCurrentTween();
        currentTween = cachedTransform.DOPunchRotation(new Vector3(0, 0, 30), duration, 10, 1);
        Debug.Log($"[DOTweenHelper] {name}: Z축 펀치 회전 효과 시작");
    }

    #endregion

    #region Transform 매개변수 메서드들

    /// <summary>
    /// 지정된 Transform 위치로 이동
    /// </summary>
    public void MoveToTransform(Transform target)
    {
        if (target == null)
        {
            Debug.LogWarning($"[DOTweenHelper] {name}: MoveToTransform - target이 null입니다!");
            return;
        }
        
        StopCurrentTween();
        currentTween = cachedTransform.DOMove(target.position, duration).SetEase(ease);
        Debug.Log($"[DOTweenHelper] {name}: {target.name} 위치로 이동 시작");
    }

    /// <summary>
    /// 지정된 Transform 회전으로 회전 (로컬)
    /// </summary>
    public void RotateToTransform(Transform target)
    {
        if (target == null)
        {
            Debug.LogWarning($"[DOTweenHelper] {name}: RotateToTransform - target이 null입니다!");
            return;
        }
        
        StopCurrentTween();
        currentTween = cachedTransform.DOLocalRotate(target.localEulerAngles, duration).SetEase(ease);
        Debug.Log($"[DOTweenHelper] {name}: {target.name} 로컬 회전으로 회전 시작");
    }

    /// <summary>
    /// 지정된 Transform 방향을 바라보기 (월드 좌표계 사용)
    /// </summary>
    public void LookAtTransform(Transform target)
    {
        if (target == null)
        {
            Debug.LogWarning($"[DOTweenHelper] {name}: LookAtTransform - target이 null입니다!");
            return;
        }
        
        Vector3 direction = (target.position - cachedTransform.position).normalized;
        Vector3 lookRotation = Quaternion.LookRotation(direction).eulerAngles;
        
        StopCurrentTween();
        currentTween = cachedTransform.DORotate(lookRotation, duration).SetEase(ease);
        Debug.Log($"[DOTweenHelper] {name}: {target.name} 방향으로 회전 시작 (월드 좌표계)");
    }

    /// <summary>
    /// 지정된 Transform으로 완전 이동 (위치+회전+스케일)
    /// </summary>
    public void MoveToTransformComplete(Transform target)
    {
        if (target == null)
        {
            Debug.LogWarning($"[DOTweenHelper] {name}: MoveToTransformComplete - target이 null입니다!");
            return;
        }
        
        StopCurrentTween();
        
        // 동시에 위치, 회전, 스케일 애니메이션
        Sequence seq = DOTween.Sequence();
        seq.Join(cachedTransform.DOMove(target.position, duration).SetEase(ease));
        seq.Join(cachedTransform.DOLocalRotate(target.localEulerAngles, duration).SetEase(ease));
        seq.Join(cachedTransform.DOScale(target.localScale, duration).SetEase(ease));
        
        currentTween = seq;
        Debug.Log($"[DOTweenHelper] {name}: {target.name}으로 완전 이동 시작 (위치+회전+스케일)");
    }

    #endregion

    /// <summary>
    /// 현재 실행 중인 Tween 중지
    /// </summary>
    private void StopCurrentTween()
    {
        if (currentTween != null)
        {
            Debug.Log($"[DOTweenHelper] {name}: Tween 중지 - ID: {currentTween.GetHashCode()}, IsActive: {currentTween.IsActive()}");
            currentTween.Kill();
            currentTween = null;
        }
    }

    /// <summary>
    /// 이 Transform에 연결된 모든 DOTween 강제 중지 (문제 해결용)
    /// </summary>
    [ContextMenu("🛑 모든 DOTween 강제 중지")]
    public void KillAllTweens()
    {
        // 개별 Tween들 중지
        if (floatingTween != null && floatingTween.IsActive())
        {
            floatingTween.Kill();
            floatingTween = null;
        }
        
        if (spinningTween != null && spinningTween.IsActive())
        {
            spinningTween.Kill();
            spinningTween = null;
        }
        
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
            currentTween = null;
        }
        
        // 이 Transform에 연결된 모든 Tween 강제 중지
        if (cachedTransform != null)
        {
            DOTween.Kill(cachedTransform);
            Debug.Log($"[DOTweenHelper] {name}: Transform의 모든 DOTween 강제 중지");
        }
        
        // 이 GameObject에 연결된 모든 Tween 강제 중지
        DOTween.Kill(gameObject);
        Debug.Log($"[DOTweenHelper] {name}: GameObject의 모든 DOTween 강제 중지");
        
        isFloating = false;
    }

    /// <summary>
    /// DOTween 상태 확인 (간단 진단용)
    /// </summary>
    [ContextMenu("🔍 상태 확인")]
    public void CheckStatus()
    {
        Debug.Log($"=== DOTween 상태: {name} ===");
        Debug.Log($"활성화: {gameObject.activeInHierarchy}");
        Debug.Log($"스피닝 중: {(currentTween != null && currentTween.IsActive())}");
        Debug.Log($"스핀 속도: {defaultSpinSpeed}도/초");
        Debug.Log($"전체 활성 Tween 수: {DOTween.TotalPlayingTweens()}");
        Debug.Log("========================");
    }

    /// <summary>
    /// 스피닝 속도 실시간 체크 (문제 진단용)
    /// </summary>
    [ContextMenu("🔄 스피닝 속도 체크")]
    public void CheckSpinSpeed()
    {
        if (spinningTween == null || !spinningTween.IsActive())
        {
            Debug.Log($"[DOTweenHelper] {name}: 현재 스피닝 중이 아닙니다");
            return;
        }
        
        Debug.Log($"=== 스피닝 속도 체크: {name} ===");
        Debug.Log($"기본 속도: {defaultSpinSpeed}도/초");
        Debug.Log($"Tween ID: {spinningTween.GetHashCode()}");
        Debug.Log($"Tween 활성 상태: {spinningTween.IsActive()}");
        Debug.Log($"Tween 재생 중: {spinningTween.IsPlaying()}");
        Debug.Log($"현재 Y축 각도: {cachedTransform.localEulerAngles.y:F1}도");
        Debug.Log("==============================");
    }

    /// <summary>
    /// 빠른 속도로 Y축 스피닝 (360도/초)
    /// </summary>
    public void SetFastSpin()
    {
        StartSpinningY(360f);
    }

    /// <summary>
    /// 보통 속도로 Y축 스피닝 (180도/초)
    /// </summary>
    public void SetNormalSpin()
    {
        StartSpinningY(180f);
    }

    /// <summary>
    /// 느린 속도로 Y축 스피닝 (90도/초)
    /// </summary>
    public void SetSlowSpin()
    {
        StartSpinningY(90f);
    }

    /// <summary>
    /// 둥둥 효과와 회전 효과를 동시에 시작
    /// </summary>
    [ContextMenu("🎯 둥둥+회전 동시 시작")]
    public void StartFloatingAndSpinning()
    {
        StartFloating();
        StartSpinningY();
        Debug.Log($"[DOTweenHelper] {name}: 둥둥 효과와 회전 효과 동시 시작!");
    }

    /// <summary>
    /// 모든 효과 중지
    /// </summary>
    [ContextMenu("⏹️ 모든 효과 중지")]
    public void StopAllEffects()
    {
        StopFloating();
        StopSpinning();
        Debug.Log($"[DOTweenHelper] {name}: 모든 효과 중지");
    }

    /// <summary>
    /// 현재 스피닝을 기본 속도로 재시작
    /// </summary>
    private void RestartCurrentSpin()
    {
        if (spinningTween == null || !spinningTween.IsActive()) return;

        // 스피닝 중지 후 기본 속도로 재시작
        StopSpinning();
        StartSpinningY();
        Debug.Log($"[DOTweenHelper] {name}: 스피닝 재시작 완료 ({defaultSpinSpeed}도/초)");
    }

    private void OnDestroy()
    {
        // DOTween 메모리 누수 방지
        KillAllTweens();
        Debug.Log($"[DOTweenHelper] {name}: OnDestroy - 모든 Tween 정리 완료");
    }
} 