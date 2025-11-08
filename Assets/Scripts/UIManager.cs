using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    //Includes the "Awake" method.
    #region Singleton
    private static UIManager _instance;

    public static UIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                Debug.LogError("UIManager is null!");
            }
            return _instance;
        }
    }

    private void Awake()
    {
        _instance = this;
    }
    #endregion

    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TextMeshProUGUI _scoreTextShadow;

    private int _currentScore;

    public void UpdateScoreText(int score)
    {
        _currentScore += score;

        if (_scoreText != null && _scoreTextShadow != null)
        {
            _scoreText.text = _currentScore.ToString();
            _scoreTextShadow.text = _currentScore.ToString();
        }
    }
}
