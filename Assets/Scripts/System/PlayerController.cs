using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance; // 싱글톤 인스턴스

    public float moveSpeed = 5f;           // 이동 속도
    public float lookSpeed = 1.5f;           // 회전 속도
    public float jumpForce = 1f;           // 점프 힘
    public float gravity = -9.81f;         // 중력 값

    private CharacterController characterController;
    private Vector3 velocity;               // 속도 벡터
    private bool isGrounded;                // 지면에 닿아있는지 확인
    private Transform cameraTransform;      // 카메라 Transform
    private float xRotation = 0f;           // 상하 회전 각도

    private bool isUIActive = false;        // UI 활성 상태를 나타내는 변수

    private void Awake()
    {
        // 싱글톤 구현
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // 이미 인스턴스가 있다면 중복 제거
        }

        characterController = GetComponent<CharacterController>();
        cameraTransform = Camera.main.transform;

        // 커서 숨기기 및 잠금
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (isUIActive)
        {
            return; // UI가 활성화된 동안 이동 및 회전 비활성화
        }

        MovePlayer();
        RotatePlayer();
        RotateCamera();
        JumpPlayer();
    }

    /// UI 활성화/비활성화 토글 함수
    public void ToggleUI(bool active)
    {
        isUIActive = active;

        if (isUIActive)
        {
            // UI 활성화: 커서를 보이게 하고 잠금 해제
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // UI 비활성화: 커서를 숨기고 잠금
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /// 플레이어 이동 함수
    private void MovePlayer()
    {
        isGrounded = characterController.isGrounded;  // 지면에 닿아있는지 확인

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // 지면에 닿아있을 때 속도를 리셋
        }

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        characterController.Move(move * moveSpeed * Time.deltaTime);
    }

    /// 플레이어 회전 함수
    private void RotatePlayer()
    {
        float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
        transform.Rotate(Vector3.up * mouseX);  // 캐릭터의 좌우 회전
    }

    /// 카메라 회전 함수
    private void RotateCamera()
    {
        float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // 각도 제한

        // 카메라 상하 회전
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    /// 점프 및 중력 처리 함수
    private void JumpPlayer()
    {
        // 점프 입력 처리
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }

        // 중력 적용
        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }
}
