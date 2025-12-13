using UnityEngine;

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

    void InitializeAudio()
    {
        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();

        musicSource.spatialBlend = 0f; 
        sfxSource.spatialBlend = 0f;
        musicSource.loop = true;

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
        if (winnerSound != null) sfxSource.PlayOneShot(winnerSound, 3.0f);
    }
}
