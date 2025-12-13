using UnityEngine;
using Photon.Pun;

public class KillZone : MonoBehaviour
{
    [SerializeField] private AudioClip deathSound;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        
        if (deathSound == null) deathSound = Resources.Load<AudioClip>("Audio/death");
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (deathSound != null && audioSource != null)
            audioSource.PlayOneShot(deathSound, 3.0f);

        PhotonView pv = other.GetComponent<PhotonView>();
        if (pv != null && pv.IsMine) GameManager.Instance?.RespawnPlayer(other.gameObject);
    }
}
