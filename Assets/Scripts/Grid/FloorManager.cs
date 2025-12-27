using UnityEngine;
using System;
using System.Collections.Generic;

namespace StationeersBuildPlanner.Grid
{
    /// <summary>
    /// Manages vertical floor layers for Sims-style floor navigation.
    /// </summary>
    public class FloorManager : MonoBehaviour
    {
        public static FloorManager Instance { get; private set; }

        [Header("Floor Settings")]
        [SerializeField] private int minFloor = -5;
        [SerializeField] private int maxFloor = 10;
        [SerializeField] private int currentFloor = 0;

        // Floor visibility state
        private Dictionary<int, bool> floorVisibility = new Dictionary<int, bool>();

        // Events
        public event Action<int> OnFloorChanged;
        public event Action<int, bool> OnFloorVisibilityChanged;

        public int CurrentFloor => currentFloor;
        public int MinFloor => minFloor;
        public int MaxFloor => maxFloor;

        /// <summary>
        /// World Y position of current floor.
        /// </summary>
        public float CurrentFloorY => GridMath.GetFloorY(currentFloor);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Initialize all floors as visible
            for (int i = minFloor; i <= maxFloor; i++)
            {
                floorVisibility[i] = true;
            }
        }

        /// <summary>
        /// Move to a specific floor.
        /// </summary>
        public void SetFloor(int floor)
        {
            floor = Mathf.Clamp(floor, minFloor, maxFloor);
            if (floor != currentFloor)
            {
                currentFloor = floor;
                OnFloorChanged?.Invoke(currentFloor);
                Debug.Log($"[FloorManager] Changed to floor {currentFloor} (Y={CurrentFloorY})");
            }
        }

        /// <summary>
        /// Move up one floor.
        /// </summary>
        public void FloorUp()
        {
            SetFloor(currentFloor + 1);
        }

        /// <summary>
        /// Move down one floor.
        /// </summary>
        public void FloorDown()
        {
            SetFloor(currentFloor - 1);
        }

        /// <summary>
        /// Set visibility for a specific floor.
        /// </summary>
        public void SetFloorVisibility(int floor, bool visible)
        {
            if (floor < minFloor || floor > maxFloor) return;

            if (floorVisibility[floor] != visible)
            {
                floorVisibility[floor] = visible;
                OnFloorVisibilityChanged?.Invoke(floor, visible);
            }
        }

        /// <summary>
        /// Check if a floor is visible.
        /// </summary>
        public bool IsFloorVisible(int floor)
        {
            return floorVisibility.TryGetValue(floor, out bool visible) && visible;
        }

        /// <summary>
        /// Get floor index from world Y position.
        /// </summary>
        public int GetFloorFromY(float y)
        {
            return GridMath.GetFloorIndex(y);
        }
    }
}
