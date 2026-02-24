using UnityEngine;
using UnityEngine.InputSystem;

#region Class Declaration
// Requires a Rigidbody component to be attached to the GameObject this script is on
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    #endregion

    #region Serialized Fields - Movement
    [Header("Movement Settings")]
    [Tooltip("The base walking speed of the player.")]
    [SerializeField] private float baseWalkSpeed = 5f;
    
    [Tooltip("The maximum sprint speed the player can reach.")]
    [SerializeField] private float maxSprintSpeed = 10f;
    
    [Tooltip("How quickly the player accelerates when holding the Shift key.")]
    [SerializeField] private float accelerationRate = 5f;

    [Tooltip("The speed reduction when the player is crouching.")]
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;
    #endregion

    #region Serialized Fields - Jumping
    [Header("Jump Settings")]
    [Tooltip("The desired height of the first jump in Unity units.")]
    [SerializeField] private float jumpHeight = 3f;
    
    [Tooltip("The desired height of the second (double) jump in Unity units.")]
    [SerializeField] private float doubleJumpHeight = 2.5f;
    #endregion

    #region Serialized Fields - Ground Detection
    [Header("Ground Detection")]
    [Tooltip("The point at the bottom of the player to check for the ground.")]
    [SerializeField] private Transform groundCheckPoint;
    
    [Tooltip("The radius of the invisible sphere used to check for ground.")]
    [SerializeField] private float groundCheckRadius = 0.2f;
    
    [Tooltip("Which physics layers are considered 'Ground'.")]
    [SerializeField] private LayerMask groundLayer;
    #endregion

    #region Serialized Fields - Input Actions
    [Header("Input Actions (Assign in Inspector)")]
    [Tooltip("Vector2 input for movement (WASD or Left Stick).")]
    [SerializeField] private InputActionReference moveAction;
    
    [Tooltip("Button input for jumping.")]
    [SerializeField] private InputActionReference jumpAction;
    
    [Tooltip("Button input for accelerating/sprinting (Shift).")]
    [SerializeField] private InputActionReference sprintAction;
    
    [Tooltip("Button input for shooting (Spacebar).")]
    [SerializeField] private InputActionReference shootAction;

    [Tooltip("Button input for crouching.")]
    [SerializeField] private InputActionReference crouchAction;
    #endregion

    #region Private Variables
    private Rigidbody rb;
    private Vector2 moveInput;
    private float currentSpeed;
    
    private bool isGrounded;
    private bool canDoubleJump;
    private bool isCrouching;
    private float startJumpY;
    
    // Default collider height to restore after crouching
    private CapsuleCollider capsuleCollider;
    private float originalColliderHeight;
    private Vector3 originalColliderCenter;
    #endregion

    #region Unity Methods (Initialization)
    private void Awake()
    {
        // Get the attached Rigidbody and CapsuleCollider components
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        // Store original collider settings for crouching logic
        if (capsuleCollider != null)
        {
            originalColliderHeight = capsuleCollider.height;
            originalColliderCenter = capsuleCollider.center;
        }

        currentSpeed = baseWalkSpeed;
    }

    private void OnEnable()
    {
        // Enable the input actions when the script is active
        moveAction.action.Enable();
        jumpAction.action.Enable();
        sprintAction.action.Enable();
        shootAction.action.Enable();
        crouchAction.action.Enable();

        // Subscribe to input events (Triggers)
        jumpAction.action.performed += OnJump;
        shootAction.action.performed += OnShoot;
        crouchAction.action.performed += OnCrouchStart;
        crouchAction.action.canceled += OnCrouchEnd;
    }

    private void OnDisable()
    {
        // Disable the input actions and unsubscribe to prevent memory leaks
        moveAction.action.Disable();
        jumpAction.action.Disable();
        sprintAction.action.Disable();
        shootAction.action.Disable();
        crouchAction.action.Disable();

        jumpAction.action.performed -= OnJump;
        shootAction.action.performed -= OnShoot;
        crouchAction.action.performed -= OnCrouchStart;
        crouchAction.action.canceled -= OnCrouchEnd;
    }
    #endregion

    #region Unity Methods (Update & Physics)
    private void Update()
    {
        // Read the movement input constantly (returns X and Y values from -1 to 1)
        moveInput = moveAction.action.ReadValue<Vector2>();

        // Handle Ground Detection
        CheckGrounded();
        
        // Handle Sprinting / Acceleration
        HandleAcceleration();
    }

    private void FixedUpdate()
    {
        // Apply physics-based movement in FixedUpdate
        HandleMovement();
    }
    #endregion

    #region Movement Logic
    private void HandleMovement()
    {
        // Map 2D input (X, Y) to 3D movement on the X and Z axes (Flat Plane)
        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        // Apply crouch modifier if crouching
        float actualSpeed = isCrouching ? currentSpeed * crouchSpeedMultiplier : currentSpeed;

        // Apply movement to Rigidbody velocity while preserving vertical (Y) velocity for gravity/falling
        rb.linearVelocity = new Vector3(moveDirection.x * actualSpeed, rb.linearVelocity.y, moveDirection.z * actualSpeed);
    }

    private void HandleAcceleration()
    {
        // If the sprint button is held down and we aren't crouching
        if (sprintAction.action.IsPressed() && !isCrouching)
        {
            // Smoothly increase speed up to maxSprintSpeed
            currentSpeed = Mathf.MoveTowards(currentSpeed, maxSprintSpeed, accelerationRate * Time.deltaTime);
        }
        else
        {
            // Smoothly decrease speed back down to baseWalkSpeed
            currentSpeed = Mathf.MoveTowards(currentSpeed, baseWalkSpeed, accelerationRate * Time.deltaTime);
        }
    }
    #endregion

    #region Jumping Logic
    private void CheckGrounded()
    {
        // Create a small invisible sphere at groundCheckPoint to check for collision with the groundLayer
        bool wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(groundCheckPoint.position, groundCheckRadius, groundLayer);

        // Reset double jump when we hit the ground
        if (isGrounded && !wasGrounded)
        {
            canDoubleJump = false; // Must perform a first jump before a double jump
        }
    }

    private void OnJump(InputAction.CallbackContext context)
    {

Debug.Log("JUMP EVENT FIRED! isGrounded is currently: " + isGrounded);

        if (isGrounded)
        {
            // FIRST JUMP
            PerformJump(jumpHeight);
            canDoubleJump = true;
            
            // Record the Y position where the jump started to calculate the 3/4 height mark later
            startJumpY = transform.position.y;
        }
        else if (canDoubleJump)
        {
            // DOUBLE JUMP
            // Check if player has reached 3/4 of their target jump height
            float requiredHeightForDoubleJump = startJumpY + (jumpHeight * 0.75f);
            
            if (transform.position.y >= requiredHeightForDoubleJump)
            {
                // Reset vertical velocity to 0 before applying double jump to ensure consistent height
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                
                PerformJump(doubleJumpHeight);
                canDoubleJump = false; // Cannot jump again until grounded
            }
        }
    }

    private void PerformJump(float targetHeight)
    {
        // Physics formula to calculate required force for a specific jump height
        // v = sqrt(2 * gravity * height)
        float jumpForce = Mathf.Sqrt(targetHeight * -2f * Physics.gravity.y);
        
        // Apply the upward force as an impulse
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }
    #endregion

    #region Combat Logic
    private void OnShoot(InputAction.CallbackContext context)
    {
        // Placeholder for shooting logic
        Debug.Log("Player Fired Weapon!");
        // TODO: Instantiate projectile, play sound, trigger animation
    }
    #endregion

    #region Crouching Logic
    private void OnCrouchStart(InputAction.CallbackContext context)
    {
        isCrouching = true;
        
        if (capsuleCollider != null)
        {
            // Halve the collider height and adjust center to keep the bottom flush with the ground
            capsuleCollider.height = originalColliderHeight / 2f;
            capsuleCollider.center = new Vector3(originalColliderCenter.x, originalColliderCenter.y - (originalColliderHeight / 4f), originalColliderCenter.z);
        }
    }

    private void OnCrouchEnd(InputAction.CallbackContext context)
    {
        isCrouching = false;
        
        if (capsuleCollider != null)
        {
            // Restore original collider size
            capsuleCollider.height = originalColliderHeight;
            capsuleCollider.center = originalColliderCenter;
        }
    }
    #endregion

    #region Debugging / Editor Visualization
    private void OnDrawGizmosSelected()
    {
        // Draws a helpful red sphere in the Unity Scene view to show the Ground Check area
        if (groundCheckPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
        }
    }
    #endregion

    /* ========================================================================================================
    IMPLEMENTATION INSTRUCTIONS
    ========================================================================================================
    
    1. TAGS & LAYERS:
       - Go to Edit > Project Settings > Tags and Layers.
       - Ensure you have a Tag named "Player" and assign it to your Player GameObject.
       - Ensure you have a Layer named "Player" and a Layer named "Ground".
       - Select your Ground objects in the scene, and set their Layer dropdown at the top right to "Ground".

    2. PLAYER SETUP (GameObject):
       - Attach this `PlayerController` script to your Player GameObject.
       - Attach a `Rigidbody` component.
       - Attach a `CapsuleCollider` component. Adjust the Center, Radius, and Height so it perfectly surrounds your 3D model.

    3. RIGIDBODY SETTINGS:
       - Mass: 1 (Leave as default, jump calculations rely on standard physics gravity).
       - Drag: 0
       - Angular Drag: 0
       - Constraints > Freeze Rotation: Check the boxes for X, Y, and Z. (This prevents the player from falling over like a physics block).
       - Constraints > Freeze Position: If you want a STRICT 2D side-scroller, freeze Z. If you truly want to move in all directions on a plane, leave position unchecked.

    4. PHYSICS MATERIAL (Crucial Step to prevent sticking to walls):
       - Right-click in your Project window > Create > Physic Material. Name it "ZeroFriction".
       - Set Dynamic Friction to 0, Static Friction to 0, and Friction Combine to Minimum.
       - Drag this material into the "Material" slot on your Player's CapsuleCollider.

    5. GROUND CHECK SETUP:
       - Right-click your Player GameObject in the Hierarchy and select "Create Empty". Name it "GroundCheck".
       - Move "GroundCheck" to the very bottom center of your player's feet.
       - Drag "GroundCheck" into the "Ground Check Point" slot in the PlayerController script in the Inspector.
       - Set the "Ground Layer" mask in the script to "Ground".

    6. INPUT SYSTEM SETUP:
       - Ensure the Input System package is installed (Window > Package Manager > Unity Registry > Input System).
       - Right-click in the Project window > Create > Input Actions. Name it "PlayerControls".
       - Open it and create an Action Map named "Player".
       - Create the following Actions:
         a. "Move" (Action Type: Value, Control Type: Vector2). Add WASD or Left Stick bindings.
         b. "Jump" (Action Type: Button). Bind to Up Arrow, W, or Gamepad South.
         c. "Sprint" (Action Type: Button). Bind to Left Shift.
         d. "Shoot" (Action Type: Button). Bind to Spacebar.
         e. "Crouch" (Action Type: Button). Bind to C or Left Ctrl.
       - Save the asset.
       - In the Inspector for your PlayerController, click the little circle icon next to each Input Action Reference and assign the ones you just created.

    ========================================================================================================
    */
}