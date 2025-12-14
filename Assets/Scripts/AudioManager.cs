using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AudioManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("AudioManager");
                    _instance = go.AddComponent<AudioManager>();
                }
            }
            return _instance;
        }
    }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Clips")]
    public AudioClip backgroundMusic;
    public AudioClip buttonClickSound;
    public AudioClip winnerSound;

    private float defaultMusicVolume = 1.0f;
    private Coroutine duckCoroutine;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        InitializeAudio();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetMusicVolume();
    }

    void InitializeAudio()
    {
        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();

        musicSource.spatialBlend = 0f; 
        sfxSource.spatialBlend = 0f;
        musicSource.loop = true;
        defaultMusicVolume = musicSource.volume > 0 ? musicSource.volume : 1.0f;

        if (backgroundMusic == null) backgroundMusic = Resources.Load<AudioClip>("Audio/backsound");
        if (buttonClickSound == null) buttonClickSound = Resources.Load<AudioClip>("Audio/start");
        if (winnerSound == null) winnerSound = Resources.Load<AudioClip>("Audio/pemenang");

        PlayMusic();
    }

    public void PlayMusic()
    {
        if (backgroundMusic != null && !musicSource.isPlaying)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }
    }

    public void PlayButtonSound()
    {
        if (buttonClickSound == null) buttonClickSound = Resources.Load<AudioClip>("Audio/start");
        if (buttonClickSound != null) sfxSource.PlayOneShot(buttonClickSound, 3.0f);
    }

    public void PlayWinnerSound()
    {
        if (winnerSound == null) winnerSound = Resources.Load<AudioClip>("Audio/pemenang");
        if (winnerSound != null) 
        {
            sfxSource.PlayOneShot(winnerSound, 3.0f);
            if (duckCoroutine != null) StopCoroutine(duckCoroutine);
            duckCoroutine = StartCoroutine(DuckMusicRoutine(winnerSound.length));
        }
    }

    public void TriggerDuckMusic(float duration)
    {
        if (duckCoroutine != null) StopCoroutine(duckCoroutine);
        duckCoroutine = StartCoroutine(DuckMusicRoutine(duration));
    }

    public void ResetMusicVolume()
    {
        if (duckCoroutine != null)
        {
            StopCoroutine(duckCoroutine);
            duckCoroutine = null;
        }
        if (musicSource != null) musicSource.volume = defaultMusicVolume;
    }

    private System.Collections.IEnumerator DuckMusicRoutine(float duration)
    {
        float targetVolume = 0.1f;
        
        musicSource.volume = targetVolume;
        
        yield return new WaitForSeconds(duration);
        
        float timer = 0f;
        while (timer < 1.0f)
        {
            timer += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(targetVolume, defaultMusicVolume, timer);
            yield return null;
        }
        musicSource.volume = defaultMusicVolume;
        duckCoroutine = null;
    }
}
