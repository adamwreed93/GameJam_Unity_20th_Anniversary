using UnityEngine;

public class DuckHunt_Target : MonoBehaviour
{
    [Header("Motion")]
    [SerializeField] private float _speed = 550f;
    [SerializeField] private float _jitterAmp = 120f;
    [SerializeField] private float _jitterFreq = 3f;
    [SerializeField] private float _lifetime = 4.5f;

    private RectTransform _rect;
    private RectTransform _playfield;
    private System.Action<DuckHunt_Target, bool> _onGone;
    private Vector2 _dir;           // base direction (normalized)
    private float _t;
    private float _sideSign;        // perpendicular for sine wobble

    private static readonly Vector3[] _corners = new Vector3[4];
    private static readonly Vector3[] _pfCorners = new Vector3[4];

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        if (_rect == null) _rect = gameObject.AddComponent<RectTransform>();
    }

    public void Init(RectTransform playfield, Vector2 startPos, Vector2 dir, float speed, float lifetime, float jitterAmp, float jitterFreq, System.Action<DuckHunt_Target, bool> onGone)
    {
        _playfield = playfield;
        _rect.anchoredPosition = startPos;
        _rect.localPosition = new Vector3(_rect.localPosition.x, _rect.localPosition.y, 0f);

        _dir = (dir.sqrMagnitude > 0.0001f) ? dir.normalized : Vector2.right;
        _speed = speed;
        _lifetime = lifetime;
        _jitterAmp = jitterAmp;
        _jitterFreq = Mathf.Max(0.01f, jitterFreq);
        _onGone = onGone;
        _t = 0f;

        // choose a consistent perpendicular for wobble
        Vector2 perp = new Vector2(-_dir.y, _dir.x);
        _sideSign = Mathf.Sign(Random.Range(-1f, 1f));
        if (Mathf.Abs(_sideSign) < 0.5f) _sideSign = 1f;
        _sideSign *= (perp.sqrMagnitude > 0.001f) ? 1f : 0f;
    }

    private void Update()
    {
        if (_rect == null) return;

        _t += Time.deltaTime;

        // base movement
        Vector2 pos = _rect.anchoredPosition;
        pos += _dir * _speed * Time.deltaTime;

        // sine wobble perpendicular to direction
        if (_sideSign != 0f)
        {
            Vector2 perp = new Vector2(-_dir.y, _dir.x);
            float wobble = Mathf.Sin(_t * _jitterFreq) * _jitterAmp * _sideSign;
            pos += perp * wobble * Time.deltaTime; // smooth wobble offset over time
        }

        _rect.anchoredPosition = pos;

        // lifetime
        if (_t >= _lifetime)
        {
            _onGone?.Invoke(this, false); // false = expired
            Destroy(gameObject);
            return;
        }

        // offscreen check with padding
        if (_playfield != null)
        {
            Rect r = WorldRect(_rect, _corners);
            Rect pf = WorldRect(_playfield, _pfCorners);
            float pad = 200f;
            if (r.xMax < pf.xMin - pad || r.xMin > pf.xMax + pad || r.yMax < pf.yMin - pad || r.yMin > pf.yMax + pad)
            {
                _onGone?.Invoke(this, false); // left screen
                Destroy(gameObject);
                return;
            }
        }
    }

    public bool ContainsWorldPoint(Vector3 worldPoint)
    {
        Rect r = WorldRect(_rect, _corners);
        return r.Contains(new Vector2(worldPoint.x, worldPoint.y));
    }

    private static Rect WorldRect(RectTransform rt, Vector3[] buf)
    {
        rt.GetWorldCorners(buf);
        Vector2 bl = new Vector2(buf[0].x, buf[0].y);
        Vector2 tr = new Vector2(buf[2].x, buf[2].y);
        return new Rect(bl, tr - bl);
    }
}
