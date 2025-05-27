// PlayerCameraController.cs
using UnityEngine;
using DG.Tweening;

public class PlayerCameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform cameraTransform;
    public Transform defaultAnchor;

    private Tween currentTween;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (cameraTransform == null)
        {
            var cam = GetComponentInChildren<Camera>();
            if (cam != null) cameraTransform = cam.transform;
        }
        if (defaultAnchor == null)
        {
            var anchor = transform.Find("PlayerCameraAnchor");
            if (anchor != null) defaultAnchor = anchor;
        }
    }
#endif

    private void Awake()
    {
        // Fallback assignment
        if (cameraTransform == null)
            cameraTransform = GetComponentInChildren<Camera>()?.transform;
        if (defaultAnchor == null)
            defaultAnchor = transform.Find("PlayerCameraAnchor");
    }

    /// <summary>Move camera to a target transform.</summary>
    public void MoveTo(Transform target, float duration = 0.5f)
    {
        if (cameraTransform == null || target == null) return;
        PlayerController.Instance.ToggleUI(true);
        KillTween();
        Debug.Log($"[Camera] MoveTo target:{target.name}, duration:{duration}");
        currentTween = DOTween.Sequence()
            .Append(cameraTransform.DOMove(target.position, duration).SetEase(Ease.InOutSine))
            .Join(cameraTransform.DORotateQuaternion(target.rotation, duration).SetEase(Ease.InOutSine))
            .OnKill(() => currentTween = null);
    }

    /// <summary>Move camera to specified world position & rotation.</summary>
    public void MoveTo(Vector3 worldPosition, Quaternion worldRotation, float duration = 0.5f)
    {
        if (cameraTransform == null) return;
        PlayerController.Instance.ToggleUI(true);
        KillTween();
        Debug.Log($"[Camera] MoveTo pos:{worldPosition}, rot:{worldRotation.eulerAngles}, duration:{duration}");
        currentTween = DOTween.Sequence()
            .Append(cameraTransform.DOMove(worldPosition, duration).SetEase(Ease.InOutSine))
            .Join(cameraTransform.DORotateQuaternion(worldRotation, duration).SetEase(Ease.InOutSine))
            .OnKill(() => currentTween = null);
    }

    /// <summary>Reset camera back to default anchor.</summary>
    public void ResetToDefault(float duration = 0.5f, bool unlockUI = true)
    {
        if (cameraTransform == null || defaultAnchor == null) return;
        PlayerController.Instance.ToggleUI(true);
        KillTween();
        Debug.Log($"[Camera] ResetToDefault duration:{duration}");
        currentTween = DOTween.Sequence()
            .Append(cameraTransform.DOMove(defaultAnchor.position, duration).SetEase(Ease.InOutSine))
            .Join(cameraTransform.DORotateQuaternion(defaultAnchor.rotation, duration).SetEase(Ease.InOutSine))
            .OnComplete(() =>
            {
                // Sync pitch to prevent jerk
                float e = defaultAnchor.localRotation.eulerAngles.x;
                float pitch = e > 180f ? e - 360f : e;
                PlayerController.Instance?.SetCameraPitch(pitch);
                if (unlockUI) PlayerController.Instance.ToggleUI(false);
            })
            .OnKill(() => currentTween = null);
    }

    public void ShakeCamera(float duration = 0.2f, float strength = 0.1f)
    {
        if (cameraTransform == null)
        {
            Debug.LogWarning("[CameraController] cameraTransform이 할당되지 않았습니다.");
            return;
        }

        // DOTween으로 위치 기반 흔들기
        cameraTransform
            .DOShakePosition(duration, strength)
            .SetEase(Ease.Linear);

        Debug.Log($"[CameraController] ShakeCamera 호출 → duration: {duration}s, strength: {strength}");
    }

    private void KillTween()
    {
        if (currentTween != null && currentTween.IsActive())
            currentTween.Kill();
    }   
}