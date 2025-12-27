using UnityEngine;
using StationeersBuildPlanner.Core;

namespace StationeersBuildPlanner.Grid
{
    /// <summary>
    /// Hides the ground plane when viewing negative floors (below terrain level).
    /// Attach this to the Ground GameObject.
    /// </summary>
    public class GroundVisibility : MonoBehaviour
    {
        private Renderer groundRenderer;

        private void Start()
        {
            groundRenderer = GetComponent<Renderer>();

            if (groundRenderer == null)
            {
                Debug.LogError("[GroundVisibility] No Renderer found on Ground object!");
                enabled = false;
                return;
            }

            // Subscribe to floor changes
            if (FloorManager.Instance != null)
            {
                FloorManager.Instance.OnFloorChanged += OnFloorChanged;
                // Apply initial state
                UpdateVisibility(FloorManager.Instance.CurrentFloor);
            }

            // Subscribe to game state changes
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged += OnGameStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (FloorManager.Instance != null)
            {
                FloorManager.Instance.OnFloorChanged -= OnFloorChanged;
            }

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged -= OnGameStateChanged;
            }
        }

        private void OnFloorChanged(int floor)
        {
            UpdateVisibility(floor);
        }

        private void OnGameStateChanged(GameState state)
        {
            // Always show ground when not in build mode
            if (state != GameState.BuildMode)
            {
                groundRenderer.enabled = true;
            }
            else
            {
                // Apply floor-based visibility when entering build mode
                UpdateVisibility(FloorManager.Instance?.CurrentFloor ?? 0);
            }
        }

        private void UpdateVisibility(int floor)
        {
            // Only apply visibility logic in build mode
            if (GameStateManager.Instance?.CurrentState != GameState.BuildMode)
                return;

            // Hide ground when viewing negative floors (below terrain)
            groundRenderer.enabled = floor >= 0;
        }
    }
}
