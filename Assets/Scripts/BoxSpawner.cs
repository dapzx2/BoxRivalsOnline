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
        while (GameManager.Instance == null && timer < 3.0f) { timer += Time.deltaTime; yield return null; }
        if (GameManager.Instance == null) SceneManager.LoadScene(SceneNames.Lobby);
        boxesCollectedCount = 0;
    }

    public void SetBoxCounts(int biasa, int bonus)
    {
        jumlahKotakBiasa = biasa;
        jumlahKotakBonus = bonus;
        totalBoxCount = biasa + bonus;
        boxesCollectedCount = 0;
    }

    public void SpawnForVaultLevel(Collider platformCollider, float openAreaSize)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        totalBoxCount = jumlahKotakBiasa + jumlahKotakBonus;
        Bounds bounds = platformCollider.bounds;
        float padding = 3.0f;

        for (int i = 0; i < jumlahKotakBonus; i++)
        {
            float x = Random.Range(bounds.center.x - bounds.extents.x + padding, bounds.center.x + bounds.extents.x - padding);
            float z = Random.Range(bounds.center.z - bounds.extents.z + padding, bounds.center.z + bounds.extents.z - padding);
            PhotonNetwork.Instantiate(boxBonusPrefab.name, new Vector3(x, bounds.max.y + 0.75f, z), Quaternion.identity);
        }

        var positions = GetValidOpenAreaPositions(openAreaSize, openAreaSize, bounds).OrderBy(a => Random.value).ToList();
        int count = Mathf.Min(jumlahKotakBiasa, positions.Count);
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = positions[i]; pos.y = 0.5f;
            PhotonNetwork.Instantiate(boxPrefab.name, pos, Quaternion.identity);
        }
    }

    private List<Vector3> GetValidOpenAreaPositions(float areaX, float areaZ, Bounds exclusionZone)
    {
        List<Vector3> validPositions = new List<Vector3>();
        int gridW = Mathf.FloorToInt(areaX / minDistance);
        int gridH = Mathf.FloorToInt(areaZ / minDistance);
        exclusionZone.Expand(minDistance * 2);

        for (int x = 0; x < gridW; x++)
        {
            for (int z = 0; z < gridH; z++)
            {
                Vector3 pos = new Vector3(x * minDistance - (areaX / 2f), 0, z * minDistance - (areaZ / 2f));
                if (!exclusionZone.Contains(pos)) validPositions.Add(pos);
            }
        }
        return validPositions;
    }

    public void SpawnBoxesInMaze(List<Vector2Int> floorPositions, MazeGenerator mazeGen)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        totalBoxCount = jumlahKotakBiasa + jumlahKotakBonus;
        SpawnBoxesWithMinimumDistance(floorPositions.Select(p => mazeGen.GetWorldPosition(p, 0f)).ToList());
    }

    public void SpawnBoxesInOpenArea(float areaX, float areaZ)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        totalBoxCount = jumlahKotakBiasa + jumlahKotakBonus;

        List<Vector3> positions = new List<Vector3>();
        int gridW = Mathf.FloorToInt(areaX / minDistance);
        int gridH = Mathf.FloorToInt(areaZ / minDistance);

        for (int x = 0; x < gridW; x++)
            for (int z = 0; z < gridH; z++)
                positions.Add(new Vector3(x * minDistance - (areaX / 2f) + (minDistance / 2f), 0, z * minDistance - (areaZ / 2f) + (minDistance / 2f)));

        SpawnBoxesWithMinimumDistance(positions);
    }
    
    public void SpawnBoxesWithMinimumDistance(List<Vector3> validPositions)
    {
        var positions = validPositions.OrderBy(a => Random.value).ToList();
        int total = Mathf.Min(jumlahKotakBiasa + jumlahKotakBonus, positions.Count);
        int spawned = 0;

        for (int i = 0; i < jumlahKotakBonus && spawned < total; i++, spawned++)
        {
            Vector3 pos = positions[spawned]; pos.y = 0.75f;
            PhotonNetwork.Instantiate(boxBonusPrefab.name, pos, Quaternion.identity);
        }

        for (int i = 0; i < jumlahKotakBiasa && spawned < total; i++, spawned++)
        {
            Vector3 pos = positions[spawned]; pos.y = 0.5f;
            PhotonNetwork.Instantiate(boxPrefab.name, pos, Quaternion.identity);
        }
    }

    [PunRPC]
    public void RpcDestroyBox(int viewID)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        PhotonView targetView = PhotonView.Find(viewID);
        if (targetView != null)
        {
            PhotonNetwork.Destroy(targetView.gameObject);
            boxesCollectedCount++;
            if (boxesCollectedCount >= totalBoxCount) GameManager.Instance?.EndGame();
        }
    }
}