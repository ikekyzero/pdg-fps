using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    [Header("Look Settings")]
    [SerializeField] private Transform cameraTarget;     // Обычно neck/head bone
    [SerializeField] private float sensitivityX = 0.15f;
    [SerializeField] private float sensitivityY = 0.15f;
    [SerializeField] private float verticalClamp = 80f;

    [Header("Optional")]
    [SerializeField] private bool invertY = false;
    [SerializeField] private bool smoothRotation = false;
    [SerializeField, Range(0f, 30f)] private float rotationSmoothTime = 12f;

    [Header("Crouch")]
    [SerializeField] private float crouchCameraOffset = -0.65f;
    [SerializeField] private float cameraTransitionSpeed = 12f;
    private Vector3 cameraStartLocalPos;

    private float currentCrouchOffset = 0f;

    private Transform bodyTransform;
    private PlayerInputHandler input;
    private PlayerMovement movement;

    private float yaw;
    private float pitch;

    private void Awake()
    {
        input = GetComponent<PlayerInputHandler>();
        bodyTransform = transform;
        movement = GetComponent<PlayerMovement>();
        cameraStartLocalPos = cameraTarget.localPosition;

        // Скрываем курсор
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Если нужно — начальный поворот
        if (cameraTarget != null)
            pitch = cameraTarget.localEulerAngles.x;
        
        // Приводим pitch к диапазону -180..180
        if (pitch > 180f) pitch -= 360f;
    }

    private void LateUpdate()
    {
        if (input == null || cameraTarget == null) return;

        Vector2 look = input.LookInput;

        // Применяем чувствительность
        float mouseX = look.x * sensitivityX;
        float mouseY = look.y * sensitivityY * (invertY ? 1f : -1f);

        // Горизонтальный поворот (тело)
        yaw += mouseX;

        Quaternion targetBodyRotation = Quaternion.Euler(0f, yaw, 0f);
        
        if (smoothRotation)
        {
            bodyTransform.rotation = Quaternion.Slerp(
                bodyTransform.rotation,
                targetBodyRotation,
                rotationSmoothTime * Time.deltaTime
            );
        }
        else
        {
            bodyTransform.rotation = targetBodyRotation;
        }

        // Вертикальный поворот (голова/камера)
        pitch += mouseY;
        pitch = Mathf.Clamp(pitch, -verticalClamp, verticalClamp);

        Quaternion targetHeadRotation = Quaternion.Euler(pitch, 0f, 0f);

        if (smoothRotation)
        {
            cameraTarget.localRotation = Quaternion.Slerp(
                cameraTarget.localRotation, 
                targetHeadRotation, 
                rotationSmoothTime * Time.deltaTime
            );
        }
        else
        {
            cameraTarget.localRotation = targetHeadRotation;
        }

        bool shouldLowerCamera = movement != null && movement.IsCrouching;

        float targetOffset = shouldLowerCamera ? crouchCameraOffset : 0f;
        currentCrouchOffset = Mathf.Lerp(currentCrouchOffset, targetOffset, cameraTransitionSpeed * Time.deltaTime);

        if (cameraTarget != null)
        {
            Vector3 localPos = cameraStartLocalPos;
            localPos.y += currentCrouchOffset;
            cameraTarget.localPosition = localPos;
        }
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Для отладки в инспекторе
    private void OnValidate()
    {
        sensitivityY = Mathf.Max(0.01f, sensitivityY);
        sensitivityX = Mathf.Max(0.01f, sensitivityX);
        verticalClamp = Mathf.Clamp(verticalClamp, 0f, 80f);
    }
}