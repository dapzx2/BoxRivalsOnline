using UnityEngine;

public class RespawnZone : MonoBehaviour
{
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private float respawnDelay = 0.5f;
    private Collider playerToRespawn;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerToRespawn = other;
        Invoke(nameof(Respawn), respawnDelay);
    }

    void Respawn()
    {
        if (respawnPoint != null && playerToRespawn != null)
        {
            playerToRespawn.transform.position = respawnPoint.position;
            Rigidbody rb = playerToRespawn.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = Vector3.zero;
        }
        playerToRespawn = null;
    }
}
