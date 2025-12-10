using UnityEngine;

public class BouncePad : MonoBehaviour
{
    [SerializeField] private float bounceForce = 15f;
    [SerializeField] private bool resetVerticalVelocity = true;

    private Animator animator;
    private AudioSource bounceSound;
    private ParticleSystem bounceParticles;

    void Awake()
    {
        animator = GetComponent<Animator>();
        bounceSound = GetComponent<AudioSource>();
        bounceParticles = GetComponentInChildren<ParticleSystem>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        
        Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
        if (playerRb == null) return;

        if (resetVerticalVelocity)
            playerRb.velocity = new Vector3(playerRb.velocity.x, 0f, playerRb.velocity.z);

        playerRb.AddForce(Vector3.up * bounceForce, ForceMode.Impulse);
        PlayFeedback();
    }

    void PlayFeedback()
    {
        if (animator != null) animator.SetTrigger("Bounce");
        if (bounceSound != null) bounceSound.Play();
        if (bounceParticles != null) bounceParticles.Play();
    }
}
