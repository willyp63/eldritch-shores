using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Kraken : MonoBehaviour
{
    [Header("Wander Settings")]
    public Bounds wanderBounds;
    public float moveSpeed = 1f;
    public float wanderInterval = 3f; // Time between picking new targets
    public float stoppingDistance = 0.5f; // How close to get to target before picking new one

    [Header("Angry Behavior")]
    public float angryLightThreshold = 0.25f;
    public float angrySpeedMultiplier = 2f; // Move speed multiplier when angry
    public float boatDetectionRadius = 10f; // How far the kraken can detect boats when angry
    public LayerMask boatLayerMask = -1; // Layer mask for boats

    [Header("Obstacle Avoidance")]
    public LayerMask obstacleLayerMask = -1;
    public float raycastDistance = 3f;
    public float avoidanceForce = 1f;
    public float avoidanceBuffer = 0.05f;

    private LightAbsorber lightAbsorber;
    private Rigidbody2D rb;
    private CircleCollider2D krakenCollider;
    private Animator animator;

    private float currentMoveSpeed = 1f;
    private Vector2 currentTarget;
    private float wanderTimer = 0f;
    private Vector2 avoidanceDirection = Vector2.zero;
    private bool isAngry = false;

    // Boat chasing variables
    private Transform targetBoat = null;
    private float boatChaseTimer = 0f;
    public float boatChaseUpdateInterval = 0.5f; // How often to update boat target when chasing

    void Start()
    {
        lightAbsorber = GetComponent<LightAbsorber>();
        rb = GetComponent<Rigidbody2D>();
        krakenCollider = GetComponent<CircleCollider2D>();
        animator = GetComponent<Animator>();
        currentMoveSpeed = moveSpeed;

        // Pick initial target
        PickNewTarget();
    }

    void Update()
    {
        bool wasAngry = isAngry;
        isAngry = lightAbsorber.GetLightLevel() >= angryLightThreshold;

        // Handle angry state changes
        if (isAngry && !wasAngry)
        {
            // Just became angry
            OnBecomeAngry();
        }
        else if (!isAngry && wasAngry)
        {
            // Just calmed down
            OnCalmDown();
        }

        if (isAngry)
        {
            // When angry, look for boats to chase
            HandleAngryBehavior();
        }
        else
        {
            // When not angry, do normal wandering
            HandleWanderingBehavior();
        }
    }

    private void HandleAngryBehavior()
    {
        // Update boat chase timer
        boatChaseTimer += Time.deltaTime;

        if (boatChaseTimer >= boatChaseUpdateInterval)
        {
            // Look for the nearest boat
            FindNearestBoat();
            boatChaseTimer = 0f;
        }

        // If we have a target boat, update our target to follow it
        if (targetBoat != null)
        {
            currentTarget = targetBoat.position;

            // Check if we've reached the boat
            float distanceToBoat = Vector2.Distance(GetKrakenPosition(), currentTarget);
            if (distanceToBoat <= stoppingDistance)
            {
                Vector3 boatPosition = targetBoat.GetComponent<Boat>().GetBoatPosition();

                AnalyticsManager.Instance.SendEvent("kraken_sunk_boat");

                FloatingTextManager.Instance.SpawnText("WRECKED!", boatPosition, Color.red, 1f);

                GameManager.Instance.LoseLife();
                GameManager.Instance.SpawnExplosion(boatPosition);

                // Destroy the boat
                Destroy(targetBoat.gameObject);
                targetBoat = null;

                lightAbsorber.SetLightLevel(0f);
            }
        }
        else
        {
            HandleWanderingBehavior();
        }
    }

    private void HandleWanderingBehavior()
    {
        // Reset boat chasing state
        targetBoat = null;

        // Update wander timer
        wanderTimer += Time.deltaTime;

        // Check if we should pick a new target
        if (wanderTimer >= wanderInterval)
        {
            PickNewTarget();
            wanderTimer = 0f;
        }

        // Check if we've reached current target
        float distanceToTarget = Vector2.Distance(GetKrakenPosition(), currentTarget);
        if (distanceToTarget <= stoppingDistance)
        {
            PickNewTarget();
            wanderTimer = 0f;
        }
    }

    private void FindNearestBoat()
    {
        Vector2 krakenPosition = GetKrakenPosition();

        // Find all boats within detection radius
        Collider2D[] nearbyBoats = Physics2D.OverlapCircleAll(
            krakenPosition,
            boatDetectionRadius,
            boatLayerMask
        );

        float closestDistance = float.MaxValue;
        Transform closestBoat = null;

        foreach (Collider2D boatCollider in nearbyBoats)
        {
            Boat boat = boatCollider.GetComponent<Boat>();
            if (boat != null && !boat.IsScored) // Don't chase scored boats
            {
                float distance = Vector2.Distance(krakenPosition, boatCollider.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestBoat = boatCollider.transform;
                }
            }
        }

        targetBoat = closestBoat;
    }

    private void OnBecomeAngry()
    {
        // Increase animation speed
        if (animator != null)
        {
            animator.speed = angrySpeedMultiplier;
        }

        // Increase move speed
        currentMoveSpeed = moveSpeed * angrySpeedMultiplier;

        // Reset boat chase timer
        boatChaseTimer = 0f;
    }

    private void OnCalmDown()
    {
        // Reset animation speed
        if (animator != null)
        {
            animator.speed = 1f;
        }

        // Reset move speed
        currentMoveSpeed = moveSpeed;

        // Clear boat target and return to wandering
        targetBoat = null;
        PickNewTarget();
    }

    void FixedUpdate()
    {
        // Check for obstacles ahead
        CheckForObstacles();

        // Calculate movement direction
        Vector2 moveDirection = CalculateMoveDirection();

        // Apply movement force with angry multiplier
        rb.AddForce(moveDirection * currentMoveSpeed, ForceMode2D.Force);
    }

    private void PickNewTarget()
    {
        // Pick a random point within the wander bounds
        float randomX = Random.Range(wanderBounds.min.x, wanderBounds.max.x);
        float randomY = Random.Range(wanderBounds.min.y, wanderBounds.max.y);
        currentTarget = new Vector2(randomX, randomY);
    }

    private Vector2 CalculateMoveDirection()
    {
        Vector2 krakenPosition = GetKrakenPosition();
        Vector2 toTarget = (currentTarget - krakenPosition).normalized;

        // If avoiding obstacles, blend avoidance with target direction
        if (avoidanceDirection != Vector2.zero)
        {
            return Vector2.Lerp(toTarget, avoidanceDirection, avoidanceForce).normalized;
        }

        return toTarget;
    }

    private void CheckForObstacles()
    {
        avoidanceDirection = Vector2.zero;

        Vector2 krakenPosition = GetKrakenPosition();
        Vector2 targetDirection = (currentTarget - krakenPosition).normalized;

        // Get the kraken's radius for offset rays
        float krakenRadius = GetKrakenRadius();

        // Calculate perpendicular direction (90 degrees to the right of target direction)
        Vector2 rightPerpendicularDirection = Vector2.Perpendicular(targetDirection);
        Vector2 leftPerpendicularDirection = -rightPerpendicularDirection;

        // Define the three ray directions
        Vector2[] rayStartPositions = new Vector2[3];
        rayStartPositions[0] = krakenPosition; // Center ray
        rayStartPositions[1] =
            krakenPosition + leftPerpendicularDirection * (krakenRadius + avoidanceBuffer); // Left offset ray
        rayStartPositions[2] =
            krakenPosition + rightPerpendicularDirection * (krakenRadius + avoidanceBuffer); // Right offset ray

        bool hasObstacle = false;
        Vector2 obstacleCenter = Vector2.zero;
        float obstacleRadius = 0f;

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
                    hasObstacle = true;
                    obstacleCenter = hit.collider.transform.position;
                    obstacleRadius = circleCollider.radius * hit.collider.transform.localScale.x;
                    break; // Found an obstacle, no need to check other rays
                }
            }
        }

        if (hasObstacle)
        {
            // Calculate which side to steer around the obstacle
            Vector2 toObstacle = obstacleCenter - krakenPosition;
            Vector2 toTarget = (currentTarget - krakenPosition).normalized;

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
            avoidanceDirection = Vector2
                .Lerp(toTarget, perpendicularDirection, avoidanceForce)
                .normalized;
        }
    }

    private float GetKrakenRadius()
    {
        return krakenCollider != null ? krakenCollider.radius * transform.localScale.x : 0.5f;
    }

    private Vector2 GetKrakenPosition()
    {
        return krakenCollider != null
            ? (Vector2)transform.position + krakenCollider.offset
            : (Vector2)transform.position;
    }

    // Debug visualization
    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;

        // Draw wander bounds
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(wanderBounds.center, wanderBounds.size);

        // Draw current target
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(currentTarget, 0.5f);

        // Draw boat detection radius when angry
        if (isAngry)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(GetKrakenPosition(), boatDetectionRadius);

            // Draw line to target boat if chasing one
            if (targetBoat != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(GetKrakenPosition(), targetBoat.position);
            }
        }

        // Draw raycasts
        if (krakenCollider == null)
            krakenCollider = GetComponent<CircleCollider2D>();

        Gizmos.color = Color.yellow;
        Vector2 krakenPosition = GetKrakenPosition();
        Vector2 targetDirection = (currentTarget - krakenPosition).normalized;

        float krakenRadius = GetKrakenRadius();
        Vector2 rightPerpendicularDirection = Vector2.Perpendicular(targetDirection);
        Vector2 leftPerpendicularDirection = -rightPerpendicularDirection;

        Vector2[] rayStartPositions = new Vector2[3];
        rayStartPositions[0] = krakenPosition;
        rayStartPositions[1] =
            krakenPosition + leftPerpendicularDirection * (krakenRadius + avoidanceBuffer);
        rayStartPositions[2] =
            krakenPosition + rightPerpendicularDirection * (krakenRadius + avoidanceBuffer);

        for (int i = 0; i < 3; i++)
        {
            Vector2 rayStart = rayStartPositions[i];
            Vector2 rayDirection = targetDirection;
            Vector3 rayEnd = (Vector3)(rayStart + rayDirection * raycastDistance);

            Gizmos.DrawLine(rayStart, rayEnd);
        }

        // Draw avoidance direction if avoiding
        if (avoidanceDirection != Vector2.zero)
        {
            Gizmos.color = Color.red;
            Vector3 avoidanceStart = krakenPosition;
            Vector3 avoidanceEnd = avoidanceStart + (Vector3)(avoidanceDirection * 2f);
            Gizmos.DrawLine(avoidanceStart, avoidanceEnd);
        }
    }
}
