using UnityEngine;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float groundAcceleration = 80f;
    public float groundDeceleration = 20f;
    public float airAcceleration = 40f;
    public float airDeceleration = 10f;

    private Rigidbody rb;
    private Vector2 inputGerakan;
    private Transform kamera;
    private PhotonView photonView;
    private PhotonView boxSpawnerView;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        photonView = GetComponent<PhotonView>();

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.drag = 0f;

        if (photonView != null && photonView.IsMine)
        {
            kamera = Camera.main.transform;
            BoxSpawner spawner = FindObjectOfType<BoxSpawner>();
            if (spawner != null) boxSpawnerView = spawner.GetComponent<PhotonView>();

            CameraController camScript = kamera.GetComponent<CameraController>();
            if (camScript != null) { camScript.currentTarget = transform; camScript.isControlActive = true; }
            GameManager.Instance?.UpdateScore(0);
        }
    }

    void Update()
    {
        if (photonView == null || !photonView.IsMine) return;

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

        float rate = inputGerakan.magnitude > 0.1f 
            ? (isGrounded ? groundAcceleration : airAcceleration) 
            : (isGrounded ? groundDeceleration : airDeceleration);

        Vector3 newVel = Vector3.MoveTowards(currentVel, inputGerakan.magnitude > 0.1f ? targetVel : Vector3.zero, rate * Time.fixedDeltaTime);
        rb.velocity = new Vector3(newVel.x, rb.velocity.y, newVel.z);
    }

    void OnTriggerEnter(Collider other)
    {
        if (photonView == null || !photonView.IsMine) return;

        int skor = 0;
        if (other.CompareTag("KotakKoleksi")) skor = 1;
        else if (other.CompareTag("KotakBonus")) skor = 5;

        if (skor <= 0) return;

        PhotonView boxView = other.GetComponent<PhotonView>();
        if (boxView == null) return;

        Collider boxCollider = other.GetComponent<Collider>();
        if (boxCollider != null) boxCollider.enabled = false;
        other.gameObject.SetActive(false);

        int skorLama = PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("score", out object s) ? (int)s : 0;
        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { "score", skorLama + skor } });

        if (boxSpawnerView != null) boxSpawnerView.RPC("RpcDestroyBox", RpcTarget.MasterClient, boxView.ViewID);
        else if (PhotonNetwork.IsMasterClient) PhotonNetwork.Destroy(other.gameObject);
    }
}