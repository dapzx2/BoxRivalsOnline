using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PhotonView))]
public class MazeGenerator : MonoBehaviourPun
{
    [Header("Maze Settings")]
    public int width = 20;
    public int height = 20;
    public float cellSize = 2f;
    public GameObject wallPrefab;

    [Header("Arena Settings")]
    public Transform arenaCenter; // Optional: Center of the maze

    private bool[,] visited;
    private List<Vector3> floorPositions = new List<Vector3>();
    
    // Flag to check if maze is ready (for clients)
    public bool IsMazeReady { get; private set; } = false;

    // Directions: Up, Down, Right, Left
    private readonly Vector2Int[] directions = {
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0)
    };

    System.Collections.IEnumerator Start()
    {
        if (photonView == null)
        {
            Debug.LogError("MazeGenerator: MISSING PHOTONVIEW! RPCs will not work.");
        }

        // Retry loop: Try to find GameManager for up to 1 second
        float timer = 0;
        while (GameManager.Instance == null && timer < 3.0f)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        // Redirect to Lobby ONLY if GameManager is still missing after retries
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("MazeGenerator: GameManager not found after waiting. Redirecting to Lobby...");
            SceneManager.LoadScene(SceneNames.Lobby);
            yield break;
        }

        // CLIENT: Check if maze seed exists in Room Properties (for rejoining or scene reload)
        // Skip if maze already generated (e.g., via RPC)
        if (!PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom != null && !IsMazeReady)
        {
            yield return new WaitForSeconds(0.5f); // Wait for room properties to sync
            
            // Double-check IsMazeReady after waiting (RPC might have arrived during wait)
            if (!IsMazeReady && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("MazeSeed", out object seedObj))
            {
                int seed = (int)seedObj;
                Debug.Log($"MazeGenerator: Client found MazeSeed {seed} in Room Properties. Generating maze...");
                GenerateMaze(seed);
            }
            else if (IsMazeReady)
            {
                Debug.Log("MazeGenerator: Maze already generated via RPC, skipping Room Properties generation.");
            }
        }
    }

    public void GenerateAndBuildMaze()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Generate a random seed
        int seed = Random.Range(0, 100000);
        
        // SIMPAN SEED DI ROOM PROPERTIES (persists dan bisa dibaca kapan saja)
        ExitGames.Client.Photon.Hashtable mazeProps = new ExitGames.Client.Photon.Hashtable
        {
            { "MazeSeed", seed }
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(mazeProps);
        Debug.Log($"MazeGenerator: Saved MazeSeed {seed} to Room Properties");
        
        // Generate locally for host
        GenerateMaze(seed);
        
        // Send seed to all clients so they generate the SAME maze logic
        // USE BUFFERED RPC so clients who load late still get the seed!
        if (photonView != null)
        {
            photonView.RPC("SyncMazeSeed", RpcTarget.OthersBuffered, seed);
        }
        else
        {
            Debug.LogError("MazeGenerator: PhotonView not found! Cannot sync maze seed.");
        }
    }

    [PunRPC]
    public void SyncMazeSeed(int seed)
    {
        Debug.Log($"MazeGenerator: Received seed {seed} from Master Client. Generating maze...");
        GenerateMaze(seed);
    }

    private void GenerateMaze(int seed)
    {
        // Use the seed for deterministic generation
        Random.InitState(seed);
        
        ClearMaze();
        InitializeMaze();
        GenerateMazeRecursive(new Vector2Int(0, 0));
        
        // BUILD WALLS LOCALLY on both Host and Client
        BuildMaze();
        
        IsMazeReady = true;
        Debug.Log($"MazeGenerator: Maze generation complete with seed {seed}. IsMazeReady = true");
    }

    private void ClearMaze()
    {
        // Destroy existing walls (Locally)
        foreach (Transform child in transform)
        {
            if (child.gameObject.name.Contains("Wall"))
            {
                Destroy(child.gameObject);
            }
        }
        
        floorPositions.Clear();
        IsMazeReady = false;
    }

    private void InitializeMaze()
    {
        // Ensure odd dimensions for walls and paths
        if (width % 2 == 0) width++;
        if (height % 2 == 0) height++;

        visited = new bool[width, height];
    }

    private void GenerateMazeRecursive(Vector2Int current)
    {
        visited[current.x, current.y] = true;
        floorPositions.Add(GetWorldPosition(current));

        // Shuffle directions
        Shuffle(directions);

        foreach (var dir in directions)
        {
            Vector2Int next = current + (dir * 2); // Jump over wall

            if (IsInside(next) && !visited[next.x, next.y])
            {
                // Mark wall between as visited (path)
                Vector2Int wall = current + dir;
                visited[wall.x, wall.y] = true;
                floorPositions.Add(GetWorldPosition(wall));

                GenerateMazeRecursive(next);
            }
        }
    }

    private void BuildMaze()
    {
        float startX = -(width * cellSize) / 2f;
        float startZ = -(height * cellSize) / 2f;

        if (arenaCenter != null)
        {
            startX += arenaCenter.position.x;
            startZ += arenaCenter.position.z;
        }

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                if (!visited[x, z]) // If not visited, it's a wall
                {
                    Vector3 pos = new Vector3(startX + x * cellSize, wallPrefab.transform.localScale.y / 2, startZ + z * cellSize);
                    
                    // LOCAL INSTANTIATION (No PhotonNetwork.Instantiate)
                    // This ensures walls appear even if prefab is not in Resources
                    GameObject wall = Instantiate(wallPrefab, pos, Quaternion.identity);
                    wall.transform.SetParent(transform);
                    wall.name = $"Wall_{x}_{z}";
                }
            }
        }
    }

    private bool IsInside(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
    }

    public Vector3 GetWorldPosition(Vector2Int gridPos, float y = 0f)
    {
        float startX = -(width * cellSize) / 2f;
        float startZ = -(height * cellSize) / 2f;

        if (arenaCenter != null)
        {
            startX += arenaCenter.position.x;
            startZ += arenaCenter.position.z;
        }

        return new Vector3(startX + gridPos.x * cellSize, y, startZ + gridPos.y * cellSize);
    }

    public List<Vector3> GetFloorPositions()
    {
        return floorPositions;
    }

    // Helper to shuffle array
    private void Shuffle<T>(T[] array)
    {
        int n = array.Length;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            T value = array[k];
            array[k] = array[n];
            array[n] = value;
        }
    }
}