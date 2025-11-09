using UnityEngine;
using UnityEngine.InputSystem;

public class Pong_PlayerPaddle : MonoBehaviour
{
    [SerializeField] private RectTransform _playerPaddle;
    [SerializeField] private float _movementSpeed = 500f;
    private float minY = -185f;
    private float maxY = 185f;

    private void Update()
    {
        Vector2 pos = _playerPaddle.anchoredPosition;
        float moveDirection = 0f;

        // Check if keys are being held down (not just pressed)
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            moveDirection = 1f;
        else if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            moveDirection = -1f;

        // Apply continuous movement based on speed and frame time
        pos.y += moveDirection * _movementSpeed * Time.deltaTime;

        // Clamp position to stay within bounds
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        _playerPaddle.anchoredPosition = pos;
    }
}
