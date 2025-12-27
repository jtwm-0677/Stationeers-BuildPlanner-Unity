using UnityEngine;
using System;

namespace StationeersBuildPlanner.Core
{
    public enum GameState
    {
        MapMode,
        BuildMode
    }

    /// <summary>
    /// Manages game state transitions between Map Mode and Build Mode.
    /// Controls which UI elements and systems are active.
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        [SerializeField] private GameState initialState = GameState.MapMode;

        public GameState CurrentState { get; private set; }

        public event Action<GameState> OnStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            SetState(initialState);
        }

        public void SetState(GameState newState)
        {
            if (CurrentState == newState) return;

            var oldState = CurrentState;
            CurrentState = newState;

            Debug.Log($"[GameStateManager] State changed: {oldState} -> {newState}");
            OnStateChanged?.Invoke(newState);
        }

        public void EnterMapMode()
        {
            SetState(GameState.MapMode);
        }

        public void EnterBuildMode()
        {
            SetState(GameState.BuildMode);
        }

        public void ToggleState()
        {
            SetState(CurrentState == GameState.MapMode ? GameState.BuildMode : GameState.MapMode);
        }
    }
}
