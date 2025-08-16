using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [Header("Lighting Settings")]
    public float chargeTime = 0.5f;
    public float drainTime = 3f;
    public bool alwaysLit = false;

    private float currentLightLevel = 0f;
    private bool isBeingCharged = false;
    private SpriteRenderer[] spriteRenderers;

    void Start()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
    }

    void Update()
    {
        HandleMouseInput();
        UpdateLightLevel();
        UpdateVisuals();
    }

    void HandleMouseInput()
    {
        // Check if mouse is over this obstacle
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D hitCollider = Physics2D.OverlapPoint(mousePosition);

        if (hitCollider != null && hitCollider.gameObject == gameObject)
        {
            isBeingCharged = true;
        }
        else
        {
            isBeingCharged = false;
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
                    // Simple transition from dark gray to white
                    Color darkGray = new Color(0.3f, 0.3f, 0.3f);
                    Color finalColor = Color.Lerp(darkGray, Color.white, currentLightLevel);

                    spriteRenderers[i].color = finalColor;
                }
            }
        }
    }

    // Public method to check if obstacle is lit
    public bool IsLit()
    {
        return currentLightLevel > 0f;
    }

    // Public method to get current light level (0-1)
    public float GetLightLevel()
    {
        return currentLightLevel;
    }
}
