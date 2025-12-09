using UnityEngine;

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
                if (resetVerticalVelocity)
                    playerRb.velocity = new Vector3(playerRb.velocity.x, 0f, playerRb.velocity.z);
                
                playerRb.AddForce(Vector3.up * bounceForce, ForceMode.Impulse);
                PlayFeedback();
            }
        }
    }

    private void PlayFeedback()
    {
        animator?.SetTrigger("Bounce");
        bounceSound?.Play();
        bounceParticles?.Play();
    }
}
