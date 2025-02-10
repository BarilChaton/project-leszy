using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    #region Variables/Refs

    public bool CanMove = true;
    public bool CanLook = true;

    [Header("Look Params")]
    [SerializeField, Range(0.1f, 0.2f)] private float mouseLookSpeedX = 0.1f;
    [SerializeField, Range(0.1f, 0.2f)] private float mouseLookSpeedY = 0.1f;
    [SerializeField, Range(1f, 180f)] private float upperLookLimit = 80.0f;
    [SerializeField, Range(1f, 180f)] private float lowerLookLimit = 80.0f;

    [Header("Movement Params")]
    [SerializeField] private float moveSpeed = 3.0f;
    [SerializeField] private float gravityMultiplier = 30.0f;
    [SerializeField] private float jumpHeight = 2.0f;

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
    [SerializeField] private float bobHeight = 0.5f;

    [Header("Input Action Asset")]
    [SerializeField] private InputActionAsset PlayerControls;

    private Camera playerCamera;
    private CharacterController characterController;

    private float rotationX = 0;
    private float defaultYPos = 0;
    private float timer = 0;

    private Vector3 moveDirection;
    private Vector2 currentInput;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector2 smoothedLookInput;
    private Vector2 lookInputSmoothVelocity;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;

    #endregion

    private void Awake() {
        playerCamera = Camera.main;

        moveAction = PlayerControls.FindActionMap("Player").FindAction("Movement");
        lookAction = PlayerControls.FindActionMap("Player").FindAction("Look");
        jumpAction = PlayerControls.FindActionMap("Player").FindAction("Jump");

        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        defaultYPos = playerCamera.transform.localPosition.y;
    }
    private void OnEnable() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        moveAction.Enable();
        lookAction.Enable();
        jumpAction.Enable();

        jumpAction.performed += ctx => HandleJump();
    }

    private void OnDisable() {
        moveAction.Disable();
        lookAction.Disable();
        jumpAction.Disable();
    }
    private void Update() {
        if (CanMove) {
            HandleMovementInput();
            HandleHeadBobbing();
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

    private void HandleMouseLook() {
        lookInput = lookAction.ReadValue<Vector2>();
        smoothedLookInput = Vector2.SmoothDamp(smoothedLookInput, lookInput, ref lookInputSmoothVelocity, 0f);

        rotationX -= smoothedLookInput.y * mouseLookSpeedY;
        rotationX = Mathf.Clamp(rotationX, -upperLookLimit, lowerLookLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.rotation *= Quaternion.Euler(0, smoothedLookInput.x * mouseLookSpeedX, 0);
    }

    private void HandleMovementInput() {
        moveInput = moveAction.ReadValue<Vector2>();

        currentInput = new Vector2(moveSpeed * moveInput.y, moveSpeed * moveInput.x);
        float moveDirectionY = moveDirection.y;
        moveDirection = (transform.TransformDirection(Vector3.forward) * currentInput.x) + (transform.TransformDirection(Vector3.right) * currentInput.y);
        moveDirection.y = moveDirectionY;
    }
    private void HandleHeadBobbing() {
        float bobOffset = 0;

        if (characterController.velocity.magnitude > 0.1f) {
            float frequency = bobFrequency;
            timer += Time.deltaTime * frequency;
            timer %= Mathf.PI * 2;

            float newYPos = defaultYPos + Mathf.Sin(timer) * bobHeight;
            playerCamera.transform.localPosition = new Vector3(playerCamera.transform.localPosition.x, newYPos, playerCamera.transform.localPosition.z);
        } else {
            timer = 0;
        }

        float bounceOffset = 0;
        if (isBouncing) {
            float t = (Time.time - landingBounceStartTime) / landingBounceDuration;
            if (t >= 1) {
                isBouncing = false;
            } else {
                bounceOffset = landingBounceCurve.Evaluate(t) * -landingBounceAmount;
            }
        }

        playerCamera.transform.localPosition = new Vector3(
            playerCamera.transform.localPosition.x,
            defaultYPos + bobOffset + bounceOffset,
            playerCamera.transform.localPosition.z
        );
    }

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
}
