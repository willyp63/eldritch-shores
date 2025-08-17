using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LightAbsorber : MonoBehaviour
{
    [Header("Lighting Settings")]
    public float chargeTime = 0.5f;
    public float drainTime = 3f;
    public float maxGlow = 5f;
    public bool alwaysLit = false;
    public bool randomizeColors = false;

    [Header("Interaction Settings")]
    public float interactionDistance = 1f; // Distance from collider edge to start charging

    private float currentLightLevel = 0f;
    private bool isBeingCharged = false;
    private SpriteRenderer[] spriteRenderers;
    private CircleCollider2D circleCollider;

    void Start()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        circleCollider = GetComponent<CircleCollider2D>();

        spriteRenderers = spriteRenderers
            .Where(sr => sr.gameObject.name != "Ripples Sprite")
            .ToArray();

        if (randomizeColors)
        {
            float randomShade = Random.Range(0.9f, 1f);
            Color randomColor = new Color(randomShade, randomShade, randomShade);
            foreach (var spriteRenderer in spriteRenderers)
            {
                spriteRenderer.color = randomColor;
            }
        }

        if (circleCollider == null)
        {
            Debug.LogWarning("Obstacle requires a CircleCollider2D component!");
        }
    }

    void Update()
    {
        HandleMouseInput();
        UpdateLightLevel();
        UpdateVisuals();
    }

    void HandleMouseInput()
    {
        // Check if mouse is within interaction distance of this obstacle
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (circleCollider != null)
        {
            // Calculate distance from mouse to the center of the collider
            float distanceToCenter = Vector2.Distance(mousePosition, (Vector2)transform.position);

            // Calculate the total interaction radius (collider radius + interaction distance)
            float totalInteractionRadius = circleCollider.radius + interactionDistance;

            // Check if mouse is within the interaction range
            if (distanceToCenter <= totalInteractionRadius)
            {
                isBeingCharged = true;
            }
            else
            {
                isBeingCharged = false;
            }
        }
    }

    void UpdateLightLevel()
    {
        if (isBeingCharged || alwaysLit)
        {
            // Charge up the light level from 0 to 1
            currentLightLevel += Time.deltaTime / chargeTime;
            currentLightLevel = Mathf.Min(currentLightLevel, 1f);
        }
        else
        {
            // Gradually drain the light level from 1 to 0
            if (currentLightLevel > 0f)
            {
                currentLightLevel -= Time.deltaTime / drainTime;
                currentLightLevel = Mathf.Max(currentLightLevel, 0f);
            }
        }
    }

    void UpdateVisuals()
    {
        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    spriteRenderers[i].material.SetFloat("_Glow", currentLightLevel * maxGlow);
                }
            }
        }
    }

    // Public method to check if obstacle is lit
    public bool IsLit()
    {
        return currentLightLevel > 0f;
    }

    public bool IsFullyLit()
    {
        return currentLightLevel >= 1f;
    }

    // Public method to get current light level (0-1)
    public float GetLightLevel()
    {
        return currentLightLevel;
    }

    public void SetLightLevel(float level)
    {
        currentLightLevel = level;
    }
}
