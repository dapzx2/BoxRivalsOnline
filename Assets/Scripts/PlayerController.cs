using UnityEngine;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float groundAcceleration = 80f;
    public float groundDeceleration = 50f;
    public float airAcceleration = 60f;
    public float airDeceleration = 30f;

    [SerializeField] private AudioClip eatSmallSound;
    [SerializeField] private AudioClip eatBigSound;

    private Rigidbody rb;
    private Vector2 inputGerakan;
    private Transform kamera;
    private PhotonView photonView;
    private AudioSource audioSource;
    private bool isGrounded;
    private BoxSpawner cachedBoxSpawner;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;

        if (eatSmallSound == null) eatSmallSound = Resources.Load<AudioClip>("Audio/makan_kotak_kecil");
        if (eatBigSound == null) eatBigSound = Resources.Load<AudioClip>("Audio/makan_kotak_besar");
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        photonView = GetComponent<PhotonView>();

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.drag = 0f;

        if (photonView != null && photonView.IsMine)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                kamera = mainCam.transform;
                CameraController camScript = kamera.GetComponent<CameraController>();
                if (camScript != null)
                {
                    camScript.currentTarget = transform;
                    camScript.isControlActive = true;
                }
            }
            else
            {

            }
            
            GameManager.Instance?.UpdateScore(0);
        }

        // OPTIMIZATION: Cache BoxSpawner once
        if (cachedBoxSpawner == null) cachedBoxSpawner = FindObjectOfType<BoxSpawner>();
    }

    void Update()
    {
        if (photonView == null || !photonView.IsMine) return;
        

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            inputGerakan = Vector2.zero;
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
            return;
        }

        inputGerakan = Vector2.zero;
        if (Input.GetKey(KeyCode.W)) inputGerakan.y = 1f;
        else if (Input.GetKey(KeyCode.S)) inputGerakan.y = -1f;
        if (Input.GetKey(KeyCode.A)) inputGerakan.x = -1f;
        else if (Input.GetKey(KeyCode.D)) inputGerakan.x = 1f;

        if (inputGerakan.magnitude > 1f) inputGerakan.Normalize();
        
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 0.6f);
    }

    void FixedUpdate()
    {
        if (photonView == null || !photonView.IsMine || kamera == null) return;

        Vector3 camForward = Vector3.ProjectOnPlane(kamera.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(kamera.right, Vector3.up).normalized;
        Vector3 moveDir = (camForward * inputGerakan.y) + (camRight * inputGerakan.x);

        Vector3 currentVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        Vector3 targetVel = moveDir * moveSpeed;

        float rate;
        if (inputGerakan.magnitude > 0.1f)
            rate = isGrounded ? groundAcceleration : airAcceleration;
        else
            rate = isGrounded ? groundDeceleration : airDeceleration;

        Vector3 target = inputGerakan.magnitude > 0.1f ? targetVel : Vector3.zero;
        Vector3 newVel = Vector3.MoveTowards(currentVel, target, rate * Time.fixedDeltaTime);
        
        rb.velocity = new Vector3(newVel.x, rb.velocity.y, newVel.z);
    }

    void OnTriggerEnter(Collider other)
    {
        if (photonView == null || !photonView.IsMine) return;

        int scoreToAdd = 0;
        AudioClip clipToPlay = null;

        if (other.CompareTag("KotakKoleksi"))
        {
            scoreToAdd = 1;
            clipToPlay = eatSmallSound;
        }
        else if (other.CompareTag("KotakBonus"))
        {
            scoreToAdd = 5;
            clipToPlay = eatBigSound;
        }

        if (scoreToAdd <= 0) return;

        PlaySound(clipToPlay);
        ProcessBoxCollection(other, scoreToAdd);
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            float volume = (clip == eatBigSound) ? 1.0f : 3.0f;
            audioSource.PlayOneShot(clip, volume);
        }
    }

    void ProcessBoxCollection(Collider boxCollider, int scoreToAdd)
    {
        PhotonView boxView = boxCollider.GetComponent<PhotonView>();
        if (boxView == null) return;

        boxCollider.enabled = false;
        boxCollider.gameObject.SetActive(false);

        int currentScore = 0;
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(Constants.PlayerScoreProperty, out object s)) currentScore = (int)s;
        
        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { Constants.PlayerScoreProperty, currentScore + scoreToAdd } });



        if (cachedBoxSpawner != null)
        {
            photonView.RPC(nameof(CmdDestroyBox), RpcTarget.MasterClient, boxView.ViewID);
        }
    }

    [PunRPC]
    void CmdDestroyBox(int viewID)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        if (cachedBoxSpawner != null)
        {
            cachedBoxSpawner.RpcDestroyBox(viewID);
        }
    }
}