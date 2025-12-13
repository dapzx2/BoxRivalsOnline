using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private Vector3 moveOffset = new Vector3(0, 3f, 0);
    [SerializeField] private float moveSpeed = 2f;

    private Vector3 startPosition;
    private Vector3 endPosition;
    private bool movingToEnd = true;

    void Start()
    {
        startPosition = transform.position;
        endPosition = startPosition + moveOffset;
    }

    void Update()
    {
        Vector3 target = movingToEnd ? endPosition : startPosition;
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < 0.1f)
            movingToEnd = !movingToEnd;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player")) 
        {
            foreach(ContactPoint contact in collision.contacts)
            {
                if(contact.normal.y > 0.5f) 
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
            collision.transform.SetParent(null);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector3 start = Application.isPlaying ? startPosition : transform.position;
        Vector3 end = start + moveOffset;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(end, 0.3f);
    }
}
