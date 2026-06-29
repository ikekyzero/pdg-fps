using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMotor : MonoBehaviour
{
    public Vector3 Velocity { get; private set; }
    public bool IsGrounded => controller.isGrounded;
    public CollisionFlags LastCollisionFlags { get; private set; }
    public bool TouchedWallThisFrame => wallHitCount > 0;
    public bool IsCrouching { get; private set; }

    private CharacterController controller;
    private float standingHeight;
    private float crouchHeight;
    private Vector3 standingCenter;
    private Vector3 crouchCenter;

    private Vector3 accumulatedWallNormal;
    private Vector3 accumulatedWallContactPoint;
    private int wallHitCount;
    private Vector3 lastWallNormal = Vector3.zero;
    private Vector3 lastWallContactPoint;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        standingHeight = controller.height;
        crouchHeight = 1.15f;
        standingCenter = controller.center;
        crouchCenter = new Vector3(
            standingCenter.x,
            crouchHeight * 0.5f,
            standingCenter.z
        );
    }

    public void BeginMoveFrame()
    {
        wallHitCount = 0;
        accumulatedWallNormal = Vector3.zero;
        accumulatedWallContactPoint = Vector3.zero;
    }

    public void SetVelocity(Vector3 newVelocity)
    {
        Velocity = newVelocity;
    }

    public void AddVelocity(Vector3 velocity)
    {
        Velocity += velocity;
    }

    public void Move()
    {
        LastCollisionFlags = controller.Move(Velocity * Time.deltaTime);
    }

    public bool IsTouchingWall =>
        (LastCollisionFlags & CollisionFlags.Sides) != 0;

    public bool IsTouchingCeiling =>
        (LastCollisionFlags & CollisionFlags.Above) != 0;

    public Vector3 GetWallNormal()
    {
        if (wallHitCount > 0)
        {
            Vector3 normal = accumulatedWallNormal;
            normal.y = 0f;

            if (normal.sqrMagnitude < 0.0001f)
                return lastWallNormal;

            return normal.normalized;
        }

        return lastWallNormal;
    }

    public Vector3 GetWallNormalTowardPlayer()
    {
        Vector3 toPlayer = transform.position - lastWallContactPoint;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude > 0.0001f)
            return toPlayer.normalized;

        Vector3 normal = GetWallNormal();

        if (normal.sqrMagnitude < 0.0001f)
            return normal;

        Vector3 horizontal = Velocity;
        horizontal.y = 0f;

        if (horizontal.sqrMagnitude > 0.01f && Vector3.Dot(horizontal, normal) > 0f)
            normal = -normal;

        return normal;
    }

    public void FinalizeMoveFrame()
    {
        if (wallHitCount == 0)
            return;

        Vector3 normal = accumulatedWallNormal;
        normal.y = 0f;

        if (normal.sqrMagnitude < 0.0001f)
            return;

        lastWallNormal = normal.normalized;

        Vector3 contactPoint = accumulatedWallContactPoint;
        contactPoint /= wallHitCount;
        lastWallContactPoint = contactPoint;
    }

    public void SetCrouching(bool crouch)
    {
        if (crouch == IsCrouching)
            return;

        if (crouch)
        {
            controller.height = crouchHeight;
            controller.center = crouchCenter;
            IsCrouching = true;
            return;
        }

        if (!CanStandUp())
            return;

        controller.height = standingHeight;
        controller.center = standingCenter;
        IsCrouching = false;
    }

    public bool CanStandUp()
    {
        float clearance = standingHeight - crouchHeight;
        Vector3 origin = transform.position + standingCenter;
        float radius = controller.radius * 0.9f;

        return !Physics.SphereCast(
            origin,
            radius,
            Vector3.up,
            out _,
            clearance,
            ~0,
            QueryTriggerInteraction.Ignore
        );
    }
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;

        if (body != null && !body.isKinematic)
        {
            if (hit.moveDirection.y > -0.3f)
            {
                Vector3 pushDirection = Vector3.ProjectOnPlane(hit.moveDirection, Vector3.up);

                if (pushDirection.sqrMagnitude > 0.001f)
                {
                    float playerSpeed = new Vector3(
                        Velocity.x,
                        0f,
                        Velocity.z
                    ).magnitude;

                    float pushStrength = 0.35f;

                    body.AddForceAtPosition(
                        pushDirection.normalized * playerSpeed * pushStrength,
                        hit.point,
                        ForceMode.Impulse);
                }
            }
        }

        if (hit.normal.y > 0.5f)
            return;

        accumulatedWallNormal += hit.normal;
        accumulatedWallContactPoint += hit.point;
        wallHitCount++;
    }
}

