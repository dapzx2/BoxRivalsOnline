using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Rigidbody))]
public class PlayerJumpController : MonoBehaviourPun
{
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float fallGravityMultiplier = 2.5f;

    private Rigidbody rb;
    private bool isGrounded;
    private bool hasJumped;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (!photonView.IsMine) enabled = false;
    }

    void Update()
    {
        if (!photonView.IsMine) return;
        
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !hasJumped)
        {
            rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
            isGrounded = false;
            hasJumped = true;
        }
    }

    void FixedUpdate()
    {
        // Natural gravity with faster fall
        if (rb.velocity.y < 0)
        {
            rb.velocity += Vector3.up * Physics.gravity.y * (fallGravityMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    private void OnCollisionEnter(Collision collision) => CheckGround(collision);
    private void OnCollisionStay(Collision collision) => CheckGround(collision);
    private void OnCollisionExit(Collision collision)
    {
        if (IsGround(collision)) isGrounded = false;
    }

    private void CheckGround(Collision collision)
    {
        if (rb == null) return;
        if (IsGround(collision) && rb.velocity.y <= 0.5f)
        {
            isGrounded = true;
            hasJumped = false;
        }
    }

    private bool IsGround(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
            if (contact.normal.y > 0.5f) return true;
        return false;
    }
}
