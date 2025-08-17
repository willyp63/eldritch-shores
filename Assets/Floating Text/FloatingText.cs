using TMPro;
using UnityEngine;

public class FloatingText : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI textComponent;

    [SerializeField]
    private float lifetime = 2f;

    [SerializeField]
    private float riseSpeed = 2f;

    [SerializeField]
    private AnimationCurve fadeCurve;

    [SerializeField]
    private float positionRandomness = 0.5f;

    [SerializeField]
    private float directionRandomness = 0.3f;

    private float timer;
    private Vector3 startPosition;
    private Vector3 randomOffset;
    private Vector3 riseDirection;

    public void Initialize(string text, Vector3 worldPosition, Color color, float fontSize = 1f)
    {
        textComponent.text = text;
        textComponent.color = color;
        textComponent.fontSize *= fontSize;

        // Add random offset to initial position
        randomOffset = new Vector3(
            Random.Range(-positionRandomness, positionRandomness),
            Random.Range(-positionRandomness, positionRandomness),
            0f
        );

        // Create random rise direction (mostly upward with some horizontal variation)
        riseDirection = new Vector3(
            Random.Range(-directionRandomness, directionRandomness),
            1f,
            0f
        ).normalized;

        transform.position = worldPosition + randomOffset;
        startPosition = transform.position;
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Move in random direction
        transform.position = startPosition + riseDirection * (riseSpeed * timer);

        // Fade out
        float alpha = fadeCurve.Evaluate(timer / lifetime);
        Color color = textComponent.color;
        color.a = alpha;
        textComponent.color = color;

        // Destroy when done
        if (timer >= lifetime)
            Destroy(gameObject);
    }
}
