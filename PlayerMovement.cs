/*
 * MrDaveDev's 2D Movement Controller (Lite Edition)
 * Full Version on Unity Asset Store... sometime.
 * Written by MrDaveDev
 * Copyright © 2026 MrDaveDev
 */

using UnityEngine;
using UnityEngine.InputSystem;

namespace MrDaveDev.Movement {
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        private Rigidbody2D rb;
        
        [Header("Movement Settings")]
        [Tooltip("The layer of objects that the player can jump from.")] public LayerMask groundLayer;
        [Space] [Tooltip("Max velocity of player, on x-axis.")] public float maxSpeed = 12f;
        [Tooltip("Number of player sprites that fit below the player, at the peak of the jump.")] public float jumpHeight = 3;
    
        // Movement
        private float horizontalInput;
        private float ascentTimer, ascentGravity, coyoteTimer;
        
        // Jump states
        private bool isAscending;
        private bool canCoyoteJump, hasJumped;
    
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }
    
        private void FixedUpdate()
        {
            var isGrounded = IsGrounded();
            
            HandleHorizontalMovement(isGrounded);
            HandleJumpPhysics();
            HandleCoyoteTime(isGrounded);
    
            // Reset the jump state after landing.
            if (isGrounded && !isAscending) hasJumped = false;
        }
    
        private void HandleHorizontalMovement(bool isGrounded)
        {
            // Calculate acceleration based on how quickly the player should reach the max speed.
            if (horizontalInput != 0)
            {
                var groundAcceleration = maxSpeed / 0.2f;
                var airAcceleration = maxSpeed / 0.2f;
                
                // Use a separate acceleration when changing the direction, so the player can turn quickly.
                if (!Mathf.Approximately(Mathf.Sign(horizontalInput), Mathf.Sign(rb.linearVelocity.x)))
                {
                    groundAcceleration = maxSpeed / 0.1f;
                    airAcceleration = maxSpeed / 0.1f;
                }
                
                // Ground and air movement use different acceleration values.
                if (isGrounded) rb.linearVelocity += new Vector2(groundAcceleration * horizontalInput * Time.fixedDeltaTime, 0);
                else rb.linearVelocity += new Vector2(airAcceleration * horizontalInput * Time.fixedDeltaTime, 0);
            }
            else
            {
                // Slow down the player when there is no movement input.
                // Takes different amount of time to slow down in the air vs on the ground.
                rb.linearVelocity = IsGrounded() ? new Vector2(Mathf.MoveTowards(rb.linearVelocity.x, 0f, (maxSpeed / 0.15f) * Time.fixedDeltaTime), rb.linearVelocity.y) : new Vector2(Mathf.MoveTowards(rb.linearVelocity.x, 0f, (maxSpeed / 0.5f) * Time.fixedDeltaTime), rb.linearVelocity.y);
            }
            
            // Prevent the player from being faster than the max speed.
            rb.linearVelocity = new Vector2(
                Mathf.Clamp(rb.linearVelocity.x, -maxSpeed, maxSpeed),
                Mathf.Clamp(rb.linearVelocity.y, -22, Mathf.Infinity)
            );
        }
    
        private void HandleJumpPhysics()
        {
            // Apply custom gravity while ascending.
            if (isAscending)
            {
                ascentTimer += Time.fixedDeltaTime;
    
                rb.linearVelocity += new Vector2(0f, ascentGravity * Time.fixedDeltaTime);
    
                // If the player reaches their peak height, stop their vertical movement and begin the 0.1f.
                if (ascentTimer >= 0.25f)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
    
                    isAscending = false;
                }
            }
            else
            {
                if (!IsGrounded())
                {
                    // Once the player is no longer ascending or hanging, apply normal gravity.
                    rb.linearVelocity += new Vector2(0f, -9.81f * 6 * Time.fixedDeltaTime);
                }
            }
        }
    
        private void HandleCoyoteTime(bool isGrounded)
        {
            // Allow the player to jump for a short time after leaving the ground.
            if (!isGrounded)
            {
                if (coyoteTimer <= 0.15f && !hasJumped)
                {
                    canCoyoteJump = true;
                    coyoteTimer += Time.fixedDeltaTime;
                }
                else
                {
                    canCoyoteJump = false;
                }
            }
            
            // Reset the coyote timer once the player has been grounded.
            if (isGrounded && !isAscending && coyoteTimer >= 0.15f)
            {
                coyoteTimer = 0;
            }
        }
    
        public void Move(InputAction.CallbackContext context)
        {
            // Stores the horizontal input.
            horizontalInput = context.ReadValue<Vector2>().x;
        }
        
        public void Jump(InputAction.CallbackContext context)
        {
            // Only allow jumping while grounded or during the coyote time window.
            if ((IsGrounded() || canCoyoteJump) && context.performed)
            {
                hasJumped = true;
                canCoyoteJump = false;
                
                // Set the jump height to match the character's height.
                var height = jumpHeight * 0.981038f;
    
                // Calculate the gravity to reach the height in 0.25f seconds.
                ascentGravity = -(2f * height) / (0.25f * 0.25f);
    
                // Calculate the velocity to reach the peak.
                var jumpVel = -ascentGravity * 0.25f;
    
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpVel);
    
                // Begin the ascending state of the jump.
                isAscending = true;
                ascentTimer = 0f;
            }
        }
    
        // Returns true when the player is close enough to the ground to jump.
        private bool IsGrounded()
        {
            Bounds bounds = GetComponent<Collider2D>().bounds;
            
            return Physics2D.OverlapCircle(new Vector2(bounds.center.x, bounds.min.y), 0.3f, groundLayer);
        }
    }
}
