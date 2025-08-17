using System.Collections;
using UnityEngine;

public class MouseLight : MonoBehaviour
{
    [SerializeField]
    private Camera cam;

    void Start()
    {
        // If no camera is assigned, use the main camera
        if (cam == null)
            cam = Camera.main;
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
    }
}
