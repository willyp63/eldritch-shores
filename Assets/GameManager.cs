using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Boat Spawning")]
    public GameObject boatPrefab;
    public float spawnInterval = 3f;

    [Header("Spawn Bounds")]
    public Bounds spawnBounds;
    public Bounds targetBounds;

    [Header("Boat Management")]
    public int maxBoats = 10; // Maximum number of boats in the scene

    private List<Boat> activeBoats = new List<Boat>();
    private Coroutine spawnCoroutine;

    void Start()
    {
        // Start spawning boats
        spawnCoroutine = StartCoroutine(SpawnBoatsRoutine());
    }

    void Update()
    {
        // Check if any boats have reached their target and need to be despawned
        CheckForBoatsToDespawn();
    }

    IEnumerator SpawnBoatsRoutine()
    {
        while (true)
        {
            // Wait for the spawn interval
            yield return new WaitForSeconds(spawnInterval);

            // Only spawn if we haven't reached the maximum number of boats
            if (activeBoats.Count < maxBoats)
            {
                SpawnBoat();
            }
        }
    }

    void SpawnBoat()
    {
        if (boatPrefab == null)
        {
            Debug.LogWarning("Boat prefab is not assigned in GameManager!");
            return;
        }

        // Generate random spawn position within bounds
        Vector3 spawnPosition = new Vector3(
            Random.Range(spawnBounds.min.x, spawnBounds.max.x),
            Random.Range(spawnBounds.min.y, spawnBounds.max.y),
            0f
        );

        // Generate random target position within bounds
        Vector3 targetPosition = new Vector3(
            Random.Range(targetBounds.min.x, targetBounds.max.x),
            Random.Range(targetBounds.min.y, targetBounds.max.y),
            0f
        );

        // Instantiate the boat
        GameObject boatObject = Instantiate(boatPrefab, spawnPosition, Quaternion.identity);
        Boat boat = boatObject.GetComponent<Boat>();

        if (boat != null)
        {
            // Set the boat's target
            boat.target = CreateTargetTransform(targetPosition);
            boat.moveSpeed *= Random.Range(0.9f, 1.1f); // Randomly adjust the boat's speed

            // Add to active boats list
            activeBoats.Add(boat);
        }
        else
        {
            Debug.LogError("Boat prefab doesn't have a Boat component!");
            Destroy(boatObject);
        }
    }

    Transform CreateTargetTransform(Vector3 position)
    {
        // Create a temporary GameObject to serve as the target
        GameObject targetObject = new GameObject("BoatTarget");
        targetObject.transform.position = position;

        // The target will be destroyed when the boat is despawned
        return targetObject.transform;
    }

    void CheckForBoatsToDespawn()
    {
        // Check each boat to see if it has reached its target
        for (int i = activeBoats.Count - 1; i >= 0; i--)
        {
            Boat boat = activeBoats[i];

            if (boat == null)
            {
                // Boat was destroyed somehow, remove from list
                activeBoats.RemoveAt(i);
                continue;
            }

            // Check if boat has reached its target
            if (boat.target != null)
            {
                float distanceToTarget = Vector2.Distance(
                    boat.transform.position,
                    boat.target.position
                );

                if (distanceToTarget <= boat.stoppingDistance)
                {
                    // Boat has reached target, despawn it
                    DespawnBoat(boat, i);
                }
            }
        }
    }

    void DespawnBoat(Boat boat, int index)
    {
        if (boat == null)
            return;

        // Destroy the target GameObject
        if (boat.target != null)
        {
            Destroy(boat.target.gameObject);
        }

        // Remove from active boats list
        activeBoats.RemoveAt(index);

        // Destroy the boat GameObject
        Destroy(boat.gameObject);
    }

    // Public method to manually spawn a boat (useful for testing)
    public void SpawnBoatManually()
    {
        if (activeBoats.Count < maxBoats)
        {
            SpawnBoat();
        }
    }

    // Public method to get current boat count
    public int GetActiveBoatCount()
    {
        return activeBoats.Count;
    }

    // Public method to stop spawning
    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    // Public method to start spawning again
    public void StartSpawning()
    {
        if (spawnCoroutine == null)
        {
            spawnCoroutine = StartCoroutine(SpawnBoatsRoutine());
        }
    }

    // Clean up when GameManager is destroyed
    void OnDestroy()
    {
        // Despawn all remaining boats
        for (int i = activeBoats.Count - 1; i >= 0; i--)
        {
            if (activeBoats[i] != null)
            {
                DespawnBoat(activeBoats[i], i);
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(spawnBounds.center, spawnBounds.size);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(targetBounds.center, targetBounds.size);
    }
}
