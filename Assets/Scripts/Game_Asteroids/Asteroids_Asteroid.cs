using UnityEngine;

public class Asteroids_Asteroid : MonoBehaviour
{
    [SerializeField] private float _speed = 180f;
    [SerializeField] private bool _continuousHoming = true;

    private RectTransform _rect;
    private RectTransform _playfield;
    private RectTransform _ship;
    private System.Action<RectTransform> _onDestroyed;
    private Vector2 _dir;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        if (_rect == null) _rect = gameObject.AddComponent<RectTransform>();
    }

    public void Init(RectTransform playfield, RectTransform ship, float speed, bool continuousHoming, System.Action<RectTransform> onDestroyed)
    {
        _playfield = playfield;
        _ship = ship;
        _speed = speed;
        _continuousHoming = continuousHoming;
        _onDestroyed = onDestroyed;

        Vector2 toShip = (_ship != null)
            ? ((Vector2)_ship.anchoredPosition - (Vector2)_rect.anchoredPosition)
            : Vector2.down;
        _dir = toShip.sqrMagnitude > 0.0001f ? toShip.normalized : Vector2.down;

        var lp = _rect.localPosition;
        _rect.localPosition = new Vector3(lp.x, lp.y, 0f);
    }

    private void Update()
    {
        if (_rect == null) return;

        // Move toward ship
        Vector2 moveDir = _dir;
        if (_continuousHoming && _ship != null)
        {
            Vector2 toShip = (Vector2)_ship.anchoredPosition - (Vector2)_rect.anchoredPosition;
            if (toShip.sqrMagnitude > 0.0001f) moveDir = toShip.normalized;
        }
        _rect.anchoredPosition += moveDir * _speed * Time.deltaTime;

        // Destroy if far outside
        if (IsFarOutsidePlayfield(240f))
        {
            _onDestroyed?.Invoke(_rect);
            Destroy(gameObject);
            return;
        }

        // Check collisions
        CheckBulletHits();
        CheckShipHit();
    }

    private bool IsFarOutsidePlayfield(float pad)
    {
        if (_playfield == null) return false;
        Vector2 half = _playfield.rect.size * 0.5f;
        Vector2 p = _rect.anchoredPosition;
        return (p.x < -half.x - pad || p.x > half.x + pad ||
                p.y < -half.y - pad || p.y > half.y + pad);
    }

    private void CheckBulletHits()
    {
        if (_playfield == null) return;

        Rect aRect = MakeRect(_rect);
        var bullets = _playfield.GetComponentsInChildren<Asteroids_Bullet>(false);
        for (int i = 0; i < bullets.Length; i++)
        {
            var b = bullets[i];
            if (b == null) continue;

            var bRectTf = b.GetComponent<RectTransform>();
            if (bRectTf == null) continue;

            if (aRect.Overlaps(MakeRect(bRectTf)))
            {
                Destroy(b.gameObject);
                _onDestroyed?.Invoke(_rect);
                Destroy(gameObject);
                return;
            }
        }
    }

    private void CheckShipHit()
    {
        if (_ship == null) return;

        Rect aRect = MakeRect(_rect);
        Rect sRect = MakeRect(_ship);

        if (aRect.Overlaps(sRect))
        {
            // Asteroid hit player
            _onDestroyed?.Invoke(_rect);
            Destroy(gameObject);

            if (UIManager.Instance != null)
                UIManager.Instance.TriggerDeathEffects();
        }
    }

    private Rect MakeRect(RectTransform rt)
    {
        Vector2 size = rt.rect.size;
        Vector2 half = size * 0.5f;
        Vector2 pos = rt.anchoredPosition;
        return new Rect(pos - half, size);
    }
}
