using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Asteroids_Spawner : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private RectTransform _playfield;
    [SerializeField] private RectTransform _ship;

    [Header("Asteroid Prefabs")]
    [SerializeField] private RectTransform[] _asteroidPrefabs; 

    [Header("Spawning")]
    [SerializeField] private float _spawnIntervalMin = 0.8f;
    [SerializeField] private float _spawnIntervalMax = 1.8f;
    [SerializeField] private float _spawnMargin = 120f;
    [SerializeField] private int _maxConcurrent = 30;

    [Header("Asteroid Motion")]
    [SerializeField] private float _speedMin = 120f;
    [SerializeField] private float _speedMax = 260f;
    [SerializeField] private bool _continuousHoming = true;

    private readonly List<RectTransform> _active = new List<RectTransform>();
    private Coroutine _loop;

    private void OnEnable()
    {
        _loop = StartCoroutine(SpawnLoop());
    }

    private void OnDisable()
    {
        if (_loop != null) StopCoroutine(_loop);
        _loop = null;

        for (int i = _active.Count - 1; i >= 0; i--)
            if (_active[i] != null) Destroy(_active[i].gameObject);
        _active.Clear();
    }

    private IEnumerator SpawnLoop()
    {
        if (_playfield == null || _ship == null || _asteroidPrefabs == null || _asteroidPrefabs.Length == 0)
            yield break;

        while (true)
        {
            if (_active.Count < _maxConcurrent)
                SpawnOne();

            yield return new WaitForSeconds(Random.Range(_spawnIntervalMin, _spawnIntervalMax));
        }
    }

    private void SpawnOne()
    {
        // Pick a random prefab from your list
        RectTransform prefab = _asteroidPrefabs[Random.Range(0, _asteroidPrefabs.Length)];
        if (prefab == null) return;

        Vector2 spawnPos = GetOutsideSpawnPos();
        RectTransform a = Instantiate(prefab, _playfield);
        a.anchoredPosition = spawnPos;
        a.localPosition = new Vector3(a.localPosition.x, a.localPosition.y, 0f);

        var comp = a.GetComponent<Asteroids_Asteroid>();
        if (comp == null) comp = a.gameObject.AddComponent<Asteroids_Asteroid>();

        float speed = Random.Range(_speedMin, _speedMax);
        comp.Init(_playfield, _ship, speed, _continuousHoming, OnAsteroidDestroyed);

        _active.Add(a);
    }

    private Vector2 GetOutsideSpawnPos()
    {
        Vector2 half = _playfield.rect.size * 0.5f;
        float m = _spawnMargin;
        int side = Random.Range(0, 4);
        float x = 0f, y = 0f;

        switch (side)
        {
            case 0: x = -half.x - m; y = Random.Range(-half.y, half.y); break; // left
            case 1: x = half.x + m; y = Random.Range(-half.y, half.y); break; // right
            case 2: x = Random.Range(-half.x, half.x); y = half.y + m; break; // top
            case 3: x = Random.Range(-half.x, half.x); y = -half.y - m; break; // bottom
        }
        return new Vector2(x, y);
    }

    private void OnAsteroidDestroyed(RectTransform a)
    {
        _active.Remove(a);
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateScoreText(25);
    }
}
