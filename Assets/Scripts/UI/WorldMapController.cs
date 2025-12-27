using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using StationeersBuildPlanner.Core;
using StationeersBuildPlanner.World;
using System.Collections.Generic;

namespace StationeersBuildPlanner.UI
{
    /// <summary>
    /// Main controller for the World Map UI.
    /// Handles planet selection, map display, and location selection.
    /// </summary>
    public class WorldMapController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private UIDocument uiDocument;

        [Header("Configuration")]
        [SerializeField] private int defaultPlanetIndex = 2; // Mars

        // UI Elements - Main
        private VisualElement root;
        private DropdownField planetDropdown;
        private VisualElement mapContainer;
        private VisualElement mapImage;
        private VisualElement oreOverlay;
        private VisualElement spawnMarkers;
        private Label coordinateText;
        private VisualElement infoPanel;
        private Label infoCoordinates;
        private Label infoNearestSpawn;
        private Label infoOreAccess;
        private Button confirmButton;

        // UI Elements - Side Panel
        private VisualElement sidePanelContainer;
        private Button sidePanelTab;
        private Toggle toggleOre;
        private Slider oreOpacitySlider;
        private Toggle toggleSpawns;
        private Toggle toggleLabels;
        private VisualElement oreLegend;
        private Label hoverInfo;

        // State
        private Dictionary<string, WorldData> loadedWorlds;
        private WorldData currentWorld;
        private Vector2? selectedLocation;
        private Vector2 clickScreenPosition;
        private bool sidePanelCollapsed;

        // Tooltip positioning constants
        private const float TOOLTIP_OFFSET = 15f;
        private const float TOOLTIP_WIDTH = 220f;
        private const float TOOLTIP_HEIGHT = 100f;

