using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    #region Singleton
    private static UIManager _instance;
    public static UIManager Instance
    {
        get
        {
            if (_instance == null) Debug.LogError("UIManager is null!");
            return _instance;
        }
    }
    private void Awake() { _instance = this; }
    #endregion

    [SerializeField] private float _sceneIntroFadeDuration = 1.5f;
    [SerializeField] private bool _fadeInOnSceneStart = true;

    [Header("Score")]
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TextMeshProUGUI _scoreTextShadow;
    private int _currentScore;

    [Header("Player Models")]
    [SerializeField] private GameObject _playerContainer;
    private readonly List<GameObject> _playerModels = new List<GameObject>();
    private int _currentPlayerIndex = 0;

    [Header("Screen Fade")]
    [SerializeField] private Image _blackScreenOverlay;

    [Header("Day/Night Sync")]
    [SerializeField] private float _dayNightSpeed = 0.05f;
    [SerializeField] private Light _directionalLight;
    [SerializeField] private Image _windowFrameContainerImage;
    [SerializeField] private Color _dayLightColor = new Color32(250, 218, 130, 255);
    [SerializeField] private Color _nightLightColor = new Color32(35, 115, 165, 255);
    [SerializeField] private Color _dayWindowColor = new Color32(60, 205, 255, 255);
    [SerializeField] private Color _nightWindowColor = new Color32(50, 60, 70, 255);
    [SerializeField] private RectTransform _celestialParent;
    [SerializeField] private GameObject _sunPrefab;
    [SerializeField] private GameObject _moonPrefab;
    [SerializeField] private float _celestialY = 260f;
    [SerializeField] private float _celestialStartX = -700f;
    [SerializeField] private float _celestialEndX = 700f;

    private float _dayNightPhase;
    private RectTransform _sunInstance;
    private RectTransform _moonInstance;

    [Header("Clouds")]
    [SerializeField] private RectTransform _cloudParent;
    [SerializeField] private GameObject[] _cloudPrefabs;
    [SerializeField] private float _cloudSpeed = 50f;
    [SerializeField] private float _cloudSpawnRateMin = 3f;
    [SerializeField] private float _cloudSpawnRateMax = 10f;
    [SerializeField] private float _cloudSpawnX = 800f;
    [SerializeField] private float _cloudDespawnX = -800f;
    [SerializeField] private Vector2 _cloudSpawnYRange = new Vector2(-280f, 280f);
    [SerializeField] private Color _dayCloudColor = new Color32(255, 255, 255, 255);
    [SerializeField] private Color _nightCloudColor = new Color32(125, 150, 220, 255);

    private readonly List<RectTransform> _activeClouds = new List<RectTransform>();
    private Coroutine _cloudSpawnerRoutine;

    [Header("Death Effects")]
    [SerializeField] private float _deathCloudSpeedMultiplier = 2f;
    [SerializeField] private float _deathDayNightSpeedMultiplier = 2f;
    [SerializeField] private float _deathCloudSpawnIntervalScale = 0.5f;
    [SerializeField] private float _deathFadeDuration = 2f;
    [SerializeField] private float _deathBlackHoldTime = 0.5f;
    [SerializeField] private float _deathPhaseDuration = 3f;

    private bool _deathEffectsActive;
    private float _origDayNightSpeed;
    private float _origCloudSpeed, _origSpawnMin, _origSpawnMax;

    [Header("End Texts")]
    [SerializeField] private TextMeshProUGUI _endText1;
    [SerializeField] private TextMeshProUGUI _endText2;
    [SerializeField] private TextMeshProUGUI _endText3;
    [SerializeField] private float _textFadeDuration = 1.5f;
    [SerializeField] private float _delayBetweenTexts = 1f;
    [SerializeField] private float _delayBeforeFirstText = 0.5f;
    [SerializeField] private TextMeshProUGUI _restartText;
    [SerializeField] private float _restartTextDelayAfterSecond = 1.5f;

    private bool _isGameOver;

    private void Start()
    {
        if (_playerContainer != null)
        {
            _playerModels.Clear();
            foreach (Transform child in _playerContainer.transform)
                _playerModels.Add(child.gameObject);
            for (int i = 0; i < _playerModels.Count; i++)
                _playerModels[i].SetActive(i == 0);
        }

        if (_sunPrefab != null && _celestialParent != null)
            _sunInstance = Instantiate(_sunPrefab, _celestialParent).GetComponent<RectTransform>();
        if (_moonPrefab != null && _celestialParent != null)
            _moonInstance = Instantiate(_moonPrefab, _celestialParent).GetComponent<RectTransform>();
        if (_sunInstance != null) _sunInstance.gameObject.SetActive(true);
        if (_moonInstance != null) _moonInstance.gameObject.SetActive(false);

        _cloudSpawnerRoutine = StartCoroutine(CloudSpawner());

        if (_fadeInOnSceneStart && _blackScreenOverlay != null)
        {
            StartCoroutine(FadeFromBlackRoutine(_sceneIntroFadeDuration));
        }
    }

    private void Update()
    {
        if (_isGameOver && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);

        UpdateDayNightSync();
        UpdateClouds();
    }

    public void UpdateScoreText(int score)
    {
        _currentScore += score;
        if (_scoreText != null) _scoreText.text = _currentScore.ToString();
        if (_scoreTextShadow != null) _scoreTextShadow.text = _currentScore.ToString();
    }

    public void TriggerDeathEffects()
    {
        if (_deathEffectsActive) return;
        _deathEffectsActive = true;

        ClearAllBulletsByName();

        _origDayNightSpeed = _dayNightSpeed;
        _origCloudSpeed = _cloudSpeed;
        _origSpawnMin = _cloudSpawnRateMin;
        _origSpawnMax = _cloudSpawnRateMax;

        FadeBlackScreen(_deathFadeDuration);
    }

    private void UpdateDayNightSync()
    {
        _dayNightPhase = Mathf.Repeat(_dayNightPhase + Time.deltaTime * Mathf.Max(0f, _dayNightSpeed), 1f);
        float colorBlend = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * (_dayNightPhase - 0.25f));

        if (_directionalLight != null)
            _directionalLight.color = Color.Lerp(_dayLightColor, _nightLightColor, colorBlend);
        if (_windowFrameContainerImage != null)
            _windowFrameContainerImage.color = Color.Lerp(_dayWindowColor, _nightWindowColor, colorBlend);

        if (_sunInstance != null && _moonInstance != null)
        {
            if (_dayNightPhase < 0.5f)
            {
                float progress = Mathf.InverseLerp(0f, 0.5f, _dayNightPhase);
                _sunInstance.gameObject.SetActive(true);
                _moonInstance.gameObject.SetActive(false);
                MoveCelestial(_sunInstance, progress);
            }
            else
            {
                float progress = Mathf.InverseLerp(0.5f, 1f, _dayNightPhase);
                _sunInstance.gameObject.SetActive(false);
                _moonInstance.gameObject.SetActive(true);
                MoveCelestial(_moonInstance, progress);
            }
        }

        Color cloudColor = Color.Lerp(_dayCloudColor, _nightCloudColor, colorBlend);
        foreach (RectTransform cloud in _activeClouds)
        {
            if (cloud == null) continue;
            Image img = cloud.GetComponent<Image>();
            if (img != null) img.color = cloudColor;
        }
    }

    private void MoveCelestial(RectTransform target, float progress01)
    {
        float x = Mathf.Lerp(_celestialStartX, _celestialEndX, progress01);
        target.anchoredPosition = new Vector2(x, _celestialY);
    }

    private void UpdateClouds()
    {
        for (int i = _activeClouds.Count - 1; i >= 0; i--)
        {
            RectTransform rect = _activeClouds[i];
            if (rect == null) { _activeClouds.RemoveAt(i); continue; }

            Vector2 pos = rect.anchoredPosition;
            pos.x -= _cloudSpeed * Time.deltaTime;
            rect.anchoredPosition = pos;

            if (pos.x <= _cloudDespawnX)
            {
                Destroy(rect.gameObject);
                _activeClouds.RemoveAt(i);
            }
        }
    }

    private IEnumerator CloudSpawner()
    {
        if (_cloudParent == null || _cloudPrefabs == null || _cloudPrefabs.Length == 0)
            yield break;

        while (true)
        {
            SpawnCloud();
            float wait = Random.Range(_cloudSpawnRateMin, _cloudSpawnRateMax);
            yield return new WaitForSeconds(wait);
        }
    }

    private void SpawnCloud()
    {
        if (_cloudParent == null || _cloudPrefabs.Length == 0) return;

        int index = Random.Range(0, _cloudPrefabs.Length);
        GameObject cloud = Instantiate(_cloudPrefabs[index], _cloudParent);
        RectTransform rect = cloud.GetComponent<RectTransform>();
        if (rect == null) { Destroy(cloud); return; }

        float y = Random.Range(_cloudSpawnYRange.x, _cloudSpawnYRange.y);
        rect.anchoredPosition = new Vector2(_cloudSpawnX, y);
        rect.localPosition = new Vector3(rect.localPosition.x, rect.localPosition.y, 0f);
        _activeClouds.Add(rect);
    }

    private IEnumerator RestoreAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        _dayNightSpeed = _origDayNightSpeed;
        _cloudSpeed = _origCloudSpeed;
        _cloudSpawnRateMin = _origSpawnMin;
        _cloudSpawnRateMax = _origSpawnMax;
        _deathEffectsActive = false;
        if (_cloudSpawnerRoutine != null) StopCoroutine(_cloudSpawnerRoutine);
        _cloudSpawnerRoutine = StartCoroutine(CloudSpawner());
    }

    private void AdvancePlayerModel()
    {
        if (_playerModels.Count == 0) return;
        _playerModels[_currentPlayerIndex].SetActive(false);
        _currentPlayerIndex = Mathf.Min(_currentPlayerIndex + 1, _playerModels.Count - 1);
        _playerModels[_currentPlayerIndex].SetActive(true);
    }

    private void FadeBlackScreen(float duration)
    {
        if (_blackScreenOverlay == null) return;
        StartCoroutine(FadeBlackScreenRoutine(duration));
    }

    private void ApplyDeathBoostsAndStartRestore()
    {
        _dayNightSpeed *= _deathDayNightSpeedMultiplier;
        _cloudSpeed *= _deathCloudSpeedMultiplier;
        _cloudSpawnRateMin *= _deathCloudSpawnIntervalScale;
        _cloudSpawnRateMax *= _deathCloudSpawnIntervalScale;
        if (_cloudSpawnerRoutine != null) StopCoroutine(_cloudSpawnerRoutine);
        _cloudSpawnerRoutine = StartCoroutine(CloudSpawner());
        StartCoroutine(RestoreAfter(_deathPhaseDuration));
    }

    private IEnumerator FadeBlackScreenRoutine(float duration)
    {
        float hold = _deathBlackHoldTime;
        float fade = Mathf.Max(0.01f, (duration - hold) * 0.5f);

        Color c = _blackScreenOverlay.color;
        c.a = 0f;
        _blackScreenOverlay.color = c;

        for (float t = 0; t < fade; t += Time.deltaTime)
        {
            c.a = Mathf.Lerp(0f, 1f, t / fade);
            _blackScreenOverlay.color = c;
            yield return null;
        }
        c.a = 1f;
        _blackScreenOverlay.color = c;

        bool final = IsOnFinalPlayerModel();

        if (!final)
        {
            AdvancePlayerModel();
            if (TVGameManager.Instance != null)
                TVGameManager.Instance.AdvanceToNextGame();
            ApplyDeathBoostsAndStartRestore();
            yield return new WaitForSeconds(hold);
            for (float t = 0; t < fade; t += Time.deltaTime)
            {
                c.a = Mathf.Lerp(1f, 0f, t / fade);
                _blackScreenOverlay.color = c;
                yield return null;
            }
            c.a = 0f;
            _blackScreenOverlay.color = c;
        }
        else
        {
            if (_cloudSpawnerRoutine != null) StopCoroutine(_cloudSpawnerRoutine);
            _cloudSpawnerRoutine = null;
            _dayNightSpeed = 0f;
            _cloudSpeed = 0f;
            StartCoroutine(ShowFinalTextSequence());
        }
    }

    private bool IsOnFinalPlayerModel()
    {
        return _playerModels.Count == 0 || _currentPlayerIndex >= _playerModels.Count - 1;
    }

    private IEnumerator ShowFinalTextSequence()
    {
        yield return new WaitForSeconds(_delayBeforeFirstText);

        if (_endText1 != null)
        {
            _endText1.gameObject.SetActive(true);
            yield return StartCoroutine(FadeTextIn(_endText1, _textFadeDuration));
        }

        yield return new WaitForSeconds(_delayBetweenTexts);

        if (_endText2 != null)
        {
            _endText2.gameObject.SetActive(true);
            yield return StartCoroutine(FadeTextIn(_endText2, _textFadeDuration));
        }

        yield return new WaitForSeconds(_delayBetweenTexts);

        if (_endText3 != null)
        {
            _endText3.gameObject.SetActive(true);
            yield return StartCoroutine(FadeTextIn(_endText3, _textFadeDuration));
        }

        yield return new WaitForSeconds(_restartTextDelayAfterSecond);

        if (_restartText != null)
        {
            _restartText.gameObject.SetActive(true);
            _isGameOver = true;
            StartCoroutine(FadeTextLoop(_restartText));
        }
    }

    private IEnumerator FadeTextIn(TextMeshProUGUI text, float duration)
    {
        if (text == null) yield break;
        Color c = text.color;
        c.a = 0f;
        text.color = c;
        duration = Mathf.Max(0.01f, duration);
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            c.a = Mathf.Lerp(0f, 1f, t / duration);
            text.color = c;
            yield return null;
        }
        c.a = 1f;
        text.color = c;
    }

    private IEnumerator FadeTextLoop(TextMeshProUGUI text)
    {
        if (text == null) yield break;
        Color c = text.color;
        float min = 0.1f;
        float max = 1f;
        float dur = 1f;
        c.a = min;
        text.color = c;

        while (true)
        {
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                c.a = Mathf.Lerp(min, max, t / dur);
                text.color = c;
                yield return null;
            }
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                c.a = Mathf.Lerp(max, min, t / dur);
                text.color = c;
                yield return null;
            }
        }
    }

    private void ClearAllBulletsByName()
    {
        Transform bulletContainer = GameObject.Find("Bullet Container")?.transform;
        if (bulletContainer == null) return;
        for (int i = bulletContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = bulletContainer.GetChild(i);
            if (child != null) Destroy(child.gameObject);
        }
    }

    private IEnumerator FadeFromBlackRoutine(float duration)
    {
        Color c = _blackScreenOverlay.color;
        c.a = 1f;
        _blackScreenOverlay.color = c;

        duration = Mathf.Max(0.01f, duration);
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            c.a = Mathf.Lerp(1f, 0f, t / duration);
            _blackScreenOverlay.color = c;
            yield return null;
        }

        c.a = 0f;
        _blackScreenOverlay.color = c;
    }

}
