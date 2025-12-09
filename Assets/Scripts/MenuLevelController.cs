using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon; 
using UnityEngine.SceneManagement; 

public class MenuLevelController : MonoBehaviourPunCallbacks
{
    public Button level1Button;
    public Button level2Button;
    public Button level3Button;
    public Button backToLobbyButton; 
    public TextMeshProUGUI teksMenungguHost;
    
    private int levelToLoad = 0; 

    void Start()
    {
        // Check if this is a restart (returning from game)
        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null)
        {
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("IsRestart", out object isRestart) 
                && isRestart is bool && (bool)isRestart)
            {
                StartCoroutine(AutoRestartGame());
                return;
            }
        }
        
        // Client: Tampilkan tombol tapi disable (tidak bisa diklik)
        if (!PhotonNetwork.IsMasterClient)
        {
            if (level1Button != null) level1Button.interactable = false;
            if (level2Button != null) level2Button.interactable = false;
            if (level3Button != null) level3Button.interactable = false;
            if (backToLobbyButton != null) backToLobbyButton.interactable = false;
            
            if (teksMenungguHost != null)
            {
                teksMenungguHost.gameObject.SetActive(true);
                string hostName = PhotonNetwork.MasterClient != null ? PhotonNetwork.MasterClient.NickName : "Host";
                teksMenungguHost.text = "<i>Sedang menunggu " + hostName + " memilih level...</i>";
            }
            return;
        }
        
        // Host: Sembunyikan teks menunggu
        if (teksMenungguHost != null) teksMenungguHost.gameObject.SetActive(false);
        
        // Normal flow - setup level buttons (Host only)
        level1Button.onClick.AddListener(() => PrepareLoad(1));
        level2Button.onClick.AddListener(() => PrepareLoad(2));
        level3Button.onClick.AddListener(() => PrepareLoad(3));

        if (backToLobbyButton != null)
        {
            backToLobbyButton.onClick.AddListener(BackToLobby);
        }
    }
    
    private System.Collections.IEnumerator AutoRestartGame()
    {
        // Wait untuk properties sync
        yield return new WaitForSeconds(0.3f);
        
        if (PhotonNetwork.IsMasterClient)
        {
            // Get level to restart to
            int level = 2; // default
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("RestartToLevel", out object lvl))
                level = (int)lvl;
            
            Debug.Log($"MenuLevelController: Auto-restarting to level {level}");
            
            // Clear restart flag
            Hashtable props = new Hashtable { { "IsRestart", false } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            
            // Set level dan load
            levelToLoad = level;
            Hashtable levelProps = new Hashtable { { "SelectedLevel", level } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(levelProps);
        }
    }

    // --- 1. PREPARE LOAD: Menyimpan Pilihan Level ---
    void PrepareLoad(int index)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        levelToLoad = index;

        // 1. Set properti yang akan disimpan
        ExitGames.Client.Photon.Hashtable roomProperties = new ExitGames.Client.Photon.Hashtable();
        roomProperties.Add("SelectedLevel", levelToLoad); 
        roomProperties.Add("Reloading", false); 
        
        // Baris ini mengirim data ke server. Kita TIDAK langsung LoadLevel.
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
        
        // Nonaktifkan tombol agar tidak diklik dua kali
        level1Button.interactable = false;
        level2Button.interactable = false;
        level3Button.interactable = false;
    }

    // --- 2. CALLBACK: Menunggu Konfirmasi Penyimpanan ---
    // Fungsi ini dipanggil Photon saat Room Properties diubah
    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        // Cek apakah properti SelectedLevel sudah di-update di sisi Host
        if (changedProps.ContainsKey("SelectedLevel") && (int)changedProps["SelectedLevel"] == levelToLoad)
        {
            
            // Tutup Room
            if(PhotonNetwork.InRoom) {
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.CurrentRoom.IsVisible = false;
            }

            // Paksa semua pemain pindah ke scene yang sesuai
            string sceneName = SceneNames.GameArena; // Default Level 1

            switch (levelToLoad)
            {
                case 2:
                    sceneName = SceneNames.Level2;
                    break;
                case 3:
                    sceneName = SceneNames.MapRoulette; // Load roulette first for Level 3
                    break;
            }

            PhotonNetwork.LoadLevel(sceneName); 
        }
    }

    void BackToLobby()
    {
        // Pastikan masih terkoneksi sebelum meninggalkan room
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        
        SceneManager.LoadScene(SceneNames.Lobby);
    }
}