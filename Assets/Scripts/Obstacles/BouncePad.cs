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
        if (bounceSound == null) bounceSound = gameObject.AddComponent<AudioSource>();
        
        bounceSound.playOnAwake = false;
        bounceSound.playOnAwake = false;
        bounceSound.spatialBlend = 0f; 
        bounceSound.volume = 1.0f;
        
        AudioClip clip = Resources.Load<AudioClip>("Audio/bounce_pad");
        if (clip != null) bounceSound.clip = clip;
        
        bounceParticles = GetComponentInChildren<ParticleSystem>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        
        Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
        if (playerRb == null) return;

        if (resetVerticalVelocity)
        {
            playerRb.velocity = new Vector3(playerRb.velocity.x, 0f, playerRb.velocity.z);
        }

        playerRb.AddForce(Vector3.up * bounceForce, ForceMode.Impulse);
        PlayFeedback();
    }

    void PlayFeedback()
    {
        if (animator != null) animator.SetTrigger("Bounce");
        if (bounceSound != null && bounceSound.clip != null) bounceSound.Play();
        if (bounceParticles != null) bounceParticles.Play();
    }
}
