using UnityEngine;
using DG.Tweening;

public class PlayerCameraController : MonoBehaviour
{
    private const string LOG_PREFIX = "[PlayerCameraController]";

    [Header("Camera Settings")]
    public Transform cameraTransform;
    public Transform defaultAnchor;

    private Tween currentTween;

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
        Debug.Log($"{LOG_PREFIX} Awake: cameraTransform={cameraTransform}, defaultAnchor={defaultAnchor}");
    }

    public void MoveTo(Transform target, float duration = 0.5f)
    {
        if (cameraTransform == null || target == null) return;
        Debug.Log($"{LOG_PREFIX} MoveTo(transform): target={target.name}, duration={duration}");
        PlayerController.Instance.ToggleUI(true);
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
}
