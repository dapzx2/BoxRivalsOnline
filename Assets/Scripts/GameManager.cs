using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using Hashtable = ExitGames.Client.Photon.Hashtable;

[RequireComponent(typeof(PhotonView))]
public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance { get; private set; }

    [Header("Player")]
    public GameObject playerPrefab;
    public GameObject playerPrefabClient;
    
    [Header("Referensi Level")]
    public float openAreaSize = 40f;

    private int currentLevelIndex = 1;
    private PhotonView pv;
    private bool isGameOver = false;
    private BoxSpawner boxSpawner;
    private MazeGenerator mazeGenerator;
    private UIManager uiManager;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        pv = GetComponent<PhotonView>();
        if (pv == null) pv = gameObject.AddComponent<PhotonView>();
        if (pv.ViewID == 1) { pv.ViewID = 0; pv.ViewID = 901; }
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public override void OnEnable() { base.OnEnable(); SceneManager.sceneLoaded += OnSceneLoaded; }
    public override void OnDisable() { base.OnDisable(); SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == SceneNames.GameArena || scene.name == SceneNames.Level2 || 
            scene.name == SceneNames.Level3_SkyPlatforms || scene.name == SceneNames.Level3_ObstacleRush || 
            scene.name == SceneNames.Level3_RampRace)
        {
            foreach (var mgr in FindObjectsOfType<GameManager>())
                if (mgr != this) Destroy(mgr.gameObject);

            boxSpawner = FindObjectOfType<BoxSpawner>();
            mazeGenerator = FindObjectOfType<MazeGenerator>();
            uiManager = FindObjectOfType<UIManager>();

            currentLevelIndex = 1;
            if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("SelectedLevel", out object lvl))
                currentLevelIndex = (int)lvl;

            isGameOver = false;
            if (pv.ViewID == 0 && PhotonNetwork.IsMasterClient) PhotonNetwork.AllocateViewID(pv);
            InitializeGame();
        }
    }

    private void InitializeGame()
    {
        if (!PhotonNetwork.IsConnected) { SceneManager.LoadScene(SceneNames.Lobby); return; }

        if (PhotonNetwork.LocalPlayer != null)
            PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { "score", 0 } });

        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom != null)
        {
            PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { "gameRunning", true } });

            switch (currentLevelIndex)
            {
                case 1:
                    if (boxSpawner != null) { boxSpawner.SetBoxCounts(18, 2); boxSpawner.SpawnBoxesInOpenArea(openAreaSize, openAreaSize); }
                    break;
                case 2:
                    if (mazeGenerator != null)
                    {
                        mazeGenerator.GenerateAndBuildMaze();
                        boxSpawner.SetBoxCounts(25, 5);
                        boxSpawner.SpawnBoxesWithMinimumDistance(mazeGenerator.GetFloorPositions());
                    }
                    else if (boxSpawner != null) { boxSpawner.SetBoxCounts(25, 5); boxSpawner.SpawnBoxesInOpenArea(openAreaSize, openAreaSize); }
                    break;
                case 3:
                    PhotonNetwork.LoadLevel(SceneNames.MapRoulette);
                    break;
                default:
                    if (boxSpawner != null) { boxSpawner.SetBoxCounts(18, 2); boxSpawner.SpawnBoxesInOpenArea(openAreaSize, openAreaSize); }
                    break;
            }
        }

        if (HasLocalPlayer()) return;
        
        switch (currentLevelIndex)
        {
            case 2:
                if (mazeGenerator != null) StartCoroutine(WaitForMazeAndSpawn(mazeGenerator));
                else SpawnPlayersDefault();
                break;
            case 3:
                Vector3 pos = PhotonNetwork.IsMasterClient ? new Vector3(0, 2, 0) : new Vector3(0, 10, 25);
                PhotonNetwork.Instantiate(PhotonNetwork.IsMasterClient ? "Pemain1" : "Pemain2", pos, Quaternion.identity);
                break;
            default:
                SpawnPlayersDefault();
                break;
        }
    }

    private bool HasLocalPlayer()
    {
        foreach (var player in FindObjectsOfType<PlayerController>())
            if (player.GetComponent<PhotonView>()?.IsMine == true) return true;
        return false;
    }

    System.Collections.IEnumerator WaitForMazeAndSpawn(MazeGenerator mazeGen)
    {
        float timer = 0f;
        while (!mazeGen.IsMazeReady && timer < 5f) { timer += Time.deltaTime; yield return null; }
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
        if (positions.Count < 2) { SpawnPlayersDefault(); return; }

        Vector3 pos = positions[Random.Range(0, positions.Count)];
        pos.y = 2f;
        PhotonNetwork.Instantiate(PhotonNetwork.IsMasterClient ? "Pemain1" : "Pemain2", pos, Quaternion.identity);
    }

    public void UpdateScore(int newScore)
    {
        if (PhotonNetwork.LocalPlayer != null)
            PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { "score", newScore } });
    }

    public void EndGame()
    {
        if (isGameOver) return;
        if (PhotonNetwork.IsMasterClient)
        {
            isGameOver = true;
            string msg = DetermineWinner();
            if (pv != null && pv.IsMine) pv.RPC("RpcEndGame", RpcTarget.All, msg);
            else RpcEndGame(msg);
        }
    }

    [PunRPC]
    public void RpcEndGame(string message)
    {
        isGameOver = true;
        uiManager = uiManager ?? FindObjectOfType<UIManager>();
        uiManager?.ShowGameOverScreen(message);
    }

    private string DetermineWinner()
    {
        var players = PhotonNetwork.PlayerList;
        int GetScore(int i) => players.Length > i && players[i].CustomProperties.TryGetValue("score", out object s) ? (int)s : 0;
        int s1 = GetScore(0), s2 = GetScore(1);
        string winner = s1 > s2 ? $"{players[0].NickName} Menang!" : s2 > s1 ? $"{players[1].NickName} Menang!" : "Hasilnya Seri!";
        return $"{winner}\nSkor Akhir: {s1} - {s2}";
    }
}