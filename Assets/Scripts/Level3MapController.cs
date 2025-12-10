using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

public class Level3MapController : MonoBehaviourPun
{
    [SerializeField] private Transform[] playerSpawnPoints;
    [SerializeField] private Transform[] boxSpawnPoints;
    [SerializeField] private Transform respawnPoint;

    public Vector3 GetPlayerSpawnPosition(bool isMasterClient)
    {
        if (playerSpawnPoints.Length >= 2)
            return isMasterClient ? playerSpawnPoints[0].position : playerSpawnPoints[1].position;
        return new Vector3(0, 2, 0);
    }

    public List<Vector3> GetBoxSpawnPositions()
    {
        List<Vector3> positions = new List<Vector3>();
        foreach (Transform point in boxSpawnPoints)
            if (point != null) positions.Add(point.position);
        return positions;
    }

    public Transform GetRespawnPoint() => respawnPoint;
}
