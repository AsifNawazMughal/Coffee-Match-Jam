using UnityEngine;

// Attach to Main Camera to match the game's angled top-down view
[ExecuteAlways]
public class CameraSetup : MonoBehaviour
{
    [Header("Camera Preset — matches reference screenshots")]
    public Vector3 position = new(0f, 12f, -7f);
    public Vector3 rotation = new(55f, 0f, 0f);
    public float fieldOfView = 60f;

    void OnValidate()
    {
        Apply();
    }

    void Start()
    {
        Apply();
    }

    void Apply()
    {
        transform.position = position;
        transform.eulerAngles = rotation;
        if (TryGetComponent<Camera>(out var cam))
            cam.fieldOfView = fieldOfView;
    }
}
