using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }
    
    // 이벤트 시스템
    public static event System.Action<bool> OnUIToggled;
    public static event System.Action OnJumped;
    public static event System.Action OnLanded;

    [Header("External References")]
    public PlayerCameraController cam;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float lookSpeed = 1.5f;
    public float jumpForce = 8f;
    public float gravity = -9.81f;
    
    [Header("Ground Settings")]
    public float groundedVelocity = -2f;
    public float groundCheckDistance = 0.1f;
    
    [Header("Camera Settings")]
    public float minCameraPitch = -90f;
    public float maxCameraPitch = 90f;

    private CharacterController characterController;
    private Vector3 velocity;
    private bool isGrounded;
    private bool wasGrounded;
    private float xRotation = 0f;
    private bool isUIActive = false;
    
    // 입력 캐싱
    private Vector2 moveInput;
    private Vector2 mouseInput;
    private bool jumpInput;
    
    // DOTween 이동 관련
    private bool isDOTweenMoving = false;
    private Tween currentMoveTween;
    
    private const string LOG_PREFIX = "[PlayerController]";

    private void Awake()
    {
        InitializeSingleton();
        InitializeComponents();
        ToggleUI(false);
    }

    private void InitializeSingleton()
    {
        if (Instance == null) 
        {
            Instance = this;
        }
        else 
        { 
            Destroy(gameObject); 
            return; 
        }
    }

    private void InitializeComponents()
    {
        characterController = GetComponent<CharacterController>();

        if (cam == null)
        {
            cam = GetComponent<PlayerCameraController>();
            if (cam == null)
                Debug.LogError("[PlayerController] PlayerCameraController component missing.");
        }
    }

    private void Update()
    {
        if (isUIActive || isDOTweenMoving) return;  // DOTween 이동 중에는 입력 무시

        CacheInputs();
        HandleMouseLook();
        HandleJumpInput();
    }

    private void FixedUpdate()
    {
        if (isUIActive || isDOTweenMoving) return;  // DOTween 이동 중에는 물리 이동 무시

        CheckGroundedState();
        MovePlayer();
        ApplyGravityAndJump();
        CheckLandingState();
    }

    private void CacheInputs()
    {
        moveInput.x = Input.GetAxis("Horizontal");
        moveInput.y = Input.GetAxis("Vertical");
        mouseInput.x = Input.GetAxis("Mouse X");
        mouseInput.y = Input.GetAxis("Mouse Y");
        jumpInput = Input.GetButtonDown("Jump");
    }

    private void HandleMouseLook()
    {
        // 플레이어 좌우 회전 (Y축)
        float mouseX = mouseInput.x * lookSpeed;
        transform.Rotate(0f, mouseX, 0f);

        // 카메라 상하 회전 (X축)
        float mouseY = mouseInput.y * lookSpeed;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minCameraPitch, maxCameraPitch);

        if (cam?.cameraTransform != null)
        {
            cam.cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }

    private void HandleJumpInput()
    {
        if (isGrounded && jumpInput)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            OnJumped?.Invoke();
            Debug.Log($"{LOG_PREFIX} Jump initiated, velocity.y = {velocity.y:F2}");
        }
    }

    private void CheckGroundedState()
    {
        wasGrounded = isGrounded;
        isGrounded = characterController.isGrounded;

        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = groundedVelocity;
        }
    }

    private void MovePlayer()
    {
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        move = Vector3.ClampMagnitude(move, 1f); // 대각선 이동 속도 정규화
        characterController.Move(move * moveSpeed * Time.fixedDeltaTime);
    }

    private void ApplyGravityAndJump()
    {
        velocity.y += gravity * Time.fixedDeltaTime;
        characterController.Move(velocity * Time.fixedDeltaTime);
    }

    private void CheckLandingState()
    {
        if (!wasGrounded && isGrounded)
        {
            OnLanded?.Invoke();
            Debug.Log($"{LOG_PREFIX} Landed");
        }
    }

    public void ToggleUI(bool active)
    {
        isUIActive = active;
        Cursor.lockState = active ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = active;
        OnUIToggled?.Invoke(active);
        Debug.Log($"{LOG_PREFIX} UI Active: {active}");
    }

    public bool IsUIActive()
    {
        return isUIActive;
    }

    public void SetCameraPitch(float pitch)
    {
        xRotation = Mathf.Clamp(pitch, minCameraPitch, maxCameraPitch);
        if (cam?.cameraTransform != null)
        {
            cam.cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
        Debug.Log($"{LOG_PREFIX} Camera pitch synced: {xRotation:F1}°");
    }

    public float GetCameraPitch()
    {
        return xRotation;
    }

    public bool IsGrounded()
    {
        return isGrounded;
    }

    public Vector3 GetVelocity()
    {
        return velocity;
    }

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = Mathf.Max(0f, speed);
    }

    public void SetLookSensitivity(float sensitivity)
    {
        lookSpeed = Mathf.Max(0f, sensitivity);
    }

    // 외부에서 플레이어를 강제로 이동시킬 때 사용
    public void AddForce(Vector3 force)
    {
        velocity += force;
    }

    // 텔레포트 등에서 사용
    public void ResetVelocity()
    {
        velocity = Vector3.zero;
    }

    #region DOTween 이동 메서드들

    /// <summary>
    /// Player를 DOTween으로 안전하게 이동 (기본 시간 1초)
    /// </summary>
    public void DOTweenMoveTo(Vector3 targetPosition)
    {
        DOTweenMoveTo(targetPosition, 1f);
    }

    /// <summary>
    /// Player를 DOTween으로 안전하게 이동 (커스텀 시간)
    /// </summary>
    public void DOTweenMoveTo(Vector3 targetPosition, float duration)
    {
        if (isDOTweenMoving)
        {
            Debug.LogWarning($"{LOG_PREFIX} DOTweenMoveTo: 이미 이동 중입니다");
            return;
        }

        Debug.Log($"{LOG_PREFIX} DOTweenMoveTo: {targetPosition}로 이동 시작 (시간: {duration}초)");
        
        StopCurrentMoveTween();
        isDOTweenMoving = true;
        
        // 현재 속도 초기화
        ResetVelocity();
        
        currentMoveTween = transform.DOMove(targetPosition, duration)
            .SetEase(Ease.InOutSine)
            .OnComplete(() => {
                isDOTweenMoving = false;
                Debug.Log($"{LOG_PREFIX} DOTweenMoveTo: 이동 완료");
            })
            .OnKill(() => {
                isDOTweenMoving = false;
                Debug.Log($"{LOG_PREFIX} DOTweenMoveTo: 이동 중단됨");
            });
    }

    /// <summary>
    /// Player를 X 좌표로 이동 (UnityEvent용)
    /// </summary>
    public void DOTweenMoveToX(float x)
    {
        Vector3 targetPosition = new Vector3(x, transform.position.y, transform.position.z);
        DOTweenMoveTo(targetPosition);
    }

    /// <summary>
    /// Player를 Y 좌표로 이동 (UnityEvent용)
    /// </summary>
    public void DOTweenMoveToY(float y)
    {
        Vector3 targetPosition = new Vector3(transform.position.x, y, transform.position.z);
        DOTweenMoveTo(targetPosition);
    }

    /// <summary>
    /// Player를 Z 좌표로 이동 (UnityEvent용)
    /// </summary>
    public void DOTweenMoveToZ(float z)
    {
        Vector3 targetPosition = new Vector3(transform.position.x, transform.position.y, z);
        DOTweenMoveTo(targetPosition);
    }

    /// <summary>
    /// Player를 다른 Transform 위치로 이동
    /// </summary>
    public void DOTweenMoveToTransform(Transform target)
    {
        if (target == null)
        {
            Debug.LogWarning($"{LOG_PREFIX} DOTweenMoveToTransform: target이 null입니다");
            return;
        }
        
        DOTweenMoveTo(target.position);
    }

    /// <summary>
    /// 현재 DOTween 이동 중단
    /// </summary>
    public void StopDOTweenMove()
    {
        StopCurrentMoveTween();
        Debug.Log($"{LOG_PREFIX} StopDOTweenMove: 이동 중단");
    }

    /// <summary>
    /// DOTween 이동 중인지 확인
    /// </summary>
    public bool IsMovingWithDOTween()
    {
        return isDOTweenMoving;
    }

    private void StopCurrentMoveTween()
    {
        if (currentMoveTween != null && currentMoveTween.IsActive())
        {
            currentMoveTween.Kill();
        }
        currentMoveTween = null;
        isDOTweenMoving = false;
    }

    #endregion

    private void OnDestroy()
    {
        StopCurrentMoveTween();  // DOTween 정리
        
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // 디버그용 기즈모
    private void OnDrawGizmosSelected()
    {
        if (characterController != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Vector3 center = transform.position + characterController.center;
            Gizmos.DrawWireSphere(center - Vector3.up * (characterController.height * 0.5f + groundCheckDistance), 0.1f);
        }
    }
}