using UnityEngine;

[RequireComponent(typeof(PlayerMotor))]
[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(PlayerStamina))]
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Walk")]
    [SerializeField] private float walkWishSpeed = 4.4f;
    [SerializeField] private float walkAcceleration = 5f;
    [SerializeField] private float momentumBuildRate = 0.35f;
    [SerializeField] private float maxMomentumBonus = 6f;

    [Header("Run")]
    [SerializeField] private float runWishSpeed = 7f;
    [SerializeField] private float runAcceleration = 26f;

    [Header("Slide")]
    [SerializeField] private float slideEnterSpeed = 7f;
    [SerializeField] private float slideExitSpeed = 4f;
    [SerializeField] private float slideFriction = 10f;
    [SerializeField] private float slideDecelerationMultiplier = 1f;
    [SerializeField, Tooltip(
        "Max degrees/second the slide direction can be steered toward the " +
        "movement input. Steering only ever changes DIRECTION, never speed - " +
        "speed loss is handled entirely by friction below. This is what keeps " +
        "turning the camera mid-slide from being able to add energy.")]
    private float slideSteerSpeedDegrees = 180f;

    [Header("Ground")]
    [SerializeField] private float groundFriction = 30f;
    [SerializeField] private float counterMovement = 15f;

    [Header("Air")]
    [SerializeField] private float airWishSpeed = 4f;
    [SerializeField] private float airAcceleration = 10f;

    [Header("Gravity")]
    [SerializeField] private float gravity = 30f;
    [SerializeField] private float groundStickVelocity = 2f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    [SerializeField] private float jumpGroundGraceTime = 0.1f;
    [SerializeField] private float wallJumpVerticalBoost = 14f;
    [SerializeField] private float wallJumpWindow = 0.5f;
    [SerializeField] private float wallJumpMinSpeed = 6f;
    [SerializeField] private float wallJumpSpeedRetention = 0.95f;
    [SerializeField] private float wallJumpInputWeight = 0.3f;
    [SerializeField] private float wallJumpCrossInputScale = 0.4f;

    [Header("Dash")]
    [SerializeField] private float dashImpulse = 5f;
    [SerializeField] private float diagonalInputThreshold = 0.35f;

    [Header("Contact")]
    [SerializeField] private float wallStopSpeed = 0.05f;

    [Header("Stamina")]
    [SerializeField] private float runStaminaDrainPerSecond = 0.3f;
    [SerializeField] private float airJumpStaminaCost = 1f;
    [SerializeField] private float wallJumpStaminaCost = 1f;
    [SerializeField] private float dashStaminaCost = 1f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private static readonly int WalkHash = Animator.StringToHash("Walk");
    private static readonly int RunHash = Animator.StringToHash("Run");

    [SerializeField, Range(0f, 1f)]
    private float jumpFreezeTime = 0.45f;

    private bool isGrounded;
    private bool forceAirborne;
    private float forceAirborneTimer;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float moveHoldTimer;
    private bool usedAirJump;
    private bool isSliding;
    private bool wasSliding;
    private float slideEntrySpeed;
    private float wallJumpTimer;
    private Vector3 wallJumpDirection;
    private float wallJumpSpeed;
    private Vector3 wallJumpWallNormal;

    private PlayerMotor motor;
    private PlayerInputHandler input;
    private PlayerStamina stamina;

    public float CurrentSpeed => GetHorizontalVelocity().magnitude;
    public bool IsGrounded => isGrounded;
    public bool IsCrouching => motor.IsCrouching;
    public bool IsSliding => isSliding;
    public float SlideEntrySpeed => slideEntrySpeed;
    public bool IsRunning { get; private set; }
    public bool IsWallJumping => wallJumpTimer > 0f;

    private float dt;

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        input = GetComponent<PlayerInputHandler>();
        stamina = GetComponent<PlayerStamina>();
    }

    private void Update()
    {
        if (input.JumpPressed)
        {
            input.ConsumeJump();
            jumpBufferTimer = Mathf.Max(jumpBufferTimer, jumpBufferTime);
        }
        dt = Time.deltaTime;

        motor.BeginMoveFrame();

        UpdateForceAirborne();
        UpdateCoyoteTime();
        UpdateJumpBuffer();
        UpdateMoveHoldTimer();
        UpdateCrouchAndSlide();
        UpdateWallJumpState();

        HandleRunStamina();
        TryDash();
        Jump();

        Move();
        ApplyWallJumpMomentum();
        ApplyCounterMovement();
        ApplyFriction();
        ApplyGravity();

        motor.Move();
        motor.FinalizeMoveFrame();
        UpdateSlideAfterMove();
        UpdateGrounded();
        UpdateAnimator();
    }
    public string CurrentState
    {
        get
        {
            if (wallJumpTimer > 0f)
                return "WallJump";

            if (!isGrounded)
                return "Air";

            if (isSliding)
                return "Slide";

            if (IsRunning)
                return "Run";

            if (CurrentSpeed > 0.1f)
                return "Walk";

            return "Idle";
        }
    }

    private void UpdateGrounded()
    {
        if (forceAirborne)
        {
            isGrounded = false;
            return;
        }

        isGrounded = motor.IsGrounded;

        if (isGrounded)
        {
            usedAirJump = false;
            wallJumpTimer = 0f;
        }
    }

    private void UpdateForceAirborne()
    {
        if (!forceAirborne) return;

        forceAirborneTimer -= dt;
        if (forceAirborneTimer <= 0f)
            forceAirborne = false;
    }

    private void UpdateCoyoteTime()
    {
        if (isGrounded)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= dt;
    }

    private void UpdateJumpBuffer()
    {
        if (jumpBufferTimer > 0f)
            jumpBufferTimer -= dt;
    }

    private void UpdateMoveHoldTimer()
    {
        if (input.MoveInput.sqrMagnitude > 0.01f)
            moveHoldTimer += dt;
        else
            moveHoldTimer = 0f;
    }

    private void UpdateCrouchAndSlide()
    {
        wasSliding = isSliding;

        float speed = CurrentSpeed;

        if (!input.CrouchHeld)
        {
            isSliding = false;
            motor.SetCrouching(false);
            return;
        }

        // игрок присел
        motor.SetCrouching(true);

        // начать слайд только один раз
        if (!isSliding && isGrounded && speed >= slideEnterSpeed)
        {
            isSliding = true;
            slideEntrySpeed = speed;
        }

        // закончить слайд, но остаться в приседе
        if (isSliding && speed < slideExitSpeed)
        {
            isSliding = false;
        }
    }

    private void UpdateSlideAfterMove()
    {
        if (wasSliding && !isSliding && !input.CrouchHeld)
            motor.SetCrouching(false);

        if (isSliding && CurrentSpeed < slideExitSpeed)
            isSliding = false;
    }

    private void HandleRunStamina()
    {
        bool wantsRun = input.SprintHeld && input.MoveInput.sqrMagnitude > 0.01f;

        if (!wantsRun)
        {
            IsRunning = false;
            stamina.SetRegenerationEnabled(true);
            return;
        }

        float drain = runStaminaDrainPerSecond * dt;

        if (!stamina.HasAtLeast(drain))
        {
            IsRunning = false;
            stamina.SetRegenerationEnabled(true);
            return;
        }

        stamina.Drain(drain);
        stamina.SetRegenerationEnabled(false);
        IsRunning = true;
    }

    private void Move()
    {
        if (wallJumpTimer > 0f)
        {
            ApplyWallJumpMove();
            return;
        }

        Vector2 moveInput = input.MoveInput;
        Vector3 wishDirection = GetWishDirection(moveInput);

        // Sliding is handled completely separately from normal accelerate-toward-wishspeed
        // movement. It must NEVER go through Accelerate() with a wishSpeed derived from
        // CurrentSpeed - see the warning above Accelerate() for why.
        if (isGrounded && isSliding)
        {
            HandleSlideSteering(wishDirection);
            return;
        }

        if (wishDirection.sqrMagnitude < 0.0001f)
            return;

        wishDirection.Normalize();

        float wishSpeed;
        float accel;

        if (isGrounded)
        {
            if (IsRunning)
            {
                wishSpeed = runWishSpeed + GetMomentumBonus();
                accel = runAcceleration;
            }
            else
            {
                wishSpeed = walkWishSpeed + GetMomentumBonus();
                accel = walkAcceleration;
            }
        }
        else
        {
            wishSpeed = airWishSpeed;
            accel = airAcceleration;
        }

        Accelerate(wishDirection, wishSpeed, accel);
    }

    /// <summary>
    /// Rotates the current horizontal velocity toward the wish direction by a capped
    /// angle per second, WITHOUT changing its magnitude. This lets the player steer a
    /// slide with movement/camera input, but guarantees turning can never add speed -
    /// all speed loss during a slide comes from ApplyFriction() instead.
    /// </summary>
    private void HandleSlideSteering(Vector3 wishDirection)
    {
        if (wishDirection.sqrMagnitude < 0.0001f)
            return;

        Vector3 horizontalVelocity = GetHorizontalVelocity();
        float speed = horizontalVelocity.magnitude;

        if (speed < 0.01f)
            return;

        Vector3 currentDirection = horizontalVelocity / speed;
        Vector3 desiredDirection = wishDirection.normalized;

        float maxRadiansDelta = slideSteerSpeedDegrees * Mathf.Deg2Rad * dt;
        Vector3 steeredDirection = Vector3.RotateTowards(currentDirection, desiredDirection, maxRadiansDelta, 0f);

        Vector3 steeredVelocity = steeredDirection.normalized * speed; // magnitude preserved exactly
        motor.SetVelocity(new Vector3(steeredVelocity.x, motor.Velocity.y, steeredVelocity.z));
    }

    private float GetMomentumBonus()
    {
        return Mathf.Min(moveHoldTimer * momentumBuildRate, maxMomentumBonus);
    }

    private Vector3 GetWishDirection(Vector2 moveInput)
    {
        Vector3 wishDirection = transform.forward * moveInput.y + transform.right * moveInput.x;

        if (wishDirection.sqrMagnitude > 1f)
            wishDirection.Normalize();

        return wishDirection;
    }

    private void ApplyGravity()
    {
        Vector3 velocity = motor.Velocity;

        if (isGrounded)
        {
            if (velocity.y < 0f)
            {
                velocity.y = -groundStickVelocity;
                motor.SetVelocity(velocity);
            }

            return;
        }

        motor.AddVelocity(Vector3.down * gravity * dt);
    }

    private void Jump()
    {
        if (jumpBufferTimer <= 0f)
            return;

        if (coyoteTimer > 0f)
        {
            PerformJump(jumpForce);
            return;
        }

        if (usedAirJump)
            return;

        usedAirJump = true;

        bool atWall =
            motor.IsTouchingWall &&
            motor.GetWallNormalTowardPlayer().sqrMagnitude > 0.0001f;

        float staminaCost = atWall
            ? wallJumpStaminaCost
            : airJumpStaminaCost;

        if (!stamina.TrySpend(staminaCost))
            return;

        if (atWall)
            PerformWallJump();
        else
            PerformJump(jumpForce);
    }

    private void PerformJump(float verticalForce)
    {
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        forceAirborne = true;
        forceAirborneTimer = jumpGroundGraceTime;
        isGrounded = false;
        isSliding = false;
        wallJumpTimer = 0f;

        Vector3 velocity = motor.Velocity;
        motor.SetVelocity(new Vector3(velocity.x, verticalForce, velocity.z));
    }

    private void PerformWallJump()
    {
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        forceAirborne = true;
        forceAirborneTimer = jumpGroundGraceTime;
        isGrounded = false;
        isSliding = false;

        Vector3 wallNormal = motor.GetWallNormalTowardPlayer();
        if (wallNormal.sqrMagnitude < 0.0001f)
        {
            PerformJump(jumpForce);
            return;
        }

        wallNormal.y = 0f;
        wallNormal.Normalize();

        Vector3 wishFromInput = GetWishDirection(input.MoveInput);
        Vector3 horizontal = GetHorizontalVelocity();

        Vector3 bounceHorizontal = ComputeWallJumpHorizontalVelocity(wishFromInput, horizontal, wallNormal);

        wallJumpDirection = bounceHorizontal.normalized;
        wallJumpSpeed = bounceHorizontal.magnitude;
        wallJumpWallNormal = wallNormal;
        wallJumpTimer = wallJumpWindow;

        motor.SetVelocity(new Vector3(bounceHorizontal.x, wallJumpVerticalBoost, bounceHorizontal.z));
    }

    private Vector3 ComputeWallJumpHorizontalVelocity(Vector3 wishFromInput, Vector3 horizontal, Vector3 wallNormal)
    {
        float intoVelocity = Vector3.Dot(horizontal, wallNormal);

        if (intoVelocity < -0.05f && horizontal.sqrMagnitude > 0.01f)
        {
            Vector3 reflected = horizontal - 2f * intoVelocity * wallNormal;
            float reflectedSpeed = Mathf.Max(reflected.magnitude, wallJumpMinSpeed) * wallJumpSpeedRetention;
            return reflected.normalized * reflectedSpeed;
        }

        Vector3 wishDir = BuildWallJumpWishDirection(wishFromInput, horizontal, wallNormal);
        Vector3 bounceDir = ReflectDirectionOffWall(wishDir, wallNormal);

        float bounceSpeed = Mathf.Max(horizontal.magnitude, wallJumpMinSpeed) * wallJumpSpeedRetention;
        return bounceDir * bounceSpeed;
    }

    private Vector3 BuildWallJumpWishDirection(Vector3 wishFromInput, Vector3 horizontal, Vector3 wallNormal)
    {
        if (IsDiagonalInput(input.MoveInput) && wishFromInput.sqrMagnitude > 0.01f)
            return wishFromInput.normalized;

        bool hasInput = wishFromInput.sqrMagnitude > 0.01f;
        bool hasVelocity = horizontal.sqrMagnitude > 0.01f;

        if (hasInput && hasVelocity)
        {
            Vector3 blended = wishFromInput.normalized * wallJumpInputWeight + horizontal.normalized * (1f - wallJumpInputWeight);
            if (blended.sqrMagnitude > 0.0001f)
                return blended.normalized;
        }

        if (hasInput) return wishFromInput.normalized;
        if (hasVelocity) return horizontal.normalized;
        return wallNormal;
    }

    private Vector3 ReflectDirectionOffWall(Vector3 direction, Vector3 wallNormal)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            return wallNormal;

        direction.Normalize();

        float intoWall = Vector3.Dot(direction, wallNormal);
        if (intoWall < 0f)
            direction -= 2f * intoWall * wallNormal;

        return direction.sqrMagnitude < 0.0001f ? wallNormal : direction.normalized;
    }

    private void UpdateWallJumpState()
    {
        if (wallJumpTimer <= 0f) return;

        if (isGrounded)
            wallJumpTimer = 0f;
        else
            wallJumpTimer -= dt;
    }

    private void ApplyWallJumpMove()
    {
        Vector3 wish = GetWishDirection(input.MoveInput);
        if (wish.sqrMagnitude < 0.0001f) return;

        wish.Normalize();

        float intoWall = Vector3.Dot(wish, wallJumpWallNormal);
        if (intoWall < 0f)
            wish -= 2f * intoWall * wallJumpWallNormal;

        if (wish.sqrMagnitude < 0.0001f) return;

        Accelerate(wish.normalized, airWishSpeed * wallJumpCrossInputScale, airAcceleration * wallJumpCrossInputScale);
    }

    private void ApplyWallJumpMomentum()
    {
        if (wallJumpTimer <= 0f) return;

        Vector3 velocity = motor.Velocity;
        Vector3 horizontal = GetHorizontalVelocity();

        float intoWall = Vector3.Dot(horizontal, wallJumpWallNormal);
        if (intoWall < 0f)
            horizontal -= intoWall * wallJumpWallNormal;

        Vector3 bounceComponent = wallJumpDirection * Mathf.Max(Vector3.Dot(horizontal, wallJumpDirection), wallJumpSpeed);
        Vector3 perpendicular = horizontal - Vector3.Project(horizontal, wallJumpDirection);

        horizontal = bounceComponent + perpendicular * 0.25f;

        motor.SetVelocity(new Vector3(horizontal.x, velocity.y, horizontal.z));
    }

    private void TryDash()
    {
        if (!input.DashPressed) return;

        Vector2 moveInput = input.MoveInput;
        input.ConsumeDash();

        if (!stamina.TrySpend(dashStaminaCost)) return;

        Vector3 dashDirection;

        if (moveInput.sqrMagnitude < 0.01f)
            dashDirection = transform.forward;
        else if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
            dashDirection = moveInput.x > 0f ? transform.right : -transform.right;
        else
            dashDirection = moveInput.y > 0f ? transform.forward : -transform.forward;

        dashDirection.y = 0f;
        dashDirection.Normalize();

        motor.AddVelocity(dashDirection * dashImpulse);
    }

    private bool IsDiagonalInput(Vector2 moveInput)
    {
        float absX = Mathf.Abs(moveInput.x);
        float absY = Mathf.Abs(moveInput.y);
        return absX > diagonalInputThreshold && absY > diagonalInputThreshold;
    }

    private void ApplyFriction()
    {
        if (!isGrounded) return;

        // While sliding, friction always applies (even with movement input held) -
        // that's the only thing slowing the slide down now that Move() no longer does.
        if (input.MoveInput.sqrMagnitude > 0.01f && !isSliding) return;

        Vector3 horizontalVelocity = GetHorizontalVelocity();
        float speed = horizontalVelocity.magnitude;

        if (speed <= wallStopSpeed)
        {
            motor.SetVelocity(new Vector3(0f, motor.Velocity.y, 0f));
            return;
        }

        float friction = isSliding
            ? slideFriction * slideDecelerationMultiplier
            : groundFriction;

        float drop = friction * dt;
        speed = Mathf.Max(speed - drop, 0f);

        horizontalVelocity = horizontalVelocity.normalized * speed;
        motor.SetVelocity(new Vector3(horizontalVelocity.x, motor.Velocity.y, horizontalVelocity.z));
    }

    private Vector3 GetHorizontalVelocity()
    {
        Vector3 velocity = motor.Velocity;
        return new Vector3(velocity.x, 0f, velocity.z);
    }

    /// <summary>
    /// Source/Quake-style "accelerate toward a wish direction" helper.
    ///
    /// IMPORTANT: wishSpeed must always be a value that is INDEPENDENT of the
    /// player's current speed (a fixed design constant like walkWishSpeed or
    /// airWishSpeed). If you ever pass in something derived from CurrentSpeed,
    /// you re-create the slide acceleration bug: turning wishDirection away from
    /// the velocity vector makes Dot(velocity, wishDirection) drop below the
    /// (now also inflated) wishSpeed, so this function keeps injecting extra
    /// velocity every frame with no upper bound. That's exactly the "infinite
    /// acceleration while turning the camera mid-slide" issue - don't reintroduce it.
    /// </summary>
    private void Accelerate(Vector3 wishDirection, float wishSpeed, float accel)
    {
        Vector3 horizontalVelocity = GetHorizontalVelocity();
        float currentSpeed = Vector3.Dot(horizontalVelocity, wishDirection);

        float addSpeed = wishSpeed - currentSpeed;
        if (addSpeed <= 0f) return;

        float accelSpeed = accel * dt * wishSpeed;
        if (accelSpeed > addSpeed) accelSpeed = addSpeed;

        motor.AddVelocity(wishDirection * accelSpeed);
    }

    private void ApplyCounterMovement()
    {
        if (wallJumpTimer > 0f || !isGrounded || isSliding) return;
        if (input.MoveInput.sqrMagnitude < 0.01f) return;

        Vector3 wishDirection = GetWishDirection(input.MoveInput);
        if (wishDirection.sqrMagnitude < 0.0001f) return;
        wishDirection.Normalize();

        Vector3 horizontalVelocity = GetHorizontalVelocity();
        Vector3 unwantedVelocity = horizontalVelocity - Vector3.Project(horizontalVelocity, wishDirection);

        motor.AddVelocity(-unwantedVelocity * counterMovement * dt);
    }
    private void UpdateAnimator()
    {
        if (animator == null)
            return;

        animator.SetBool(WalkHash,
            CurrentSpeed > 0.1f &&
            !IsRunning &&
            isGrounded);

        animator.SetBool(RunHash,
            IsRunning &&
            isGrounded);
    }
}