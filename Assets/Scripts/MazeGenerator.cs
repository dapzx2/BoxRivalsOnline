using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine.SceneManagement;
using System.Linq;
using Hashtable = ExitGames.Client.Photon.Hashtable;

[RequireComponent(typeof(PhotonView))]
public class MazeGenerator : MonoBehaviourPun
{
    [Header("Maze Settings")]
    public float targetCellSize = 2f;
    public GameObject wallPrefab;
    
    [Header("Size Mode")]
    public bool useAutoFit = true;
    public bool autoStretchCells = true;
    
    [Tooltip("Deducts units from Total Bounds to account for Wall Thickness.")]
    public float safetyMargin = 2.0f;
    
    [Header("Manual Settings (if AutoFit OFF)")]
    public int manualWidth = 25;
    public int manualHeight = 37;

    [Header("Detection Settings")]
    public bool useAggressiveScan = true;
    public Transform floorObject; 
    public Transform arenaCenter; 

    // Runtime Calculations
    public int GridWidth { get; private set; }
    public int GridHeight { get; private set; }
    public float RealCellSizeX { get; private set; }
    public float RealCellSizeZ { get; private set; }
    public bool IsMazeReady { get; private set; }
    
    private bool[,] visited;
    private List<Vector3> floorPositions = new List<Vector3>();
    private Bounds lastDetectedBounds;
    private Bounds lastPlayableBounds;

    private readonly Vector2Int[] directions = {
        new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(1, 0), new Vector2Int(-1, 0)
    };

    private System.Collections.IEnumerator Start()
    {
        InitializeDimensions();

        float timer = 0;
        while (GameManager.Instance == null && timer < 3.0f) 
        { 
            timer += Time.deltaTime; 
            yield return null; 
        }

        if (GameManager.Instance == null)
        {
            SceneManager.LoadScene(SceneNames.Lobby);
            yield break;
        }

        if (!PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom != null && !IsMazeReady)
        {
            yield return new WaitForSeconds(0.5f);
            if (!IsMazeReady && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("MazeSeed", out object seedObj))
            {
                GenerateMaze((int)seedObj);
            }
        }
    }

    private void InitializeDimensions()
    {
        if (useAutoFit)
        {
            CalculateMazeDimensions();
        }
        else
        {
            GridWidth = manualWidth;
            GridHeight = manualHeight;
            RealCellSizeX = targetCellSize;
            RealCellSizeZ = targetCellSize;
        }
    }

    [ContextMenu("Recalculate Bounds")]
    public void CalculateMazeDimensions()
    {
        Bounds combinedBounds = CalculateCombinedAreaBounds();
        lastDetectedBounds = combinedBounds;

        if (combinedBounds.size != Vector3.zero)
        {
            float playableX = Mathf.Max(0, combinedBounds.size.x - safetyMargin);
            float playableZ = Mathf.Max(0, combinedBounds.size.z - safetyMargin);
            lastPlayableBounds = new Bounds(combinedBounds.center, new Vector3(playableX, combinedBounds.size.y, playableZ));

            GridWidth = Mathf.RoundToInt(playableX / targetCellSize);
            GridHeight = Mathf.RoundToInt(playableZ / targetCellSize);
            
            if (GridWidth < 2) GridWidth = 2;
            if (GridHeight < 2) GridHeight = 2;

            if (autoStretchCells)
            {
                RealCellSizeX = playableX / GridWidth;
                RealCellSizeZ = playableZ / GridHeight;
            }
            else
            {
                RealCellSizeX = targetCellSize;
                RealCellSizeZ = targetCellSize;
                GridWidth = Mathf.FloorToInt(playableX / targetCellSize);
                GridHeight = Mathf.FloorToInt(playableZ / targetCellSize);
            }

            if (arenaCenter == null)
            {
                GameObject centerObj = GameObject.Find("MazeCenter_Auto");
                if (centerObj == null) centerObj = new GameObject("MazeCenter_Auto");
                centerObj.transform.position = combinedBounds.center;
                arenaCenter = centerObj.transform;
            }
        }
        else
        {
            Debug.LogError("[MazeGenerator] Bounds check failed. Using manual defaults.");
            GridWidth = manualWidth;
            GridHeight = manualHeight;
            RealCellSizeX = targetCellSize;
            RealCellSizeZ = targetCellSize;
        }
    }

    private Bounds CalculateCombinedAreaBounds()
    {
        if (floorObject != null && !useAggressiveScan) return GetBounds(floorObject.gameObject);

        List<GameObject> candidates = new List<GameObject>();
        var allColliders = FindObjectsOfType<Collider>();
        
        foreach (var col in allColliders)
        {
            if (IsValidForAggressiveScan(col.gameObject)) 
                candidates.Add(col.gameObject);
        }

        if (candidates.Count == 0) return new Bounds(Vector3.zero, Vector3.zero);

        Bounds totalBounds = new Bounds(Vector3.zero, Vector3.zero);
        bool first = true;

        foreach (var go in candidates)
        {
            Bounds b = GetBounds(go);
            if (b.size == Vector3.zero || (b.size.x < 1 && b.size.z < 1)) continue;

            if (first) { totalBounds = b; first = false; }
            else totalBounds.Encapsulate(b);
        }
        return totalBounds;
    }
    
