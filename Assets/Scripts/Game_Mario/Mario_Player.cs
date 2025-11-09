using UnityEngine;
using UnityEngine.InputSystem;

public class Mario_Player : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private RectTransform _playfield;
    [SerializeField] private RectTransform _player;

    [Header("Jump")]
    [SerializeField] private float _jumpVelocity = 700f;
    [SerializeField] private float _gravity = 1800f;
    [SerializeField] private float _fallGravityMultiplier = 2.2f;       // stronger when falling
    [SerializeField] private float _lowJumpGravityMultiplier = 2.5f;    // stronger when jump released early
    [SerializeField] private float _groundY = -220f;
    [SerializeField] private float _maxFallSpeed = -2500f;

    [Header("Assist")]
    [SerializeField] private float _coyoteTime = 0.10f;   // jump shortly after leaving ground
    [SerializeField] private float _jumpBuffer = 0.12f;   // press jump slightly before landing

    private float _vy;
    private bool _isGrounded;
    private float _coyoteTimer;
    private float _jumpBufferTimer;

    private void Awake()
    {
        if (_player == null) _player = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        if (_player == null) return;
        Vector2 p = _player.anchoredPosition;
        p.y = _groundY;
        _player.anchoredPosition = p;
        _vy = 0f;
        _isGrounded = true;
        _coyoteTimer = 0f;
        _jumpBufferTimer = 0f;
    }

    private void Update()
    {
        if (_player == null || _playfield == null) return;

        bool jumpPressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        bool jumpHeld = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;

        if (jumpPressed) _jumpBufferTimer = _jumpBuffer;
        else _jumpBufferTimer -= Time.deltaTime;

        if (_isGrounded) _coyoteTimer = _coyoteTime;
        else _coyoteTimer -= Time.deltaTime;

        if (_jumpBufferTimer > 0f && (_isGrounded || _coyoteTimer > 0f))
        {
            _vy = _jumpVelocity;
            _isGrounded = false;
            _coyoteTimer = 0f;
            _jumpBufferTimer = 0f;
        }

        float g = _gravity;
        if (_vy < 0f) g *= _fallGravityMultiplier;
        else if (!jumpHeld) g *= _lowJumpGravityMultiplier;

        _vy -= g * Time.deltaTime;
        if (_vy < _maxFallSpeed) _vy = _maxFallSpeed;

        Vector2 p2 = _player.anchoredPosition;
        p2.y += _vy * Time.deltaTime;

        if (p2.y <= _groundY)
        {
            p2.y = _groundY;
            _vy = 0f;
            _isGrounded = true;
        }
        else
        {
            _isGrounded = false;
        }

        _player.anchoredPosition = p2;
    }

    public Rect GetHitbox()
    {
        Vector2 size = _player.rect.size;
        Vector2 half = size * 0.5f;
        Vector2 pos = _player.anchoredPosition;
        return new Rect(pos - half, size);
    }
}
