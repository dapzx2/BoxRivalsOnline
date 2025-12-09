using UnityEngine;

public class RotatingObstacle : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private Vector3 rotationAxis = Vector3.up;
    [SerializeField] private float rotationSpeed = 50f;

    [Header("Knockback Settings")]
    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private bool enableKnockback = true;

    void Update() => transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);

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
