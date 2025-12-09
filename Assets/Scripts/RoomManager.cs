using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using ExitGames.Client.Photon; 

public class RoomManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public TMP_Text roomNameText;
    public TMP_Text player1Text;
    public TMP_Text player2Text;
    public Button startGameButton; 
    public Button leaveRoomButton;

    void Start()
    {
        roomNameText.text = "Room: " + PhotonNetwork.CurrentRoom.Name;
        
        leaveRoomButton.onClick.AddListener(LeaveRoom);
        startGameButton.onClick.AddListener(StartGame);

        UpdatePlayerListAndUI();
    }

    void UpdatePlayerListAndUI()
    {
        startGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);

        if (PhotonNetwork.PlayerList.Length > 0)
            player1Text.text = "Pemain 1: " + PhotonNetwork.PlayerList[0].NickName;
        else
            player1Text.text = "Pemain 1: (Kosong)";

        if (PhotonNetwork.PlayerList.Length > 1)
        {
            player2Text.text = "Pemain 2: " + PhotonNetwork.PlayerList[1].NickName;
            if (!PhotonNetwork.IsMasterClient)
            {
                string hostName = PhotonNetwork.MasterClient != null ? PhotonNetwork.MasterClient.NickName : "Host";
                player2Text.text += "\n<i>Sedang menunggu " + hostName + " memilih...</i>";
            }
        }
        else
        {
            player2Text.text = "Pemain 2: <i>Sedang Menunggu...</i>";
        }
    }

    void LeaveRoom()
    {
        if (leaveRoomButton != null)
        {
            leaveRoomButton.interactable = false;
        }

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        else
        {
             SceneManager.LoadScene(SceneNames.Lobby);
        }
    }

    void StartGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // Pindahkan Host Saja ke MenuLevel
            SceneManager.LoadScene(SceneNames.MenuLevel);
        }
    }

    // --- CALLBACKS PHOTON ---
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerListAndUI();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerListAndUI();
    }

    public override void OnLeftRoom()
    {
        if (leaveRoomButton != null)
        {
            leaveRoomButton.interactable = true;
        }
        SceneManager.LoadScene(SceneNames.Lobby);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        UpdatePlayerListAndUI();
    }
    
    // Dipanggil saat Room Properties diubah (misalnya, Host Reload)
    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable changedProps)
    {
        // Cek apakah Host mengirim tanda untuk Reload
        if (!PhotonNetwork.IsMasterClient && changedProps.ContainsKey("Reloading") && (bool)changedProps["Reloading"] == true)
        {
            Debug.Log("RoomManager: Host memberi tanda Reloading! Pindah ke RoomScene.");
            
            // Pindahkan Client ke RoomScene untuk menunggu Host memilih level
            if (SceneManager.GetActiveScene().name != SceneNames.Room)
            {
                SceneManager.LoadScene(SceneNames.Room);
            }
        }
    }
}