using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class MouseLight : MonoBehaviour
{
    [SerializeField]
    private bool isAlwaysOn = false;

    [SerializeField]
    private Light2D mouseLight;

    [SerializeField]
    private Camera cam;

    [SerializeField]
    private float lightIntensity = 1f;

    [SerializeField]
    private float fadeInDuration = 0.2f; // Duration of fade in transition

    [SerializeField]
    private float fadeOutDuration = 0.3f; // Duration of fade out transition

    private Coroutine fadeCoroutine;
    private bool isLightOn = false;
    public bool IsLightOn => isLightOn;

    void Start()
    {
        // If no camera is assigned, use the main camera
        if (cam == null)
            cam = Camera.main;

        // Start with light off
        mouseLight.intensity = 0f;
    }

    void Update()
    {
        // Get mouse position in screen coordinates
        Vector3 mousePosition = Input.mousePosition;

        // Set the Z position (distance from camera)
        mousePosition.z = cam.nearClipPlane;

        // Convert screen coordinates to world coordinates
        Vector3 worldPosition = cam.ScreenToWorldPoint(mousePosition);

        // Update the object's position
        transform.position = worldPosition;

        if (Input.GetMouseButtonDown(0) && !isAlwaysOn)
        {
            if (!isLightOn)
            {
                TurnOnLight();
            }
        }
        else if (Input.GetMouseButtonUp(0) && !isAlwaysOn)
        {
            if (isLightOn)
            {
                TurnOffLight();
            }
        }
        else if (isAlwaysOn)
        {
            mouseLight.intensity = lightIntensity;
            isLightOn = true;
        }
    }

    private void TurnOnLight()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeLight(0f, lightIntensity, fadeInDuration));
        isLightOn = true;
    }

    private void TurnOffLight()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeLight(lightIntensity, 0f, fadeOutDuration));
        isLightOn = false;
    }

    private IEnumerator FadeLight(float startIntensity, float targetIntensity, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            mouseLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, t);
            yield return null;
        }

        mouseLight.intensity = targetIntensity;
        fadeCoroutine = null;
    }
}
