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

    [Header("Headbobbing Params")]
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

    #endregion

    private void Awake() {
        playerCamera = Camera.main;
        moveAction = PlayerControls.FindActionMap("Player").FindAction("Movement");
        lookAction = PlayerControls.FindActionMap("Player").FindAction("Look");
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
    }

    private void OnDisable() {
        moveAction.Disable();
        lookAction.Disable();
    }
    private void Update() {
        if (CanMove) {
            HandleMovementInput();
            HandleHeadBobbing();
        }

        if (CanLook) {
            HandleMouseLook();
        }

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
        if (characterController.velocity.magnitude > 0.1f) {
            float frequency = bobFrequency;
            timer += Time.deltaTime * frequency;
            timer %= Mathf.PI * 2;

            float newYPos = defaultYPos + Mathf.Sin(timer) * bobHeight;
            playerCamera.transform.localPosition = new Vector3(playerCamera.transform.localPosition.x, newYPos, playerCamera.transform.localPosition.z);
        } else {
            timer = 0;
            playerCamera.transform.localPosition = new Vector3(playerCamera.transform.localPosition.x, defaultYPos, playerCamera.transform.localPosition.z);
        }
    }

    private void ApplyFinalMovements() {
        if (!characterController.isGrounded) {
            moveDirection.y -= gravityMultiplier * Time.deltaTime;
        }

        characterController.Move(moveDirection * Time.deltaTime);
    }
}
