using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;           // 이동 속도
    public float lookSpeed = 2f;           // 회전 속도
    public float jumpForce = 1f;           // 점프 힘
    public float gravity = -9.81f;         // 중력 값

    private CharacterController characterController;
    private Vector3 velocity;               // 속도 벡터
    private bool isGrounded;                // 지면에 닿아있는지 확인
    private Transform cameraTransform;      // 카메라 Transform
    private float xRotation = 0f;           // 상하 회전 각도

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        cameraTransform = Camera.main.transform;

        // 커서 숨기기 및 잠금
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        MovePlayer();
        RotatePlayer();
        RotateCamera();
        JumpPlayer();
    }

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

    private void RotatePlayer()
    {
        float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
        transform.Rotate(Vector3.up * mouseX);  // 캐릭터의 좌우 회전
    }

    private void RotateCamera()
    {
        float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // 각도 제한

        // 카메라 상하 회전
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

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
