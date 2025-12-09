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

    // Variabel untuk tracking kotak (NON-STATIC agar reset tiap level)
    public int totalBoxCount = 0;
    private int boxesCollectedCount = 0;

    System.Collections.IEnumerator Start()
    {
        // Retry loop: Try to find GameManager for up to 3 seconds
        float timer = 0;
        while (GameManager.Instance == null && timer < 3.0f)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("BoxSpawner: GameManager instance not found after waiting. Redirecting to Lobby...");
            SceneManager.LoadScene(SceneNames.Lobby);
        }
        
        // Pastikan counter di-reset saat mulai
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
        if (PhotonNetwork.IsMasterClient)
        {
            totalBoxCount = jumlahKotakBiasa + jumlahKotakBonus;
            Bounds platformBounds = platformCollider.bounds;
            float padding = 3.0f;

            for (int i = 0; i < jumlahKotakBonus; i++)
            {
                float randomX = Random.Range(platformBounds.center.x - platformBounds.extents.x + padding,
                                               platformBounds.center.x + platformBounds.extents.x - padding);
                float randomZ = Random.Range(platformBounds.center.z - platformBounds.extents.z + padding,
                                               platformBounds.center.z + platformBounds.extents.z - padding);

                Vector3 spawnPosition = new Vector3(randomX, platformBounds.max.y + 0.75f, randomZ);
                PhotonNetwork.Instantiate(boxBonusPrefab.name, spawnPosition, Quaternion.identity);
            }

            List<Vector3> validPositions = GetValidOpenAreaPositions(openAreaSize, openAreaSize, platformBounds);
            List<Vector3> shuffledPositions = validPositions.OrderBy(a => Random.value).ToList();

            int spawnCount = Mathf.Min(jumlahKotakBiasa, shuffledPositions.Count);
            for (int i = 0; i < spawnCount; i++)
            {
                Vector3 spawnPosition = shuffledPositions[i];
                spawnPosition.y = 0.5f;
                PhotonNetwork.Instantiate(boxPrefab.name, spawnPosition, Quaternion.identity);
            }
        }
    }

    private List<Vector3> GetValidOpenAreaPositions(float areaX, float areaZ, Bounds exclusionZone)
    {
        List<Vector3> validPositions = new List<Vector3>();
        int gridWidth = Mathf.FloorToInt(areaX / minDistance);
        int gridHeight = Mathf.FloorToInt(areaZ / minDistance);
        exclusionZone.Expand(minDistance * 2);
        for (int x = 0; x < gridWidth; x++) {
            for (int z = 0; z < gridHeight; z++) {
                float worldX = x * minDistance - (areaX / 2f);
                float worldZ = z * minDistance - (areaZ / 2f);
                Vector3 currentPos = new Vector3(worldX, 0, worldZ);
                if (!exclusionZone.Contains(currentPos)) {
                    validPositions.Add(currentPos);
                }
            }
        }
        return validPositions;
    }

    public void SpawnBoxesInMaze(List<Vector2Int> floorPositions, MazeGenerator mazeGen)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            totalBoxCount = jumlahKotakBiasa + jumlahKotakBonus;
            List<Vector3> validWorldPositions = new List<Vector3>();
            foreach (var gridPos in floorPositions)
            {
                validWorldPositions.Add(mazeGen.GetWorldPosition(gridPos, 0f));
            }
            SpawnBoxesWithMinimumDistance(validWorldPositions);
        }
    }

    public void SpawnBoxesInOpenArea(float areaX, float areaZ)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            totalBoxCount = jumlahKotakBiasa + jumlahKotakBonus;
            List<Vector3> validWorldPositions = new List<Vector3>();
            int gridWidth = Mathf.FloorToInt(areaX / minDistance);
            int gridHeight = Mathf.FloorToInt(areaZ / minDistance);

            for (int x = 0; x < gridWidth; x++) {
                for (int z = 0; z < gridHeight; z++) {
                    float worldX = x * minDistance - (areaX / 2f) + (minDistance / 2f);
                    float worldZ = z * minDistance - (areaZ / 2f) + (minDistance / 2f);
                    validWorldPositions.Add(new Vector3(worldX, 0, worldZ));
                }
            }
            SpawnBoxesWithMinimumDistance(validWorldPositions);
        }
    }
    
    public void SpawnBoxesWithMinimumDistance(List<Vector3> validPositions)
    {
        List<Vector3> shuffledPositions = validPositions.OrderBy(a => Random.value).ToList();
        int totalBoxesToSpawn = jumlahKotakBiasa + jumlahKotakBonus;
        int spawnedCount = 0;

        if (shuffledPositions.Count < totalBoxesToSpawn) {
            totalBoxesToSpawn = shuffledPositions.Count;
        }

        // Spawn Bonus Box
        for (int i = 0; i < jumlahKotakBonus && spawnedCount < totalBoxesToSpawn; i++)
        {
            Vector3 spawnPosition = shuffledPositions[spawnedCount];
            spawnPosition.y = 0.75f;
            PhotonNetwork.Instantiate(boxBonusPrefab.name, spawnPosition, Quaternion.identity);
            spawnedCount++;
        }

        // Spawn Box Biasa
        for (int i = 0; i < jumlahKotakBiasa && spawnedCount < totalBoxesToSpawn; i++)
        {
            Vector3 spawnPosition = shuffledPositions[spawnedCount];
            spawnPosition.y = 0.5f;
            PhotonNetwork.Instantiate(boxPrefab.name, spawnPosition, Quaternion.identity);
            spawnedCount++;
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
            Debug.Log("BoxSpawner: Box collected. Collected: " + boxesCollectedCount + " / " + totalBoxCount);

            // Cek jika semua kotak sudah terkumpul
            if (boxesCollectedCount >= totalBoxCount)
            {
                Debug.Log("BoxSpawner: All boxes collected!");
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.EndGame();
                }
                else
                {
                    Debug.LogWarning("BoxSpawner: GameManager not found. Cannot call EndGame().");
                }
            }
        }
    }
}