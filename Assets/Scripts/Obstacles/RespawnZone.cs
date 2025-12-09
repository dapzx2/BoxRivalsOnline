using UnityEngine;

/// <summary>
/// Respawn zone that teleports player back to spawn point when they fall
/// </summary>
public class RespawnZone : MonoBehaviour
{
    [Header("Respawn Settings")]
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private float respawnDelay = 0.5f;

    private Collider playerToRespawn;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerToRespawn = other;
            Invoke(nameof(Respawn), respawnDelay);
        }
    }

    private void Respawn()
    {
        if (respawnPoint != null && playerToRespawn != null)
        {
            playerToRespawn.transform.position = respawnPoint.position;
            
            Rigidbody rb = playerToRespawn.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
            }
        }
        playerToRespawn = null;
    }
}
