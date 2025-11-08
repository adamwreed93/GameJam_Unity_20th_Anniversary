using UnityEngine;
using TMPro;
using UnityEngine.Experimental.GlobalIllumination;

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

    [SerializeField] private GameObject _playerContainer;
    [SerializeField] private GameObject _windowFrameContainer;

    [Header("Background Color Cycle")]
    [SerializeField] private UnityEngine.UI.Image _windowFrameContainerImage;
    [SerializeField] private float _windowColorCycleSpeed = 1f;

    private Color _dayWindowColor = new Color32(60, 205, 255, 255);
    private Color _nightWindowColor = new Color32(50, 60, 70, 255);
    private bool _isFadingImageToNight = true;
    private float _imageLerpProgress = 0f;

    [Header("Day/Night Cycle")]
    [SerializeField] private Light _directionalLight;
    [SerializeField] private float _cycleSpeed = 1f;

    private Color _dayColor = new Color32(250, 218, 130, 255);
    private Color _nightColor = new Color32(35, 115, 165, 255);
    private bool _isFadingToNight = true;
    private float _colorLerpProgress = 0f;

    private int _currentScore;

    private void Update()
    {
        if (_directionalLight == null) return;

        _colorLerpProgress += Time.deltaTime * _cycleSpeed;

        if (_isFadingToNight)
        {
            _directionalLight.color = Color.Lerp(_dayColor, _nightColor, _colorLerpProgress);
            if (_colorLerpProgress >= 1f)
            {
                _colorLerpProgress = 0f;
                _isFadingToNight = false;
            }
        }
        else
        {
            _directionalLight.color = Color.Lerp(_nightColor, _dayColor, _colorLerpProgress);
            if (_colorLerpProgress >= 1f)
            {
                _colorLerpProgress = 0f;
                _isFadingToNight = true;
            }
        }

        if (_windowFrameContainerImage != null)
        {
            _imageLerpProgress += Time.deltaTime * _windowColorCycleSpeed;

            if (_isFadingImageToNight)
            {
                _windowFrameContainerImage.color = Color.Lerp(_dayWindowColor, _nightWindowColor, _imageLerpProgress);
                if (_imageLerpProgress >= 1f)
                {
                    _imageLerpProgress = 0f;
                    _isFadingImageToNight = false;
                }
            }
            else
            {
                _windowFrameContainerImage.color = Color.Lerp(_nightWindowColor, _dayWindowColor, _imageLerpProgress);
                if (_imageLerpProgress >= 1f)
                {
                    _imageLerpProgress = 0f;
                    _isFadingImageToNight = true;
                }
            }
        }
    }

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
