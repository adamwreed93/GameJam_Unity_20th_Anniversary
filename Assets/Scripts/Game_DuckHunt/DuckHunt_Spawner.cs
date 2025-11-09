using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DuckHunt_Spawner : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private RectTransform _playfield;
    [SerializeField] private RectTransform _targetContainer;

    [Header("Targets")]
    [SerializeField] private RectTransform[] _targetPrefabs;
    [SerializeField] private Vector2 _spawnInterval = new Vector2(0.6f, 1.2f);
    [SerializeField] private Vector2 _speedRange = new Vector2(420f, 680f);
    [SerializeField] private Vector2 _jitterAmpRange = new Vector2(80f, 160f);
    [SerializeField] private Vector2 _jitterFreqRange = new Vector2(2.0f, 4.0f);
    [SerializeField] private Vector2 _lifetimeRange = new Vector2(3.5f, 5.0f);
    [SerializeField] private int _maxConcurrent = 6;
    [SerializeField] private int _scorePerHit = 20;

    [Header("Fail")]
    [SerializeField] private int _missLimit = 3;

    private readonly List<DuckHunt_Target> _active = new List<DuckHunt_Target>();
    private Coroutine _loop;
    private int _misses;
    private bool _ended;

    private static readonly Vector3[] _pfCorners = new Vector3[4];

    private void OnEnable()
    {
        _misses = 0;
        _ended = false;
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
        if (_playfield == null || _targetPrefabs == null || _targetPrefabs.Length == 0) yield break;

        while (true)
        {
            if (_active.Count < _maxConcurrent) SpawnOne();
            yield return new WaitForSeconds(Random.Range(_spawnInterval.x, _spawnInterval.y));
        }
    }

    public void TryHitAt(Vector3 worldPoint)
    {
        if (_ended) return;

        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var t = _active[i];
            if (t == null) { _active.RemoveAt(i); continue; }
            if (t.ContainsWorldPoint(worldPoint))
            {
                _active.RemoveAt(i);
                if (UIManager.Instance != null) UIManager.Instance.UpdateScoreText(_scorePerHit);
                Destroy(t.gameObject);
                return;
            }
        }
    }

    private void SpawnOne()
    {
        RectTransform prefab = _targetPrefabs[Random.Range(0, _targetPrefabs.Length)];
        if (prefab == null) return;

        RectTransform parent = _targetContainer != null ? _targetContainer : _playfield;
        RectTransform r = Instantiate(prefab, parent);

        Vector2 spawnPos; Vector2 dir;
        ChooseEdgeSpawn(out spawnPos, out dir);

        r.anchoredPosition = spawnPos;
        r.localPosition = new Vector3(r.localPosition.x, r.localPosition.y, 0f);

        float spd = Random.Range(_speedRange.x, _speedRange.y);
        float amp = Random.Range(_jitterAmpRange.x, _jitterAmpRange.y);
        float freq = Random.Range(_jitterFreqRange.x, _jitterFreqRange.y);
        float life = Random.Range(_lifetimeRange.x, _lifetimeRange.y);

        DuckHunt_Target comp = r.GetComponent<DuckHunt_Target>();
        if (comp == null) comp = r.gameObject.AddComponent<DuckHunt_Target>();
        comp.Init(_playfield, spawnPos, dir, spd, life, amp, freq, OnTargetGone);
        _active.Add(comp);
    }

    private void OnTargetGone(DuckHunt_Target t, bool hit)
    {
        if (_ended) return;

        _active.Remove(t);

        if (!hit)
        {
            _misses++;
            if (_misses >= _missLimit)
            {
                _ended = true;
                if (_loop != null) StopCoroutine(_loop);
                _loop = null;

                // cleanup
                for (int i = _active.Count - 1; i >= 0; i--)
                    if (_active[i] != null) Destroy(_active[i].gameObject);
                _active.Clear();

                if (UIManager.Instance != null)
                    UIManager.Instance.TriggerDeathEffects();

                return;
            }
        }
    }

    private void ChooseEdgeSpawn(out Vector2 spawn, out Vector2 dir)
    {
        Vector2 half = _playfield.rect.size * 0.5f;
        float pad = 80f;
        int side = Random.Range(0, 4);

        switch (side)
        {
            case 0: spawn = new Vector2(-half.x + -pad, Random.Range(-half.y * 0.8f, half.y * 0.8f)); dir = Vector2.right; break;
            case 1: spawn = new Vector2(half.x + pad, Random.Range(-half.y * 0.8f, half.y * 0.8f)); dir = Vector2.left; break;
            case 2: spawn = new Vector2(Random.Range(-half.x * 0.8f, half.x * 0.8f), half.y + pad); dir = Vector2.down; break;
            default: spawn = new Vector2(Random.Range(-half.x * 0.8f, half.x * 0.8f), -half.y - pad); dir = Vector2.up; break;
        }

        float ang = Random.Range(-20f, 20f) * Mathf.Deg2Rad;
        Vector2 rot = new Vector2(dir.x * Mathf.Cos(ang) - dir.y * Mathf.Sin(ang),
                                  dir.x * Mathf.Sin(ang) + dir.y * Mathf.Cos(ang));
        dir = rot.normalized;
    }

    private static Rect WorldRect(RectTransform rt, Vector3[] buf)
    {
        rt.GetWorldCorners(buf);
        Vector2 bl = new Vector2(buf[0].x, buf[0].y);
        Vector2 tr = new Vector2(buf[2].x, buf[2].y);
        return new Rect(bl, tr - bl);
    }
}
