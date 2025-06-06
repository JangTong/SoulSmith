using UnityEngine;
using DG.Tweening;

public class PlayerCameraController : MonoBehaviour
{
    private const string LOG_PREFIX = "[PlayerCameraController]";

    [Header("Camera Settings")]
    public Transform cameraTransform;
    public Transform defaultAnchor;
    
    [Header("Orthographic Settings")]
    public bool enableOrthographicMode = false; // 직교 투영 모드 활성화 여부
    public float orthographicSize = 3f; // 직교 투영 크기

    private Tween currentTween;
    private Camera camera;
    
    // 원래 카메라 설정 저장
    private bool originalOrthographic;
    private float originalFieldOfView;
    private float originalOrthographicSize;

#if UNITY_EDITOR
    private void OnValidate()
    {
        Debug.Log($"{LOG_PREFIX} OnValidate: Validating camera settings");
        if (cameraTransform == null)
            cameraTransform = GetComponentInChildren<Camera>()?.transform;
        if (defaultAnchor == null)
            defaultAnchor = transform.Find("PlayerCameraAnchor");
    }
#endif

    private void Awake()
    {
        Debug.Log($"{LOG_PREFIX} Awake: Initializing");
        // Fallback assignment
        if (cameraTransform == null)
            cameraTransform = GetComponentInChildren<Camera>()?.transform;
        if (defaultAnchor == null)
            defaultAnchor = transform.Find("PlayerCameraAnchor");
            
        // 카메라 컴포넌트 가져오기
        camera = cameraTransform?.GetComponent<Camera>();
        if (camera == null)
            camera = Camera.main;
            
        // 원래 카메라 설정 저장
        if (camera != null)
        {
            originalOrthographic = camera.orthographic;
            originalFieldOfView = camera.fieldOfView;
            originalOrthographicSize = camera.orthographicSize;
        }
            
        Debug.Log($"{LOG_PREFIX} Awake: cameraTransform={cameraTransform}, defaultAnchor={defaultAnchor}, camera={camera}");
    }

    public void MoveTo(Transform target, float duration = 0.5f)
    {
        if (cameraTransform == null || target == null) return;
        Debug.Log($"{LOG_PREFIX} MoveTo(transform): target={target.name}, duration={duration}");
        PlayerController.Instance.ToggleUI(true);
        
        // 직교 투영 모드 활성화
        if (enableOrthographicMode)
            SetOrthographicProjection();
            
        KillTween();
        currentTween = DOTween.Sequence()
            .Append(cameraTransform.DOMove(target.position, duration).SetEase(Ease.InOutSine))
            .Join(cameraTransform.DORotateQuaternion(target.rotation, duration).SetEase(Ease.InOutSine))
            .OnKill(() => currentTween = null);
    }

    public void MoveTo(Vector3 worldPosition, Quaternion worldRotation, float duration = 0.5f)
    {
        if (cameraTransform == null) return;
        Debug.Log($"{LOG_PREFIX} MoveTo(world): position={worldPosition}, rotation={worldRotation.eulerAngles}, duration={duration}");
        PlayerController.Instance.ToggleUI(true);
        
        // 직교 투영 모드 활성화
        if (enableOrthographicMode)
            SetOrthographicProjection();
            
        KillTween();
        currentTween = DOTween.Sequence()
            .Append(cameraTransform.DOMove(worldPosition, duration).SetEase(Ease.InOutSine))
            .Join(cameraTransform.DORotateQuaternion(worldRotation, duration).SetEase(Ease.InOutSine))
            .OnKill(() => currentTween = null);
    }

    public void ResetToDefault(float duration = 0.5f, bool unlockUI = true)
    {
        if (cameraTransform == null || defaultAnchor == null) return;
        Debug.Log($"{LOG_PREFIX} ResetToDefault: duration={duration}, unlockUI={unlockUI}");
        
        // 직교 투영 모드 복원
        if (enableOrthographicMode)
            RestorePerspectiveProjection();
            
        PlayerController.Instance.ToggleUI(true);
        KillTween();
        currentTween = DOTween.Sequence()
            .Append(cameraTransform.DOMove(defaultAnchor.position, duration).SetEase(Ease.InOutSine))
            .Join(cameraTransform.DORotateQuaternion(defaultAnchor.rotation, duration).SetEase(Ease.InOutSine))
            .OnComplete(() =>
            {
                // Sync pitch to prevent jerk
                float e = defaultAnchor.localRotation.eulerAngles.x;
                float pitch = e > 180f ? e - 360f : e;
                Debug.Log($"{LOG_PREFIX} ResetToDefault OnComplete: pitch={pitch}");
                PlayerController.Instance?.SetCameraPitch(pitch);
                if (unlockUI)
                {
                    Debug.Log($"{LOG_PREFIX} ResetToDefault OnComplete: Unlocking UI");
                    PlayerController.Instance.ToggleUI(false);
                }
            })
            .OnKill(() => currentTween = null);
    }

    public void ShakeCamera(float duration = 0.2f, float strength = 0.1f)
    {
        if (cameraTransform == null)
        {
            Debug.LogWarning($"{LOG_PREFIX} ShakeCamera: cameraTransform is not assigned.");
            return;
        }

        Debug.Log($"{LOG_PREFIX} ShakeCamera: duration={duration}, strength={strength}");
        KillTween();
        currentTween = cameraTransform
            .DOShakePosition(duration, strength)
            .SetEase(Ease.Linear)
            .OnKill(() => currentTween = null);
    }

    private void KillTween()
    {
        if (currentTween != null && currentTween.IsActive())
        {
            Debug.Log($"{LOG_PREFIX} KillTween: Killing existing tween");
            currentTween.Kill();
        }
    }

    // 직교 투영으로 변경
    private void SetOrthographicProjection()
    {
        if (camera == null) return;
        
        Debug.Log($"{LOG_PREFIX} SetOrthographicProjection: 직교 투영 활성화 (크기: {orthographicSize})");
        camera.orthographic = true;
        camera.orthographicSize = orthographicSize;
    }
    
    // 원근 투영으로 복원
    private void RestorePerspectiveProjection()
    {
        if (camera == null) return;
        
        Debug.Log($"{LOG_PREFIX} RestorePerspectiveProjection: 원근 투영 복원 (FOV: {originalFieldOfView})");
        camera.orthographic = originalOrthographic;
        camera.orthographicSize = originalOrthographicSize;
        camera.fieldOfView = originalFieldOfView;
    }
    
    // 외부에서 직교 투영 모드를 제어할 수 있는 함수들
    public void EnableOrthographicMode(float size = -1f)
    {
        enableOrthographicMode = true;
        if (size > 0f) orthographicSize = size;
        SetOrthographicProjection();
        Debug.Log($"{LOG_PREFIX} EnableOrthographicMode: 직교 투영 모드 활성화됨");
    }
    
    public void DisableOrthographicMode()
    {
        enableOrthographicMode = false;
        RestorePerspectiveProjection();
        Debug.Log($"{LOG_PREFIX} DisableOrthographicMode: 직교 투영 모드 비활성화됨");
    }
    
    // === UnityEvent용 간단한 wrapper 함수들 ===
    
    public void MoveToTargetEvent(Transform target)
    {
        MoveTo(target, 0.5f);
    }
    
    public void ResetCameraEvent()
    {
        ResetToDefault(0.5f, true);
    }
    
    public void ShakeCameraEvent()
    {
        ShakeCamera(0.2f, 0.1f);
    }
}
