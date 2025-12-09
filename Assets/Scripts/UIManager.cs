using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine.UI;

public class UIManager : MonoBehaviourPunCallbacks
{
    [Header("UI Panel")]
    public GameObject panelCeritaAwal;
    public GameObject panelMisiBerhasil;
    public GameObject panelPause;
    public TextMeshProUGUI teksCeritaAwal;
    public TextMeshProUGUI logPemain1Text;
    public TextMeshProUGUI teksSkorP1;
    public TextMeshProUGUI teksSkorP2;
    public TextMeshProUGUI teksWaktu;
    public TextMeshProUGUI teksPemenang;
    public Button tombolMulai;

    [Header("Tombol Pause Menu")]
    public Button tombolLanjutkan;
    public Button tombolKembaliKeMenu;
    
    [Header("Tombol Game Over")]
    public Button tombolMainLagi;

    [Header("Pengaturan Game")]
    public float waktuLevel = 60f;

    private bool gameBerjalan = false;
    private double waktuSelesaiGame;

    private bool isReturningToMenu = false;

    void OnDestroy()
    {
        isReturningToMenu = false;
    }

    void Start()
    {
        // Setup UI dan Timer
        if (panelCeritaAwal != null) panelCeritaAwal.SetActive(false);
        if (panelMisiBerhasil != null) panelMisiBerhasil.SetActive(false); // Sembunyikan Game Over
        if (panelPause != null) panelPause.SetActive(false); // Sembunyikan Pause

        if (tombolMainLagi != null)
        {
            tombolMainLagi.onClick.RemoveAllListeners();
            tombolMainLagi.onClick.AddListener(MainLagi);
        }
        else
        {
            Debug.LogWarning("UIManager: Tombol Main Lagi belum di-assign di Inspector!");
        }

        TampilkanCeritaAwal();
    }

