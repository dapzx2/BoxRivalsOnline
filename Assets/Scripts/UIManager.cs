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

    void OnDestroy() => isReturningToMenu = false;

    void Start()
    {
        if (panelCeritaAwal != null) panelCeritaAwal.SetActive(false);
        if (panelMisiBerhasil != null) panelMisiBerhasil.SetActive(false);
        if (panelPause != null) panelPause.SetActive(false);
        TampilkanCeritaAwal();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && PhotonNetwork.IsMasterClient) TogglePauseMenu();
        if (!gameBerjalan) return;

        double sisaWaktu = waktuSelesaiGame - PhotonNetwork.Time;
        if (teksWaktu != null) teksWaktu.text = "Sisa Waktu: " + Mathf.Max(0, Mathf.CeilToInt((float)sisaWaktu)).ToString() + " detik";

        if (sisaWaktu <= 0 && gameBerjalan)
        {
            gameBerjalan = false;
            if (PhotonNetwork.IsMasterClient) GameManager.Instance.EndGame();
        }
    }

    void TogglePauseMenu()
    {
        if (panelPause == null) return;
        panelPause.SetActive(!panelPause.activeInHierarchy);
        bool isHost = PhotonNetwork.IsMasterClient;
        if (tombolLanjutkan != null) tombolLanjutkan.interactable = isHost;
        if (tombolKembaliKeMenu != null) tombolKembaliKeMenu.interactable = isHost;
    }

    public void ResumeGame() { if (panelPause != null) panelPause.SetActive(false); }

    public void KembaliKeMenu()
    {
        if (isReturningToMenu) return;
        isReturningToMenu = true;
        gameBerjalan = false;

        if (PhotonNetwork.IsMasterClient)
        {
            if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                Hashtable props = new Hashtable { { "Reloading", true } };
                PhotonNetwork.CurrentRoom.SetCustomProperties(props);
                PhotonNetwork.LoadLevel(SceneNames.MenuLevel);
            }
            else SceneManager.LoadScene(SceneNames.Lobby);
        }
        else
        {
            if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom();
            else SceneManager.LoadScene(SceneNames.MenuLevel);
        }
    }

    public override void OnEnable() { base.OnEnable(); PhotonNetwork.AddCallbackTarget(this); }
    public override void OnDisable() { base.OnDisable(); PhotonNetwork.RemoveCallbackTarget(this); }
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) => UpdateScoreboard();
    public override void OnPlayerEnteredRoom(Player newPlayer) => UpdateScoreboard();

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateScoreboard();
        if (otherPlayer.IsMasterClient && gameBerjalan)
        {
            gameBerjalan = false;
            AkhiriGame("Host Telah Keluar");
            if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom();
            else SceneManager.LoadScene(SceneNames.Lobby);
        }
    }

    public override void OnLeftRoom() => SceneManager.LoadScene(SceneNames.Lobby);

    void UpdateScoreboard()
    {
        Player[] players = PhotonNetwork.PlayerList;

        if (players.Length > 0)
        {
            int score1 = 0;
            if (players[0].CustomProperties.TryGetValue("score", out object s1)) score1 = (int)s1;
            if (teksSkorP1 != null) teksSkorP1.text = "Skor " + players[0].NickName + ": " + score1;
        }
        else if (teksSkorP1 != null) teksSkorP1.text = "Skor Pemain 1: 0";

        if (players.Length > 1)
        {
            int score2 = 0;
            if (players[1].CustomProperties.TryGetValue("score", out object s2)) score2 = (int)s2;
            if (teksSkorP2 != null) teksSkorP2.text = "Skor " + players[1].NickName + ": " + score2;
        }
        else if (teksSkorP2 != null) teksSkorP2.text = "<i>Pemain 2: Sedang Menunggu...</i>";
    }

    void AkhiriGame(string customMessage = null)
    {
        if (!gameBerjalan) return;
        gameBerjalan = false;
        if (customMessage != null) ShowGameOverScreen(customMessage);
    }

    public void ShowGameOverScreen(string message)
    {
        if (panelMisiBerhasil != null)
        {
            panelMisiBerhasil.SetActive(true);
            if (teksPemenang != null) teksPemenang.text = message;
            if (tombolMenuGameOver != null) tombolMenuGameOver.gameObject.SetActive(PhotonNetwork.IsMasterClient);
            
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
        if (tombolMulai != null) tombolMulai.gameObject.SetActive(PhotonNetwork.IsMasterClient);
        
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

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey("StartTime"))
        {
            double startTime = (double)propertiesThatChanged["StartTime"];
            waktuSelesaiGame = startTime + waktuLevel;
            
            if (panelCeritaAwal != null) panelCeritaAwal.SetActive(false);
            if (panelMisiBerhasil != null) panelMisiBerhasil.SetActive(false);
            if (panelPause != null) panelPause.SetActive(false);
            
            gameBerjalan = true;
        }
    }

    public void MulaiGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Hashtable props = new Hashtable { { "StartTime", PhotonNetwork.Time } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
    }
}