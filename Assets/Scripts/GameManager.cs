using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using System.Collections.Generic;
using Hashtable = ExitGames.Client.Photon.Hashtable;

[RequireComponent(typeof(PhotonView))]
public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance { get; private set; }
    public GameObject playerPrefab;
    public GameObject playerPrefabClient;
    public float openAreaSize = 40f;

    private int currentLevelIndex = 1;
    private PhotonView pv;
    private bool isGameOver;
    private BoxSpawner boxSpawner;
    private MazeGenerator mazeGenerator;
    private UIManager uiManager;

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
        if (pv == null) pv = gameObject.AddComponent<PhotonView>();
        
        if (pv.ViewID == 1)
        {
            pv.ViewID = 0;
            pv.ViewID = 901;
        }
        
        PhotonNetwork.AutomaticallySyncScene = true;
        _ = AudioManager.Instance;

        if (FindObjectOfType<AudioListener>() == null)
        {
            Camera cam = Camera.main;
            if (cam != null) cam.gameObject.AddComponent<AudioListener>();
        }
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
        if (!IsGameScene(scene.name)) return;

        foreach (var mgr in FindObjectsOfType<GameManager>())
        {
            if (mgr != this) Destroy(mgr.gameObject);
        }

        boxSpawner = FindObjectOfType<BoxSpawner>();
        mazeGenerator = FindObjectOfType<MazeGenerator>();
        uiManager = FindObjectOfType<UIManager>();

        currentLevelIndex = 1;
        if (PhotonNetwork.CurrentRoom?.CustomProperties.TryGetValue("SelectedLevel", out object lvl) == true)
        {
            currentLevelIndex = (int)lvl;
        }

        isGameOver = false;

        if (pv.ViewID == 0 && PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.AllocateViewID(pv);
        }

        SetupSceneLighting();
        InitializeGame();
    }

    bool IsGameScene(string sceneName)
    {
        return sceneName == SceneNames.GameArena || 
               sceneName == SceneNames.Level2 || 
               sceneName.StartsWith("Level3_");
    }

    void InitializeGame()
    {
        if (!PhotonNetwork.IsConnected)
        {
            SceneManager.LoadScene(SceneNames.Lobby);
            return;
        }

        PhotonNetwork.LocalPlayer?.SetCustomProperties(new Hashtable { { "score", 0 } });

        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom != null)
        {
            PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { "gameRunning", true } });
            SetupLevelContent();
        }

        if (!HasLocalPlayer()) SpawnPlayer();
    }

    void SetupLevelContent()
    {
        switch (currentLevelIndex)
        {
            case 2:
                if (mazeGenerator != null)
                {
                    mazeGenerator.GenerateAndBuildMaze();
                    boxSpawner?.SetBoxCounts(25, 5);
                    boxSpawner?.SpawnBoxesWithMinimumDistance(mazeGenerator.GetFloorPositions());
                }
                else
                {
                    SetupDefaultBoxes();
                }
                break;
            case 3:
                if (boxSpawner != null) SpawnLevel3Boxes();
                break;
            default:
                SetupDefaultBoxes();
                break;
        }
    }

    void SetupDefaultBoxes()
    {
        if (boxSpawner != null)
        {
            boxSpawner.SetBoxCounts(18, 2);
            boxSpawner.SpawnBoxesInOpenArea(openAreaSize, openAreaSize);
        }
    }

    void SpawnPlayer()
    {
        switch (currentLevelIndex)
        {
            case 2:
                if (mazeGenerator != null) StartCoroutine(WaitForMazeAndSpawn(mazeGenerator));
                else SpawnPlayersDefault();
                break;
            case 3:
                SpawnLevel3Player();
                break;
            default:
                SpawnPlayersDefault();
                break;
        }
    }

    bool HasLocalPlayer()
    {
        foreach (var player in FindObjectsOfType<PlayerController>())
        {
            if (player.GetComponent<PhotonView>()?.IsMine == true) return true;
        }
        return false;
    }

    System.Collections.IEnumerator WaitForMazeAndSpawn(MazeGenerator mazeGen)
    {
        float timer = 0f;
        while (!mazeGen.IsMazeReady && timer < 5f)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (mazeGen.IsMazeReady) SpawnPlayersMaze(mazeGen);
        else SpawnPlayersDefault();
    }

    void SpawnPlayersDefault()
    {
        if (HasLocalPlayer()) return;
        Vector3 pos = PhotonNetwork.IsMasterClient ? new Vector3(-5, 3f, 0) : new Vector3(5, 3f, 0);
        PhotonNetwork.Instantiate(PhotonNetwork.IsMasterClient ? "Pemain1" : "Pemain2", pos, Quaternion.identity);
    }

    void SpawnPlayersMaze(MazeGenerator mazeGen)
    {
        if (HasLocalPlayer()) return;
        List<Vector3> positions = mazeGen.GetFloorPositions();
        
        if (positions.Count < 2)
        {
            SpawnPlayersDefault();
            return;
        }

        Vector3 pos = positions[Random.Range(0, positions.Count)];
        pos.y = 2f;
        PhotonNetwork.Instantiate(PhotonNetwork.IsMasterClient ? "Pemain1" : "Pemain2", pos, Quaternion.identity);
    }

    void SpawnLevel3Player()
    {
        if (HasLocalPlayer()) return;
        float spawnY = GetLevel3SpawnY();
        Vector3 spawnPos = PhotonNetwork.IsMasterClient ? new Vector3(0, spawnY, -2) : new Vector3(0, spawnY, 2);
        PhotonNetwork.Instantiate(PhotonNetwork.IsMasterClient ? "Pemain1" : "Pemain2", spawnPos, Quaternion.identity);
    }

    public void UpdateScore(int newScore)
    {
        PhotonNetwork.LocalPlayer?.SetCustomProperties(new Hashtable { { "score", newScore } });
    }

    public void EndGame()
    {
        if (isGameOver || !PhotonNetwork.IsMasterClient) return;
        
        isGameOver = true;
        string msg = DetermineWinner();
        
        if (pv != null && pv.IsMine) pv.RPC(nameof(RpcEndGame), RpcTarget.All, msg);
        else RpcEndGame(msg);
    }

    [PunRPC]
    public void RpcEndGame(string message)
    {
        isGameOver = true;
        if (uiManager == null) uiManager = FindObjectOfType<UIManager>();
        uiManager?.ShowGameOverScreen(message);
    }

    string DetermineWinner()
    {
        var players = PhotonNetwork.PlayerList;
        int GetScore(int i) => players.Length > i && players[i].CustomProperties.TryGetValue("score", out object s) ? (int)s : 0;
        
        int s1 = GetScore(0);
        int s2 = GetScore(1);
        
        string winner;
        if (s1 > s2) winner = $"{players[0].NickName} Menang!";
        else if (s2 > s1) winner = $"{players[1].NickName} Menang!";
        else winner = "Hasilnya Seri!";
        
        return $"{winner}\nSkor Akhir: {s1} - {s2}";
    }

    void SpawnLevel3Boxes()
    {
        if (!PhotonNetwork.IsMasterClient || boxSpawner == null) return;

        string sceneName = SceneManager.GetActiveScene().name;
        Vector3[] boxPositions = GetLevel3BoxPositions(sceneName);

        boxSpawner.SetBoxCounts(boxPositions.Length, 0);
        string normalPrefab = boxSpawner.boxPrefab != null ? boxSpawner.boxPrefab.name : "KotakKoleksi";
        string bonusPrefab = boxSpawner.boxBonusPrefab != null ? boxSpawner.boxBonusPrefab.name : "KotakBonus";
        
        foreach (var pos in boxPositions)
        {
            Vector3 safePos = GetSafeBoxPosition(pos, out bool wasOffset);
            string prefabToSpawn = wasOffset ? bonusPrefab : normalPrefab;
            PhotonNetwork.Instantiate(prefabToSpawn, safePos, Quaternion.identity);
        }
    }

    Vector3[] GetLevel3BoxPositions(string sceneName)
    {
        if (sceneName == SceneNames.Level3_SkyPlatforms)
        {
            return new Vector3[] {
                new Vector3(0, 1.5f, 0), new Vector3(12, 2.5f, 0), new Vector3(20, 3.5f, 5),
                new Vector3(28, 2.5f, -3), new Vector3(35, 4.5f, 2), new Vector3(42, 3.5f, -5),
                new Vector3(50, 5.5f, 0), new Vector3(58, 4.5f, 3), new Vector3(65, 6.5f, 0),
                new Vector3(75, 6.5f, 0)
            };
        }
        
        if (sceneName == SceneNames.Level3_ObstacleRush)
        {
            return new Vector3[] {
                new Vector3(0, 1.5f, 0), new Vector3(12, 2.5f, 0), new Vector3(22, 3f, 0),
                new Vector3(32, 3.5f, 0), new Vector3(42, 4f, 0), new Vector3(52, 4.5f, 0),
                new Vector3(62, 5f, 0), new Vector3(72, 5.5f, 0), new Vector3(82, 6f, 0),
                new Vector3(92, 6.5f, 0)
            };
        }
        
        if (sceneName == SceneNames.Level3_RampRace)
        {
            return new Vector3[] {
                new Vector3(0, 6f, 0), new Vector3(25, 5f, 0), new Vector3(45, 7f, 0),
                new Vector3(65, 9f, 0), new Vector3(85, 11f, 0), new Vector3(105, 13f, 0),
                new Vector3(125, 15f, 0), new Vector3(135, 13f, 0)
            };
        }
        
        return new Vector3[] { new Vector3(0, 1.5f, 0) };
    }

    Vector3 GetSafeBoxPosition(Vector3 originalPos, out bool wasOffset)
    {
        wasOffset = false;
        float checkRadius = 1.5f;
        float offsetStep = 2f;
        int maxAttempts = 5;
        Vector3 pos = originalPos;

        for (int i = 0; i < maxAttempts; i++)
        {
            bool hasObstacle = false;
            Collider[] obstacles = Physics.OverlapSphere(pos, checkRadius);
            
            foreach (var col in obstacles)
            {
                if (col.GetComponent<RotatingObstacle>() != null)
                {
                    hasObstacle = true;
                    break;
                }
            }

            if (!hasObstacle) return pos;
            
            pos.y += offsetStep;
            wasOffset = true;
        }

        return pos;
    }

    public void RespawnPlayer(GameObject player)
    {
        if (player == null) return;
        float spawnY = currentLevelIndex == 3 ? GetLevel3SpawnY() : 3f;
        player.transform.position = new Vector3(0, spawnY, 0);
        
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null) rb.velocity = Vector3.zero;
    }

    float GetLevel3SpawnY()
    {
        return SceneManager.GetActiveScene().name == SceneNames.Level3_RampRace ? 4f : 2f;
    }

    void SetupSceneLighting()
    {
        Light mainLight = FindDirectionalLight();
        if (mainLight == null) mainLight = CreateDefaultLight();

        mainLight.intensity = 1.0f;
        mainLight.shadows = LightShadows.Soft;
        mainLight.shadowStrength = 0.7f;

        if (RenderSettings.skybox == null) SetupProceduralSkybox();

        RenderSettings.sun = mainLight;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.65f, 0.65f, 0.65f);
        RenderSettings.ambientIntensity = 1.0f;

        Camera cam = Camera.main;
        if (cam != null)
        {
            if (RenderSettings.skybox != null) cam.clearFlags = CameraClearFlags.Skybox;
            else
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.4f, 0.6f, 0.9f);
            }
        }
    }

    Light FindDirectionalLight()
    {
        foreach (var l in FindObjectsOfType<Light>())
        {
            if (l.type == LightType.Directional) return l;
        }
        return null;
    }

    Light CreateDefaultLight()
    {
        GameObject lightGO = new GameObject("Main Light");
        Light l = lightGO.AddComponent<Light>();
        l.type = LightType.Directional;
        l.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        return l;
    }

    void SetupProceduralSkybox()
    {
        Shader skyShader = Shader.Find("Skybox/Procedural");
        if (skyShader != null)
        {
            Material skyMat = new Material(skyShader);
            skyMat.SetFloat("_SunSize", 0.04f);
            skyMat.SetFloat("_AtmosphereThickness", 1.0f);
            RenderSettings.skybox = skyMat;
        }
    }
}