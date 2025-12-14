using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene to Load")]
    public string nextSceneName = "LobbyScene";

    void Start()
    {

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic();
        }
    }

    public void PlayGame()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonSound();
        SceneManager.LoadScene(nextSceneName);
    }

    public void QuitGame()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonSound();
        Application.Quit();
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
