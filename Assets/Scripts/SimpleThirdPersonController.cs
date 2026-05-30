using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class SimpleThirdPersonController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Animator modelAnimator;
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string jumpTriggerParameter = "Jump";
    [SerializeField] private string groundedParameter = "Grounded";

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 4.5f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float acceleration = 18f;
    [SerializeField] private float rotationSmoothTime = 0.08f;

    [Header("Jumping")]
    [SerializeField] private float jumpHeight = 1.6f;
    [SerializeField] private float gravity = -24f;
    [SerializeField] private float groundedStickForce = -2f;

    private CharacterController characterController;
    private SimpleThirdPersonCamera thirdPersonCamera;
    private Vector3 horizontalVelocity;
    private float turnVelocity;
    private float verticalVelocity;
    private int speedParameterHash;
    private int jumpTriggerParameterHash;
    private int groundedParameterHash;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraTransform != null)
        {
            thirdPersonCamera = cameraTransform.GetComponent<SimpleThirdPersonCamera>();
        }

        if (modelAnimator == null)
        {
            modelAnimator = GetComponentInChildren<Animator>(true);
        }

        speedParameterHash = Animator.StringToHash(speedParameter);
        jumpTriggerParameterHash = Animator.StringToHash(jumpTriggerParameter);
        groundedParameterHash = Animator.StringToHash(groundedParameter);
    }

    private void Update()
    {
        if (!GameInputContext.PlayerEnabled)
        {
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, acceleration * Time.deltaTime);
            return;
        }

        Vector2 moveInput = ReadMoveInput();
        Vector3 moveDirection = GetCameraRelativeMove(moveInput);
        bool wantsToRun = IsRunning();
        bool jumpStarted = false;
        float targetSpeed = wantsToRun ? runSpeed : walkSpeed;
        Vector3 targetHorizontalVelocity = moveDirection * targetSpeed;

        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            targetHorizontalVelocity,
            acceleration * Time.deltaTime);

        ApplyGravityAndJump(WasJumpPressed(), ref jumpStarted);

        Vector3 velocity = horizontalVelocity + Vector3.up * verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);
        UpdateAnimator(moveInput, jumpStarted, characterController.isGrounded);
        FaceMoveDirection(moveDirection);
    }

    private static Vector2 ReadMoveInput()
    {
        Vector2 input = Vector2.zero;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
        }

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            input += gamepad.leftStick.ReadValue();
        }

        return Vector2.ClampMagnitude(input, 1f);
    }

    private Vector3 GetCameraRelativeMove(Vector2 input)
    {
        if (input.sqrMagnitude < 0.001f)
        {
            return Vector3.zero;
        }

        Quaternion basisRotation = thirdPersonCamera != null
            ? thirdPersonCamera.YawRotation
            : Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        Vector3 forward = basisRotation * Vector3.forward;
        Vector3 right = basisRotation * Vector3.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        return (forward * input.y + right * input.x).normalized;
    }

    private void FaceMoveDirection(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
        float smoothAngle = Mathf.SmoothDampAngle(
            transform.eulerAngles.y,
            targetAngle,
            ref turnVelocity,
            rotationSmoothTime);
        transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
    }

    private void ApplyGravityAndJump(bool wantsToJump, ref bool jumpStarted)
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedStickForce;
        }

        if (characterController.isGrounded && wantsToJump)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpStarted = true;
        }

        verticalVelocity += gravity * Time.deltaTime;
    }

    private void UpdateAnimator(Vector2 moveInput, bool jumpStarted, bool isGrounded)
    {
        if (modelAnimator == null)
        {
            return;
        }

        modelAnimator.SetFloat(speedParameterHash, moveInput.magnitude);
        modelAnimator.SetBool(groundedParameterHash, isGrounded);
        if (jumpStarted)
        {
            modelAnimator.SetTrigger(jumpTriggerParameterHash);
        }
    }

    private static bool IsRunning()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed))
        {
            return true;
        }

        Gamepad gamepad = Gamepad.current;
        return gamepad != null && gamepad.leftStickButton.isPressed;
    }

    private static bool WasJumpPressed()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
        {
            return true;
        }

        Gamepad gamepad = Gamepad.current;
        return gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
    }
}
