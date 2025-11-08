using UnityEngine;

public class Pong_Opponent : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private RectTransform _paddle;  // Opponent paddle RectTransform
    [SerializeField] private RectTransform _ball;    // The ball to track
    [SerializeField] private RectTransform _playfield;

    [Header("AI Settings")]
    [SerializeField] private float _moveSpeed = 400f;     // How fast the paddle moves vertically
    [SerializeField] private float _trackingAccuracy = 1f; // 0–1 ? how closely it follows the ball
    [SerializeField] private float _reactionDelay = 0.15f; // Reaction lag in seconds

    private float _minY, _maxY;
    private float _targetY;
    private float _reactionTimer = 0f;

    void Start()
    {
        if (_paddle == null) _paddle = GetComponent<RectTransform>();
        Vector2 halfField = _playfield.rect.size * 0.5f;
        Vector2 halfPaddle = _paddle.rect.size * 0.5f;

        _minY = -halfField.y + halfPaddle.y;
        _maxY = halfField.y - halfPaddle.y;
    }

    void Update()
    {
        if (_ball == null) return;

        _reactionTimer -= Time.deltaTime;

        // Only update target occasionally (adds a little lag)
        if (_reactionTimer <= 0f)
        {
            _reactionTimer = _reactionDelay;
            float predictedY = _ball.anchoredPosition.y;

            // Add a small imperfection for human-like behavior
            float randomOffset = Random.Range(-40f, 40f) * (1f - _trackingAccuracy);
            _targetY = Mathf.Clamp(predictedY + randomOffset, _minY, _maxY);
        }

        // Smoothly move toward the target
        Vector2 pos = _paddle.anchoredPosition;
        pos.y = Mathf.MoveTowards(pos.y, _targetY, _moveSpeed * Time.deltaTime);
        pos.y = Mathf.Clamp(pos.y, _minY, _maxY);
        _paddle.anchoredPosition = pos;
    }
}
