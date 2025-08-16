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

    [Header("Boat Repulsion Settings")]
    public float repulsionCheckInterval = 0.2f; // Check every 0.2 seconds
    public float repulsionRadius = 1f; // Distance at which boats start repelling each other
    public float repulsionForce = 1f; // 0 to 1, 0 = no repulsion, 1 = full repulsion
    public LayerMask boatLayerMask = -1; // Layer mask for boats
    public float repulsionSpeedMultiplier = 0.5f;

    [Header("Pathfinding Settings")]
    public float raycastDistance = 3f;
    public LayerMask obstacleLayerMask = -1;
    public float avoidanceForce = 1f; // 0 to 1, 0 = no avoidance, 1 = full avoidance
    public float avoidanceBuffer = 0.05f;

    [Header("Sprites")]
    public Sprite rightSprite;
    public Sprite downRightSprite;
    public Sprite downSprite;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private CircleCollider2D boatCollider;
    private Camera mainCamera;
    private Vector2 targetPosition;
    private float currentDirection; // Internal direction the boat is facing (in degrees)
    private Vector2 currentVelocity; // Track velocity for sprite updates

    // Pathfinding variables
    private Vector2 avoidanceDirection = Vector2.zero;

    // Boat repulsion variables
    private float repulsionCheckTimer = 0f;
    private Vector2 repulsionDirection = Vector2.zero;
    private bool isRepellingBoatAhead = false;

    void Start()
    {
        // Get components
        rb = GetComponent<Rigidbody2D>();
        boatCollider = GetComponent<CircleCollider2D>();
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

        // Update repulsion check timer
        repulsionCheckTimer += Time.deltaTime;
        if (repulsionCheckTimer >= repulsionCheckInterval)
        {
            CheckForNearbyBoats();
            repulsionCheckTimer = 0f;
        }

        // Update sprite based on simulated direction
        UpdateSprite();
    }

    private void CheckForNearbyBoats()
    {
        repulsionDirection = Vector2.zero;
        isRepellingBoatAhead = false;

        Vector2 boatPosition = GetBoatPosition();

        // Find all boats within the repulsion radius
        Collider2D[] nearbyBoats = Physics2D.OverlapCircleAll(
            boatPosition,
            repulsionRadius,
            boatLayerMask
        );

        foreach (Collider2D boatCollider in nearbyBoats)
        {
            // Skip self
            if (boatCollider.gameObject == gameObject)
                continue;

            Boat otherBoat = boatCollider.GetComponent<Boat>();
            if (otherBoat == null)
                continue;

            // Calculate distance and direction to the other boat
            Vector2 toOtherBoat = otherBoat.GetBoatPosition() - boatPosition;
            float distance = toOtherBoat.magnitude;

            // Only apply repulsion if within the repulsion radius
            if (distance <= repulsionRadius && distance > 0)
            {
                // Calculate repulsion force (stronger when closer)
                float repulsionStrength = 1f - (distance / repulsionRadius);
                Vector2 repulsionVector = -toOtherBoat.normalized * repulsionStrength;

                // Add to total repulsion direction
                repulsionDirection += repulsionVector;

                if (otherBoat.GetBoatPosition().y < boatPosition.y)
                {
                    isRepellingBoatAhead = true;
                }
            }
        }

        // Normalize the repulsion direction
        if (repulsionDirection.magnitude > 0)
        {
            repulsionDirection.Normalize();
        }
    }

    void FixedUpdate()
    {
        // Check for obstacles ahead
        CheckForObstacles();

        Vector2 boatPosition = GetBoatPosition();

        // Calculate distance to target
        float distanceToTarget = Vector2.Distance(boatPosition, targetPosition);

        // Only move if we're not close enough to the target
        if (distanceToTarget > stoppingDistance)
        {
            Vector2 steerDirection =
                avoidanceDirection != Vector2.zero
                    ? avoidanceDirection
                    : (targetPosition - boatPosition).normalized;

            // Apply repulsion force if there are nearby boats
            if (repulsionDirection.magnitude > 0)
            {
                // Blend repulsion with steering direction
                steerDirection = Vector2
                    .Lerp(steerDirection, repulsionDirection, repulsionForce)
                    .normalized;
            }

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
            float adjustedMoveSpeed = isRepellingBoatAhead
                ? moveSpeed * repulsionSpeedMultiplier
                : moveSpeed;
            rb.AddForce(moveDirection * adjustedMoveSpeed, ForceMode2D.Force);

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

        // Get the boat's CircleCollider2D to determine the radius for offset rays
        float boatRadius = GetBoatRadius();
        Vector2 boatPosition = GetBoatPosition();

        // Cast 3 rays: center, left offset, and right offset
        Vector2 targetDirection = (targetPosition - boatPosition).normalized;

        // Calculate perpendicular direction (90 degrees to the right of target direction)
        Vector2 rightPerpendicularDirection = Vector2.Perpendicular(targetDirection);
        Vector2 leftPerpendicularDirection = -rightPerpendicularDirection;

        // Define the three ray directions
        Vector2[] rayStartPositions = new Vector2[3];
        rayStartPositions[0] = boatPosition; // Center ray
        rayStartPositions[1] =
            boatPosition + leftPerpendicularDirection * (boatRadius + avoidanceBuffer); // Left offset ray
        rayStartPositions[2] =
            boatPosition + rightPerpendicularDirection * (boatRadius + avoidanceBuffer); // Right offset ray

        // Cast all three rays
        for (int i = 0; i < 3; i++)
        {
            RaycastHit2D hit = Physics2D.Raycast(
                rayStartPositions[i],
                targetDirection,
                raycastDistance,
                obstacleLayerMask
            );

            if (hit.collider != null)
            {
                // Check if the obstacle is a circle (has a CircleCollider2D)
                CircleCollider2D circleCollider = hit.collider.GetComponent<CircleCollider2D>();
                if (circleCollider != null)
                {
                    // Check if the obstacle is lit up before considering it for avoidance
                    Obstacle obstacle = hit.collider.GetComponent<Obstacle>();
                    if (obstacle != null && obstacle.IsLit())
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
        }

        if (result.hasObstacle)
        {
            // Calculate which side to steer around the obstacle
            Vector2 toObstacle = result.obstacleCenter - boatPosition;
            Vector2 toTarget = (targetPosition - boatPosition).normalized;

            // Calculate cross product to determine which side is "left" and "right"
            float crossProduct = toTarget.x * toObstacle.y - toTarget.y * toObstacle.x;

            // Calculate perpendicular direction (90 degrees to the right of forward direction)
            Vector2 perpRightDirection = new Vector2(toTarget.y, -toTarget.x);

            Vector2 perpendicularDirection;
            if (crossProduct > 0)
            {
                // Obstacle is to the left, steer right
                perpendicularDirection = perpRightDirection;
            }
            else
            {
                // Obstacle is to the right, steer left
                perpendicularDirection = -perpRightDirection;
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

        if (boatCollider == null)
            boatCollider = GetComponent<CircleCollider2D>();

        // Draw raycasts (3 rays: center, left offset, right offset)
        Gizmos.color = Color.yellow;

        // Get the boat's CircleCollider2D to determine the radius for offset rays
        float boatRadius = GetBoatRadius();
        Vector2 boatPosition = GetBoatPosition();
        Vector2 targetDirection = (targetPosition - boatPosition).normalized;

        // Calculate perpendicular direction (90 degrees to the right of target direction)
        Vector2 rightPerpendicularDirection = Vector2.Perpendicular(targetDirection);
        Vector2 leftPerpendicularDirection = -rightPerpendicularDirection;

        // Define the three ray directions
        Vector2[] rayStartPositions = new Vector2[3];
        rayStartPositions[0] = boatPosition; // Center ray
        rayStartPositions[1] =
            boatPosition + leftPerpendicularDirection * (boatRadius + avoidanceBuffer); // Left offset ray
        rayStartPositions[2] =
            boatPosition + rightPerpendicularDirection * (boatRadius + avoidanceBuffer); // Right offset ray

        for (int i = 0; i < 3; i++)
        {
            Vector2 rayStart = rayStartPositions[i];
            Vector2 rayDirection = targetDirection;
            Vector3 rayEnd = (Vector3)(rayStart + rayDirection * raycastDistance);

            Gizmos.DrawLine(rayStart, rayEnd);
        }

        // Draw target position
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(targetPosition, 0.5f);

        // Draw avoidance direction if avoiding
        if (avoidanceDirection != Vector2.zero)
        {
            Gizmos.color = Color.red;
            Vector3 avoidanceStart = boatPosition;
            Vector3 avoidanceEnd = avoidanceStart + (Vector3)(avoidanceDirection * 2f);
            Gizmos.DrawLine(avoidanceStart, avoidanceEnd);
        }

        // Draw repulsion radius
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(boatPosition, repulsionRadius);

        // Draw repulsion direction if repelling
        if (repulsionDirection != Vector2.zero)
        {
            Gizmos.color = Color.magenta;
            Vector3 repulsionStart = boatPosition;
            Vector3 repulsionEnd = repulsionStart + (Vector3)(repulsionDirection * 2f);
            Gizmos.DrawLine(repulsionStart, repulsionEnd);
        }
    }

    public float GetBoatRadius()
    {
        return boatCollider != null ? boatCollider.radius * transform.localScale.x : 0.5f;
    }

    public Vector2 GetBoatPosition()
    {
        return boatCollider != null
            ? (Vector2)transform.position + boatCollider.offset
            : (Vector2)transform.position;
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
        if (directionAngle >= 0f && directionAngle < 112.5f)
        {
            // Facing right
            spriteRenderer.sprite = rightSprite;
            spriteRenderer.flipX = false;
        }
        else if (directionAngle >= 112.5f && directionAngle < 157.5f)
        {
            // Facing down-right
            spriteRenderer.sprite = downRightSprite;
            spriteRenderer.flipX = false;
        }
        else if (directionAngle >= 157.5f && directionAngle < 202.5f)
        {
            // Facing down
            spriteRenderer.sprite = downSprite;
            spriteRenderer.flipX = false;
        }
        else if (directionAngle >= 202.5f && directionAngle < 247.5f)
        {
            // Facing down-left
            spriteRenderer.sprite = downRightSprite;
            spriteRenderer.flipX = true;
        }
        else if (directionAngle >= 247.5f)
        {
            // Facing left
            spriteRenderer.sprite = rightSprite;
            spriteRenderer.flipX = true;
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
            Debug.Log("Boat hit obstacle!!");

            // Destroy the boat
            Destroy(gameObject);
        }
    }
}
