using System;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    // Axis inputs
    public event Action<Vector2> OnMoveAxis;
    public event Action<Vector2> OnLookAxis;

    // Button presses
    public event Action OnJump;
    public event Action OnConfirm;      // e.g. Space, Enter
    public event Action OnCancel;       // e.g. Esc

    // Action inputs
    public event Action OnPrimaryAction;            // e.g. Left click
    public event Action OnSecondaryActionLeft;      // e.g. Q
    public event Action OnSecondaryActionRight;     // e.g. Right click
    public event Action<float> OnScroll;            // Mouse scroll wheel
    public event Action OnSummon;                   // e.g. custom key

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        // Axis polling
        Vector2 move = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        if (move.sqrMagnitude > 0f) OnMoveAxis?.Invoke(move);

        Vector2 look = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        if (look.sqrMagnitude > 0f) OnLookAxis?.Invoke(look);

        // Buttons
        if (Input.GetButtonDown("Jump")) OnJump?.Invoke();
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) OnConfirm?.Invoke();
        if (Input.GetKeyDown(KeyCode.Escape)) OnCancel?.Invoke();

        // Actions
        if (Input.GetMouseButtonDown(0)) OnPrimaryAction?.Invoke();
        if (Input.GetKeyDown(KeyCode.Q)) OnSecondaryActionLeft?.Invoke();
        if (Input.GetMouseButtonDown(1)) OnSecondaryActionRight?.Invoke();

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f) OnScroll?.Invoke(scroll);

        if (Input.GetKeyDown(KeyCode.F)) OnSummon?.Invoke();
    }
}