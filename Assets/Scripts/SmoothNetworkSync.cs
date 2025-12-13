using UnityEngine;
using Photon.Pun;

public class SmoothNetworkSync : MonoBehaviourPun, IPunObservable
{
    public float positionLerpSpeed = 10f;
    public float rotationLerpSpeed = 15f;

    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private Rigidbody rb;
    private bool isRemotePlayer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        networkPosition = transform.position;
        networkRotation = transform.rotation;
    }

    void Start()
    {
        Invoke(nameof(SetupRemotePlayer), 0.1f);
    }

    void SetupRemotePlayer()
    {
        if (photonView.IsMine) return;
        
        isRemotePlayer = true;
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    void Update()
    {
        if (!isRemotePlayer) return;
        
        transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * positionLerpSpeed);
        transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * rotationLerpSpeed);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();

            if (Vector3.Distance(transform.position, networkPosition) > 5f)
            {
                transform.position = networkPosition;
                transform.rotation = networkRotation;
            }
        }
    }
}
