using UnityEngine;

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
        if (isUIActive) return;

        CacheInputs();
        HandleMouseLook();
        HandleJumpInput();
    }

    private void FixedUpdate()
    {
        if (isUIActive) return;

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
            Debug.Log($"[PlayerController] Jump initiated, velocity.y = {velocity.y:F2}");
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
            Debug.Log("[PlayerController] Landed");
        }
    }

    public void ToggleUI(bool active)
    {
        isUIActive = active;
        Cursor.lockState = active ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = active;
        OnUIToggled?.Invoke(active);
        Debug.Log($"[PlayerController] UI Active: {active}");
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
        Debug.Log($"[PlayerController] Camera pitch synced: {xRotation:F1}°");
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

    private void OnDestroy()
    {
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