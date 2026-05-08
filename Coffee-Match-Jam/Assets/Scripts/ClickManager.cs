using UnityEngine;
using UnityEngine.InputSystem;

// Sits on Main Camera. Uses the new Input System — works for mouse and touch.
public class ClickManager : MonoBehaviour
{
    Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        // Pointer covers both Mouse and Touchscreen
        var pointer = Pointer.current;
        if (pointer == null || !pointer.press.wasPressedThisFrame) return;

        Vector2 screenPos = pointer.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(screenPos);

        if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) return;

        var handler = hit.collider.GetComponentInParent<ContainerClickHandler>();
        if (handler == null || handler.lane == null) return;

        if (handler.TryGetComponent<Container>(out var box))
            handler.lane.OnBoxClicked(box);
    }
}
