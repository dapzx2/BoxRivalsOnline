using UnityEngine;
using Photon.Pun;

public class RotatingObstacle : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private Vector3 rotationAxis = Vector3.up;
    [SerializeField] private float rotationSpeed = 50f;

    [Header("Knockback Settings")]
    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private bool enableKnockback = true;

    private Quaternion startRotation;

    void Start()
    {
        startRotation = transform.rotation;
    }

    void Update()
    {
        double time = PhotonNetwork.InRoom ? PhotonNetwork.Time : Time.time;
        float angle = (float)time * rotationSpeed;
        transform.rotation = startRotation * Quaternion.AngleAxis(angle, rotationAxis);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!enableKnockback || !collision.gameObject.CompareTag("Player")) return;

        Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            Vector3 knockbackDirection = (collision.transform.position - transform.position).normalized;
            knockbackDirection.y = 0.5f;
            playerRb.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
        }
    }
}
