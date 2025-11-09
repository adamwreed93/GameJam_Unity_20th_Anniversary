using UnityEngine;
using UnityEngine.InputSystem;

public class Asteroids_Ship : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private RectTransform _playfield;     // TV game area (RectTransform)
    [SerializeField] private RectTransform _ship;          // UI Image for the ship
    [SerializeField] private RectTransform _bulletPrefab;  // UI Image prefab for the bullet
    [SerializeField] private Canvas _canvas;               // Canvas containing the playfield

    [Header("Shooting")]
    [SerializeField] private float _bulletSpeed = 900f;
    [SerializeField] private float _bulletLifetime = 5f;

    private Vector2 _lastAimDir = Vector2.up; // default facing up

    private void Awake()
    {
        if (_ship == null) _ship = GetComponent<RectTransform>();
        if (_canvas == null && _playfield != null)
            _canvas = _playfield.GetComponentInParent<Canvas>();
    }

    private void Update()
    {
        if (_ship == null || _playfield == null) return;
        if (Mouse.current == null) return;

        // Convert screen mouse pos -> playfield local (anchored) pos
        Vector2 localMouse;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _playfield,
            Mouse.current.position.ReadValue(),
            _canvas != null ? _canvas.worldCamera : null,
            out localMouse
        );

        // ROTATE ONLY (do NOT move ship)
        Vector2 dir = (localMouse - _ship.anchoredPosition);
        if (dir.sqrMagnitude > 0.0001f) _lastAimDir = dir.normalized;

        float angle = Mathf.Atan2(_lastAimDir.y, _lastAimDir.x) * Mathf.Rad2Deg - 90f; // sprite faces up
        _ship.localEulerAngles = new Vector3(0f, 0f, angle);

        // Shoot on left click (just-pressed)
        if (Mouse.current.leftButton.wasPressedThisFrame)
            Fire(_lastAimDir);
    }

    private void Fire(Vector2 dir)
    {
        if (_bulletPrefab == null || _playfield == null) return;

        RectTransform bullet = Instantiate(_bulletPrefab, _playfield);
        bullet.anchoredPosition = _ship.anchoredPosition;
        bullet.localEulerAngles = _ship.localEulerAngles; // purely visual

        var comp = bullet.GetComponent<Asteroids_Bullet>();
        if (comp == null) comp = bullet.gameObject.AddComponent<Asteroids_Bullet>();
        comp.Init(dir.normalized, _bulletSpeed, _bulletLifetime);
    }
}