    // --- UPDATE & TIMER ---
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu();
        }

        if (!gameBerjalan) return;

        double sisaWaktu = waktuSelesaiGame - PhotonNetwork.Time;

        if (teksWaktu != null)
        {
            teksWaktu.text = "Waktu: " + Mathf.Max(0, Mathf.CeilToInt((float)sisaWaktu)).ToString();
        }

        if (sisaWaktu <= 0)
        {
            if (gameBerjalan)
            {
                gameBerjalan = false;
                if (PhotonNetwork.IsMasterClient)
                {
                    GameManager.Instance.EndGame();
                }
            }
        }
    }

    // --- LOGIKA PAUSE MENU & RELOAD ---
    void TogglePauseMenu()
    {
        if (panelPause == null) return;
        panelPause.SetActive(!panelPause.activeInHierarchy);

        bool isHost = PhotonNetwork.IsMasterClient;
        if (tombolLanjutkan != null) tombolLanjutkan.interactable = isHost;
        if (tombolKembaliKeMenu != null) tombolKembaliKeMenu.interactable = isHost;
    }

    public void ResumeGame()
    {
        if (panelPause != null) panelPause.SetActive(false);
    }

    public void KembaliKeMenu()
    {
        if (isReturningToMenu) return;
        isReturningToMenu = true;

        // Stop the game logic to prevent further updates
        gameBerjalan = false;

        if (PhotonNetwork.IsMasterClient)
        {
            if (PhotonNetwork.InRoom)
            {
                Debug.Log("Host membatalkan game. Memberi tanda Reloading dan kembali ke MenuLevel.");
                
                // This property can signal other clients that the host is reloading the level.
                ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
                props.Add("Reloading", true);
                PhotonNetwork.CurrentRoom.SetCustomProperties(props);
                
                PhotonNetwork.LoadLevel(SceneNames.MenuLevel);
            }
            else
            {
                SceneManager.LoadScene(SceneNames.Lobby);
            }
        }
        else
        {
            if (PhotonNetwork.InRoom)
            {
                // Non-master clients should just leave the room. 
                // OnLeftRoom callback will handle returning to the lobby.
                PhotonNetwork.LeaveRoom();
            }
            else
            {
                SceneManager.LoadScene(SceneNames.MenuLevel);
            }
        }
    }

    // --- CALLBACKS PHOTON ---
    public override void OnEnable() { base.OnEnable(); PhotonNetwork.AddCallbackTarget(this); }
    public override void OnDisable() { base.OnDisable(); PhotonNetwork.RemoveCallbackTarget(this); }

    // PERBAIKAN: Callback OnPlayerPropertiesUpdate sudah benar
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps) { UpdateScoreboard(); }

    public override void OnPlayerEnteredRoom(Player newPlayer) { UpdateScoreboard(); }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateScoreboard();
        if (otherPlayer.IsMasterClient && gameBerjalan)
        {
            Debug.Log("Host keluar, mengakhiri game untuk Client.");
            gameBerjalan = false;
            AkhiriGame("Host Telah Keluar");
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
            }
            else
            {
                SceneManager.LoadScene(SceneNames.Lobby);
            }
        }
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Berhasil keluar room, kembali ke LobbyScene.");
        SceneManager.LoadScene(SceneNames.Lobby);
    }

    // --- LOGIKA SKOR & GAME END (Menggunakan TryGetValue - FIXED) ---
    void UpdateScoreboard()
    {
        Player[] players = PhotonNetwork.PlayerList;

        if (players.Length > 0)
        {
            Player player1 = players[0];
            int score1 = 0;
            if (player1.CustomProperties.TryGetValue("score", out object scoreObject1))
            {
                score1 = (int)scoreObject1;
            }
            teksSkorP1.text = player1.NickName + ": " + score1;
        }
        else { teksSkorP1.text = "Player 1: 0"; }

        if (players.Length > 1)
        {
            Player player2 = players[1];
            int score2 = 0;
            if (player2.CustomProperties.TryGetValue("score", out object scoreObject2))
            {
                score2 = (int)scoreObject2;
            }
            teksSkorP2.text = player2.NickName + ": " + score2;
        }
        else { teksSkorP2.text = "Player 2: (Mencari...)"; }
    }

    void AkhiriGame(string customMessage = null)
    {
        if (!gameBerjalan) return; // Mencegah pemanggilan ganda
        gameBerjalan = false;

        if (customMessage != null)
        {
            ShowGameOverScreen(customMessage);
        }
        // Logika penentuan pemenang karena waktu habis telah dipindahkan ke GameManager.
    }

    public void ShowGameOverScreen(string message)
    {
        if (panelMisiBerhasil != null)
        {
            panelMisiBerhasil.SetActive(true);
            if (teksPemenang != null) teksPemenang.text = message;
        }
    }

    void TampilkanCeritaAwal()
    {
        if (panelCeritaAwal != null) panelCeritaAwal.SetActive(true); // MUNCULKAN PANEL CERITA

        int level = 1;
        if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("SelectedLevel")) 
            level = (int)PhotonNetwork.CurrentRoom.CustomProperties["SelectedLevel"];
        if (level == 1) teksCeritaAwal.text = "Selamat datang di Arena Latihan! Kumpulkan semua kotak untuk membuktikan kecepatanmu!";
        else if (level == 2) teksCeritaAwal.text = "Labirin Menanti! Kecepatan saja tidak cukup, tunjukkan kelihaianmu menemukan jalan!";
        else if (level == 3) teksCeritaAwal.text = "The Vault! Rebut kotak bonus di tengah arena untuk meraih kemenangan!";
    }

    // --- TIMER SYNC IMPLEMENTATION ---
    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        // Handle StartTime - game timer sync
        if (propertiesThatChanged.ContainsKey("StartTime"))
        {
            double startTime = (double)propertiesThatChanged["StartTime"];
            waktuSelesaiGame = startTime + waktuLevel;
            
            // Start game locally for everyone
            if (panelCeritaAwal != null) panelCeritaAwal.SetActive(false);
            if (panelMisiBerhasil != null) panelMisiBerhasil.SetActive(false);
            if (panelPause != null) panelPause.SetActive(false);
            
            gameBerjalan = true;
            Debug.Log($"UIManager: Game Started! StartTime: {startTime}, EndTime: {waktuSelesaiGame}");
        }
    }

    public void MulaiGame()
    {
        // Only Master Client can start the game
        if (PhotonNetwork.IsMasterClient)
        {
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
            {
                { "StartTime", PhotonNetwork.Time }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
        else
        {
            Debug.LogWarning("UIManager: Only Master Client can start the game.");
        }
    }

    private bool isRestarting = false;
    
    public void MainLagi()
    {
        Debug.Log("UIManager: Tombol Main Lagi ditekan!");

        if (isRestarting)
        {
            Debug.LogWarning("UIManager: Restart already in progress, ignoring...");
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            isRestarting = true;
            
            int currentLevel = 1;
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("SelectedLevel", out object lvl))
                currentLevel = (int)lvl;
            
            Debug.Log($"UIManager: Restarting to level {currentLevel} via MenuLevel...");
            
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
            {
                { "RestartToLevel", currentLevel },
                { "IsRestart", true }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            
            PhotonNetwork.LoadLevel(SceneNames.MenuLevel);
        }
        else
        {
            Debug.LogWarning("UIManager: Anda bukan MasterClient, tidak bisa restart level.");
        }
    }

    public void UpdateLogP1(string input)
    {
        if (logPemain1Text != null) logPemain1Text.text = "Anda: " + input;
    }
}