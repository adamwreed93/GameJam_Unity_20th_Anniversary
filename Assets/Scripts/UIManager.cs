using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

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

    // ---------------------------------------------------------------------
    // Score
    // ---------------------------------------------------------------------
    private int _currentScore;
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TextMeshProUGUI _scoreTextShadow;

    // ---------------------------------------------------------------------
    // Player progression (character models under Player Container)
    // ---------------------------------------------------------------------
    [SerializeField] private GameObject _playerContainer;
    private readonly List<GameObject> _playerModels = new List<GameObject>();
    private int _currentPlayerIndex = 0;

    // ---------------------------------------------------------------------
    // Screen fade overlay
    // ---------------------------------------------------------------------
    [SerializeField] private Image _blackScreenOverlay;

    // ---------------------------------------------------------------------
    // Day/Night Sync (single source of truth)
    // Drives: directional light color, window image color, sun and moon motion
    // ---------------------------------------------------------------------
    [Header("Day/Night Sync")]
    [SerializeField] private float _dayNightSpeed = 0.05f;      // phase speed
    [SerializeField] private Light _directionalLight;
    [SerializeField] private Image _windowFrameContainerImage;

    // Colors
    [SerializeField] private Color _dayLightColor = new Color32(250, 218, 130, 255);
    [SerializeField] private Color _nightLightColor = new Color32(35, 115, 165, 255);
    [SerializeField] private Color _dayWindowColor = new Color32(60, 205, 255, 255);
    [SerializeField] private Color _nightWindowColor = new Color32(50, 60, 70, 255);

    // Celestials
    [SerializeField] private RectTransform _celestialParent;
    [SerializeField] private GameObject _sunPrefab;
    [SerializeField] private GameObject _moonPrefab;
    [SerializeField] private float _celestialY = 260f;
    [SerializeField] private float _celestialStartX = -700f;
    [SerializeField] private float _celestialEndX = 700f;

    private float _dayNightPhase = 0f; // 0..1 looping
    private RectTransform _sunInstance;
    private RectTransform _moonInstance;

    // ---------------------------------------------------------------------
    // Clouds
    // ---------------------------------------------------------------------
    [Header("Clouds")]
    [SerializeField] private RectTransform _cloudParent;
    [SerializeField] private GameObject[] _cloudPrefabs; // 10 options
    [SerializeField] private float _cloudSpeed = 50f;
    [SerializeField] private float _cloudSpawnRateMin = 3f;
    [SerializeField] private float _cloudSpawnRateMax = 10f;
    [SerializeField] private float _cloudSpawnX = 800f;
    [SerializeField] private float _cloudDespawnX = -800f;
    [SerializeField] private Vector2 _cloudSpawnYRange = new Vector2(-280f, 280f);

    private readonly List<RectTransform> _activeClouds = new List<RectTransform>();
    private Coroutine _cloudSpawnerRoutine;

    [SerializeField] private Color _dayCloudColor = new Color32(255, 255, 255, 255);
    [SerializeField] private Color _nightCloudColor = new Color32(125, 150, 220, 255);

    // ---------------------------------------------------------------------
    // Death Effects (temporary speed-up and fade)
    // ---------------------------------------------------------------------
    [Header("Death Effects")]
    [SerializeField] private float _deathCloudSpeedMultiplier = 2f;
    [SerializeField] private float _deathDayNightSpeedMultiplier = 2f;  // speeds up colors and celestials together
    [SerializeField] private float _deathCloudSpawnIntervalScale = 0.5f; // < 1 = faster spawns
    [SerializeField] private float _deathFadeDuration = 2f;
    [SerializeField] private float _deathBlackHoldTime = 0.5f;
    [SerializeField] private float _deathPhaseDuration = 3f;

    private bool _deathEffectsActive = false;

    // Originals to restore
    private float _origDayNightSpeed;
    private float _origCloudSpeed, _origSpawnMin, _origSpawnMax;

    // ---------------------------------------------------------------------
    // End Text (appears after final fade to black)
    // ---------------------------------------------------------------------
    [SerializeField] private TextMeshProUGUI _endText1;   // e.g. "The End"
    [SerializeField] private TextMeshProUGUI _endText2;   // e.g. "Thanks for Playing"
    [SerializeField] private float _textFadeDuration = 1.5f;
    [SerializeField] private float _delayBetweenTexts = 1f;
    [SerializeField] private float _delayBeforeFirstText = 0.5f;


    private void Start()
    {
        // Cache player models and set only the first active
        if (_playerContainer != null)
        {
            _playerModels.Clear();
            foreach (Transform child in _playerContainer.transform)
                _playerModels.Add(child.gameObject);

            for (int i = 0; i < _playerModels.Count; i++)
                _playerModels[i].SetActive(i == 0);
        }

        // Create persistent sun and moon (we toggle them during phase)
        if (_sunPrefab != null && _celestialParent != null)
            _sunInstance = Instantiate(_sunPrefab, _celestialParent).GetComponent<RectTransform>();
        if (_moonPrefab != null && _celestialParent != null)
            _moonInstance = Instantiate(_moonPrefab, _celestialParent).GetComponent<RectTransform>();

        if (_sunInstance != null) _sunInstance.gameObject.SetActive(true);
        if (_moonInstance != null) _moonInstance.gameObject.SetActive(false);

        // Start cloud spawning
        _cloudSpawnerRoutine = StartCoroutine(CloudSpawner());
    }

    private void Update()
    {
        UpdateDayNightSync();
        UpdateClouds();
    }

    // ---------------------------------------------------------------------
    // Public API
    // ---------------------------------------------------------------------
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

        // Save originals now (before we boost)
        _origDayNightSpeed = _dayNightSpeed;
        _origCloudSpeed = _cloudSpeed;
        _origSpawnMin = _cloudSpawnRateMin;
        _origSpawnMax = _cloudSpawnRateMax;

        // Start fade; boosts will be applied when fully black inside the fade routine
        FadeBlackScreen(_deathFadeDuration);
    }

    // ---------------------------------------------------------------------
    // Day/Night sync
    // ---------------------------------------------------------------------
    private void UpdateDayNightSync()
    {
        // Advance shared phase 0..1
        _dayNightPhase += Time.deltaTime * _dayNightSpeed;
        if (_dayNightPhase >= 1f) _dayNightPhase -= 1f;

        // Color blend with phase offset so: sun midpoint = brightest day, moon midpoint = darkest night
        float colorBlend = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * (_dayNightPhase - 0.25f));

        if (_directionalLight != null)
            _directionalLight.color = Color.Lerp(_dayLightColor, _nightLightColor, colorBlend);

        if (_windowFrameContainerImage != null)
            _windowFrameContainerImage.color = Color.Lerp(_dayWindowColor, _nightWindowColor, colorBlend);

        // Celestial motion driven by the same phase
        if (_sunInstance != null && _moonInstance != null)
        {
            if (_dayNightPhase < 0.5f)
            {
                float progress = Mathf.InverseLerp(0f, 0.5f, _dayNightPhase); // 0..1
                _sunInstance.gameObject.SetActive(true);
                _moonInstance.gameObject.SetActive(false);
                MoveCelestial(_sunInstance, progress);
            }
            else
            {
                float progress = Mathf.InverseLerp(0.5f, 1f, _dayNightPhase); // 0..1
                _sunInstance.gameObject.SetActive(false);
                _moonInstance.gameObject.SetActive(true);
                MoveCelestial(_moonInstance, progress);
            }
        }

        Color currentCloudColor = Color.Lerp(_dayCloudColor, _nightCloudColor, colorBlend);
        foreach (RectTransform cloud in _activeClouds)
        {
            if (cloud != null)
            {
                Image image = cloud.GetComponent<Image>();
                if (image != null)
                    image.color = currentCloudColor;
            }
        }
    }

    private void MoveCelestial(RectTransform target, float progress01)
    {
        float x = Mathf.Lerp(_celestialStartX, _celestialEndX, progress01);
        target.anchoredPosition = new Vector2(x, _celestialY);
    }

    // ---------------------------------------------------------------------
    // Clouds
    // ---------------------------------------------------------------------
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
        GameObject cloud = Instantiate(_cloudPrefabs[index], _cloudParent);
        RectTransform rect = cloud.GetComponent<RectTransform>();
        if (rect == null) { Destroy(cloud); return; }

        float spawnY = Random.Range(_cloudSpawnYRange.x, _cloudSpawnYRange.y);
        rect.anchoredPosition = new Vector2(_cloudSpawnX, spawnY);
        _activeClouds.Add(rect);
    }

    // ---------------------------------------------------------------------
    // Restore after death effects
    // ---------------------------------------------------------------------
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

    // ---------------------------------------------------------------------
    // Player model progression
    // ---------------------------------------------------------------------
    private void AdvancePlayerModel()
    {
        if (_playerModels.Count == 0) return;

        _playerModels[_currentPlayerIndex].SetActive(false);
        _currentPlayerIndex = Mathf.Min(_currentPlayerIndex + 1, _playerModels.Count - 1);
        _playerModels[_currentPlayerIndex].SetActive(true);
    }

    // ---------------------------------------------------------------------
    // Fade to black -> advance player -> fade back to transparent
    // ---------------------------------------------------------------------
    private void FadeBlackScreen(float totalDuration)
    {
        if (_blackScreenOverlay == null) return;
        StartCoroutine(FadeBlackScreenRoutine(totalDuration));
    }

    private void ApplyDeathBoostsAndStartRestore()
    {
        // Apply boosts (player will not see the change yet because screen is black)
        _dayNightSpeed *= _deathDayNightSpeedMultiplier;  // colors + sun/moon speed together
        _cloudSpeed *= _deathCloudSpeedMultiplier;
        _cloudSpawnRateMin *= _deathCloudSpawnIntervalScale;
        _cloudSpawnRateMax *= _deathCloudSpawnIntervalScale;

        // Restart spawner so new intervals apply immediately
        if (_cloudSpawnerRoutine != null) StopCoroutine(_cloudSpawnerRoutine);
        _cloudSpawnerRoutine = StartCoroutine(CloudSpawner());

        // Begin timer to return to normal
        StartCoroutine(RestoreAfter(_deathPhaseDuration));
    }

    private IEnumerator FadeBlackScreenRoutine(float totalDuration)
    {
        float holdTime = _deathBlackHoldTime;
        float fadeDuration = Mathf.Max(0.01f, (totalDuration - holdTime) * 0.5f);

        Color overlayColor = _blackScreenOverlay.color;
        overlayColor.a = 0f;
        _blackScreenOverlay.color = overlayColor;

        // Fade to black
        for (float elapsed = 0f; elapsed < fadeDuration; elapsed += Time.deltaTime)
        {
            overlayColor.a = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            _blackScreenOverlay.color = overlayColor;
            yield return null;
        }
        overlayColor.a = 1f;
        _blackScreenOverlay.color = overlayColor;

        // Determine if this death happens on the final character
        bool finalLife = IsOnFinalPlayerModel(); // FINAL-LIFE: check before advancing

        if (!finalLife)
        {
            // Swap model while black
            AdvancePlayerModel();

            // If you're cycling mini-games, do it invisibly here (optional)
            if (TVGameManager.Instance != null)
                TVGameManager.Instance.AdvanceToNextGame();

            // APPLY BOOSTS NOW (player can't see the speed-up yet)
            ApplyDeathBoostsAndStartRestore();

            // Hold at black, then fade back out as usual
            yield return new WaitForSeconds(holdTime);

            // Fade back to transparent
            for (float elapsed = 0f; elapsed < fadeDuration; elapsed += Time.deltaTime)
            {
                overlayColor.a = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                _blackScreenOverlay.color = overlayColor;
                yield return null;
            }
            overlayColor.a = 0f;
            _blackScreenOverlay.color = overlayColor;
        }
        else
        {
            // FINAL-LIFE: stay black permanently and stop transient activity

            // Stop cloud spawning to keep things quiet
            if (_cloudSpawnerRoutine != null) StopCoroutine(_cloudSpawnerRoutine);
            _cloudSpawnerRoutine = null;

            // Optionally freeze motion systems (safe to omit if you prefer)
            _dayNightSpeed = 0f;
            _cloudSpeed = 0f;

            // Keep death effects "active" so they can't be retriggered
            // (we intentionally do NOT call ApplyDeathBoostsAndStartRestore)
            // No fade-out, just end the coroutine here.

            // Kick off the two-text fade sequence
            StartCoroutine(ShowFinalTextSequence());
            yield break;
        }
    }


    private bool IsOnFinalPlayerModel()
    {
        return _playerModels.Count == 0 || _currentPlayerIndex >= _playerModels.Count - 1;
    }

    private IEnumerator ShowFinalTextSequence()
    {
        // Optional pause on full black
        yield return new WaitForSeconds(_delayBeforeFirstText);

        // --- First text ---
        if (_endText1 != null)
        {
            _endText1.gameObject.SetActive(true);
            yield return StartCoroutine(FadeTextIn(_endText1, _textFadeDuration));
        }

        // Wait before showing second
        yield return new WaitForSeconds(_delayBetweenTexts);

        // --- Second text ---
        if (_endText2 != null)
        {
            _endText2.gameObject.SetActive(true);
            yield return StartCoroutine(FadeTextIn(_endText2, _textFadeDuration));
        }
    }

    private IEnumerator FadeTextIn(TextMeshProUGUI text, float duration)
    {
        if (text == null) yield break;

        Color c = text.color;
        c.a = 0f;
        text.color = c;

        duration = Mathf.Max(0.01f, duration);
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            c.a = Mathf.Lerp(0f, 1f, t / duration);
            text.color = c;
            yield return null;
        }
        c.a = 1f;
        text.color = c;
    }

}
