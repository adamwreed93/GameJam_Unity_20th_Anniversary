using UnityEngine;

public class Mario_Obstacle : MonoBehaviour
{
    [SerializeField] private float _speed = 400f;
    [SerializeField] private float _lifetime = 3f;

    private RectTransform _rect;
    private RectTransform _playfield;
    private RectTransform _playerRect;
    private System.Action<Mario_Obstacle> _onRemoved;
    private bool _scored;
    private float _timer; // NEW

    private static readonly Vector3[] _cornersA = new Vector3[4];
    private static readonly Vector3[] _cornersB = new Vector3[4];
    private static readonly Vector3[] _cornersPlay = new Vector3[4];

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        if (_rect == null) _rect = gameObject.AddComponent<RectTransform>();
    }

    public void Init(RectTransform playfield, RectTransform playerRect, float speed, System.Action<Mario_Obstacle> onRemoved)
    {
        _playfield = playfield;
        _playerRect = playerRect;
        _speed = speed;
        _onRemoved = onRemoved;

        var lp = _rect.localPosition;
        _rect.localPosition = new Vector3(lp.x, lp.y, 0f);
        _timer = 0f; // NEW: reset lifetime
    }

    private void Update()
    {
        if (_rect == null) return;

        // Move left
        Vector2 pos = _rect.anchoredPosition;
        pos.x -= _speed * Time.deltaTime;
        _rect.anchoredPosition = pos;

        // Lifetime timer
        _timer += Time.deltaTime;
        if (_timer >= _lifetime)
        {
            _onRemoved?.Invoke(this);
            Destroy(gameObject);
            return;
        }

        // Scoring
        if (!_scored && _playerRect != null)
        {
            Rect a = GetWorldRect(_rect, _cornersA);
            Rect p = GetWorldRect(_playerRect, _cornersB);
            if (a.xMax < p.xMin)
            {
                _scored = true;
                if (UIManager.Instance != null) UIManager.Instance.UpdateScoreText(10);
            }
        }

        // Optional playfield-based despawn
        if (_playfield != null)
        {
            Rect a = GetWorldRect(_rect, _cornersA);
            Rect pr = GetWorldRect(_playfield, _cornersPlay);
            if (a.xMax < pr.xMin - 200f)
            {
                _onRemoved?.Invoke(this);
                Destroy(gameObject);
                return;
            }
        }

        CheckHitPlayer();
    }

    private void CheckHitPlayer()
    {
        if (_playerRect == null) return;
        Rect a = GetWorldRect(_rect, _cornersA);
        Rect p = GetWorldRect(_playerRect, _cornersB);
        if (a.Overlaps(p))
        {
            _onRemoved?.Invoke(this);
            Destroy(gameObject);
            if (UIManager.Instance != null) UIManager.Instance.TriggerDeathEffects();
        }
    }

    private static Rect GetWorldRect(RectTransform rt, Vector3[] buffer)
    {
        rt.GetWorldCorners(buffer);
        Vector2 bl = new Vector2(buffer[0].x, buffer[0].y);
        Vector2 tr = new Vector2(buffer[2].x, buffer[2].y);
        return new Rect(bl, tr - bl);
    }
}
