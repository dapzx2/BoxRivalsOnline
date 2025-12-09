using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

/// <summary>
/// Base controller for Level 3 map variants
/// Handles box spawning and player spawn points specific to parkour maps
/// </summary>
public class Level3MapController : MonoBehaviourPun
{
    [Header("Map Info")]
    [SerializeField] private string mapName = "Unknown Map";
    
    [Header("Spawn Points")]
    [SerializeField] private Transform[] playerSpawnPoints;
    [SerializeField] private Transform[] boxSpawnPoints;

    [Header("Respawn")]
    [SerializeField] private Transform respawnPoint;

    void Start()
    {
        Debug.Log($"Level 3 Map: {mapName} loaded!");
        
        // GameManager will handle actual spawning
        // This just provides the spawn points
    }

    public Vector3 GetPlayerSpawnPosition(bool isMasterClient)
    {
        if (playerSpawnPoints.Length >= 2)
        {
            return isMasterClient ? playerSpawnPoints[0].position : playerSpawnPoints[1].position;
        }
        
        // Fallback
        return new Vector3(0, 2, 0);
    }

    public List<Vector3> GetBoxSpawnPositions()
    {
        List<Vector3> positions = new List<Vector3>();
        
        foreach (Transform point in boxSpawnPoints)
        {
            if (point != null)
            {
                positions.Add(point.position);
            }
        }
        
        return positions;
    }

    public Transform GetRespawnPoint()
    {
        return respawnPoint;
    }
}
