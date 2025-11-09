using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Events;

public class PongBall : MonoBehaviour, IResettableGame
{
    [Header("Scene References")]
    [SerializeField] private RectTransform _ball;
    [SerializeField] private RectTransform _playerPaddle;
    [SerializeField] private RectTransform _opponentPaddle;
    [SerializeField] private RectTransform _playfield;

    [Header("Ball Tuning")]
    [SerializeField] private float _startSpeed = 500f;
    [SerializeField] private float _maxSpeed = 1400f;
    [SerializeField] private float _speedIncrement = 60f;
    [SerializeField] private float _spinFactor = 0.75f;
    [SerializeField] private float _serveDelay = 0.6f;

    //[Header("Scoring Events")]
    //public UnityEvent OnLeftPlayerScored;   // when enemy misses
    //public UnityEvent OnRightPlayerScored;  // when player misses

    private Vector2 _velocity;
    private Vector2 _queuedServeDir = Vector2.right;
    private bool _gameOver = false;

    private void EnsureRefs()
    {
        if (_ball == null) _ball = GetComponent<RectTransform>();
    }

    void Start()
    {
        EnsureRefs();
        ResetAndServe(); // first run
    }

    void Update()
    {
        if (_gameOver) return; // stop updating if player lost

        Vector2 pos = _ball.anchoredPosition;
        pos += _velocity * Time.deltaTime;

        Vector2 halfField = _playfield.rect.size * 0.5f;
        Vector2 ballHalf = _ball.rect.size * 0.5f;

        // Top / Bottom bounce
        if (pos.y + ballHalf.y >= halfField.y && _velocity.y > 0f)
        {
            pos.y = halfField.y - ballHalf.y;
            _velocity.y = -_velocity.y;
        }
        else if (pos.y - ballHalf.y <= -halfField.y && _velocity.y < 0f)
        {
            pos.y = -halfField.y + ballHalf.y;
            _velocity.y = -_velocity.y;
        }

        // Paddle collisions
        CheckPaddleCollision(_playerPaddle, true, ref pos);
        CheckPaddleCollision(_opponentPaddle, false, ref pos);

        _ball.anchoredPosition = pos;

        // Goal detection
        if (pos.x - ballHalf.x > halfField.x)
        {
            // Enemy missed ? left player scores ? continue
            //OnLeftPlayerScored?.Invoke();
            UIManager.Instance.UpdateScoreText(100);
            ResetAndServe(towardsLeft: false);
        }
        else if (pos.x + ballHalf.x < -halfField.x)
        {
            // Player missed ? stop the game
            //OnRightPlayerScored?.Invoke();
            GameOver();
        }
    }


    void OnEnable()
    {
        EnsureRefs();
        _gameOver = false;
        CancelInvoke(nameof(BeginServe));
        ResetAndServe();                 // always reset when the game turns on
    }

    void OnDisable()
    {
        CancelInvoke(nameof(BeginServe));
        _velocity = Vector2.zero;
    }

    // IResettableGame support (still fine to keep)
    public void ResetGame()
    {
        _gameOver = false;
        _velocity = Vector2.zero;
        ResetAndServe();
    }


    void ResetAndServe(bool? towardsLeft = null)
    {
        _ball.anchoredPosition = Vector2.zero;

        int dirX = towardsLeft.HasValue
            ? (towardsLeft.Value ? -1 : 1)
            : (Random.value < 0.5f ? -1 : 1);

        float dirY = Random.Range(-0.5f, 0.5f);
        _queuedServeDir = new Vector2(dirX, dirY).normalized;

        _velocity = Vector2.zero;
        CancelInvoke(nameof(BeginServe));
        Invoke(nameof(BeginServe), _serveDelay);
    }

    private void BeginServe()
    {
        _velocity = _queuedServeDir * _startSpeed;
    }

    private void GameOver()
    {
        _velocity = Vector2.zero;
        _gameOver = true;
        CancelInvoke(nameof(BeginServe));

        UIManager.Instance.TriggerDeathEffects();
    }

    void CheckPaddleCollision(RectTransform paddle, bool isLeftPaddle, ref Vector2 ballPos)
    {
        Vector2 pSize = paddle.rect.size;
        Vector2 pPos = paddle.anchoredPosition;
        Vector2 bHalf = _ball.rect.size * 0.5f;

        Rect paddleRect = new Rect(pPos - pSize * 0.5f, pSize);
        Rect ballRect = new Rect(ballPos - bHalf, _ball.rect.size);

        if (isLeftPaddle && _velocity.x >= 0f) return;
       
        if (!isLeftPaddle && _velocity.x <= 0f) return;

        if (paddleRect.Overlaps(ballRect))
        {
            if (isLeftPaddle) 
            {
                ballPos.x = paddleRect.xMax + bHalf.x;
                UIManager.Instance.UpdateScoreText(10); 
            }
            else ballPos.x = paddleRect.xMin - bHalf.x;

            float offset = Mathf.Clamp((ballPos.y - pPos.y) / (pSize.y * 0.5f), -1f, 1f);

            float outward = isLeftPaddle ? 1f : -1f;
            _velocity.x = Mathf.Abs(_velocity.x) * outward;
            _velocity.y += offset * _startSpeed * _spinFactor;

            float newSpeed = Mathf.Min(_velocity.magnitude + _speedIncrement, _maxSpeed);
            _velocity = _velocity.normalized * newSpeed;
        }
    }
}