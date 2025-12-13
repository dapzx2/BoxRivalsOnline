using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

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

        if (!PhotonNetwork.IsConnected) PhotonNetwork.ConnectUsingSettings();
        else
        {
            statusText.text = "Sudah terhubung.";
            OnConnectedToMaster();
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
        PhotonNetwork.CreateRoom(namaRoomInput.text);
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
}