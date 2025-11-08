using UnityEngine;
using UnityEngine.InputSystem;

public class Pong : MonoBehaviour
{
    [SerializeField] private RectTransform _playerPaddle;
    [SerializeField] private float _movementSpeed = 500f;
    private float minY = -185f;
    private float maxY = 185f;

    void Update()
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

//When Pong is active

//Set paddle movement speed

//Get user input for up and down
// Move the Left Paddle up and down between -185 x 185

//Ball bounces off of paddles

//Make Right paddle AI track the ball and then choose to either miss or not miss. (chance to miss is 5%)

//10 points awarded to player for every hit. 100 points if the enmey misses

//If player misses then say game over, fade to black, Age the player character, start next game
