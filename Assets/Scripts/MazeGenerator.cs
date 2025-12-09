using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PhotonView))]
public class MazeGenerator : MonoBehaviourPun
{
    public int width = 20;
    public int height = 20;
    public float cellSize = 2f;
    public GameObject wallPrefab;
    public Transform arenaCenter;

    private bool[,] visited;
    private List<Vector3> floorPositions = new List<Vector3>();
    public bool IsMazeReady { get; private set; }

    private readonly Vector2Int[] directions = {
        new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(1, 0), new Vector2Int(-1, 0)
    };

    System.Collections.IEnumerator Start()
    {
        float timer = 0;
        while (GameManager.Instance == null && timer < 3.0f) { timer += Time.deltaTime; yield return null; }
        if (GameManager.Instance == null) { SceneManager.LoadScene(SceneNames.Lobby); yield break; }

        if (!PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom != null && !IsMazeReady)
        {
            yield return new WaitForSeconds(0.5f);
            if (!IsMazeReady && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("MazeSeed", out object seedObj))
                GenerateMaze((int)seedObj);
        }
    }

    public void GenerateAndBuildMaze()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        int seed = Random.Range(0, 100000);
        PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "MazeSeed", seed } });
        GenerateMaze(seed);
        photonView?.RPC(nameof(SyncMazeSeed), RpcTarget.OthersBuffered, seed);
    }

    [PunRPC]
    public void SyncMazeSeed(int seed) => GenerateMaze(seed);

    void GenerateMaze(int seed)
    {
        Random.InitState(seed);
        ClearMaze();
        InitializeMaze();
        GenerateMazeRecursive(new Vector2Int(0, 0));
        BuildMaze();
        IsMazeReady = true;
    }

    void ClearMaze()
    {
        foreach (Transform child in transform)
            if (child.gameObject.name.Contains("Wall")) Destroy(child.gameObject);
        floorPositions.Clear();
        IsMazeReady = false;
    }

    void InitializeMaze()
    {
        if (width % 2 == 0) width++;
        if (height % 2 == 0) height++;
        visited = new bool[width, height];
    }

    void GenerateMazeRecursive(Vector2Int current)
    {
        visited[current.x, current.y] = true;
        floorPositions.Add(GetWorldPosition(current));
        Shuffle(directions);

        foreach (var dir in directions)
        {
            Vector2Int next = current + (dir * 2);
            if (IsInside(next) && !visited[next.x, next.y])
            {
                Vector2Int wall = current + dir;
                visited[wall.x, wall.y] = true;
                floorPositions.Add(GetWorldPosition(wall));
                GenerateMazeRecursive(next);
            }
        }
    }

    void BuildMaze()
    {
        float startX = -(width * cellSize) / 2f + (arenaCenter != null ? arenaCenter.position.x : 0);
        float startZ = -(height * cellSize) / 2f + (arenaCenter != null ? arenaCenter.position.z : 0);

        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
                if (!visited[x, z])
                {
                    Vector3 pos = new Vector3(startX + x * cellSize, wallPrefab.transform.localScale.y / 2, startZ + z * cellSize);
                    GameObject wall = Instantiate(wallPrefab, pos, Quaternion.identity, transform);
                    wall.name = $"Wall_{x}_{z}";
                }
    }

    bool IsInside(Vector2Int pos) => pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;

    public Vector3 GetWorldPosition(Vector2Int gridPos, float y = 0f)
    {
        float startX = -(width * cellSize) / 2f + (arenaCenter != null ? arenaCenter.position.x : 0);
        float startZ = -(height * cellSize) / 2f + (arenaCenter != null ? arenaCenter.position.z : 0);
        return new Vector3(startX + gridPos.x * cellSize, y, startZ + gridPos.y * cellSize);
    }

    public List<Vector3> GetFloorPositions() => floorPositions;

    void Shuffle<T>(T[] array)
    {
        int n = array.Length;
        while (n > 1) { n--; int k = Random.Range(0, n + 1); (array[k], array[n]) = (array[n], array[k]); }
    }
}