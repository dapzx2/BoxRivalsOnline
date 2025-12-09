using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviour
{
    [Header("Pengaturan Gerak")]
    public float kecepatan = 3f;
    public float kekuatanRem = 0.95f;

    private Rigidbody rb;
    private Vector2 inputGerakan;
    private Transform kamera;
    private PhotonView photonView;
    private PhotonView boxSpawnerView;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        photonView = GetComponent<PhotonView>();

        if (photonView != null && photonView.IsMine)
        {
            kamera = Camera.main.transform; 
            
            BoxSpawner spawner = FindObjectOfType<BoxSpawner>();
            if (spawner != null)
            {
                 boxSpawnerView = spawner.GetComponent<PhotonView>();
            }

            CameraController camScript = kamera.GetComponent<CameraController>();
            if (camScript != null)
            {
                camScript.currentTarget = this.transform;
                camScript.isControlActive = true; 
            }

            GameManager.Instance.UpdateScore(0);
        }
    }

    void Update()
    {
        if (photonView == null || !photonView.IsMine) return;

        // Semua pemain gunakan WASD (setiap client punya window sendiri)
        inputGerakan = Vector2.zero;
        
        if (Input.GetKey(KeyCode.W)) inputGerakan.y = 1f;
        else if (Input.GetKey(KeyCode.S)) inputGerakan.y = -1f;
        if (Input.GetKey(KeyCode.A)) inputGerakan.x = -1f;
        else if (Input.GetKey(KeyCode.D)) inputGerakan.x = 1f;
    }

    void FixedUpdate()
    {
        if (photonView == null || !photonView.IsMine) return;
        if (kamera == null) return; 

        if (inputGerakan.magnitude > 0.1f)
        {
            Vector3 camForward = Vector3.ProjectOnPlane(kamera.forward, Vector3.up).normalized;
            Vector3 camRight = Vector3.ProjectOnPlane(kamera.right, Vector3.up).normalized;
            Vector3 arahGerakan = (camForward * inputGerakan.y) + (camRight * inputGerakan.x);
            
            // AIR CONTROL: Check if player is grounded
            bool isGrounded = Physics.Raycast(transform.position, Vector3.down, 0.6f);
            
            if (isGrounded)
            {
                // Full control on ground
                rb.AddForce(arahGerakan * kecepatan);
            }
            else
            {
                // PROPORTIONAL AIR CONTROL: Based on current horizontal velocity
                // Fast ground speed = more air control
                // Slow ground speed = less air control
                Vector3 horizontalVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
                float currentHorizontalSpeed = horizontalVel.magnitude;
                
                // Calculate air control multiplier (0-1) based on current speed
                // Speed 0 = 0% control, Speed 10+ = 100% control
                float airControlMultiplier = Mathf.Clamp01(currentHorizontalSpeed / 10f);
                
                // Apply reduced force (15% of ground control * momentum multiplier)
                rb.AddForce(arahGerakan * kecepatan * airControlMultiplier * 0.15f);
            }
        }
        else { rb.velocity *= kekuatanRem; }
    }

    // --- PERBAIKAN UTAMA: MENGGUNAKAN RPC ---
    void OnTriggerEnter(Collider other)
    {
        if (photonView == null || !photonView.IsMine) return;

        int skorYangDidapat = 0;
        
        if (other.gameObject.CompareTag("KotakKoleksi"))
        {
            skorYangDidapat = 1;
        }
        else if (other.gameObject.CompareTag("KotakBonus"))
        {
            skorYangDidapat = 5;
        }

        if (skorYangDidapat > 0)
        {
            PhotonView boxView = other.GetComponent<PhotonView>();
            if (boxView == null) return; 

            // PENTING: Disable collider dan sembunyikan box segera agar tidak bisa diambil lagi
            // Ini mencegah double-collection dan visual feedback langsung
            Collider boxCollider = other.GetComponent<Collider>();
            if (boxCollider != null)
            {
                boxCollider.enabled = false;
            }
            
            // Sembunyikan box secara visual segera (sebelum destroy sampai dari network)
            other.gameObject.SetActive(false);

            // 1. Update Skor Online
            int skorLama = 0;
            if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("score"))
            {
                skorLama = (int)PhotonNetwork.LocalPlayer.CustomProperties["score"];
            }
            int skorBaru = skorLama + skorYangDidapat;
            ExitGames.Client.Photon.Hashtable properties = new ExitGames.Client.Photon.Hashtable();
            properties.Add("score", skorBaru);
            PhotonNetwork.LocalPlayer.SetCustomProperties(properties);

            // 2. Kirim Perintah Hapus ke Host
            // PENTING: Kita SELALU menggunakan RPC ke BoxSpawner (bahkan jika kita adalah Master Client)
            // agar BoxSpawner bisa menghitung jumlah kotak yang terkumpul (boxesCollectedCount).
            // Jika Master Client langsung destroy tanpa RPC, counter di BoxSpawner tidak akan bertambah.
            if (boxSpawnerView != null)
            {
                boxSpawnerView.RPC("RpcDestroyBox", RpcTarget.MasterClient, boxView.ViewID);
            }
            else
            {
                Debug.LogWarning("PlayerController tidak dapat menemukan PhotonView milik BoxSpawner. Tidak dapat menghancurkan kotak.");
                
                // Fallback darurat jika BoxSpawner hilang (tapi counter tidak akan bertambah)
                if (PhotonNetwork.IsMasterClient)
                {
                    PhotonNetwork.Destroy(other.gameObject);
                }
            }
        }
    }
}