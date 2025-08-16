using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boat : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 3f;
    public float stoppingDistance = 1f;
    public Transform target;

    [Header("Pathfinding Settings")]
    public float raycastDistance = 3f;
    public int numberOfRays = 5;
    public float raySpreadAngle = 45f;
    public LayerMask obstacleLayerMask = -1;
    public float avoidanceForce = 2f;

    [Header("Sprites")]
    public Sprite upSprite;
    public Sprite rightSprite;
    public Sprite downSprite;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Camera mainCamera;
    private Vector2 targetPosition;
    private float currentDirection; // Internal direction the boat is facing (in degrees)
    private Vector2 currentVelocity; // Track velocity for sprite updates

    // Pathfinding variables
    private Vector2 avoidanceDirection = Vector2.zero;

    void Start()
    {
        // Get components
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;

        // Set initial target position and direction
        targetPosition = target.position;
        currentDirection = 180f; // Start facing down
    }

    void Update()
    {
        // Get mouse position in world coordinates
        // Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        // Vector2 mousePos = new Vector2(mouseWorldPos.x, mouseWorldPos.y);
        targetPosition = target.position;

        // Update sprite based on simulated direction
        UpdateSprite();
    }

    void FixedUpdate()
    {
        // Check for obstacles ahead
        CheckForObstacles();

        // Calculate distance to target
        float distanceToTarget = Vector2.Distance(transform.position, targetPosition);

        // Only move if we're not close enough to the target
        if (distanceToTarget > stoppingDistance)
        {
            Vector2 steerDirection =
                avoidanceDirection != Vector2.zero
                    ? avoidanceDirection
                    : (targetPosition - (Vector2)transform.position).normalized;

            // Calculate target angle (0 = up, 90 = right, 180 = down, 270 = left)
            float targetAngle = Mathf.Atan2(steerDirection.x, steerDirection.y) * Mathf.Rad2Deg;

            // Smoothly rotate towards target (simulated rotation)
            float angleDifference = Mathf.DeltaAngle(currentDirection, targetAngle);
            float rotationChange = Mathf.Clamp(angleDifference, -rotationSpeed, rotationSpeed);
            currentDirection += rotationChange;

            // Normalize direction to 0-360 range
            if (currentDirection < 0f)
                currentDirection += 360f;
            else if (currentDirection >= 360f)
                currentDirection -= 360f;

            // Calculate movement direction based on simulated boat direction
            Vector2 moveDirection = GetDirectionVector(currentDirection);

            // Apply movement force
            rb.AddForce(moveDirection * moveSpeed, ForceMode2D.Force);

            // Store current velocity for sprite updates
            currentVelocity = rb.velocity;
        }
        else
        {
            // Gradually slow down when close to target
            currentVelocity = Vector2.Lerp(currentVelocity, Vector2.zero, 0.1f);
        }
    }

    struct ObstacleDetectionResult
    {
        public bool hasObstacle;
        public float closestDistance;
        public Vector2 obstacleCenter;
        public float obstacleRadius;
    }

    private void CheckForObstacles()
    {
        ObstacleDetectionResult result = new ObstacleDetectionResult();
        result.hasObstacle = false;
        result.closestDistance = float.MaxValue;

        // Cast multiple rays in a cone in front of the boat
        for (int i = 0; i < numberOfRays; i++)
        {
            Vector2 targetDirection = (targetPosition - (Vector2)transform.position).normalized;
            float targetAngle = Mathf.Atan2(targetDirection.x, targetDirection.y) * Mathf.Rad2Deg;
            float rayAngle = -raySpreadAngle / 2f + (raySpreadAngle / (numberOfRays - 1)) * i;
            float worldAngle = targetAngle + rayAngle;
            Vector2 rayDirection = GetDirectionVector(worldAngle);

            RaycastHit2D hit = Physics2D.Raycast(
                transform.position,
                rayDirection,
                raycastDistance,
                obstacleLayerMask
            );

            Debug.Log(hit.collider);

            if (hit.collider != null)
            {
                // Check if the obstacle is a circle (has a CircleCollider2D)
                CircleCollider2D circleCollider = hit.collider.GetComponent<CircleCollider2D>();
                if (circleCollider != null)
                {
                    result.hasObstacle = true;

                    if (hit.distance < result.closestDistance)
                    {
                        result.closestDistance = hit.distance;
                        result.obstacleCenter = hit.collider.transform.position;
                        result.obstacleRadius =
                            circleCollider.radius * hit.collider.transform.localScale.x;
                    }
                }
            }
        }

        if (result.hasObstacle)
        {
            // Calculate which side to steer around the obstacle
            Vector2 toObstacle = result.obstacleCenter - (Vector2)transform.position;
            Vector2 toTarget = (targetPosition - (Vector2)transform.position).normalized;

            // Calculate cross product to determine which side is "left" and "right"
            float crossProduct = toTarget.x * toObstacle.y - toTarget.y * toObstacle.x;

            // Calculate perpendicular direction (90 degrees to the right of forward direction)
            Vector2 rightDirection = new Vector2(toTarget.y, -toTarget.x);

            Vector2 perpendicularDirection;
            if (crossProduct > 0)
            {
                // Obstacle is to the left, steer right
                perpendicularDirection = rightDirection;
            }
            else
            {
                // Obstacle is to the right, steer left
                perpendicularDirection = -rightDirection;
            }

            // Use avoidanceForce to blend between going straight (0) and steering perpendicular (1)
            // When avoidanceForce is 0: boat goes straight through obstacle
            // When avoidanceForce is 1: boat steers directly to the side
            avoidanceDirection = Vector2
                .Lerp(toTarget, perpendicularDirection, avoidanceForce)
                .normalized;
        }
        else
        {
            avoidanceDirection = Vector2.zero;
        }
    }

    // Debug visualization (call this from OnDrawGizmos if you want to see the rays in the Scene view)
    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;

        // Draw raycasts
        Gizmos.color = Color.yellow;
        for (int i = 0; i < numberOfRays; i++)
        {
            Vector2 targetDirection = (targetPosition - (Vector2)transform.position).normalized;
            float targetAngle = Mathf.Atan2(targetDirection.x, targetDirection.y) * Mathf.Rad2Deg;
            float rayAngle = -raySpreadAngle / 2f + (raySpreadAngle / (numberOfRays - 1)) * i;
            float worldAngle = targetAngle + rayAngle;
            Vector2 rayDirection = GetDirectionVector(worldAngle);

            Vector3 rayStart = transform.position;
            Vector3 rayEnd = rayStart + (Vector3)(rayDirection * raycastDistance);

            Gizmos.DrawLine(rayStart, rayEnd);
        }

        // Draw target position
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(targetPosition, 0.5f);

        // Draw avoidance direction if avoiding
        if (avoidanceDirection != Vector2.zero)
        {
            Gizmos.color = Color.red;
            Vector3 avoidanceStart = transform.position;
            Vector3 avoidanceEnd = avoidanceStart + (Vector3)(avoidanceDirection * 2f);
            Gizmos.DrawLine(avoidanceStart, avoidanceEnd);
        }
    }

    Vector2 GetDirectionVector(float angle)
    {
        // Convert angle to direction vector
        // 0 degrees = up, 90 degrees = right, 180 degrees = down, 270 degrees = left
        float radians = angle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
    }

    void UpdateSprite()
    {
        if (spriteRenderer == null)
            return;

        // Use the simulated direction instead of actual velocity
        float directionAngle = currentDirection;

        // Determine which sprite to use based on simulated direction
        if (directionAngle >= 45f && directionAngle < 135f)
        {
            // Facing right
            spriteRenderer.sprite = rightSprite;
            spriteRenderer.flipX = false;
        }
        else if (directionAngle >= 135f && directionAngle < 225f)
        {
            // Facing down
            spriteRenderer.sprite = downSprite;
        }
        else if (directionAngle >= 225f && directionAngle < 315f)
        {
            // Facing left
            spriteRenderer.sprite = rightSprite;
            spriteRenderer.flipX = true;
        }
        else
        {
            // Facing up
            spriteRenderer.sprite = upSprite;
        }
    }

    // Optional: Add method to set target position programmatically
    public void SetTargetPosition(Vector2 newTarget)
    {
        targetPosition = newTarget;
    }

    // Optional: Add method to get current speed
    public float GetCurrentSpeed()
    {
        return rb != null ? rb.velocity.magnitude : 0f;
    }

    // Optional: Add method to get current direction
    public float GetCurrentDirection()
    {
        return currentDirection;
    }

    // Optional: Add method to set direction directly
    public void SetDirection(float newDirection)
    {
        currentDirection = newDirection;
        // Normalize to 0-360 range
        if (currentDirection < 0f)
            currentDirection += 360f;
        else if (currentDirection >= 360f)
            currentDirection -= 360f;
    }

    // Handle collisions with obstacles
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if the collision is with an Obstacle
        if (collision.gameObject.GetComponent<Obstacle>() != null)
        {
            // Destroy the boat
            Destroy(gameObject);
        }
    }
}
