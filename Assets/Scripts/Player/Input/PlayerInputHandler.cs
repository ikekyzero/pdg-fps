using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }

    public bool JumpPressed { get; private set; }

    public bool DashPressed { get; private set; }

    public bool SprintHeld { get; private set; }

    public bool CrouchHeld { get; private set; }   

    public Vector2 LookInput { get; private set; }

    private PlayerInputActions inputActions;
    
    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Enable();

        inputActions.Gameplay.Move.performed += OnMove;
        inputActions.Gameplay.Move.canceled += OnMoveCanceled;

        inputActions.Gameplay.Jump.performed += OnJump;

        inputActions.Gameplay.Dash.performed += OnDash;

        inputActions.Gameplay.Sprint.performed += OnSprintStarted;
        inputActions.Gameplay.Sprint.canceled += OnSprintCanceled;

        inputActions.Gameplay.Crouch.performed += OnCrouchStarted;
        inputActions.Gameplay.Crouch.canceled += OnCrouchCanceled;

        inputActions.Gameplay.Look.performed += OnLook;
        inputActions.Gameplay.Look.canceled += OnLookCanceled;
    }

    private void OnDisable()
    {
        inputActions.Gameplay.Move.performed -= OnMove;
        inputActions.Gameplay.Move.canceled -= OnMoveCanceled;

        inputActions.Gameplay.Jump.performed -= OnJump;

        inputActions.Gameplay.Dash.performed -= OnDash;

        inputActions.Gameplay.Sprint.performed -= OnSprintStarted;
        inputActions.Gameplay.Sprint.canceled -= OnSprintCanceled;

        inputActions.Gameplay.Crouch.performed -= OnCrouchStarted;
        inputActions.Gameplay.Crouch.canceled -= OnCrouchCanceled;

        inputActions.Gameplay.Look.performed -= OnLook;
        inputActions.Gameplay.Look.canceled -= OnLookCanceled;

        inputActions.Disable();
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        MoveInput = Vector2.zero;
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        JumpPressed = true;
    }

    private void OnDash(InputAction.CallbackContext context)
    {
        DashPressed = true;
    }

    private void OnSprintStarted(InputAction.CallbackContext context)
    {
        SprintHeld = true;
    }

    private void OnSprintCanceled(InputAction.CallbackContext context)
    {
        SprintHeld = false;
    }

    private void OnCrouchStarted(InputAction.CallbackContext context)
    {
        CrouchHeld = true;
    }

    private void OnCrouchCanceled(InputAction.CallbackContext context)
    {
        CrouchHeld = false;
    }

    public void ConsumeJump()
    {
        JumpPressed = false;
    }

    public void ConsumeDash()
    {
        DashPressed = false;
    }
    private void OnLook(InputAction.CallbackContext context)
    {
        LookInput = context.ReadValue<Vector2>();
    }
    private void OnLookCanceled(InputAction.CallbackContext context)
    {
        LookInput = Vector2.zero;
    }
}