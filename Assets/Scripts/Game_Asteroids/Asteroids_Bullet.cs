using UnityEngine;

public class Asteroids_Bullet : MonoBehaviour
{
    [SerializeField] private float _speed = 900f;
    [SerializeField] private float _lifetime = 2.5f;

    private RectTransform _rect;
    private Vector2 _dir;
    private float _t;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        if (_rect == null) _rect = gameObject.AddComponent<RectTransform>();
    }

    public void Init(Vector2 dir, float speed, float lifetime)
    {
        _dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.up;
        _speed = speed;
        _lifetime = lifetime;
        _t = 0f;
    }

    private void Update()
    {
        if (_rect == null) return;

        _rect.anchoredPosition += _dir * _speed * Time.deltaTime;

        _t += Time.deltaTime;
        if (_t >= _lifetime)
            Destroy(gameObject);
    }
}
