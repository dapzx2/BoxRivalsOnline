using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Linq;
using Hashtable = ExitGames.Client.Photon.Hashtable;

[RequireComponent(typeof(PhotonView))]
public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance { get; private set; }

    [Header("Player")]
    public GameObject playerPrefab;
    
    [Header("Level Obstacles")]
    public GameObject obstaclesLevel1;
    public GameObject obstaclesLevel2;
    public GameObject obstaclesLevel3;


    [Header("Referensi Level")]
    public float openAreaSize = 40f;
    [SerializeField] private MazeGenerator mazeGenerator;

    [Header("Referensi Komponen")]
    [SerializeField] private BoxSpawner boxSpawner;
    [SerializeField] private UIManager uiManager;

    private int currentLevelIndex = 1;
    private PhotonView pv;

    private bool isGameOver = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        pv = GetComponent<PhotonView>();
        if (pv == null)
        {
            Debug.LogWarning("GameManager is missing a PhotonView. Adding one at runtime.");
            pv = gameObject.AddComponent<PhotonView>();
        }

        // FIX: Avoid conflict with scene objects that use ViewID 1
        if (pv.ViewID == 1)
        {
            Debug.Log("GameManager: Changing ViewID from 1 to 901 to prevent conflicts.");
            // Must set to 0 first to release the ID, then assign new one
            pv.ViewID = 0;
            pv.ViewID = 901;
        }

        // ENSURE SCENE SYNC IS ON
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"GameManager: OnSceneLoaded called for scene '{scene.name}' (IsMasterClient: {PhotonNetwork.IsMasterClient})");
        
        if (scene.name == SceneNames.GameArena || scene.name == SceneNames.Level2 || 
            scene.name == SceneNames.Level3_SkyPlatforms || scene.name == SceneNames.Level3_ObstacleRush || 
            scene.name == SceneNames.Level3_RampRace)
        {
            // AGGRESSIVE CLEANUP: Destroy any other GameManager that might be in the scene
            GameManager[] managers = FindObjectsOfType<GameManager>();
            foreach (var mgr in managers)
            {
                if (mgr != this)
                {
                    Debug.LogWarning("GameManager: Duplicate found in scene! Destroying it to prevent ID conflict.");
                    Destroy(mgr.gameObject);
                }
            }

            // NOTE: Tidak perlu destroy player existing secara manual
            // PhotonNetwork.LoadLevel() akan otomatis destroy semua networked objects
            // dan spawn baru via InitializeGame()

            // Since GameManager persists, we need to find scene-specific objects again.
            obstaclesLevel1 = GameObject.Find("ObstaclesLevel1");
            obstaclesLevel2 = GameObject.Find("ObstaclesLevel2");
            boxSpawner = FindObjectOfType<BoxSpawner>();
            mazeGenerator = FindObjectOfType<MazeGenerator>();
            uiManager = FindObjectOfType<UIManager>();

            if (boxSpawner == null) Debug.LogError("GameManager: BoxSpawner not found in scene!");
            if (uiManager == null) Debug.LogError("GameManager: UIManager not found in scene!");
            // MazeGenerator is now optional since we are building levels manually
            if (mazeGenerator == null) Debug.Log("GameManager: MazeGenerator not found (Optional for manual levels).");

            if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("SelectedLevel", out object levelIndex))
            {
                currentLevelIndex = (int)levelIndex;
            }
            else
            {
                Debug.LogWarning("Could not find 'SelectedLevel' in room properties (or not in a room). Defaulting to level 1.");
                currentLevelIndex = 1;
            }

            if (obstaclesLevel1 != null)
            {
                obstaclesLevel1.SetActive(currentLevelIndex == 1);
            }
            else
            {
                Debug.LogWarning("Could not find 'ObstaclesLevel1' in the scene.");
            }

            if (obstaclesLevel2 != null)
            {
                obstaclesLevel2.SetActive(currentLevelIndex == 2);
            }
            else
            {
                Debug.LogWarning("Could not find 'ObstaclesLevel2' in the scene.");
            }

            // Reset state for a new game
            isGameOver = false;
            
            // Ensure PhotonView is ready
            if (pv.ViewID == 0 && PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("GameManager's PhotonView is not configured in the scene. Attempting to allocate a ViewID at runtime.");
                PhotonNetwork.AllocateViewID(pv);
            }
            
            InitializeGame();
        }
    }

    private void InitializeGame()
    {
        if (!PhotonNetwork.IsConnected)
        {
            SceneManager.LoadScene(SceneNames.Lobby);
            return;
        }

        Debug.Log($"GameManager: Preparing Level {currentLevelIndex}");

        // RESET SCORE for local player at start of each game/level
        // This is called on EVERY client when scene loads, so each client resets their own score
        if (PhotonNetwork.LocalPlayer != null)
        {
            Hashtable scoreProps = new Hashtable { { "score", 0 } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(scoreProps);
            Debug.Log($"GameManager: Score reset to 0 for {PhotonNetwork.LocalPlayer.NickName}");
        }

        // Master client handles level generation
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom != null)
        {
            Hashtable gameProps = new Hashtable { { "gameRunning", true } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(gameProps);

            switch (currentLevelIndex)
            {
                case 1:
                    if (boxSpawner != null)
                    {
                        boxSpawner.SetBoxCounts(18, 2);
                        boxSpawner.SpawnBoxesInOpenArea(openAreaSize, openAreaSize);
                    }
                    else Debug.LogError("BoxSpawner is not assigned for Level 1!");
                    break;
                case 2:
                    if (mazeGenerator != null)
                    {
                        Debug.Log("GameManager: MazeGenerator found. Generating random maze...");
                        mazeGenerator.GenerateAndBuildMaze();
                        boxSpawner.SetBoxCounts(25, 5);
                        // Spawn boxes on valid floor positions from the maze
                        List<Vector3> floorPos = mazeGenerator.GetFloorPositions();
                        boxSpawner.SpawnBoxesWithMinimumDistance(floorPos);
                    }
                    else if (boxSpawner != null)
                    {
                        Debug.Log("GameManager: MazeGenerator NOT found. Using manual level layout.");
                        // Level 2 is now a separate scene with pre-built environment
                        boxSpawner.SetBoxCounts(25, 5);
                        boxSpawner.SpawnBoxesInOpenArea(openAreaSize, openAreaSize); 
                    }
                    else Debug.LogError("BoxSpawner not assigned for Level 2!");
                    break;
                case 3:
                    // Level 3 uses map roulette - load roulette scene first
                    if (PhotonNetwork.IsMasterClient)
                    {
                        Debug.Log("GameManager: Loading Map Roulette for Level 3...");
                        PhotonNetwork.LoadLevel(SceneNames.MapRoulette);
                    }
                    break;
                default:
                    Debug.LogWarning($"Invalid level index ({currentLevelIndex}). Defaulting to Level 1.");
                    if (boxSpawner != null)
                    {
                        boxSpawner.SetBoxCounts(18, 2);
                        boxSpawner.SpawnBoxesInOpenArea(openAreaSize, openAreaSize);
                    }
                    break;
            }
        }

        // All clients handle player spawning
        // Pastikan player belum ada sebelum spawn
        if (HasLocalPlayer())
        {
            Debug.Log("GameManager: Local player already exists, skipping spawn.");
            return;
        }
        
        Debug.Log($"GameManager: Spawning player for {PhotonNetwork.LocalPlayer.NickName} (IsMasterClient: {PhotonNetwork.IsMasterClient})");
        
        switch (currentLevelIndex)
        {
            case 2:
                if (mazeGenerator != null)
                {
                    // Wait for maze to be ready (synced) before spawning
                    StartCoroutine(WaitForMazeAndSpawn(mazeGenerator));
                }
                else
                {
                    SpawnPlayersDefault();
                }
                break;
            case 3:
                Vector3 spawnPos1 = new Vector3(0, 2, 0);
                Vector3 spawnPos2 = new Vector3(0, 10, 25);
                Vector3 spawnPositionLvl3 = (PhotonNetwork.IsMasterClient) ? spawnPos1 : spawnPos2;
                PhotonNetwork.Instantiate(playerPrefab.name, spawnPositionLvl3, Quaternion.identity);
                break;
            default: // Level 1 and fallback
                SpawnPlayersDefault();
                break;
        }
    }

    private bool HasLocalPlayer()
    {
        PlayerController[] players = FindObjectsOfType<PlayerController>();
        foreach (var player in players)
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                return true;
            }
        }
        return false;
    }

    System.Collections.IEnumerator WaitForMazeAndSpawn(MazeGenerator mazeGen)
    {
        Debug.Log("GameManager: Waiting for maze generation...");
        
        float timeout = 5f;
        float timer = 0f;

        while (!mazeGen.IsMazeReady && timer < timeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (mazeGen.IsMazeReady)
        {
            Debug.Log("GameManager: Maze ready! Spawning players.");
            SpawnPlayersMaze(mazeGen);
        }
        else
        {
            Debug.LogError("GameManager: Maze generation timed out! Using default spawn.");
            SpawnPlayersDefault();
        }
    }

    void SpawnPlayersDefault()
    {
        // Check lagi apakah player sudah ada (untuk kasus spawn dari coroutine)
        if (HasLocalPlayer())
        {
            Debug.Log("GameManager: Local player already exists in SpawnPlayersDefault, skipping.");
            return;
        }
        
        // Increase Y to 3f to prevent spawning inside floor/ground
        Vector3 spawnPosition = (PhotonNetwork.IsMasterClient) ? new Vector3(-5, 3f, 0) : new Vector3(5, 3f, 0);
        Debug.Log($"GameManager: Spawning player at {spawnPosition}");
        PhotonNetwork.Instantiate(playerPrefab.name, spawnPosition, Quaternion.identity);
    }

    void SpawnPlayersMaze(MazeGenerator mazeGen)
    {
        // Check lagi apakah player sudah ada
        if (HasLocalPlayer())
        {
            Debug.Log("GameManager: Local player already exists in SpawnPlayersMaze, skipping.");
            return;
        }
        
        List<Vector3> safeSpawnPositions = mazeGen.GetFloorPositions();
        
        if (safeSpawnPositions.Count < 2)
        {
            Debug.LogError("Not enough safe spawn positions found in maze. Using default spawns.");
            SpawnPlayersDefault();
            return;
        }

        // Pick random positions
        Vector3 spawnPosition = (PhotonNetwork.IsMasterClient) 
            ? safeSpawnPositions[Random.Range(0, safeSpawnPositions.Count)]
            : safeSpawnPositions[Random.Range(0, safeSpawnPositions.Count)];
        
        // Ensure Y position is correct (slightly above ground)
        spawnPosition.y = 2f;

        Debug.Log($"GameManager: Spawning player at maze position {spawnPosition}");
        PhotonNetwork.Instantiate(playerPrefab.name, spawnPosition, Quaternion.identity);
    }



    public void UpdateScore(int newScore)
    {
        if (PhotonNetwork.LocalPlayer != null)
        {
            Hashtable scoreProps = new Hashtable { { "score", newScore } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(scoreProps);
        }
    }

    public void EndGame()
    {
        if (isGameOver) return;

        if (PhotonNetwork.IsMasterClient)
        {
            isGameOver = true;
            string winnerMessage = DetermineWinner();

            if (pv != null && pv.IsMine)
            {
                pv.RPC("RpcEndGame", RpcTarget.All, winnerMessage);
            }
            else 
            {
                RpcEndGame(winnerMessage);
            }
        }
    }

    [PunRPC]
    public void RpcEndGame(string message)
    {
        isGameOver = true;
        if (GetUIManager() != null)
        {
            GetUIManager().ShowGameOverScreen(message);
        }
    }

    private UIManager GetUIManager()
    {
        if (uiManager == null)
        {
            uiManager = FindObjectOfType<UIManager>();
        }
        return uiManager;
    }

    private string DetermineWinner()
    {
        var players = PhotonNetwork.PlayerList;
        
        int GetScore(int index) => players.Length > index && 
            players[index].CustomProperties.TryGetValue("score", out object s) ? (int)s : 0;
        
        int score1 = GetScore(0);
        int score2 = GetScore(1);
        
        string winner = score1 > score2 ? $"{players[0].NickName} Menang!" :
                        score2 > score1 ? $"{players[1].NickName} Menang!" :
                        "Hasilnya Seri!";
        
        return $"{winner}\nSkor Akhir: {score1} - {score2}";
    }
}