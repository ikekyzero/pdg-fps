using UnityEngine;

[CreateAssetMenu(fileName = "MovementConfig", menuName = "Player/Movement Config")]
public class MovementConfig : ScriptableObject
{
    [Header("Walk")]
    public float walkWishSpeed = 7f;
    public float walkAcceleration = 16f;
    public float momentumBuildRate = 0.35f;
    public float maxMomentumBonus = 6f;

    [Header("Run")]
    public float runWishSpeed = 11f;
    public float runAcceleration = 26f;
    public float runStaminaDrainPerSecond = 0.1f;

    [Header("Slide")]
    public float slideEnterSpeed = 9f;
    public float slideExitSpeed = 6f;
    public float slideFriction = 4.5f;
    public float slideDecelerationMultiplier = 2.3f;
    public float slideSteerSpeedDegrees = 180f;

    [Header("Ground")]
    public float groundFriction = 8f;
    public float counterMovement = 18f;

    [Header("Air")]
    public float airWishSpeed = 10f;
    public float airAcceleration = 28f;

    [Header("Gravity")]
    public float gravity = 25f;
    public float groundStickVelocity = 2f;

    [Header("Jump")]
    public float jumpForce = 8f;
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.15f;
    public float jumpGroundGraceTime = 0.1f;

    public float wallJumpVerticalBoost = 7f;
    public float wallJumpWindow = 0.5f;
    public float wallJumpMinSpeed = 7f;
    public float wallJumpSpeedRetention = 0.95f;
    public float wallJumpInputWeight = 0.7f;
    public float wallJumpCrossInputScale = 0.4f;

    [Header("Dash")]
    public float dashImpulse = 13f;
    public float diagonalInputThreshold = 0.35f;

    [Header("Contact")]
    public float wallStopSpeed = 0.05f;
}   