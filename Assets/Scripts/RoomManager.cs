using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;

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
        player1Text.text = PhotonNetwork.PlayerList.Length > 0 ? "Pemain 1: " + PhotonNetwork.PlayerList[0].NickName : "Pemain 1: (Kosong)";

        if (PhotonNetwork.PlayerList.Length > 1)
        {
            player2Text.text = "Pemain 2: " + PhotonNetwork.PlayerList[1].NickName;
            if (!PhotonNetwork.IsMasterClient)
            {
                string hostName = PhotonNetwork.MasterClient?.NickName ?? "Host";
                player2Text.text += $"\n<i>Sedang menunggu {hostName} memilih...</i>";
            }
        }
        else player2Text.text = "Pemain 2: <i>Sedang Menunggu...</i>";
    }

    void LeaveRoom()
    {
        if (leaveRoomButton != null) leaveRoomButton.interactable = false;
        if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom();
        else SceneManager.LoadScene(SceneNames.Lobby);
    }

    void StartGame() { if (PhotonNetwork.IsMasterClient) SceneManager.LoadScene(SceneNames.MenuLevel); }

    public override void OnPlayerEnteredRoom(Player newPlayer) => UpdatePlayerListAndUI();
    public override void OnPlayerLeftRoom(Player otherPlayer) => UpdatePlayerListAndUI();
    public override void OnMasterClientSwitched(Player newMasterClient) => UpdatePlayerListAndUI();

    public override void OnLeftRoom()
    {
        if (leaveRoomButton != null) leaveRoomButton.interactable = true;
        SceneManager.LoadScene(SceneNames.Lobby);
    }
    
    public override void OnRoomPropertiesUpdate(Hashtable changedProps)
    {
        if (!PhotonNetwork.IsMasterClient && changedProps.TryGetValue("Reloading", out object r) && r is bool b && b)
        {
            if (SceneManager.GetActiveScene().name != SceneNames.Room)
                SceneManager.LoadScene(SceneNames.Room);
        }
    }
}