using UnityEngine;

public class FloatingTextManager : Singleton<FloatingTextManager>
{
    [SerializeField]
    private FloatingText floatingTextPrefab;

    [SerializeField]
    private Canvas canvas;

    public static readonly Color pointsColor = new Color(180f / 255f, 47f / 255f, 247f / 255f, 1f);
    public static readonly Color multiplierColor = new Color(
        247f / 255f,
        114f / 255f,
        47f / 255f,
        1f
    );

    public void SpawnText(string text, Vector3 position, Color color, float fontSize = 1f)
    {
        FloatingText instance = Instantiate(floatingTextPrefab, canvas.transform);
        instance.Initialize(text, position, color, fontSize);
    }
}
