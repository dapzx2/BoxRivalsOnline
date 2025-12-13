using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Rigidbody))]
public class PlayerJumpController : MonoBehaviourPun
{
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float fallGravityMultiplier = 2.5f;
    [SerializeField] private AudioClip jumpSound;

    private Rigidbody rb;
    private bool isGrounded;
    private bool hasJumped;
    private AudioSource audioSource;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        
        audioSource.spatialBlend = 0f;
        if (jumpSound == null) jumpSound = Resources.Load<AudioClip>("Audio/jump");

        if (!photonView.IsMine) enabled = false;
    }

    void Update()
    {
        if (!photonView.IsMine) return;
        
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !hasJumped)
        {
            transform.SetParent(null);
            rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
            isGrounded = false;
            hasJumped = true;

            if (jumpSound != null && audioSource != null)
                audioSource.PlayOneShot(jumpSound, 0.3f);
        }
    }

    void FixedUpdate()
    {
        if (rb.velocity.y < 0)
            rb.velocity += Vector3.up * Physics.gravity.y * (fallGravityMultiplier - 1) * Time.fixedDeltaTime;
    }

    void OnCollisionEnter(Collision c) => CheckGround(c);
    void OnCollisionStay(Collision c) => CheckGround(c);
    void OnCollisionExit(Collision c) { if (IsGround(c)) isGrounded = false; }

    void CheckGround(Collision c)
    {
        if (rb != null && IsGround(c) && rb.velocity.y <= 0.5f)
        {
            isGrounded = true;
            hasJumped = false;
        }
    }

    bool IsGround(Collision c)
    {
        foreach (ContactPoint contact in c.contacts)
            if (contact.normal.y > 0.5f) return true;
        return false;
    }
}
