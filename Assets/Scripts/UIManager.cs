using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;

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


    private int _currentScore;

    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TextMeshProUGUI _scoreTextShadow;
    [SerializeField] private Image _blackScreenOverlay;

    [SerializeField] private GameObject _playerContainer;
    private int _currentPlayerIndex = 0;
    private List<GameObject> _playerModels = new List<GameObject>();

    [Header("Background Color Cycle")]
    [SerializeField] private GameObject _windowFrameContainer;

    [SerializeField] private Image _windowFrameContainerImage;
    [SerializeField] private float _windowColorCycleSpeed = 0.01f;

    private Color _dayWindowColor = new Color32(60, 205, 255, 255);
    private Color _nightWindowColor = new Color32(50, 60, 70, 255);
    private bool _isFadingImageToNight = true;
    private float _imageLerpProgress = 0f;

    [Header("Day/Night Cycle")]
    [SerializeField] private Light _directionalLight;
    [SerializeField] private float _lightCycleSpeed = 0.01f;

    private Color _dayColor = new Color32(250, 218, 130, 255);
    private Color _nightColor = new Color32(35, 115, 165, 255);
    private bool _isFadingToNight = true;
    private float _colorLerpProgress = 0f;

    [Header("Clouds")]
    [SerializeField] private RectTransform _cloudParent;
    [SerializeField] private GameObject[] _cloudPrefabs; // 10 options
    [SerializeField] private float _cloudSpeed = 50f;
    [SerializeField] private float _cloudSpawnRateMin = 3f;
    [SerializeField] private float _cloudSpawnRateMax = 10f;

    private List<RectTransform> _activeClouds = new List<RectTransform>();

    [Header("Sun/Moon")]
    [SerializeField] private RectTransform _celestialParent;
    [SerializeField] private GameObject _sunPrefab;
    [SerializeField] private GameObject _moonPrefab;
    [SerializeField] private float _sunSpeed = 200f;
    [SerializeField] private float _moonSpeed = 200f;
    [SerializeField] private float _celestialY = 260f;

    [Header("Death Effects")]
    [SerializeField] private float _deathCloudSpeedMultiplier = 2f;
    [SerializeField] private float _deathWindowColorSpeedMultiplier = 2f;
    [SerializeField] private float _deathCloudSpawnIntervalScale = 0.5f; // < 1 = faster spawns
    [SerializeField] private float _deathCelestialSpeedMultiplier = 2f;

    private bool _deathEffectsActive = false;
    private float _origCloudSpeed, _origWindowColorSpeed, _origSpawnMin, _origSpawnMax;
    private float _origSunSpeed, _origMoonSpeed;
    private float _origLightCycleSpeed;

    private Coroutine _cloudSpawnerRoutine;



    private void Start()
    {
        // Cache all player model children
        if (_playerContainer != null)
        {
            foreach (Transform child in _playerContainer.transform)
                _playerModels.Add(child.gameObject);

            // Ensure only the first one starts active
            for (int i = 0; i < _playerModels.Count; i++)
                _playerModels[i].SetActive(i == 0);
        }

        _cloudSpawnerRoutine = StartCoroutine(CloudSpawner());

        StartCoroutine(SunMoonLoop());
    }

    private void Update()
    {
        UpdateLights();
        UpdateWindowColor();
        UpdateClouds();
    }

    private void UpdateLights()
    {
        if (_directionalLight == null) return;

        _colorLerpProgress += Time.deltaTime * _lightCycleSpeed;

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
    }

    private void UpdateWindowColor()
    {
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

    private void UpdateClouds()
    {
        for (int i = _activeClouds.Count - 1; i >= 0; i--)
        {
            RectTransform rt = _activeClouds[i];
            if (rt == null)
            {
                _activeClouds.RemoveAt(i);
                continue;
            }

            Vector2 p = rt.anchoredPosition;
            p.x -= _cloudSpeed * Time.deltaTime;
            rt.anchoredPosition = p;

            if (p.x <= -800f)
            {
                Destroy(rt.gameObject);
                _activeClouds.RemoveAt(i);
            }
        }
    }

    private IEnumerator CloudSpawner()
    {
        while (true)
        {
            SpawnCloud();
            float waitTime = Random.Range(_cloudSpawnRateMin, _cloudSpawnRateMax);
            yield return new WaitForSeconds(waitTime);
        }
    }

    private void SpawnCloud()
    {
        if (_cloudPrefabs == null || _cloudPrefabs.Length == 0 || _cloudParent == null) return;

        int index = Random.Range(0, _cloudPrefabs.Length);
        GameObject go = Instantiate(_cloudPrefabs[index], _cloudParent);
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) return;

        float y = Random.Range(-280f, 280f);
        rt.anchoredPosition = new Vector2(800f, y);

        _activeClouds.Add(rt);
    }

    private IEnumerator SunMoonLoop()
    {
        while (true)
        {
            yield return SpawnAndRunCelestial(_sunPrefab);
            yield return SpawnAndRunCelestial(_moonPrefab);
        }
    }

    private IEnumerator SpawnAndRunCelestial(GameObject prefab)
    {
        if (prefab == null || _celestialParent == null) yield break;

        GameObject go = Instantiate(prefab, _celestialParent);
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) { Destroy(go); yield break; }

        rt.anchoredPosition = new Vector2(700f, _celestialY);

        while (rt != null && rt.anchoredPosition.x > -700f)
        {
            float currentSpeed = (prefab == _sunPrefab) ? _sunSpeed : _moonSpeed; // read live field
            Vector2 p = rt.anchoredPosition;
            p.x -= currentSpeed * Time.deltaTime;
            rt.anchoredPosition = p;
            yield return null;
        }

        if (rt != null) Destroy(rt.gameObject);
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

    public void TriggerDeathEffects()
    {
        if (_deathEffectsActive) return;

        FadeBlackScreen(2f);

        _deathEffectsActive = true;
        AdvancePlayerModel();

        _origCloudSpeed = _cloudSpeed;
        _origWindowColorSpeed = _windowColorCycleSpeed;
        _origSpawnMin = _cloudSpawnRateMin;
        _origSpawnMax = _cloudSpawnRateMax;
        _origSunSpeed = _sunSpeed;
        _origMoonSpeed = _moonSpeed;
        _origLightCycleSpeed = _lightCycleSpeed;

        _cloudSpeed *= _deathCloudSpeedMultiplier;
        _windowColorCycleSpeed *= _deathWindowColorSpeedMultiplier;
        _lightCycleSpeed *= _deathWindowColorSpeedMultiplier;
        _cloudSpawnRateMin *= _deathCloudSpawnIntervalScale;
        _cloudSpawnRateMax *= _deathCloudSpawnIntervalScale;
        _sunSpeed *= _deathCelestialSpeedMultiplier;
        _moonSpeed *= _deathCelestialSpeedMultiplier;

        if (_cloudSpawnerRoutine != null) StopCoroutine(_cloudSpawnerRoutine);
        _cloudSpawnerRoutine = StartCoroutine(CloudSpawner());

        StartCoroutine(RestoreAfter(3f));
    }

    private IEnumerator RestoreAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        _cloudSpeed = _origCloudSpeed;
        _windowColorCycleSpeed = _origWindowColorSpeed;
        _lightCycleSpeed = _origLightCycleSpeed;
        _cloudSpawnRateMin = _origSpawnMin;
        _cloudSpawnRateMax = _origSpawnMax;
        _sunSpeed = _origSunSpeed;
        _moonSpeed = _origMoonSpeed;

        _deathEffectsActive = false;

        if (_cloudSpawnerRoutine != null) StopCoroutine(_cloudSpawnerRoutine);
        _cloudSpawnerRoutine = StartCoroutine(CloudSpawner());
    }

    private void AdvancePlayerModel()
    {
        if (_playerModels.Count == 0) return;

        _playerModels[_currentPlayerIndex].SetActive(false);

        _currentPlayerIndex++;
        if (_currentPlayerIndex >= _playerModels.Count)
            _currentPlayerIndex = _playerModels.Count - 1; // stay on last if at end

        _playerModels[_currentPlayerIndex].SetActive(true);
    }


    private void FadeBlackScreen(float totalDuration)
    {
        if (_blackScreenOverlay == null) return;
        StartCoroutine(FadeBlackScreenRoutine(totalDuration));
    }

    private IEnumerator FadeBlackScreenRoutine(float totalDuration)
    {
        float holdTime = 0.5f;
        float fadeDuration = (totalDuration - holdTime) / 2f;
        Color overlayColor = _blackScreenOverlay.color;

        // start transparent
        overlayColor.a = 0f;
        _blackScreenOverlay.color = overlayColor;

        // fade to black
        for (float time = 0f; time < fadeDuration; time += Time.deltaTime)
        {
            overlayColor.a = Mathf.Lerp(0f, 1f, time / fadeDuration);
            _blackScreenOverlay.color = overlayColor;
            yield return null;
        }
        overlayColor.a = 1f;
        _blackScreenOverlay.color = overlayColor;

        // fully black pause
        AdvancePlayerModel();
        yield return new WaitForSeconds(holdTime);

        // fade back to transparent
        for (float time = 0f; time < fadeDuration; time += Time.deltaTime)
        {
            overlayColor.a = Mathf.Lerp(1f, 0f, time / fadeDuration);
            _blackScreenOverlay.color = overlayColor;
            yield return null;
        }
        overlayColor.a = 0f;
        _blackScreenOverlay.color = overlayColor;
    }
}
