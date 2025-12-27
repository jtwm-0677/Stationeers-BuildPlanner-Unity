using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using StationeersBuildPlanner.Core;
using StationeersBuildPlanner.Grid;

namespace StationeersBuildPlanner.UI
{
    /// <summary>
    /// Controls the Build Mode UI panel and grid settings.
    /// </summary>
    public class BuildModeController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private UIDocument uiDocument;

        [Header("Grid References")]
        [SerializeField] private GridVisualizer gridVisualizer;
        [SerializeField] private SnapIndicator snapIndicator;

        // UI Elements
        private VisualElement root;
        private Button floorUpButton;
        private Button floorDownButton;
        private Label floorLabel;
        private Toggle toggleMainGrid;
        private Toggle toggleSmallGrid;
        private Toggle toggleSnapIndicator;
        private RadioButtonGroup snapModeGroup;
        private Label coordsWorld;
        private Label coordsGrid;
        private Label coordsFloor;

        private void Awake()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();
        }

        private void Start()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged += OnGameStateChanged;
                OnGameStateChanged(GameStateManager.Instance.CurrentState);
            }

            if (FloorManager.Instance != null)
            {
                FloorManager.Instance.OnFloorChanged += OnFloorChanged;
            }
        }

        private void OnEnable()
        {
            InitializeUI();
        }

        private void OnDisable()
        {
            UnregisterCallbacks();
        }

        private void OnDestroy()
        {
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnStateChanged -= OnGameStateChanged;

            if (FloorManager.Instance != null)
                FloorManager.Instance.OnFloorChanged -= OnFloorChanged;
        }

        private void Update()
        {
            if (GameStateManager.Instance?.CurrentState != GameState.BuildMode)
                return;

            HandleInput();
            UpdateCoordinatesDisplay();
        }

        private void InitializeUI()
        {
            root = uiDocument.rootVisualElement;

            // Floor navigation
            floorUpButton = root.Q<Button>("floor-up");
            floorDownButton = root.Q<Button>("floor-down");
            floorLabel = root.Q<Label>("floor-label");

            // Grid toggles
            toggleMainGrid = root.Q<Toggle>("toggle-main-grid");
            toggleSmallGrid = root.Q<Toggle>("toggle-small-grid");
            toggleSnapIndicator = root.Q<Toggle>("toggle-snap-indicator");
            snapModeGroup = root.Q<RadioButtonGroup>("snap-mode");

            // Coordinates
            coordsWorld = root.Q<Label>("coords-world");
            coordsGrid = root.Q<Label>("coords-grid");
            coordsFloor = root.Q<Label>("coords-floor");

            RegisterCallbacks();
            SyncUIState();
        }

        private void RegisterCallbacks()
        {
            if (floorUpButton != null)
                floorUpButton.clicked += OnFloorUpClicked;

            if (floorDownButton != null)
                floorDownButton.clicked += OnFloorDownClicked;

            if (toggleMainGrid != null)
                toggleMainGrid.RegisterValueChangedCallback(OnMainGridToggled);

            if (toggleSmallGrid != null)
                toggleSmallGrid.RegisterValueChangedCallback(OnSmallGridToggled);

            if (toggleSnapIndicator != null)
                toggleSnapIndicator.RegisterValueChangedCallback(OnSnapIndicatorToggled);

            if (snapModeGroup != null)
                snapModeGroup.RegisterValueChangedCallback(OnSnapModeChanged);
        }

        private void UnregisterCallbacks()
        {
            if (floorUpButton != null)
                floorUpButton.clicked -= OnFloorUpClicked;

            if (floorDownButton != null)
                floorDownButton.clicked -= OnFloorDownClicked;

            if (toggleMainGrid != null)
                toggleMainGrid.UnregisterValueChangedCallback(OnMainGridToggled);

            if (toggleSmallGrid != null)
                toggleSmallGrid.UnregisterValueChangedCallback(OnSmallGridToggled);

            if (toggleSnapIndicator != null)
                toggleSnapIndicator.UnregisterValueChangedCallback(OnSnapIndicatorToggled);

            if (snapModeGroup != null)
                snapModeGroup.UnregisterValueChangedCallback(OnSnapModeChanged);
        }

        private void SyncUIState()
        {
            if (gridVisualizer != null)
            {
                if (toggleMainGrid != null)
                    toggleMainGrid.value = gridVisualizer.ShowMainGrid;
                if (toggleSmallGrid != null)
                    toggleSmallGrid.value = gridVisualizer.ShowSmallGrid;
            }

            if (snapIndicator != null && toggleSnapIndicator != null)
                toggleSnapIndicator.value = snapIndicator.ShowIndicator;

            UpdateFloorLabel();
        }

        private void HandleInput()
        {
            // Page Up / Page Down for floor navigation
            if (Keyboard.current.pageUpKey.wasPressedThisFrame)
            {
                FloorManager.Instance?.FloorUp();
            }
            else if (Keyboard.current.pageDownKey.wasPressedThisFrame)
            {
                FloorManager.Instance?.FloorDown();
            }

            // G toggles main grid
            if (Keyboard.current.gKey.wasPressedThisFrame)
            {
                if (gridVisualizer != null)
                {
                    gridVisualizer.ShowMainGrid = !gridVisualizer.ShowMainGrid;
                    if (toggleMainGrid != null)
                        toggleMainGrid.value = gridVisualizer.ShowMainGrid;
                }
            }

            // H toggles small grid
            if (Keyboard.current.hKey.wasPressedThisFrame)
            {
                if (gridVisualizer != null)
                {
                    gridVisualizer.ShowSmallGrid = !gridVisualizer.ShowSmallGrid;
                    if (toggleSmallGrid != null)
                        toggleSmallGrid.value = gridVisualizer.ShowSmallGrid;
                }
            }
        }

        private void UpdateCoordinatesDisplay()
        {
            if (snapIndicator == null) return;

            Vector3 worldPos = snapIndicator.CurrentSnapPosition;
            Vector3Int gridPos = GridMath.ToGrid3(worldPos);
            int floor = GridMath.GetFloorIndex(worldPos.y);

            if (coordsWorld != null)
                coordsWorld.text = $"World: ({worldPos.x:F1}, {worldPos.y:F1}, {worldPos.z:F1})";

            if (coordsGrid != null)
                coordsGrid.text = $"Grid3: ({gridPos.x}, {gridPos.y}, {gridPos.z})";

            if (coordsFloor != null)
                coordsFloor.text = $"Floor: {floor}";
        }

        private void UpdateFloorLabel()
        {
            if (floorLabel != null && FloorManager.Instance != null)
            {
                floorLabel.text = $"Floor {FloorManager.Instance.CurrentFloor}";
            }
        }

        // Event Handlers

        private void OnGameStateChanged(GameState state)
        {
            if (root != null)
            {
                root.style.display = (state == GameState.BuildMode)
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }
        }

        private void OnFloorChanged(int floor)
        {
            UpdateFloorLabel();
        }

        private void OnFloorUpClicked() => FloorManager.Instance?.FloorUp();
        private void OnFloorDownClicked() => FloorManager.Instance?.FloorDown();

        private void OnMainGridToggled(ChangeEvent<bool> evt)
        {
            if (gridVisualizer != null)
                gridVisualizer.ShowMainGrid = evt.newValue;
        }

        private void OnSmallGridToggled(ChangeEvent<bool> evt)
        {
            if (gridVisualizer != null)
                gridVisualizer.ShowSmallGrid = evt.newValue;
        }

        private void OnSnapIndicatorToggled(ChangeEvent<bool> evt)
        {
            if (snapIndicator != null)
                snapIndicator.ShowIndicator = evt.newValue;
        }

        private void OnSnapModeChanged(ChangeEvent<int> evt)
        {
            if (snapIndicator != null)
                snapIndicator.UseSmallGrid = (evt.newValue == 1);
        }
    }
}
