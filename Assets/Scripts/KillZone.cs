using UnityEngine;
using Photon.Pun;

public class KillZone : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        PhotonView pv = other.GetComponent<PhotonView>();
        if (pv != null && pv.IsMine) GameManager.Instance?.RespawnPlayer(other.gameObject);
    }
}
