using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class MenuLevelController : MonoBehaviourPunCallbacks
{
    public Button level1Button;
    public Button level2Button;
    public Button level3Button;
    public Button backToLobbyButton;
    public TextMeshProUGUI teksMenungguHost;

    private int levelToLoad;

    void Start()
    {
        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom?.CustomProperties.TryGetValue("IsRestart", out object r) == true && r is bool b && b)
        {
            StartCoroutine(AutoRestartGame());
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            if (level1Button != null) level1Button.interactable = false;
            if (level2Button != null) level2Button.interactable = false;
            if (level3Button != null) level3Button.interactable = false;
            if (backToLobbyButton != null) backToLobbyButton.interactable = false;

            if (teksMenungguHost != null)
            {
                teksMenungguHost.gameObject.SetActive(true);
                string hostName = PhotonNetwork.MasterClient?.NickName ?? "Host";
                teksMenungguHost.text = $"<i>Sedang menunggu {hostName} memilih level...</i>";
            }
            return;
        }

        if (teksMenungguHost != null) teksMenungguHost.gameObject.SetActive(false);

        level1Button.onClick.AddListener(() => PrepareLoad(1));
        level2Button.onClick.AddListener(() => PrepareLoad(2));
        level3Button.onClick.AddListener(() => PrepareLoad(3));
        if (backToLobbyButton != null) backToLobbyButton.onClick.AddListener(BackToLobby);
    }

    System.Collections.IEnumerator AutoRestartGame()
    {
        yield return new WaitForSeconds(0.3f);

        if (PhotonNetwork.IsMasterClient)
        {
            int level = PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("RestartToLevel", out object lvl) ? (int)lvl : 2;
            PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { "IsRestart", false } });
            levelToLoad = level;
            PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { "SelectedLevel", level } });
        }
    }

    void PrepareLoad(int index)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        levelToLoad = index;
        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { "SelectedLevel", levelToLoad }, { "Reloading", false } });

        level1Button.interactable = false;
        level2Button.interactable = false;
        level3Button.interactable = false;
    }

    public override void OnRoomPropertiesUpdate(Hashtable changedProps)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (changedProps.TryGetValue("SelectedLevel", out object lvl) && (int)lvl == levelToLoad)
        {
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.CurrentRoom.IsVisible = false;
            }

            string sceneName = levelToLoad switch
            {
                2 => SceneNames.Level2,
                3 => SceneNames.MapRoulette,
                _ => SceneNames.GameArena
            };
            PhotonNetwork.LoadLevel(sceneName);
        }
    }

    void BackToLobby()
    {
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene(SceneNames.Lobby);
    }
}