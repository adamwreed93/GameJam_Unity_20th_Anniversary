using UnityEngine;
using UnityEngine.InputSystem;

public class Asteroids_Ship : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private RectTransform _playfield;
    [SerializeField] private RectTransform _ship;
    [SerializeField] private RectTransform _bulletPrefab;
    [SerializeField] private RectTransform _bulletContainer;
    [SerializeField] private Canvas _canvas;

    [Header("Shooting")]
    [SerializeField] private float _bulletSpeed = 900f;
    [SerializeField] private float _bulletLifetime = 5f;

    private Vector2 _lastAimDir = Vector2.up;

    private void Awake()
    {
        if (_ship == null) _ship = GetComponent<RectTransform>();
        if (_canvas == null && _playfield != null)
            _canvas = _playfield.GetComponentInParent<Canvas>();
    }

    private void OnEnable()
    {
        // Clear any leftover bullets from previous rounds
        if (_bulletContainer != null)
        {
            for (int i = _bulletContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = _bulletContainer.GetChild(i);
                if (child != null)
                    Destroy(child.gameObject);
            }
        }
    }


    private void Update()
    {
        if (_ship == null || _playfield == null) return;
        if (Mouse.current == null) return;

        Vector2 localMouse;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _playfield,
            Mouse.current.position.ReadValue(),
            _canvas != null ? _canvas.worldCamera : null,
            out localMouse
        );

        Vector2 dir = (localMouse - _ship.anchoredPosition);
        if (dir.sqrMagnitude > 0.0001f) _lastAimDir = dir.normalized;

        float angle = Mathf.Atan2(_lastAimDir.y, _lastAimDir.x) * Mathf.Rad2Deg - 90f;
        _ship.localEulerAngles = new Vector3(0f, 0f, angle);

        if (Mouse.current.leftButton.wasPressedThisFrame)
            Fire(_lastAimDir);
    }

    private void Fire(Vector2 dir)
    {
        if (_bulletPrefab == null || _playfield == null) return;

        // Spawn bullet inside Bullet Container if assigned
        RectTransform parent = _bulletContainer != null ? _bulletContainer : _playfield;
        RectTransform bullet = Instantiate(_bulletPrefab, parent);
        bullet.anchoredPosition = _ship.anchoredPosition;
        bullet.localEulerAngles = _ship.localEulerAngles;

        var comp = bullet.GetComponent<Asteroids_Bullet>();
        if (comp == null) comp = bullet.gameObject.AddComponent<Asteroids_Bullet>();
        comp.Init(dir.normalized, _bulletSpeed, _bulletLifetime);
    }
}
