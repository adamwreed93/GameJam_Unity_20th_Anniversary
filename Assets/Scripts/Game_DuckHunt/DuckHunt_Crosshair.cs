using UnityEngine;
using UnityEngine.InputSystem;

public class DuckHunt_Crosshair : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private RectTransform _playfield;
    [SerializeField] private RectTransform _crosshair;
    [SerializeField] private DuckHunt_Spawner _spawner;
    [SerializeField] private Canvas _canvas;

    private void Awake()
    {
        if (_crosshair == null) _crosshair = GetComponent<RectTransform>();
        if (_canvas == null && _playfield != null) _canvas = _playfield.GetComponentInParent<Canvas>();
    }

    private void Update()
    {
        if (_playfield == null || _crosshair == null || Mouse.current == null) return;

        Vector2 localMouse;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _playfield,
            Mouse.current.position.ReadValue(),
            _canvas != null ? _canvas.worldCamera : null,
            out localMouse
        );

        // Move crosshair (clamped to playfield)
        Vector2 half = _playfield.rect.size * 0.5f;
        localMouse.x = Mathf.Clamp(localMouse.x, -half.x, half.x);
        localMouse.y = Mathf.Clamp(localMouse.y, -half.y, half.y);
        _crosshair.anchoredPosition = localMouse;

        // Fire
        if (Mouse.current.leftButton.wasPressedThisFrame && _spawner != null)
        {
            // World point at crosshair center
            Vector3 worldPoint = _crosshair.TransformPoint(_crosshair.rect.center);
            _spawner.TryHitAt(worldPoint);
        }
    }
}
