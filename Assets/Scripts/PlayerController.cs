using UnityEngine;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
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
            GameManager.Instance.UpdateScore(0);
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

        Vector3 currentHorizontalVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        Vector3 targetVel = moveDir * moveSpeed;

        float accel = isGrounded ? groundAcceleration : airAcceleration;
        float decel = isGrounded ? groundDeceleration : airDeceleration;

        Vector3 newVel;
        if (inputGerakan.magnitude > 0.1f)
        {
            newVel = Vector3.MoveTowards(currentHorizontalVel, targetVel, accel * Time.fixedDeltaTime);
        }
        else
        {
            newVel = Vector3.MoveTowards(currentHorizontalVel, Vector3.zero, decel * Time.fixedDeltaTime);
        }

        rb.velocity = new Vector3(newVel.x, rb.velocity.y, newVel.z);
    }

    void OnTriggerEnter(Collider other)
    {
        if (photonView == null || !photonView.IsMine) return;

        int skorYangDidapat = 0;
        if (other.gameObject.CompareTag("KotakKoleksi")) skorYangDidapat = 1;
        else if (other.gameObject.CompareTag("KotakBonus")) skorYangDidapat = 5;

        if (skorYangDidapat > 0)
        {
            PhotonView boxView = other.GetComponent<PhotonView>();
            if (boxView == null) return;

            Collider boxCollider = other.GetComponent<Collider>();
            if (boxCollider != null) boxCollider.enabled = false;
            other.gameObject.SetActive(false);

            int skorLama = 0;
            if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("score"))
                skorLama = (int)PhotonNetwork.LocalPlayer.CustomProperties["score"];
            
            PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { "score", skorLama + skorYangDidapat } });

            if (boxSpawnerView != null) boxSpawnerView.RPC("RpcDestroyBox", RpcTarget.MasterClient, boxView.ViewID);
            else if (PhotonNetwork.IsMasterClient) PhotonNetwork.Destroy(other.gameObject);
        }
    }
}