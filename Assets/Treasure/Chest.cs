using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chest : MonoBehaviour
{
    public int points = 100;
    public int lives = 0;

    public float despawnTime = 10f;

    private LightAbsorber lightAbsorber;
    private float spawnTime = 0f;

    void Start()
    {
        lightAbsorber = GetComponent<LightAbsorber>();

        spawnTime = Time.time;
    }

    void Update()
    {
        if (Time.time - spawnTime > despawnTime && !lightAbsorber.IsLit())
        {
            Destroy(gameObject);
        }

        if (lightAbsorber.IsFullyLit())
        {
            if (points > 0)
            {
                FloatingTextManager.Instance.SpawnText(
                    $"+{points} PTS",
                    transform.position,
                    FloatingTextManager.pointsColor,
                    1f
                );

                GameManager.Instance.AddScore(points);
            }

            if (lives > 0)
            {
                string livesText = lives > 1 ? "LIVES" : "LIFE";
                FloatingTextManager.Instance.SpawnText(
                    $"+{lives} {livesText}",
                    transform.position,
                    FloatingTextManager.livesColor,
                    1f
                );

                GameManager.Instance.AddLives(lives);
            }

            SFXManager.Instance.PlaySFX("minor_success");

            Destroy(gameObject);
        }
    }
}
