using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class BoxSpawner : MonoBehaviourPun
{
    public GameObject boxPrefab;
    public GameObject boxBonusPrefab;
    public int jumlahKotakBiasa = 18;
    public int jumlahKotakBonus = 2;
    public float minDistance = 2.5f;
    public int totalBoxCount = 0;
    
    private int boxesCollectedCount = 0;

    System.Collections.IEnumerator Start()
    {
        float timer = 0;
        while (GameManager.Instance == null && timer < 3.0f) 
        { 
            timer += Time.deltaTime; 
            yield return null; 
        }
        
        if (GameManager.Instance == null)
        {
            SceneManager.LoadScene(SceneNames.Lobby);
        }
        else
        {
            boxesCollectedCount = 0;
        }
    }

    public void SetBoxCounts(int biasa, int bonus)
    {
        jumlahKotakBiasa = biasa;
        jumlahKotakBonus = bonus;
        totalBoxCount = biasa + bonus;
        boxesCollectedCount = 0;
    }







    public void SpawnBoxesInOpenArea(float areaX, float areaZ)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        totalBoxCount = jumlahKotakBiasa + jumlahKotakBonus;

        List<Vector3> positions = new List<Vector3>();
        int gridW = Mathf.FloorToInt(areaX / minDistance);
        int gridH = Mathf.FloorToInt(areaZ / minDistance);

        for (int x = 0; x < gridW; x++)
        {
            for (int z = 0; z < gridH; z++)
            {
                float posX = x * minDistance - (areaX / 2f) + (minDistance / 2f);
                float posZ = z * minDistance - (areaZ / 2f) + (minDistance / 2f);
                positions.Add(new Vector3(posX, 0, posZ));
            }
        }

        SpawnBoxesWithMinimumDistance(positions);
    }
    
    public void SpawnBoxesWithMinimumDistance(List<Vector3> validPositions)
    {
        var positions = validPositions.OrderBy(a => Random.value).ToList();
        int total = Mathf.Min(jumlahKotakBiasa + jumlahKotakBonus, positions.Count);
        int spawned = 0;

        for (int i = 0; i < jumlahKotakBonus && spawned < total; i++, spawned++)
        {
            Vector3 pos = positions[spawned];
            pos.y = 0.75f;
            PhotonNetwork.Instantiate(boxBonusPrefab.name, pos, Quaternion.identity);
        }

    for (int i = 0; i < jumlahKotakBiasa && spawned < total; i++, spawned++)
        {
            Vector3 pos = positions[spawned];
            pos.y = 0.5f;
            PhotonNetwork.Instantiate(boxPrefab.name, pos, Quaternion.identity);
        }

        totalBoxCount = spawned;
    }

    [PunRPC]
    public void RpcDestroyBox(int viewID)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        PhotonView targetView = PhotonView.Find(viewID);
        if (targetView != null)
        {
            boxesCollectedCount++;
            PhotonNetwork.Destroy(targetView.gameObject);
            
            if (boxesCollectedCount >= totalBoxCount)
            {
                Invoke(nameof(TriggerEndGame), 1.0f);
            }
        }
    }

    void TriggerEndGame()
    {
        GameManager.Instance?.EndGame();
    }
}