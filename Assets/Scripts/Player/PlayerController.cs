using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    #region Variables/Refs

    public bool CanMove = true;
    public bool CanLook = true;

    #region Look Parameters
    [Header("Look Params")]
    [SerializeField, Range(0.1f, 0.2f)] private float mouseLookSpeedX = 0.1f;
    [SerializeField, Range(0.1f, 0.2f)] private float mouseLookSpeedY = 0.1f;
    [SerializeField, Range(1f, 180f)] private float upperLookLimit = 80.0f;
    [SerializeField, Range(1f, 180f)] private float lowerLookLimit = 80.0f;
    #endregion

    #region Movement Params
    [Header("Movement Params")]
    [SerializeField] private float moveSpeed = 3.0f;
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private float gravityMultiplier = 30.0f;
    [SerializeField] private float jumpHeight = 2.0f;
    #endregion

    #region Stamina Params
    [Header("Stamina Parameters")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDecreaseRate = 15f;
    [SerializeField] private float staminaRechargeRate = 10f;
    [SerializeField] private float staminaRechargeDelay = 2f;
    private bool isSprinting = false;
    private float sprintCooldownTimer = 0;
    #endregion

    #region Camera and HeadBobbing
    [Header("Camera landing Bounce Params")]
    [SerializeField] private AnimationCurve landingBounceCurve;
    [SerializeField] private float landingBounceDuration = 0.2f;
    [SerializeField] private float landingBounceAmount = 0.1f;
    private float landingBounceStartTime;
    private bool isJumping = false;
    private bool wasGrounded = true;
    private bool isBouncing = false;

    [ Header("Headbobbing Params")]
    [SerializeField] private float bobFrequency = 1.5f;
    [SerializeField] private float sprintingBobFrequency = 1.5f;
    [SerializeField] private float bobHeight = 0.5f;
    [SerializeField] private float sprintBobHeight = 0.5f;
    #endregion

    [Header("Input Action Asset")]
    [SerializeField] private InputActionAsset PlayerControls;

    private Camera playerCamera;
    private CharacterController characterController;

    private float rotationX = 0;
    private float defaultYPos = 0;
    private float timer = 0;
    private float currentStamina;

    private Vector3 moveDirection;
    private Vector2 currentInput;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector2 smoothedLookInput;
    private Vector2 lookInputSmoothVelocity;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    #endregion

    #region Built in Unity functions
    private void Awake() {
        playerCamera = Camera.main;

        moveAction = PlayerControls.FindActionMap("Player").FindAction("Movement");
        lookAction = PlayerControls.FindActionMap("Player").FindAction("Look");
        jumpAction = PlayerControls.FindActionMap("Player").FindAction("Jump");
        sprintAction = PlayerControls.FindActionMap("Player").FindAction("Sprint");

        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        defaultYPos = playerCamera.transform.localPosition.y;
        currentStamina = maxStamina;
    }
    private void OnEnable() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        moveAction.Enable();
        lookAction.Enable();
        jumpAction.Enable();
        sprintAction.Enable();

        jumpAction.performed += ctx => HandleJump();
    }
    private void OnDisable() {
        moveAction.Disable();
        lookAction.Disable();
        jumpAction.Disable();
        sprintAction.Disable();
    }
    private void Update() {
        if (CanMove) {
            HandleMovementInput();
            HandleHeadBobbing();
            UpdateStamina();
        }

        if (CanLook) {
            HandleMouseLook();
        }

        if (!wasGrounded && characterController.isGrounded) {
            StartLandingBounce();
        }

        wasGrounded = characterController.isGrounded;

        ApplyFinalMovements();
    }
    #endregion

    #region MouseLook
    private void HandleMouseLook() {
        lookInput = lookAction.ReadValue<Vector2>();
        smoothedLookInput = Vector2.SmoothDamp(smoothedLookInput, lookInput, ref lookInputSmoothVelocity, 0f);

        rotationX -= smoothedLookInput.y * mouseLookSpeedY;
        rotationX = Mathf.Clamp(rotationX, -upperLookLimit, lowerLookLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.rotation *= Quaternion.Euler(0, smoothedLookInput.x * mouseLookSpeedX, 0);
    }
    #endregion

    #region Walking & Sprinting
    private void HandleMovementInput() {
        moveInput = moveAction.ReadValue<Vector2>();
        bool isSprintingPressed = sprintAction.IsPressed();

        currentInput = new Vector2(moveSpeed * moveInput.y, moveSpeed * moveInput.x);
        float moveDirectionY = moveDirection.y;
        moveDirection = (transform.TransformDirection(Vector3.forward) * currentInput.x) + (transform.TransformDirection(Vector3.right) * currentInput.y);

        if (isSprintingPressed && currentStamina >= 50 && sprintCooldownTimer <= 0) {
            float sprintSpeed = Mathf.Lerp(moveSpeed, moveSpeed * sprintMultiplier, currentStamina / maxStamina);
            moveDirection *= sprintSpeed / moveSpeed;
            isSprinting = true;
        } else {
            isSprinting = false;
        }

        moveDirection.y = moveDirectionY;
    }
    private void HandleHeadBobbing() {
        float bobOffset = 0;
        float bounceOffset = 0;

        if (characterController.velocity.magnitude > 0.1f) {
            float frequency = isSprinting ? sprintingBobFrequency : bobFrequency;
            float currentBobHeight = isSprinting ? sprintBobHeight : bobHeight;
            timer += Time.deltaTime * frequency;
            timer %= Mathf.PI * 2;

            float newYPos = defaultYPos + Mathf.Sin(timer) * currentBobHeight;
            playerCamera.transform.localPosition = new Vector3(playerCamera.transform.localPosition.x, newYPos, playerCamera.transform.localPosition.z);
        } else {
            timer = 0;

            playerCamera.transform.localPosition = new Vector3(
                playerCamera.transform.localPosition.x,
                defaultYPos + bobOffset,
                playerCamera.transform.localPosition.z
            );
        }

        if (isBouncing) {
            float t = (Time.time - landingBounceStartTime) / landingBounceDuration;
            if (t >= 1) {
                isBouncing = false;
            } else {
                bounceOffset = landingBounceCurve.Evaluate(t) * -landingBounceAmount;
            }

            playerCamera.transform.localPosition = new Vector3(
                playerCamera.transform.localPosition.x,
                defaultYPos + bounceOffset,
                playerCamera.transform.localPosition.z
            );
        }
    }
    #endregion

    #region Jumping
    private void HandleJump() {
        if (characterController.isGrounded) {
            moveDirection.y = Mathf.Sqrt(jumpHeight * 2f * gravityMultiplier);
            isJumping = true;
        }
    }

    private void StartLandingBounce() {
        landingBounceStartTime = Time.time;
        isBouncing = true;
    }

    private void ApplyFinalMovements() {
        if (!characterController.isGrounded) {
            moveDirection.y -= gravityMultiplier * Time.deltaTime;
        } else if (isJumping) {
            isJumping = false;
        }

        characterController.Move(moveDirection * Time.deltaTime);
    }
    #endregion

    #region Stamina
    private void UpdateStamina() {
        if (isSprinting) {
            currentStamina -= staminaDecreaseRate * Time.deltaTime;
            if (currentStamina <= 0) {
                currentStamina = 0;
                isSprinting = false;
                sprintCooldownTimer = staminaRechargeDelay;
            }
        } else if (sprintCooldownTimer > 0) {
            sprintCooldownTimer -= Time.deltaTime;
        } else {
            currentStamina += staminaRechargeRate * Time.deltaTime;
            if (currentStamina > maxStamina) {
                currentStamina = maxStamina;
            }
        }
    }
    #endregion
}
