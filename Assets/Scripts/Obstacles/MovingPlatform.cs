using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

public class MovingPlatform : MonoBehaviour
{
    [SerializeField] private Vector3 moveOffset = new Vector3(0, 3f, 0);
    [SerializeField] private float speed = 3f; 

    private Vector3 startPosition;
    private bool isInitialized = false;
    private double localTimer;

    void Awake()
    {
        startPosition = transform.position;
        isInitialized = true;
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        // Ensure PhotonView doesn't interfere with our manual movement
        PhotonView pv = GetComponent<PhotonView>();
        if (pv != null)
        {
            pv.ObservedComponents = new List<Component>();
        }
    }

    void Start()
    {
        localTimer = PhotonNetwork.InRoom ? PhotonNetwork.Time : Time.timeAsDouble;
    }

    void Update()
    {
        localTimer += Time.deltaTime;
        
        float sinValue = Mathf.Sin((float)localTimer * speed);
        float factor = (sinValue + 1f) / 2f; 

        Vector3 targetPos = startPosition + (moveOffset * factor);
        transform.position = targetPos;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                if (contact.normal.y < -0.5f) 
                {
                    collision.transform.SetParent(transform);
                    break;
                }
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(null);
        }
    }

    public Vector3 GetTopPosition()
    {
        Vector3 basePos = isInitialized ? startPosition : transform.position;
        return basePos + moveOffset + new Vector3(0, 1f, 0);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Vector3 ptr = Application.isPlaying && isInitialized ? startPosition : transform.position;
        Gizmos.DrawLine(ptr, ptr + moveOffset);
        Gizmos.DrawWireSphere(ptr, 0.3f);
        Gizmos.DrawWireSphere(ptr + moveOffset, 0.3f);
    }
}
