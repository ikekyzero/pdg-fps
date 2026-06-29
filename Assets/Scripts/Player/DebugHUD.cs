using UnityEngine;
using UnityEngine.UI;

public class DebugHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Text debugText;

    [SerializeField] private Slider staminaBar1;
    [SerializeField] private Slider staminaBar2;
    [SerializeField] private Slider staminaBar3;

    private PlayerMotor motor;
    private PlayerMovement movement;
    private PlayerStamina stamina;
    private PlayerInputHandler input;

    private float fps;
    private float frameTimeMs;

    private float fpsTimer;
    private int fpsFrames;
    
    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        movement = GetComponent<PlayerMovement>();
        stamina = GetComponent<PlayerStamina>();    
        input = GetComponent<PlayerInputHandler>();
    }

    private void Update()
    {
        UpdateDebugText();
        UpdateStaminaBars();
    }

    private void UpdateDebugText()
    {
        Vector3 velocity = motor.Velocity;

        Vector3 horizontalVelocity = new Vector3(
            velocity.x,
            0f,
            velocity.z);
        fpsFrames++;
        fpsTimer += Time.unscaledDeltaTime;

        if (fpsTimer >= 0.25f)
        {
            fps = fpsFrames / fpsTimer;
            frameTimeMs = 1000f / fps;

            fpsFrames = 0;
            fpsTimer = 0f;
        }
        
        debugText.text =
            $"\n=== PERFORMANCE ===" +
            $"\nFPS: {fps:F0}" +
            $"\nFrame Time: {frameTimeMs:F2} ms" +
            $"\n\n=== STATE ===" +
            $"\nState: {movement.CurrentState}" +
            "\n\n=== INPUT ===\n" +
            $"Move X: {input.MoveInput.x:F2}\n" +
            $"Move Y: {input.MoveInput.y:F2}\n" +
            $"Look X: {input.LookInput.x:F2}\n" +
            $"Look Y: {input.LookInput.y:F2}\n" +
            $"Sprint: {input.SprintHeld}\n" +
            $"Crouch: {input.CrouchHeld}\n\n" +

            "=== MOVEMENT ===\n" +
            $"Speed: {movement.CurrentSpeed:F2}\n" +
            $"Horizontal Speed: {horizontalVelocity.magnitude:F2}\n" +
            $"Vertical Speed: {velocity.y:F2}\n\n" +

            "=== STATE ===\n" +
            $"Grounded: {movement.IsGrounded}\n" +
            $"Motor Grounded: {motor.IsGrounded}\n" +
            $"Running: {movement.IsRunning}\n" +
            $"Sliding: {movement.IsSliding}\n" +
            $"WallJump: {movement.IsWallJumping}\n" +
            $"Touching Wall: {motor.IsTouchingWall}\n\n" +

            "=== COLLISION ===\n" +
            $"Collision Flags: {motor.LastCollisionFlags}\n" +
            $"Wall Normal: {motor.GetWallNormal()}\n\n" +

            "=== STAMINA ===\n" +
            $"Current: {stamina.CurrentStamina:F2}/{stamina.MaxStamina}\n\n" +

            "=== PERFORMANCE ===\n" +
            $"FPS: {(1f / Time.unscaledDeltaTime):F0}";
    }
    private void UpdateStaminaBars()
    {
        float value = stamina.CurrentStamina;

        staminaBar1.value =
            Mathf.Clamp01(value);

        staminaBar2.value =
            Mathf.Clamp01(value - 1f);

        staminaBar3.value =
            Mathf.Clamp01(value - 2f);
    }

}