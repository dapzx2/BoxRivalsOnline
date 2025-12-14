using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    public TMP_InputField namaPemainInput;
    public TMP_InputField namaRoomInput;
    public Button createRoomButton;
    public Button joinRoomButton;
    public TMP_Text statusText;

    void Start()
    {
        DisableUI();
        statusText.text = "Menghubungkan ke server...";

        if (!PhotonNetwork.IsConnected) 
        {
            PhotonNetwork.ConnectUsingSettings();
        }
        else 
        {
            if (PhotonNetwork.InLobby)
            {
                statusText.text = "Sudah di Lobby.";
                OnJoinedLobby();
            }
            else if (PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer)
            {
                statusText.text = "Terhubung ke Master.";
                PhotonNetwork.JoinLobby();
            }
            else
            {

                statusText.text = "Menunggu status server: " + PhotonNetwork.NetworkClientState;
            }
        }

        createRoomButton.onClick.AddListener(CreateRoom);
        joinRoomButton.onClick.AddListener(JoinRoom);
    }

    void DisableUI()
    {
        namaPemainInput.interactable = false;
        namaRoomInput.interactable = false;
        createRoomButton.interactable = false;
        joinRoomButton.interactable = false;
    }

    void EnableUI()
    {
        namaPemainInput.interactable = true;
        namaRoomInput.interactable = true;
        createRoomButton.interactable = true;
        joinRoomButton.interactable = true;
    }

    public void CreateRoom()
    {
        if (string.IsNullOrEmpty(namaRoomInput.text) || string.IsNullOrEmpty(namaPemainInput.text))
        {
            statusText.text = "Nama pemain & nama room tidak boleh kosong!";
            return;
        }

        DisableUI();
        statusText.text = "Membuat room...";
        PhotonNetwork.NickName = namaPemainInput.text;

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2;
        roomOptions.IsVisible = true;
        roomOptions.IsOpen = true;

        PhotonNetwork.CreateRoom(namaRoomInput.text, roomOptions);
    }

    public void JoinRoom()
    {
        if (string.IsNullOrEmpty(namaRoomInput.text) || string.IsNullOrEmpty(namaPemainInput.text))
        {
            statusText.text = "Nama pemain & nama room tidak boleh kosong!";
            return;
        }

        DisableUI();
        statusText.text = "Bergabung ke room...";
        PhotonNetwork.NickName = namaPemainInput.text;
        PhotonNetwork.JoinRoom(namaRoomInput.text);
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        statusText.text = "Masuk Lobby Berhasil!\nSilakan buat atau join room.";
        EnableUI();
    }

    public override void OnJoinedRoom()
    {
        statusText.text = "Berhasil Masuk! Memuat RoomScene...";
        PhotonNetwork.LoadLevel(SceneNames.Room);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        statusText.text = "Gagal join room: " + message;
        EnableUI();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        statusText.text = "Gagal buat room: " + message;
        EnableUI();
    }

    public void BackToMainMenu()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonSound();

        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNames.MainMenu);
    }
}