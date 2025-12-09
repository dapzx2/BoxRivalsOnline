using UnityEngine;
using Photon.Pun;

/// <summary>
/// Handles player jump mechanics with reliable ground detection
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerJumpController : MonoBehaviourPun
{
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 8f;

    private Rigidbody rb;
    private bool isGrounded;
    private int jumpCount = 0;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Only process input for local player
        if (!photonView.IsMine)
        {
            enabled = false;
        }
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        HandleJumpInput();
    }

    private void HandleJumpInput()
    {
        // Jump when Space pressed
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // SINGLE JUMP ONLY - hardcoded check
            if (isGrounded && jumpCount == 0)
            {
                Jump(jumpForce);
            }
            else
            {
                Debug.Log($"Jump blocked - isGrounded: {isGrounded}, jumpCount: {jumpCount}");
            }
        }
    }

    private void Jump(float force)
    {
        // PRESERVE MOMENTUM: Don't clamp horizontal velocity!
        // Just reset vertical component
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        
        // Apply jump force (only upward)
        rb.AddForce(Vector3.up * force, ForceMode.Impulse);
        
        jumpCount++;
        isGrounded = false; // Set to false immediately to prevent re-jump!

        Debug.Log($"✓ Player jumped! Jump count: {jumpCount}, force: {force}, velocity: {rb.velocity}");
    }

    // RELIABLE GROUND DETECTION using collisions
    private void OnCollisionEnter(Collision collision)
    {
        CheckGroundCollision(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        CheckGroundCollision(collision);
    }

    private void OnCollisionExit(Collision collision)
    {
        // Check if we left the ground
        if (IsGroundCollision(collision))
        {
            isGrounded = false;
            Debug.Log("Player left ground");
        }
    }

    private void CheckGroundCollision(Collision collision)
    {
        // Null check untuk mencegah error saat player di-destroy
        if (rb == null) return;
        
        if (IsGroundCollision(collision))
        {
            // CRITICAL: Only set grounded if NOT moving upward (prevent instant re-ground after jump)
            if (rb.velocity.y <= 0.1f) // Allow small positive value for tolerance
            {
                bool wasGrounded = isGrounded;
                isGrounded = true;
                jumpCount = 0; // Reset jump counter when on ground
                
                if (!wasGrounded)
                {
                    Debug.Log("✓ Player landed on ground!");
                }
            }
        }
    }

    private bool IsGroundCollision(Collision collision)
    {
        // Check if collision is below player (ground)
        foreach (ContactPoint contact in collision.contacts)
        {
            // If contact normal is pointing up, it's ground
            if (contact.normal.y > 0.5f)
            {
                return true;
            }
        }
        return false;
    }
}
