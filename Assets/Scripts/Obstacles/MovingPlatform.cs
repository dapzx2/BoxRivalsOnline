using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Moving platform that travels between waypoints
/// Players standing on it will move with the platform
/// </summary>
public class MovingPlatform : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private List<Transform> waypoints = new List<Transform>();
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private bool loopPath = true; // True = loop, False = ping-pong
    
    [Header("Advanced")]
    [SerializeField] private float waypointReachThreshold = 0.1f;

    private int currentWaypointIndex = 0;
    private bool movingForward = true;

    void Update()
    {
        if (waypoints.Count < 2) return;

        MovePlatform();
    }

    private void MovePlatform()
    {
        Transform targetWaypoint = waypoints[currentWaypointIndex];
        
        // Move towards current waypoint
        transform.position = Vector3.MoveTowards(
            transform.position, 
            targetWaypoint.position, 
            moveSpeed * Time.deltaTime
        );

        // Check if reached waypoint
        if (Vector3.Distance(transform.position, targetWaypoint.position) < waypointReachThreshold)
        {
            SelectNextWaypoint();
        }
    }

    private void SelectNextWaypoint()
    {
        if (loopPath)
        {
            // Loop: 0 -> 1 -> 2 -> 0 -> 1...
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
        }
        else
        {
            // Ping-pong: 0 -> 1 -> 2 -> 1 -> 0 -> 1...
            if (movingForward)
            {
                currentWaypointIndex++;
                if (currentWaypointIndex >= waypoints.Count - 1)
                {
                    movingForward = false;
                }
            }
            else
            {
                currentWaypointIndex--;
                if (currentWaypointIndex <= 0)
                {
                    movingForward = true;
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Parent player to platform so they move with it
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(transform);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        // Unparent player when they leave platform
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(null);
        }
    }

    // Visual debug for waypoints
    private void OnDrawGizmos()
    {
        if (waypoints.Count < 2) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i + 1] != null)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }
        }

        // Draw line back to start if looping
        if (loopPath && waypoints[waypoints.Count - 1] != null && waypoints[0] != null)
        {
            Gizmos.DrawLine(waypoints[waypoints.Count - 1].position, waypoints[0].position);
        }
    }
}
