using UnityEngine;

/// <summary>
/// Bounce pad that launches players upward
/// </summary>
public class BouncePad : MonoBehaviour
{
    [Header("Bounce Settings")]
    [SerializeField] private float bounceForce = 15f;
    [SerializeField] private bool resetVerticalVelocity = true;

    [Header("Visual Feedback")]
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource bounceSound;
    [SerializeField] private ParticleSystem bounceParticles;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                // Reset vertical velocity if enabled
                if (resetVerticalVelocity)
                {
                    playerRb.velocity = new Vector3(playerRb.velocity.x, 0f, playerRb.velocity.z);
                }

                // Apply upward bounce force
                playerRb.AddForce(Vector3.up * bounceForce, ForceMode.Impulse);

                // Visual/audio feedback
                PlayFeedback();

                Debug.Log($"Player bounced! Force: {bounceForce}");
            }
        }
    }

    private void PlayFeedback()
    {
        // Trigger animation
        if (animator != null)
        {
            animator.SetTrigger("Bounce");
        }

        // Play sound
        if (bounceSound != null)
        {
            bounceSound.Play();
        }

        // Spawn particles
        if (bounceParticles != null)
        {
            bounceParticles.Play();
        }
    }
}
