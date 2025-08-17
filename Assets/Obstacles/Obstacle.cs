using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public bool isMovingObstacle = false;
    public bool isMovingRight = false;
    public float moveDelay = 0f;
    public float moveSpeed = 1f;
    public float moveDistance = 10f;

    private Vector3 originalPosition;
    private float distanceTraveled = 0f;
    private float startTime = 0f;

    void Start()
    {
        originalPosition = transform.position;
        startTime = Time.time;
    }

    void Update()
    {
        if (!isMovingObstacle)
        {
            return;
        }

        if (Time.time - startTime < moveDelay)
        {
            return;
        }

        // Calculate movement direction
        float direction = isMovingRight ? 1f : -1f;

        // Move the object by updating position
        Vector3 newPosition = transform.position;
        newPosition.x += moveSpeed * direction * Time.deltaTime;
        transform.position = newPosition;

        // Track distance traveled
        distanceTraveled += moveSpeed * Time.deltaTime;

        // Check if we've reached the move distance
        if (distanceTraveled >= moveDistance)
        {
            // Reset to original position
            transform.position = originalPosition;
            distanceTraveled = 0f;
        }
    }
}