        private void Awake()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }
        }

        private void Start()
        {
            // Subscribe to state changes (must be in Start, not OnEnable, to ensure GameStateManager.Instance exists)
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged += OnGameStateChanged;
                // Sync initial visibility
                OnGameStateChanged(GameStateManager.Instance.CurrentState);
            }
            else
            {
                Debug.LogWarning("[WorldMapController] GameStateManager.Instance is null in Start!");
            }
        }

        private void OnEnable()
        {
            InitializeUI();
            LoadWorldData();
        }

        private void OnDisable()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged -= OnGameStateChanged;
            }

            UnregisterCallbacks();
        }

        private void Update()
        {
            // M key toggles map mode
            if (Keyboard.current.mKey.wasPressedThisFrame)
            {
                if (GameStateManager.Instance != null)
                {
                    GameStateManager.Instance.ToggleState();
                }
            }
        }

        private void InitializeUI()
        {
            root = uiDocument.rootVisualElement;

            // Get main element references
            planetDropdown = root.Q<DropdownField>("planet-dropdown");
            mapContainer = root.Q<VisualElement>("map-container");
            mapImage = root.Q<VisualElement>("map-image");
            oreOverlay = root.Q<VisualElement>("ore-overlay");
            spawnMarkers = root.Q<VisualElement>("spawn-markers");
            coordinateText = root.Q<Label>("coordinate-text");
            infoPanel = root.Q<VisualElement>("info-panel");
            infoCoordinates = root.Q<Label>("info-coordinates");
            infoNearestSpawn = root.Q<Label>("info-nearest-spawn");
            infoOreAccess = root.Q<Label>("info-ore-access");
            confirmButton = root.Q<Button>("confirm-button");

            // Get side panel elements
            sidePanelContainer = root.Q<VisualElement>("side-panel-container");
            sidePanelTab = root.Q<Button>("side-panel-tab");
            toggleOre = root.Q<Toggle>("toggle-ore");
            oreOpacitySlider = root.Q<Slider>("ore-opacity");
            toggleSpawns = root.Q<Toggle>("toggle-spawns");
            toggleLabels = root.Q<Toggle>("toggle-labels");
            oreLegend = root.Q<VisualElement>("ore-legend");
            hoverInfo = root.Q<Label>("hover-info");

            RegisterCallbacks();
        }

        private void RegisterCallbacks()
        {
            if (planetDropdown != null)
                planetDropdown.RegisterValueChangedCallback(OnPlanetChanged);

            if (mapContainer != null)
            {
                mapContainer.RegisterCallback<MouseMoveEvent>(OnMapMouseMove);
                mapContainer.RegisterCallback<ClickEvent>(OnMapClicked);
            }

            if (confirmButton != null)
                confirmButton.clicked += OnConfirmClicked;

            // Side panel callbacks
            if (sidePanelTab != null)
                sidePanelTab.clicked += OnSidePanelTabClicked;

            if (toggleOre != null)
                toggleOre.RegisterValueChangedCallback(OnToggleOreChanged);

            if (oreOpacitySlider != null)
            {
                oreOpacitySlider.RegisterValueChangedCallback(OnOreOpacityChanged);
                // Apply initial opacity value
                if (oreOverlay != null)
                    oreOverlay.style.opacity = oreOpacitySlider.value;
            }

            if (toggleSpawns != null)
                toggleSpawns.RegisterValueChangedCallback(OnToggleSpawnsChanged);

            if (toggleLabels != null)
            {
                toggleLabels.RegisterValueChangedCallback(OnToggleLabelsChanged);
                // Sync initial label visibility state
                if (spawnMarkers != null && !toggleLabels.value)
                    spawnMarkers.AddToClassList("labels-hidden");
            }
        }

        private void UnregisterCallbacks()
        {
            if (planetDropdown != null)
                planetDropdown.UnregisterValueChangedCallback(OnPlanetChanged);

            if (mapContainer != null)
            {
                mapContainer.UnregisterCallback<MouseMoveEvent>(OnMapMouseMove);
                mapContainer.UnregisterCallback<ClickEvent>(OnMapClicked);
            }

            if (confirmButton != null)
                confirmButton.clicked -= OnConfirmClicked;

            // Side panel callbacks
            if (sidePanelTab != null)
                sidePanelTab.clicked -= OnSidePanelTabClicked;

            if (toggleOre != null)
                toggleOre.UnregisterValueChangedCallback(OnToggleOreChanged);

            if (oreOpacitySlider != null)
                oreOpacitySlider.UnregisterValueChangedCallback(OnOreOpacityChanged);

            if (toggleSpawns != null)
                toggleSpawns.UnregisterValueChangedCallback(OnToggleSpawnsChanged);

            if (toggleLabels != null)
                toggleLabels.UnregisterValueChangedCallback(OnToggleLabelsChanged);
        }

        private void LoadWorldData()
        {
            if (!WorldDataLoader.Initialize())
            {
                Debug.LogError("[WorldMapController] Failed to initialize WorldDataLoader");
                return;
            }

            loadedWorlds = WorldDataLoader.LoadAllWorlds();
            Debug.Log($"[WorldMapController] Loaded {loadedWorlds.Count} worlds");

            // Populate dropdown
            var choices = new List<string>();
            foreach (var world in WorldRegistry.AvailableWorlds)
            {
                choices.Add(world.DisplayName);
            }
            planetDropdown.choices = choices;
            planetDropdown.index = defaultPlanetIndex;

            // Load initial world
            SelectWorld(defaultPlanetIndex);
        }

        private void SelectWorld(int index)
        {
            if (index < 0 || index >= WorldRegistry.AvailableWorlds.Length) return;

            var worldInfo = WorldRegistry.AvailableWorlds[index];
            if (loadedWorlds.TryGetValue(worldInfo.FolderName, out var world))
            {
                currentWorld = world;
                UpdateMapDisplay();
                BuildOreLegend();
                CreateSpawnMarkers();
                ClearSelection();
                Debug.Log($"[WorldMapController] Selected world: {currentWorld.DisplayName}");
            }
        }

        private void UpdateMapDisplay()
        {
            if (currentWorld == null || mapImage == null) return;

            // Display minimap texture as background
            if (currentWorld.MinimapTexture != null)
            {
                mapImage.style.backgroundImage = new StyleBackground(currentWorld.MinimapTexture);

                // Size map to fit container while maintaining aspect ratio
                mapImage.style.width = Length.Percent(100);
                mapImage.style.height = Length.Percent(100);
                mapImage.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            }

            // Set up ore overlay texture (same sizing as map)
            if (oreOverlay != null && currentWorld.DeepMiningTexture != null)
            {
                oreOverlay.style.backgroundImage = new StyleBackground(currentWorld.DeepMiningTexture);
                oreOverlay.style.width = Length.Percent(100);
                oreOverlay.style.height = Length.Percent(100);
                oreOverlay.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            }
        }

        private void ClearSelection()
        {
            selectedLocation = null;
            infoPanel.AddToClassList("info-panel-hidden");
            if (confirmButton != null)
            {
                confirmButton.AddToClassList("confirm-button-hidden");
            }
        }

        // --- Event Handlers ---

        private void OnPlanetChanged(ChangeEvent<string> evt)
        {
            int index = planetDropdown.index;
            SelectWorld(index);
        }

        // --- Side Panel Event Handlers ---

        private void OnSidePanelTabClicked()
        {
            sidePanelCollapsed = !sidePanelCollapsed;

            if (sidePanelCollapsed)
            {
                sidePanelContainer.AddToClassList("collapsed");
                sidePanelTab.text = "▶";
            }
            else
            {
                sidePanelContainer.RemoveFromClassList("collapsed");
                sidePanelTab.text = "◀";
            }
        }

        private void OnToggleOreChanged(ChangeEvent<bool> evt)
        {
            if (oreOverlay == null) return;

            if (evt.newValue)
            {
                oreOverlay.RemoveFromClassList("ore-overlay-hidden");
            }
            else
            {
                oreOverlay.AddToClassList("ore-overlay-hidden");
            }
        }

        private void OnOreOpacityChanged(ChangeEvent<float> evt)
        {
            if (oreOverlay != null)
            {
                oreOverlay.style.opacity = evt.newValue;
            }
        }

        private void OnToggleSpawnsChanged(ChangeEvent<bool> evt)
        {
            if (spawnMarkers == null) return;

            if (evt.newValue)
            {
                spawnMarkers.RemoveFromClassList("spawn-markers-hidden");
            }
            else
            {
                spawnMarkers.AddToClassList("spawn-markers-hidden");
            }
        }

        private void OnToggleLabelsChanged(ChangeEvent<bool> evt)
        {
            if (spawnMarkers == null) return;

            if (evt.newValue)
            {
                spawnMarkers.RemoveFromClassList("labels-hidden");
            }
            else
            {
                spawnMarkers.AddToClassList("labels-hidden");
            }
        }

        private void OnMapMouseMove(MouseMoveEvent evt)
        {
            if (currentWorld == null || mapContainer == null) return;

            if (IsPositionOnMap(evt.localMousePosition))
            {
                Vector2 gameCoords = ScreenToGameCoordinates(evt.localMousePosition);
                coordinateText.text = $"X: {gameCoords.x:F0}  Y: {gameCoords.y:F0}";

                // Update hover info in side panel
                UpdateHoverInfo(gameCoords);
            }
            else
            {
                coordinateText.text = "-- Off Map --";
                if (hoverInfo != null)
                    hoverInfo.text = "--";
            }
        }

        private void UpdateHoverInfo(Vector2 gameCoords)
        {
            if (hoverInfo == null || currentWorld == null) return;

            var ore = WorldDataLoader.GetOreAtPosition(currentWorld, gameCoords);
            if (ore != null)
            {
                hoverInfo.text = $"Ore: {ore.OreType}";
            }
            else
            {
                hoverInfo.text = "No ore deposits";
            }
        }

        private void OnMapClicked(ClickEvent evt)
        {
            if (currentWorld == null) return;

            // Only register clicks on the actual map area
            if (!IsPositionOnMap(evt.localPosition)) return;

            selectedLocation = ScreenToGameCoordinates(evt.localPosition);
            clickScreenPosition = evt.localPosition;
            UpdateLocationInfo();
        }

        private void OnConfirmClicked()
        {
            if (selectedLocation == null || GameStateManager.Instance == null) return;

            Debug.Log($"[WorldMapController] Confirmed location: {selectedLocation.Value}");
            GameStateManager.Instance.EnterBuildMode();
        }

        private void OnGameStateChanged(GameState newState)
        {
            // Show/hide the entire UI based on game state
            if (root != null)
            {
                root.style.display = (newState == GameState.MapMode)
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }
        }

        // --- Coordinate Conversion ---

        /// <summary>
        /// Get the actual displayed map rect within the container, accounting for ScaleToFit aspect ratio.
        /// </summary>
        private Rect GetDisplayedMapRect()
        {
            if (mapContainer == null || currentWorld?.MinimapTexture == null)
                return Rect.zero;

            float containerWidth = mapContainer.resolvedStyle.width;
            float containerHeight = mapContainer.resolvedStyle.height;

            if (containerWidth <= 0 || containerHeight <= 0)
                return Rect.zero;

            float textureWidth = currentWorld.MinimapTexture.width;
            float textureHeight = currentWorld.MinimapTexture.height;

            float containerAspect = containerWidth / containerHeight;
            float textureAspect = textureWidth / textureHeight;

            float displayWidth, displayHeight;

            if (textureAspect > containerAspect)
            {
                // Texture is wider - fit to width, letterbox top/bottom
                displayWidth = containerWidth;
                displayHeight = containerWidth / textureAspect;
            }
            else
            {
                // Texture is taller - fit to height, pillarbox left/right
                displayHeight = containerHeight;
                displayWidth = containerHeight * textureAspect;
            }

            // Center the displayed area
            float offsetX = (containerWidth - displayWidth) / 2f;
            float offsetY = (containerHeight - displayHeight) / 2f;

            return new Rect(offsetX, offsetY, displayWidth, displayHeight);
        }

        private Vector2 ScreenToGameCoordinates(Vector2 localPos)
        {
            Rect mapRect = GetDisplayedMapRect();
            if (mapRect.width <= 0 || mapRect.height <= 0 || currentWorld == null)
                return Vector2.zero;

            // Convert local position to position within the actual map rect
            float normalizedX = (localPos.x - mapRect.x) / mapRect.width;
            float normalizedY = (localPos.y - mapRect.y) / mapRect.height;

            // Convert normalized (0-1) to game coordinates (-WorldSize/2 to +WorldSize/2)
            float gameX = (normalizedX - 0.5f) * currentWorld.WorldSize;
            float gameY = (0.5f - normalizedY) * currentWorld.WorldSize; // Flip Y

            return new Vector2(gameX, gameY);
        }

        /// <summary>
        /// Check if a local position is within the displayed map area.
        /// </summary>
        private bool IsPositionOnMap(Vector2 localPos)
        {
            Rect mapRect = GetDisplayedMapRect();
            return mapRect.Contains(localPos);
        }

        private void UpdateLocationInfo()
        {
            if (selectedLocation == null || currentWorld == null) return;

            Vector2 loc = selectedLocation.Value;

            // Show info panel and confirm button
            infoPanel.RemoveFromClassList("info-panel-hidden");
            if (confirmButton != null)
            {
                confirmButton.RemoveFromClassList("confirm-button-hidden");
            }

            // Position tooltip near click point
            PositionTooltip();

            // Update coordinates
            infoCoordinates.text = $"Coordinates: X: {loc.x:F0}  Y: {loc.y:F0}";

            // Find nearest spawn
            string nearestSpawn = FindNearestSpawn(loc, out float distance);
            infoNearestSpawn.text = $"Nearest Spawn: {nearestSpawn} ({distance:F0}m)";

            // Get ore at location
            var ore = WorldDataLoader.GetOreAtPosition(currentWorld, loc);
            infoOreAccess.text = ore != null
                ? $"Ore Access: {ore.OreType}"
                : "Ore Access: None detected";
        }

        private void PositionTooltip()
        {
            if (infoPanel == null || root == null) return;

            float rootWidth = root.resolvedStyle.width;
            float rootHeight = root.resolvedStyle.height;

            // Default: bottom-right of click point
            float tooltipX = clickScreenPosition.x + TOOLTIP_OFFSET;
            float tooltipY = clickScreenPosition.y + TOOLTIP_OFFSET;

            // Flip to left if too close to right edge
            if (tooltipX + TOOLTIP_WIDTH > rootWidth - 20)
            {
                tooltipX = clickScreenPosition.x - TOOLTIP_WIDTH - TOOLTIP_OFFSET;
            }

            // Flip to top if too close to bottom edge
            if (tooltipY + TOOLTIP_HEIGHT > rootHeight - 20)
            {
                tooltipY = clickScreenPosition.y - TOOLTIP_HEIGHT - TOOLTIP_OFFSET;
            }

            // Clamp to ensure tooltip stays on screen
            tooltipX = Mathf.Clamp(tooltipX, 10, rootWidth - TOOLTIP_WIDTH - 10);
            tooltipY = Mathf.Clamp(tooltipY, 60, rootHeight - TOOLTIP_HEIGHT - 10); // 60 to avoid top bar

            infoPanel.style.left = tooltipX;
            infoPanel.style.top = tooltipY;
        }

        private string FindNearestSpawn(Vector2 location, out float distance)
        {
            distance = float.MaxValue;
            string nearest = "--";

            if (currentWorld?.StartLocations == null) return nearest;

            foreach (var spawn in currentWorld.StartLocations)
            {
                float dist = Vector2.Distance(location, spawn.Position);
                if (dist < distance)
                {
                    distance = dist;
                    nearest = spawn.DisplayName;
                }
            }

            return nearest;
        }

        // --- Ore Legend ---

        private void BuildOreLegend()
        {
            if (oreLegend == null || currentWorld == null) return;

            // Clear existing legend items
            oreLegend.Clear();

            if (currentWorld.OreRegions == null || currentWorld.OreRegions.Count == 0)
            {
                var noOreLabel = new Label("No ore data available");
                noOreLabel.AddToClassList("ore-legend-label");
                oreLegend.Add(noOreLabel);
                return;
            }

            foreach (var ore in currentWorld.OreRegions)
            {
                var item = new VisualElement();
                item.AddToClassList("ore-legend-item");

                var swatch = new VisualElement();
                swatch.AddToClassList("ore-legend-swatch");
                swatch.style.backgroundColor = new Color(ore.Color.r / 255f, ore.Color.g / 255f, ore.Color.b / 255f);

                var label = new Label(ore.OreType);
                label.AddToClassList("ore-legend-label");

                item.Add(swatch);
                item.Add(label);
                oreLegend.Add(item);
            }
        }

        // --- Spawn Markers ---

        private void CreateSpawnMarkers()
        {
            if (spawnMarkers == null || currentWorld == null) return;

            // Clear existing markers
            spawnMarkers.Clear();

            if (currentWorld.StartLocations == null) return;

            foreach (var spawn in currentWorld.StartLocations)
            {
                var marker = new VisualElement();
                marker.AddToClassList("spawn-marker");
                marker.userData = spawn; // Store spawn data for later use

                var icon = new VisualElement();
                icon.AddToClassList("spawn-marker-icon");

                var label = new Label(spawn.DisplayName);
                label.AddToClassList("spawn-marker-label");

                marker.Add(icon);
                marker.Add(label);
                spawnMarkers.Add(marker);
            }

            // Register for geometry changes to update positions when layout is ready
            mapContainer.RegisterCallback<GeometryChangedEvent>(OnMapGeometryChanged);

            // Also try immediate positioning in case layout is already complete
            root.schedule.Execute(UpdateSpawnMarkerPositions).ExecuteLater(50);
        }

        private void OnMapGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateSpawnMarkerPositions();
        }

        private void UpdateSpawnMarkerPositions()
        {
            if (spawnMarkers == null || currentWorld == null) return;

            Rect mapRect = GetDisplayedMapRect();
            Debug.Log($"[WorldMapController] UpdateSpawnMarkerPositions - mapRect: {mapRect}");

            if (mapRect.width <= 0 || mapRect.height <= 0)
            {
                // Layout not ready yet, try again later
                root.schedule.Execute(UpdateSpawnMarkerPositions).ExecuteLater(100);
                return;
            }

            int count = 0;
            foreach (var marker in spawnMarkers.Children())
            {
                if (marker.userData is StartLocation spawn)
                {
                    Vector2 screenPos = GameToScreenCoordinates(spawn.Position, mapRect);

                    // Center the marker on the position (offset by half marker size)
                    marker.style.left = screenPos.x - 10;
                    marker.style.top = screenPos.y - 28; // Thumbtack points at the location

                    if (count == 0)
                    {
                        Debug.Log($"[WorldMapController] First spawn '{spawn.DisplayName}' gamePos={spawn.Position}, screenPos={screenPos}");
                    }
                    count++;
                }
            }
            Debug.Log($"[WorldMapController] Positioned {count} spawn markers");
        }

        private Vector2 GameToScreenCoordinates(Vector2 gamePos, Rect mapRect)
        {
            if (currentWorld == null) return Vector2.zero;

            // Convert game coordinates to normalized (0-1)
            float normalizedX = (gamePos.x / currentWorld.WorldSize) + 0.5f;
            float normalizedY = 0.5f - (gamePos.y / currentWorld.WorldSize); // Flip Y

            // Convert to screen position within map rect
            float screenX = mapRect.x + normalizedX * mapRect.width;
            float screenY = mapRect.y + normalizedY * mapRect.height;

            return new Vector2(screenX, screenY);
        }
    }
}
