using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("External References")]
    public PlayerCameraController cam;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float lookSpeed = 1.5f;
    public float jumpForce = 1f;
    public float gravity = -9.81f;

    private CharacterController characterController;
    private Vector3 velocity;
    private bool isGrounded;
    private float xRotation = 0f;
    private bool isUIActive = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        characterController = GetComponent<CharacterController>();

        if (cam == null)
        {
            cam = GetComponent<PlayerCameraController>();
            if (cam == null)
                Debug.LogError("[PlayerController] PlayerCameraController component missing.");
        }

        ToggleUI(false);
    }

    private void FixedUpdate()
    {
        if (isUIActive) return;

        MovePlayer();
        RotatePlayer();
        RotateCamera();
        JumpAndApplyGravity();
    }

    public void ToggleUI(bool active)
    {
        isUIActive = active;
        Cursor.lockState = active ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = active;
        Debug.Log($"[PlayerController] UI Active: {active}");
    }

    private void MovePlayer()
    {
        isGrounded = characterController.isGrounded;
        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        characterController.Move(move * moveSpeed * Time.deltaTime);
    }

    // DeltaTime applied to rotation for frame-rate independent yaw
    private void RotatePlayer()
    {
        float mouseX = Input.GetAxis("Mouse X") * lookSpeed * Time.deltaTime;
        transform.Rotate(0f, mouseX, 0f);
    }

    private void RotateCamera()
    {
        float mouseY = Input.GetAxis("Mouse Y") * lookSpeed * Time.deltaTime;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        if (cam != null && cam.cameraTransform != null)
            cam.cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    private void JumpAndApplyGravity()
    {
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            Debug.Log("[PlayerController] Jump initiated, velocity.y=" + velocity.y);
        }

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
        if (isGrounded && Mathf.Approximately(velocity.y, -2f))
            Debug.Log("[PlayerController] Landed");
    }


    public bool IsUIActive()
    {
        return isUIActive;
    }
    public void SetCameraPitch(float pitch)
    {
        xRotation = Mathf.Clamp(pitch, -90f, 90f);
        Debug.Log($"[PlayerController] Camera pitch synced: {xRotation}");
    }
}
