using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Mario_ObstacleSpawner : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private RectTransform _playfield;
    [SerializeField] private Mario_Player _player;
    [SerializeField] private RectTransform _obstacleContainer;

    [Header("Obstacles")]
    [SerializeField] private RectTransform[] _obstaclePrefabs;
    [SerializeField] private float _spawnX = 700f;
    [SerializeField] private float _groundY = -220f;
    [SerializeField] private Vector2 _spawnInterval = new Vector2(0.9f, 1.8f);
    [SerializeField] private Vector2 _speedRange = new Vector2(360f, 540f);
    [SerializeField] private int _maxConcurrent = 8;

    private readonly List<Mario_Obstacle> _active = new List<Mario_Obstacle>();
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
        if (_playfield == null || _player == null || _obstaclePrefabs == null || _obstaclePrefabs.Length == 0)
            yield break;

        while (true)
        {
            if (_active.Count < _maxConcurrent) SpawnOne();
            yield return new WaitForSeconds(Random.Range(_spawnInterval.x, _spawnInterval.y));
        }
    }

    private void SpawnOne()
    {
        RectTransform prefab = _obstaclePrefabs[Random.Range(0, _obstaclePrefabs.Length)];
        if (prefab == null) return;

        RectTransform parent = _obstacleContainer != null ? _obstacleContainer : _playfield;
        RectTransform r = Instantiate(prefab, parent);
        r.anchoredPosition = new Vector2(_spawnX, _groundY);
        r.localPosition = new Vector3(r.localPosition.x, r.localPosition.y, 0f);

        var comp = r.GetComponent<Mario_Obstacle>();
        if (comp == null) comp = r.gameObject.AddComponent<Mario_Obstacle>();
        float speed = Random.Range(_speedRange.x, _speedRange.y);

        // IMPORTANT: pass the player's RectTransform
        RectTransform playerRect = _player != null ? _player.GetComponent<RectTransform>() : null;
        comp.Init(_playfield, playerRect, speed, OnRemoved);

        _active.Add(comp);
    }

    private void OnRemoved(Mario_Obstacle o)
    {
        _active.Remove(o);
    }
}
