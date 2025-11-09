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
        int activeIdx = _games.FindIndex(go => go.activeSelf);
        if (activeIdx >= 0) _currentIndex = activeIdx;
    }

    private void ActivateGame(int index)
    {
        for (int i = 0; i < _games.Count; i++)
            _games[i].SetActive(i == index);

        _currentIndex = index;

        // After activating, call ResetGame() on any implementers in this game subtree.
        var go = _games[_currentIndex];
        if (go != null)
        {
            var resetters = go.GetComponentsInChildren<IResettableGame>(true);
            for (int r = 0; r < resetters.Length; r++)
                resetters[r].ResetGame();
        }
    }


    public void AdvanceToNextGame()
    {
        if (_games.Count == 0) return;

        int next = _currentIndex + 1;
        if (next >= _games.Count) next = 0;

        ActivateGame(next);
    }

    // Optional helpers if you ever need them:
    public void RestartFromTop() => ActivateGame(0);
    public GameObject GetCurrentGameGO() => (_games.Count == 0 ? null : _games[_currentIndex]);
    public int GetCurrentIndex() => _currentIndex;
    public int GetGameCount() => _games.Count;
}
