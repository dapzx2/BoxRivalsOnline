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
    public TextMeshProUGUI teksSkorP1;
    public TextMeshProUGUI teksSkorP2;
    public TextMeshProUGUI teksWaktu;
    public TextMeshProUGUI teksPemenang;
    public Button tombolMulai;

    [Header("Tombol Pause Menu")]
    public Button tombolLanjutkan;
    public Button tombolKembaliKeMenu;
    
    [Header("Game Over")]
    public TextMeshProUGUI teksMenungguHost;
    public Button tombolMenuGameOver;

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
        // Setup UI
        if (panelCeritaAwal != null) panelCeritaAwal.SetActive(false);
        if (panelMisiBerhasil != null) panelMisiBerhasil.SetActive(false);
        if (panelPause != null) panelPause.SetActive(false);

        TampilkanCeritaAwal();
    }

    // --- UPDATE & TIMER ---
    void Update()
    {
        // Hanya Host yang bisa pause
        if (Input.GetKeyDown(KeyCode.Escape) && PhotonNetwork.IsMasterClient)
        {
            TogglePauseMenu();
        }

        if (!gameBerjalan) return;

        double sisaWaktu = waktuSelesaiGame - PhotonNetwork.Time;

        if (teksWaktu != null)
        {
            teksWaktu.text = "Sisa Waktu: " + Mathf.Max(0, Mathf.CeilToInt((float)sisaWaktu)).ToString() + " detik";
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

        gameBerjalan = false;

        if (PhotonNetwork.IsMasterClient)
        {
            if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
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
            if (teksSkorP1 != null) teksSkorP1.text = "Skor " + player1.NickName + ": " + score1;
        }
        else { if (teksSkorP1 != null) teksSkorP1.text = "Skor Pemain 1: 0"; }

        if (players.Length > 1)
        {
            Player player2 = players[1];
            int score2 = 0;
            if (player2.CustomProperties.TryGetValue("score", out object scoreObject2))
            {
                score2 = (int)scoreObject2;
            }
            if (teksSkorP2 != null) teksSkorP2.text = "Skor " + player2.NickName + ": " + score2;
        }
        else { if (teksSkorP2 != null) teksSkorP2.text = "<i>Pemain 2: Sedang Menunggu...</i>"; }
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
            
            // Tombol Menu hanya untuk Host
            if (tombolMenuGameOver != null)
            {
                tombolMenuGameOver.gameObject.SetActive(PhotonNetwork.IsMasterClient);
            }
            
            // Tampilkan teks menunggu untuk Client
            if (teksMenungguHost != null)
            {
                teksMenungguHost.gameObject.SetActive(!PhotonNetwork.IsMasterClient);
                string hostName = PhotonNetwork.MasterClient != null ? PhotonNetwork.MasterClient.NickName : "Host";
                teksMenungguHost.text = "<i>Sedang menunggu " + hostName + " memilih...</i>";
            }
        }
    }

    void TampilkanCeritaAwal()
    {
        if (panelCeritaAwal != null) panelCeritaAwal.SetActive(true);

        // Client: Sembunyikan tombol Mulai, tampilkan teks menunggu
        if (tombolMulai != null)
        {
            tombolMulai.gameObject.SetActive(PhotonNetwork.IsMasterClient);
        }
        
        // Tampilkan teks menunggu untuk client di panel cerita awal
        if (teksMenungguHost != null)
        {
            teksMenungguHost.gameObject.SetActive(!PhotonNetwork.IsMasterClient);
            if (!PhotonNetwork.IsMasterClient)
            {
                string hostName = PhotonNetwork.MasterClient != null ? PhotonNetwork.MasterClient.NickName : "Host";
                teksMenungguHost.text = "<i>Sedang menunggu " + hostName + " memilih...</i>";
            }
        }

        int level = 1;
        if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("SelectedLevel")) 
            level = (int)PhotonNetwork.CurrentRoom.CustomProperties["SelectedLevel"];
        
        if (teksCeritaAwal != null)
        {
            if (level == 1) teksCeritaAwal.text = "Selamat datang di Arena Latihan! Kumpulkan semua kotak untuk membuktikan kecepatanmu!";
            else if (level == 2) teksCeritaAwal.text = "Labirin Menanti! Kecepatan saja tidak cukup, tunjukkan kelihaianmu menemukan jalan!";
            else if (level == 3) teksCeritaAwal.text = "The Vault! Rebut kotak bonus di tengah arena untuk meraih kemenangan!";
        }
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
}