    private bool IsValidForAggressiveScan(GameObject go)
    {
        if (go.CompareTag("Player")) return false;
        string name = go.name.ToLower();
        if (name.Contains("wall_")) return false; 
        if (go.transform.root.name.Contains("Canvas")) return false;
        if (go.GetComponent<RectTransform>() != null) return false;
        
        Collider c = go.GetComponent<Collider>();
        if (c != null && c.isTrigger) return false;
        return true;
    }

    private Bounds GetBounds(GameObject go)
    {
        Bounds b = new Bounds(Vector3.zero, Vector3.zero);
        Collider c = go.GetComponent<Collider>();
        Renderer r = go.GetComponent<Renderer>();
        if (c != null) b = c.bounds;
        else if (r != null) b = r.bounds;
        return b;
    }

    public void GenerateAndBuildMaze()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (GridWidth == 0) InitializeDimensions();

        int seed = Random.Range(0, 100000);
        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { "MazeSeed", seed } });
        GenerateMaze(seed);
        photonView?.RPC(nameof(SyncMazeSeed), RpcTarget.OthersBuffered, seed);
    }

    [PunRPC]
    public void SyncMazeSeed(int seed) => GenerateMaze(seed);

    private void GenerateMaze(int seed)
    {
        Random.InitState(seed);
        ClearMaze();
        InitializeMaze();
        GenerateMazeRecursive(new Vector2Int(0, 0));
        BuildMaze();
        IsMazeReady = true;
    }

    private void ClearMaze()
    {
        foreach (Transform child in transform)
        {
            if (child.gameObject.name.Contains("Wall")) Destroy(child.gameObject);
        }
        floorPositions.Clear();
        IsMazeReady = false;
    }

    private void InitializeMaze()
    {
        visited = new bool[GridWidth, GridHeight];
    }

    private void GenerateMazeRecursive(Vector2Int current)
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
                if (IsInside(wall))
                {
                    visited[wall.x, wall.y] = true;
                    floorPositions.Add(GetWorldPosition(wall));
                    GenerateMazeRecursive(next);
                }
            }
        }
    }

    private void BuildMaze()
    {
        Vector3 startPos = GetMazeStartCorner();

        for (int x = 0; x < GridWidth; x++)
        {
            for (int z = 0; z < GridHeight; z++)
            {
                if (x >= visited.GetLength(0) || z >= visited.GetLength(1)) continue;

                if (!visited[x, z])
                {
                    Vector3 pos = new Vector3(
                        startPos.x + (x * RealCellSizeX) + (RealCellSizeX / 2f), 
                        wallPrefab.transform.localScale.y / 2, 
                        startPos.z + (z * RealCellSizeZ) + (RealCellSizeZ / 2f)
                    );
                    
                    GameObject wall = Instantiate(wallPrefab, pos, Quaternion.identity, transform);
                    wall.name = $"Wall_{x}_{z}";
                    
                    float originalY = wall.transform.localScale.y;
                    wall.transform.localScale = new Vector3(RealCellSizeX, originalY, RealCellSizeZ);
                }
            }
        }
    }

    private bool IsInside(Vector2Int pos) => pos.x >= 0 && pos.x < GridWidth && pos.y >= 0 && pos.y < GridHeight;

    private Vector3 GetMazeStartCorner()
    {
        float centerX = arenaCenter != null ? arenaCenter.position.x : 0;
        float centerZ = arenaCenter != null ? arenaCenter.position.z : 0;
        return new Vector3(
            centerX - (GridWidth * RealCellSizeX) / 2f,
            0,
            centerZ - (GridHeight * RealCellSizeZ) / 2f
        );
    }

    public Vector3 GetWorldPosition(Vector2Int gridPos, float y = 0f)
    {
        Vector3 startPos = GetMazeStartCorner();
        return new Vector3(
            startPos.x + (gridPos.x * RealCellSizeX) + (RealCellSizeX / 2f), 
            y, 
            startPos.z + (gridPos.y * RealCellSizeZ) + (RealCellSizeZ / 2f)
        );
    }

    public List<Vector3> GetFloorPositions() => floorPositions;

    private void Shuffle<T>(T[] array)
    {
        int n = array.Length;
        while (n > 1) { n--; int k = Random.Range(0, n + 1); (array[k], array[n]) = (array[n], array[k]); }
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying && visited != null)
        {
            Gizmos.color = new Color(0, 0, 1, 0.3f);
            if (lastDetectedBounds.size != Vector3.zero) Gizmos.DrawCube(lastDetectedBounds.center, lastDetectedBounds.size);
            
            Gizmos.color = Color.green;
            if (lastPlayableBounds.size != Vector3.zero) Gizmos.DrawWireCube(lastDetectedBounds.center, lastPlayableBounds.size);
            
            Gizmos.color = Color.yellow;
            Vector3 center = arenaCenter != null ? arenaCenter.position : transform.position;
            Vector3 size = new Vector3(GridWidth * RealCellSizeX, 2f, GridHeight * RealCellSizeZ);
            Gizmos.DrawWireCube(center, size);
        }
    }
}