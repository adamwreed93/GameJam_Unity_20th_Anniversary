using UnityEngine;
using System.Collections.Generic;

public class TVGameManager : MonoBehaviour
{
    #region Singleton
    private static TVGameManager _instance;
    public static TVGameManager Instance
    {
        get
        {
            if (_instance == null) Debug.LogError("TVGameManager is null!");
            return _instance;
        }
    }
    private void Awake() { _instance = this; }
    #endregion

    [Header("Hierarchy")]
    [SerializeField] private Transform _tvScreenContainer;   // assign: TV Screen Container

    [Header("Startup")]
    [SerializeField] private int _startGameIndex = 0;        // which child to start with
    [SerializeField] private bool _activateOnStart = true;   // auto-activate a game at Start

    private readonly List<GameObject> _games = new List<GameObject>();
    private int _currentIndex = 0;

    private void Start()
    {
        if (_tvScreenContainer == null)
        {
            Debug.LogError("TVGameManager: _tvScreenContainer is not assigned.");
            return;
        }

        _games.Clear();
        foreach (Transform child in _tvScreenContainer)
        {
            // Treat each direct child as a game container
            _games.Add(child.gameObject);
        }

        if (_games.Count == 0)
        {
            Debug.LogWarning("TVGameManager: No games found under TV Screen Container.");
            return;
        }

        _currentIndex = Mathf.Clamp(_startGameIndex, 0, _games.Count - 1);

        if (_activateOnStart)
            ActivateGame(_currentIndex);
        else
            // If you’re manually activating one in the scene, keep track of whichever is active.
            SyncToCurrentlyActiveChild();
    }

    private void SyncToCurrentlyActiveChild()
    {
        RebuildGamesList();
        int activeIdx = _games.FindIndex(go => go != null && go.activeSelf);
        if (activeIdx >= 0) _currentIndex = activeIdx;
        else _currentIndex = Mathf.Clamp(_currentIndex, 0, Mathf.Max(0, _games.Count - 1));
    }


    private void ActivateGame(int index)
    {
        RebuildGamesList();
        if (_games.Count == 0) return;

        // Clamp/wrap safely
        if (index < 0) index = 0;
        if (index >= _games.Count) index = 0;

        // Turn ON only the target; OFF others (skip destroyed/null)
        for (int i = 0; i < _games.Count; i++)
        {
            var go = _games[i];
            if (go == null) continue;                       // <-- prevents MissingReference
            bool shouldBeActive = (i == index);
            if (go.activeSelf != shouldBeActive)
                go.SetActive(shouldBeActive);
        }

        _currentIndex = index;
    }



    public void AdvanceToNextGame()
    {
        RebuildGamesList();
        if (_games.Count == 0) return;

        // Find the currently active (handles manual changes & destroyed items)
        SyncToCurrentlyActiveChild();

        int next = _currentIndex + 1;
        if (next >= _games.Count) next = 0;

        ActivateGame(next);
    }


    private void RebuildGamesList()
    {
        _games.Clear();
        if (_tvScreenContainer == null) return;

        foreach (Transform child in _tvScreenContainer)
        {
            if (child != null && child.gameObject != null)
                _games.Add(child.gameObject);
        }
    }


    // Optional helpers if you ever need them:
    public void RestartFromTop() => ActivateGame(0);
    public GameObject GetCurrentGameGO() => (_games.Count == 0 ? null : _games[_currentIndex]);
    public int GetCurrentIndex() => _currentIndex;
    public int GetGameCount() => _games.Count;
}
