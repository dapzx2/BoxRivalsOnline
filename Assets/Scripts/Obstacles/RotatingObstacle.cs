using UnityEngine;

/// <summary>
/// Rotating obstacle that knocks back players on contact
/// Used for hammers, bars, spinning blades, etc.
/// </summary>
public class RotatingObstacle : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private Vector3 rotationAxis = Vector3.up;
    [SerializeField] private float rotationSpeed = 50f;

    [Header("Knockback Settings")]
    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private bool enableKnockback = true;

    void Update()
    {
        // Continuous rotation
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!enableKnockback) return;

        // Check if collided with player
        if (collision.gameObject.CompareTag("Player"))
        {
            Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                // Calculate knockback direction (away from obstacle)
                Vector3 knockbackDirection = (collision.transform.position - transform.position).normalized;
                knockbackDirection.y = 0.5f; // Add slight upward force
                
                // Apply knockback
                playerRb.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
                
                Debug.Log($"Player hit by rotating obstacle! Knockback applied.");
            }
        }
    }
}
