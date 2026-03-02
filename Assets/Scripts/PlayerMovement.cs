using UnityEngine;
using UnityEngine.InputSystem;

#region Class Documentation
/// <summary>
/// Advanced Rigidbody Player Controller for Unity 6.3.
/// Features: Camera-relative movement, Coyote Time, Slope Stickiness, and Auto-Camera Link.
/// </summary>
#endregion

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    #region Serialized Fields - Movement
    [Header("Camera Integration")]
    [SerializeField] [Tooltip("The PlayerCamera script. If empty, the script will search for it automatically.")]
    private PlayerCamera _playerCamera;

    [Header("Movement Settings")]
    [SerializeField] private float baseWalkSpeed = 5f;
    [SerializeField] private float maxSprintSpeed = 10f;
    [SerializeField] private float accelerationRate = 10f;
    [SerializeField] [Tooltip("Prevents bouncing when walking down slopes.")]
    private float slopeStickForce = 5f;

    [Header("Jumping & Air")]
    [SerializeField] private float jumpHeight = 2.5f;
    [SerializeField] [Tooltip("Grace period (seconds) to jump after leaving a ledge.")]
    private float coyoteTime = 0.15f;
    #endregion

    #region Serialized Fields - Detection
    [Header("Detection Settings")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private LayerMask groundLayer;
    #endregion

    #region Serialized Fields - Inputs
    [Header("Input Action References")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference sprintAction;
    #endregion

    #region Private State
    private Rigidbody rb;
    private Vector2 moveInput;
    private float currentSpeed;
    private bool isGrounded;
    private float coyoteTimer;
    private bool canDoubleJump;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate; // Required for smooth camera

        // --- THE AUTO-FIXER ---
        // If the slot is empty, we search for the camera automatically.
        if (_playerCamera == null)
        {
            if (Camera.main != null) 
                _playerCamera = Camera.main.GetComponent<PlayerCamera>();
            
            if (_playerCamera == null)
                _playerCamera = Object.FindFirstObjectByType<PlayerCamera>();
        }
    }

    private void OnEnable()
    {
        if (moveAction != null) moveAction.action.Enable();
        if (sprintAction != null) sprintAction.action.Enable();
        if (jumpAction != null)
        {
            jumpAction.action.Enable();
            jumpAction.action.performed += OnJump;
        }
    }

    private void OnDisable()
    {
        if (jumpAction != null) jumpAction.action.performed -= OnJump;
    }

    private void Update()
    {
        if (moveAction != null) moveInput = moveAction.action.ReadValue<Vector2>();
        
        CheckGrounded();
        
        // Coyote Time timer
        if (isGrounded) coyoteTimer = coyoteTime;
        else coyoteTimer -= Time.deltaTime;

        // Determine target speed
        float target = (sprintAction != null && sprintAction.action.IsPressed()) ? maxSprintSpeed : baseWalkSpeed;
        currentSpeed = Mathf.MoveTowards(currentSpeed, target, accelerationRate * Time.deltaTime);
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }
    #endregion

    #region Logic
    private void CheckGrounded()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(groundCheckPoint.position, groundCheckRadius, groundLayer);

        if (isGrounded && !wasGrounded)
        {
            canDoubleJump = true;
            if (_playerCamera != null) _playerCamera.TriggerImpactBounce();
        }
    }

    private void ApplyMovement()
    {
        // --- THE NULL GUARD ---
        // If the camera is still null, we abort movement logic to prevent the crash.
        if (_playerCamera == null) return;

        Vector3 camFwd = _playerCamera.transform.forward;
        Vector3 camRight = _playerCamera.transform.right;
        camFwd.y = 0; camRight.y = 0;
        camFwd.Normalize(); camRight.Normalize();

        Vector3 moveDir = (camFwd * moveInput.y + camRight * moveInput.x).normalized;

        // Apply velocity with slope stickiness
        Vector3 vel = new Vector3(moveDir.x * currentSpeed, rb.linearVelocity.y, moveDir.z * currentSpeed);
        if (isGrounded && rb.linearVelocity.y <= 0) vel.y = -slopeStickForce;

        rb.linearVelocity = vel;

        // Face movement direction
        if (moveDir != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(camFwd), 15f * Time.fixedDeltaTime);
        }
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (coyoteTimer > 0)
        {
            ExecuteJump(jumpHeight);
            coyoteTimer = 0;
            if (_playerCamera != null) _playerCamera.TriggerImpactBounce();
        }
        else if (canDoubleJump)
        {
            ExecuteJump(jumpHeight * 0.8f);
            canDoubleJump = false;
            if (_playerCamera != null) _playerCamera.TriggerImpactBounce();
        }
    }

    private void ExecuteJump(float h)
    {
        float v = Mathf.Sqrt(h * -2f * Physics.gravity.y);
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, v, rb.linearVelocity.z);
    }
    #endregion
}

/* ===========================================================================================
    DETAILED IMPLEMENTATION INSTRUCTIONS
===========================================================================================
1. FIXING THE NULL REFERENCE ERROR:
   - Ensure your Main Camera has the 'PlayerCamera' script attached.
   - Ensure your Main Camera is tagged as 'MainCamera' (at the top of the Inspector).
   - If you still get the error, manually drag the Camera object into the 'Player Camera' 
     slot on the Player object in the Inspector.

2. RIGIDBODY CONSTRAINTS:
   - Freeze Rotation on X, Y, and Z. The script handles rotation procedurally.
   - Set Interpolate to 'Interpolate'.

3. CAMERA OCCLUSION:
   - In the PlayerCamera script, set the 'Occlusion Layer Mask' to everything EXCEPT 
     the Player's own layer.

4. GROUND DETECTION:
   - Create a child Empty on the player named 'GroundCheck' and place it at the feet.
   - Drag this into the 'Ground Check Point' slot.
===========================================================================================
*/