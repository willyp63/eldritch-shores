using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    [Header("Boat Spawning")]
    public GameObject boatPrefab;
    public float initialBoatSpawnInterval = 12f;
    public float boatSpawnIntervalDecreasePerMinute = 1f;
    public float minBoatSpawnInterval = 4f;
    public float initialBoatSpawnDelay = 1f;
    public float scoreBoatAtY = -4.5f;

    private float boatSpawnStartTime = 0f;
    private float currentBoatSpawnInterval;

    [Header("Spawn Bounds")]
    public Bounds spawnBounds;
    public Bounds targetBounds;

    [Header("Boat Management")]
    public int maxBoats = 10; // Maximum number of boats in the scene

    [Header("Chest Spawning")]
    public GameObject chestPrefab;
    public GameObject livesChestPrefab;
    public float livesChestSpawnChance = 0.2f;
    public float chestSpawnInterval = 10f;
    public Bounds chestSpawnBounds;
    public List<Bounds> invalidChestSpawnBounds;
    public LayerMask obstacleLayer;

    [Header("Kraken Spawning")]
    public GameObject krakenPrefab;
    public float krakenSpawnInterval = 60f;
    public float initialKrakenSpawnDelay = 10f;

    [Header("Gameplay")]
    public float maxEnergy = 100f;
    public float energyGainedPerSecond = 1f;
    public float energyLostPerSecond = 1f;
    public int maxLives = 3;

    [Header("Mouse Light")]
    public MouseLight mouseLight;

    private int numLives = 0;
    private int currentScore = 0;
    private float currentEnergy = 0f;

    public System.Action<float> OnEnergyChanged;
    public System.Action<int> OnScoreChanged;
    public System.Action<int> OnLivesChanged;

    private List<Boat> activeBoats = new List<Boat>();
    private Coroutine spawnCoroutine;
    private Coroutine chestSpawnCoroutine;

    private bool isCursorVisible = true;

    void Start()
    {
        numLives = maxLives;
        currentScore = 0;
        currentEnergy = maxEnergy;

        currentBoatSpawnInterval = initialBoatSpawnInterval;
        boatSpawnStartTime = Time.time;

        OnEnergyChanged?.Invoke(currentEnergy);
        OnLivesChanged?.Invoke(numLives);
        OnScoreChanged?.Invoke(currentScore);

        // Start spawning boats
        spawnCoroutine = StartCoroutine(SpawnBoatsRoutine());

        // Start spawning chests
        chestSpawnCoroutine = StartCoroutine(SpawnChestsRoutine());

        // Start spawning kraken
        StartCoroutine(SpawnKrakenRoutine());
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isCursorVisible = !isCursorVisible;
        }
        Cursor.visible = isCursorVisible;

        // Check if any boats have reached their target and need to be despawned
        CheckForBoatsToDespawn();
        CheckForBoatsToScore();

        if (mouseLight.IsLightOn)
        {
            currentEnergy -= energyLostPerSecond * Time.deltaTime;
            currentEnergy = Mathf.Max(0f, currentEnergy);
            OnEnergyChanged?.Invoke(currentEnergy);
        }
        else
        {
            currentEnergy += energyGainedPerSecond * Time.deltaTime;
            currentEnergy = Mathf.Min(maxEnergy, currentEnergy);
            OnEnergyChanged?.Invoke(currentEnergy);
        }
    }

    void ResetGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoseLife()
    {
        numLives--;
        numLives = Mathf.Max(numLives, 0);
        OnLivesChanged?.Invoke(numLives);

        if (numLives <= 0)
        {
            // Game over
            Debug.Log("Game over");
            ResetGame();
        }
    }

    public void AddLives(int lives)
    {
        numLives += lives;
        numLives = Mathf.Min(numLives, maxLives);
        OnLivesChanged?.Invoke(numLives);
    }

    public void AddScore(int score)
    {
        currentScore += score;
        OnScoreChanged?.Invoke(currentScore);
    }

    void CheckForBoatsToScore()
    {
        foreach (Boat boat in activeBoats)
        {
            if (boat != null && !boat.IsScored && boat.GetBoatPosition().y <= scoreBoatAtY)
            {
                FloatingTextManager.Instance.SpawnText(
                    $"+{boat.points} PTS",
                    boat.GetBoatPosition(),
                    FloatingTextManager.pointsColor,
                    1f
                );

                AddScore(boat.points);
                boat.Score();
            }
        }
    }

    IEnumerator SpawnKrakenRoutine()
    {
        yield return new WaitForSeconds(initialKrakenSpawnDelay);
        SpawnKraken();

        while (true)
        {
            yield return new WaitForSeconds(krakenSpawnInterval);
            SpawnKraken();
        }
    }

    void SpawnKraken()
    {
        if (krakenPrefab == null)
        {
            Debug.LogWarning("Kraken prefab is not assigned in GameManager!");
            return;
        }

        // Generate random spawn position within bounds
        Vector3 spawnPosition = new Vector3(
            Random.Range(spawnBounds.min.x, spawnBounds.max.x),
            Random.Range(spawnBounds.min.y, spawnBounds.max.y),
            0f
        );

        // Instantiate the kraken
        GameObject krakenObject = Instantiate(krakenPrefab, spawnPosition, Quaternion.identity);
        Kraken kraken = krakenObject.GetComponent<Kraken>();
        if (kraken != null)
        {
            // TODO: Add kraken logic
        }
    }

    IEnumerator SpawnBoatsRoutine()
    {
        yield return new WaitForSeconds(initialBoatSpawnDelay);

        SpawnBoat();

        while (true)
        {
            // Wait for the spawn interval
            yield return new WaitForSeconds(currentBoatSpawnInterval);

            // Only spawn if we haven't reached the maximum number of boats
            if (activeBoats.Count < maxBoats)
            {
                SpawnBoat();
            }

            float boatSpawnDecrease =
                boatSpawnIntervalDecreasePerMinute * (Time.time - boatSpawnStartTime) / 60f;
            currentBoatSpawnInterval = Mathf.Max(
                minBoatSpawnInterval,
                initialBoatSpawnInterval - boatSpawnDecrease
            );
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

        if (chestSpawnCoroutine != null)
        {
            StopCoroutine(chestSpawnCoroutine);
            chestSpawnCoroutine = null;
        }
    }

    // Public method to start spawning again
    public void StartSpawning()
    {
        if (spawnCoroutine == null)
        {
            spawnCoroutine = StartCoroutine(SpawnBoatsRoutine());
        }

        if (chestSpawnCoroutine == null)
        {
            chestSpawnCoroutine = StartCoroutine(SpawnChestsRoutine());
        }
    }

    // Public method to stop chest spawning
    public void StopChestSpawning()
    {
        if (chestSpawnCoroutine != null)
        {
            StopCoroutine(chestSpawnCoroutine);
            chestSpawnCoroutine = null;
        }
    }

    // Public method to start chest spawning again
    public void StartChestSpawning()
    {
        if (chestSpawnCoroutine == null)
        {
            chestSpawnCoroutine = StartCoroutine(SpawnChestsRoutine());
        }
    }

    // Chest spawning methods
    IEnumerator SpawnChestsRoutine()
    {
        while (true)
        {
            // Wait for the chest spawn interval
            yield return new WaitForSeconds(chestSpawnInterval);

            // Spawn a chest
            SpawnChest();
        }
    }

    void SpawnChest()
    {
        if (chestPrefab == null || livesChestPrefab == null)
        {
            Debug.LogWarning("Chest prefab is not assigned in GameManager!");
            return;
        }

        Vector3 spawnPosition = GetValidChestSpawnPosition();

        // Check if we should spawn a lives chest
        GameObject chestToSpawn = chestPrefab;
        if (numLives < maxLives && Random.value < livesChestSpawnChance)
        {
            chestToSpawn = livesChestPrefab;
        }

        if (spawnPosition != Vector3.zero)
        {
            // Instantiate the chest at the valid position
            GameObject chestObject = Instantiate(chestToSpawn, spawnPosition, Quaternion.identity);
            Chest chest = chestObject.GetComponent<Chest>();
            chest.despawnTime = chestSpawnInterval;
            Debug.Log($"Chest spawned at position: {spawnPosition}");
        }
        else
        {
            Debug.LogWarning("Could not find a valid position to spawn chest after 10 attempts!");
        }
    }

    Vector3 GetValidChestSpawnPosition()
    {
        const int maxAttempts = 100;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Generate random position within chest spawn bounds
            Vector3 randomPosition = new Vector3(
                Random.Range(chestSpawnBounds.min.x, chestSpawnBounds.max.x),
                Random.Range(chestSpawnBounds.min.y, chestSpawnBounds.max.y),
                0f
            );

            // Check if the position overlaps with any invalid chest spawn bounds
            if (IsPositionInInvalidBounds(randomPosition))
            {
                continue;
            }

            // Check if the position overlaps with anything in the obstacle layer
            if (IsPositionOccupied(randomPosition))
            {
                continue;
            }

            return randomPosition;
        }

        // If we couldn't find a valid position after 10 attempts, return zero vector
        return Vector3.zero;
    }

    bool IsPositionInInvalidBounds(Vector3 position)
    {
        foreach (Bounds invalidBounds in invalidChestSpawnBounds)
        {
            if (invalidBounds.Contains(position))
            {
                return true;
            }
        }
        return false;
    }

    bool IsPositionOccupied(Vector3 position)
    {
        // Use Physics2D.OverlapCircle to check if there are any colliders at the position
        // The radius should be small enough to detect overlap but not too small
        float checkRadius = 0.5f;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, checkRadius, obstacleLayer);

        return colliders.Length > 0;
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

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(chestSpawnBounds.center, chestSpawnBounds.size);

        Gizmos.color = new Color(1, 0.5f, 0);
        foreach (Bounds invalidBounds in invalidChestSpawnBounds)
        {
            Gizmos.DrawWireCube(invalidBounds.center, invalidBounds.size);
        }
    }
}
