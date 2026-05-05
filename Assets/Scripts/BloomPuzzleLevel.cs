using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[AddComponentMenu("Bloom Rock Puzzle/Bloom Puzzle Level")]
public class BloomPuzzleLevel : MonoBehaviour
{
    [Header("Board")]
    [SerializeField] private bool useManualBounds = false;
    [SerializeField] private Vector2Int boundsMin = new Vector2Int(-4, -3);
    [SerializeField] private Vector2Int boundsMax = new Vector2Int(4, 3);

    [Header("Rules")]
    [SerializeField] private bool waterNeedsAdjacentFlower = true;
    [SerializeField] private int maxWaterCells = 9;

    [Header("Events")]
    [SerializeField] private UnityEvent onLevelCleared = new UnityEvent();

    [Header("Flow Visuals")]
    [SerializeField] private bool showFlowVisuals = true;
    [SerializeField] private float flowVisualSize = 0.64f;
    [SerializeField] private float flowVisualZ = 0.2f;
    [SerializeField] private float minimumFlowAlpha = 0.04f;
    [SerializeField] private Color lightVisualColor = new Color(1f, 0.92f, 0.1f, 0.45f);
    [SerializeField] private Color waterVisualColor = new Color(0.1f, 0.55f, 1f, 0.7f);
    [SerializeField] private Color muddyWaterVisualColor = new Color(0.45f, 0.28f, 0.12f, 0.7f);
    [SerializeField] private Color mixedVisualColor = new Color(0.25f, 1f, 0.45f, 0.45f);
    [SerializeField] private Sprite waterFlowSprite;
    [SerializeField] private Sprite muddyWaterFlowSprite;
    [SerializeField] private Sprite liquidBodySprite;
    [SerializeField] private Sprite lightBeamSprite;
    [SerializeField] private int flowVisualSortingOrder = 10;
    [SerializeField] private int liquidBodySortingOffset = -1;
    [SerializeField] private float spriteMinimumAlpha = 0.28f;
    [SerializeField] private float waterSpriteAlphaMultiplier = 2.1f;
    [SerializeField] private float lightSpriteAlphaMultiplier = 1.8f;
    [SerializeField] private bool useAdditiveLightBlend = true;
    [SerializeField] private float additiveLightStrength = 1.35f;
    [SerializeField] private float waterSpriteFadePerCell = 0.18f;
    [SerializeField] private float lightSpriteFadePerCell = 0.05f;
    [SerializeField] private float liquidBodyAlphaMultiplier = 0.72f;
    [SerializeField] private float liquidBodySize = 0.88f;
    [SerializeField] private float liquidConnectorWidth = 0.52f;
    [SerializeField] private float liquidConnectorLength = 1.08f;
    [SerializeField] private float liquidSurfaceFlowAlphaMultiplier = 0.58f;
    [SerializeField] private float waterStreamWidth = 0.44f;
    [SerializeField] private float waterStreamLength = 1.02f;
    [SerializeField] private float muddyWaterVisualSize = 0.72f;
    [SerializeField] private float lightBeamWidth = 0.5f;
    [SerializeField] private float waterMotionDistance = 0.28f;
    [SerializeField] private float waterMotionSpeed = 1.8f;
    [SerializeField] private float muddyWaterMotionSpeed = 0.75f;
    [SerializeField] private float lightPulseSpeed = 2.4f;
    [SerializeField] private float lightBeamLength = 0.92f;

    [Header("Clear Visual")]
    [SerializeField] private bool showClearText = true;
    [SerializeField] private string clearText = "CLEAR!";
    [SerializeField] private Vector3 clearTextPosition = new Vector3(0f, 3f, -0.5f);
    [SerializeField] private Color clearTextColor = new Color(1f, 0.95f, 0.25f, 1f);

    [Header("Stage Transition")]
    [SerializeField] private bool showNextArrowOnClear = false;
    [SerializeField] private string nextSceneName = "";
    [SerializeField] private bool autoResolveNextSceneName = true;
    [SerializeField] private string firstNumberedStageSceneName = "Stage1";
    [SerializeField] private string clearSceneName = "Clear Scene";
    [SerializeField] private Sprite nextArrowSprite;
    [SerializeField] private Vector2 nextArrowAnchoredPosition = new Vector2(-36f, 0f);
    [SerializeField] private Vector2 nextArrowSize = new Vector2(180f, 120f);
    [SerializeField] private float transitionScrollDistance = 12f;
    [SerializeField] private float transitionScrollDuration = 0.8f;
    [SerializeField] private bool playIncomingScrollTransition = true;
    [SerializeField] private Sprite transitionBackgroundSprite;
    [SerializeField] private Color transitionBackgroundColor = Color.white;
    [SerializeField] private int transitionBackgroundSortingOrder = -100;
    [SerializeField] private int incomingTransitionBackgroundSortingOrder = -100;
    [SerializeField] private bool stretchTransitionBackgroundToScrollPath = true;
    [SerializeField] private float transitionBackgroundOverlap = 1f;
    [SerializeField] private float transitionBackgroundScalePadding = 1.08f;
    [SerializeField] private float transitionBackgroundFadeDuration = 0.12f;
    [SerializeField] private Color nextArrowColor = new Color(0.62f, 0.55f, 0.82f, 1f);

    private const string TransitionSettingsResourcePath = "BloomPuzzleTransitionSettings";

    private readonly HashSet<Vector2Int> litCells = new HashSet<Vector2Int>();
    private readonly Dictionary<Vector2Int, WaterKind> waterCells = new Dictionary<Vector2Int, WaterKind>();
    private readonly Dictionary<Vector2Int, int> lightDistances = new Dictionary<Vector2Int, int>();
    private readonly Dictionary<Vector2Int, int> waterDistances = new Dictionary<Vector2Int, int>();
    private readonly Dictionary<Vector2Int, Vector2Int> waterDirections = new Dictionary<Vector2Int, Vector2Int>();
    private readonly Dictionary<Vector2Int, Vector2Int> lightDirections = new Dictionary<Vector2Int, Vector2Int>();
    private readonly List<GameObject> flowVisuals = new List<GameObject>();
    private Transform flowVisualRoot;
    private TextMesh clearTextMesh;
    private Button nextArrowButton;
    private GameObject transitionBackgroundRoot;
    private readonly List<SpriteRenderer> transitionBackgroundRenderers = new List<SpriteRenderer>();
    private bool isTransitioning;
    private bool wasCleared;
    private bool hasBuiltWaterOnce;
    private int bloomingFlowerCount;
    private int totalFlowerCount;
    private Sprite defaultWaterFlowSprite;
    private Sprite defaultMuddyWaterFlowSprite;
    private Sprite defaultLiquidBodySprite;
    private Sprite defaultLightBeamSprite;
    private Material additiveLightMaterial;
    private static BloomPuzzleTransitionSettings transitionSettings;

    private const string IncomingTransitionSceneKey = "BloomPuzzleLevel.IncomingTransitionScene";
    private const string IncomingTransitionBackgroundOffsetXKey = "BloomPuzzleLevel.IncomingTransitionBackgroundOffsetX";
    private const string IncomingTransitionBackgroundOffsetYKey = "BloomPuzzleLevel.IncomingTransitionBackgroundOffsetY";
    private const string IncomingTransitionBackgroundScaleKey = "BloomPuzzleLevel.IncomingTransitionBackgroundScale";

    public event Action<int, int> FlowerBloomCountChanged;

    public int BloomingFlowerCount => bloomingFlowerCount;
    public int TotalFlowerCount => totalFlowerCount;

    private static readonly Vector2Int[] CardinalDirections =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    private void Start()
    {
        RefreshAll();
        StartCoroutine(PlayIncomingScrollTransitionIfNeeded());
    }

    private void OnValidate()
    {
        maxWaterCells = Mathf.Max(1, maxWaterCells);
        spriteMinimumAlpha = Mathf.Clamp01(spriteMinimumAlpha);
        waterSpriteAlphaMultiplier = Mathf.Max(0f, waterSpriteAlphaMultiplier);
        lightSpriteAlphaMultiplier = Mathf.Max(0f, lightSpriteAlphaMultiplier);
        additiveLightStrength = Mathf.Max(0f, additiveLightStrength);
        liquidBodyAlphaMultiplier = Mathf.Max(0f, liquidBodyAlphaMultiplier);
        liquidBodySize = Mathf.Max(0.01f, liquidBodySize);
        liquidConnectorWidth = Mathf.Max(0.01f, liquidConnectorWidth);
        liquidConnectorLength = Mathf.Max(0.01f, liquidConnectorLength);
        liquidSurfaceFlowAlphaMultiplier = Mathf.Max(0f, liquidSurfaceFlowAlphaMultiplier);
        waterStreamWidth = Mathf.Max(0.01f, waterStreamWidth);
        waterStreamLength = Mathf.Max(0.01f, waterStreamLength);
        muddyWaterVisualSize = Mathf.Max(0.01f, muddyWaterVisualSize);
        lightBeamWidth = Mathf.Max(0.01f, lightBeamWidth);
    }

    private void OnDestroy()
    {
        if (additiveLightMaterial != null)
        {
            Destroy(additiveLightMaterial);
        }
    }

    [ContextMenu("Refresh Puzzle State")]
    private void RefreshPuzzleStateFromContextMenu()
    {
        RefreshAll();
    }

    public bool TryMoveRock(PushableRock rock, Vector2Int direction)
    {
        if (wasCleared) return false;

        if (rock == null)
        {
            return false;
        }

        Vector2Int target = rock.GridPosition + direction;
        if (!CanRockMoveTo(target))
        {
            return false;
        }

        rock.GridPosition = target;
        RefreshAll();
        return true;
    }

    public bool TryRotateLightSourceAt(Vector2Int position)
    {
        if (wasCleared)
        {
            return false;
        }

        LightSourceTile source = GetLightSourceAt(position);
        if (source == null)
        {
            return false;
        }

        source.RotateClockwise();
        RefreshAll();
        return true;
    }

    public bool TryRotatePrismAt(Vector2Int position)
    {
        if (wasCleared)
        {
            return false;
        }

        PrismTile prism = GetPrismAt(position);
        if (prism == null)
        {
            return false;
        }

        prism.RotateClockwise();
        RefreshAll();
        return true;
    }

    public void RefreshAll()
    {
        RebuildLight();
        RebuildWater();
        RefreshFlowers();
        RefreshFlowVisuals();
    }

    private void RebuildLight()
    {
        litCells.Clear();
        lightDistances.Clear();
        lightDirections.Clear();

        Queue<LightRay> rays = new Queue<LightRay>();
        HashSet<LightRayKey> visitedRays = new HashSet<LightRayKey>();

        foreach (LightSourceTile source in FindObjectsOfType<LightSourceTile>())
        {
            Vector2Int direction = ToVector(source.Direction);
            rays.Enqueue(new LightRay(source.GridPosition + direction, direction, 1));
        }

        while (rays.Count > 0)
        {
            LightRay ray = rays.Dequeue();
            Vector2Int cursor = ray.Position;
            int distance = ray.Distance;

            while (IsInsideBounds(cursor))
            {
                LightRayKey currentKey = new LightRayKey(cursor, ray.Direction);
                if (!visitedRays.Add(currentKey))
                {
                    break;
                }

                if (BlocksLight(cursor))
                {
                    break;
                }

                litCells.Add(cursor);
                RecordNearestDistance(lightDistances, cursor, distance);
                RecordNearestDirection(lightDirections, lightDistances, cursor, ray.Direction, distance);

                PrismTile prism = GetPrismAt(cursor);
                if (prism != null)
                {
                    Vector2Int refractedDirection = ToVector(prism.Direction);
                    rays.Enqueue(new LightRay(cursor + refractedDirection, refractedDirection, distance + 1));
                    break;
                }

                cursor += ray.Direction;
                distance++;
            }
        }
    }

    private void RebuildWater()
    {
        Dictionary<Vector2Int, WaterKind> previousWaterCells = new Dictionary<Vector2Int, WaterKind>(waterCells);

        waterCells.Clear();
        waterDistances.Clear();
        waterDirections.Clear();
        foreach (WaterSourceTile source in FindObjectsOfType<WaterSourceTile>())
        {
            Queue<FlowNode> frontier = new Queue<FlowNode>();
            HashSet<Vector2Int> sourceWaterCells = new HashSet<Vector2Int>();

            RecordWater(source.GridPosition, source.WaterKind);
            waterDistances[source.GridPosition] = 0;
            waterDirections[source.GridPosition] = Vector2Int.zero;
            sourceWaterCells.Add(source.GridPosition);
            frontier.Enqueue(new FlowNode(source.GridPosition, source.WaterKind, 0));

            while (frontier.Count > 0)
            {
                FlowNode current = frontier.Dequeue();

                foreach (Vector2Int direction in CardinalDirections)
                {
                    if (sourceWaterCells.Count >= maxWaterCells)
                    {
                        frontier.Clear();
                        break;
                    }

                    Vector2Int next = current.Position + direction;
                    if (!IsInsideBounds(next) || sourceWaterCells.Contains(next) || BlocksWater(next))
                    {
                        continue;
                    }

                    int nextDistance = current.Distance + 1;
                    sourceWaterCells.Add(next);
                    bool recordedWater = RecordWater(next, current.WaterKind);
                    RecordNearestDistance(waterDistances, next, nextDistance);
                    if (recordedWater || !waterDirections.ContainsKey(next))
                    {
                        RecordNearestDirection(waterDirections, waterDistances, next, direction, nextDistance);
                    }
                    frontier.Enqueue(new FlowNode(next, current.WaterKind, nextDistance));
                }
            }
        }

        PlayWaterChangeIfNeeded(previousWaterCells);
    }

    private void PlayWaterChangeIfNeeded(Dictionary<Vector2Int, WaterKind> previousWaterCells)
    {
        if (!hasBuiltWaterOnce)
        {
            hasBuiltWaterOnce = true;
            return;
        }

        if (!AreWaterCellsEqual(previousWaterCells, waterCells))
        {
            SeManager.PlayWaterChange();
        }
    }

    private static bool AreWaterCellsEqual(Dictionary<Vector2Int, WaterKind> first, Dictionary<Vector2Int, WaterKind> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        foreach (KeyValuePair<Vector2Int, WaterKind> waterCell in first)
        {
            if (!second.TryGetValue(waterCell.Key, out WaterKind foundKind) || foundKind != waterCell.Value)
            {
                return false;
            }
        }

        return true;
    }

    private void RefreshFlowers()
    {
        FlowerTile[] flowers = FindObjectsOfType<FlowerTile>();
        bool allBlooming = flowers.Length > 0;
        int currentBloomingFlowerCount = 0;

        foreach (FlowerTile flower in flowers)
        {
            bool hasCleanWater = waterNeedsAdjacentFlower ? HasAdjacentWater(flower.GridPosition, WaterKind.Clean) : HasWater(flower.GridPosition, WaterKind.Clean);
            bool hasMuddyWater = waterNeedsAdjacentFlower ? HasAdjacentWater(flower.GridPosition, WaterKind.Muddy) : HasWater(flower.GridPosition, WaterKind.Muddy);
            bool isLit = litCells.Contains(flower.GridPosition);
            flower.SetConditions(isLit, hasCleanWater, hasMuddyWater);
            if (flower.IsBlooming)
            {
                currentBloomingFlowerCount++;
            }

            allBlooming &= flower.IsBlooming;
        }

        SetFlowerBloomCount(currentBloomingFlowerCount, flowers.Length);

        if (allBlooming && !wasCleared)
        {
            wasCleared = true;
            Debug.Log("Stage clear: all flowers are blooming.");
            SeManager.PlayClear();
            RefreshClearVisual(true);
            RefreshNextArrow(true);
            onLevelCleared?.Invoke();
        }
        else if (!allBlooming)
        {
            wasCleared = false;
            RefreshClearVisual(false);
            RefreshNextArrow(false);
        }
    }

    private void SetFlowerBloomCount(int currentBloomingFlowerCount, int currentTotalFlowerCount)
    {
        if (bloomingFlowerCount == currentBloomingFlowerCount && totalFlowerCount == currentTotalFlowerCount)
        {
            return;
        }

        bloomingFlowerCount = currentBloomingFlowerCount;
        totalFlowerCount = currentTotalFlowerCount;
        FlowerBloomCountChanged?.Invoke(bloomingFlowerCount, totalFlowerCount);
    }

    private bool HasAdjacentWater(Vector2Int position, WaterKind waterKind)
    {
        foreach (Vector2Int direction in CardinalDirections)
        {
            if (HasWater(position + direction, waterKind))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasWater(Vector2Int position, WaterKind waterKind)
    {
        return waterCells.TryGetValue(position, out WaterKind foundKind) && foundKind == waterKind;
    }

    private bool RecordWater(Vector2Int position, WaterKind waterKind)
    {
        if (waterCells.TryGetValue(position, out WaterKind currentKind) && currentKind == WaterKind.Muddy)
        {
            return false;
        }

        waterCells[position] = waterKind;
        return true;
    }

    private bool CanRockMoveTo(Vector2Int position)
    {
        if (!IsInsideBounds(position))
        {
            return false;
        }

        return !BlocksRock(position);
    }

    public PushableRock GetRockAt(Vector2Int position)
    {
        foreach (PushableRock rock in FindObjectsOfType<PushableRock>())
        {
            if (rock.GridPosition == position)
            {
                return rock;
            }
        }

        return null;
    }

    public WallTile GetWallAt(Vector2Int position)
    {
        foreach (WallTile wall in FindObjectsOfType<WallTile>())
        {
            if (wall.ContainsCell(position))
            {
                return wall;
            }
        }

        return null;
    }

    public bool IsWallAt(Vector2Int position)
    {
        return GetWallAt(position) != null;
    }

    private FlowerTile GetFlowerAt(Vector2Int position)
    {
        foreach (FlowerTile flower in FindObjectsOfType<FlowerTile>())
        {
            if (flower.GridPosition == position)
            {
                return flower;
            }
        }

        return null;
    }
    private WaterSourceTile GetWaterSourceAt(Vector2Int position)
    {
        foreach (WaterSourceTile source in FindObjectsOfType<WaterSourceTile>())
        {
            if (source.GridPosition == position)
            {
                return source;
            }
        }

        return null;
    }
    public void LoadNextScene(string nextSceneName)
    {
        SceneManager.LoadScene(nextSceneName);
    }

    public void LoadNextSceneWithScroll(string sceneName)
    {
        if (isTransitioning)
        {
            return;
        }

        StartCoroutine(ScrollThenLoadScene(sceneName));
    }

    public void LoadConfiguredNextSceneWithScroll()
    {
        LoadNextSceneWithScroll(ResolveNextSceneName());
    }
    public LightSourceTile GetLightSourceAt(Vector2Int position)
    {
        foreach (LightSourceTile source in FindObjectsOfType<LightSourceTile>())
        {
            if (source.GridPosition == position)
            {
                return source;
            }
        }

        return null;
    }

    public PrismTile GetPrismAt(Vector2Int position)
    {
        foreach (PrismTile prism in FindObjectsOfType<PrismTile>())
        {
            if (prism.GridPosition == position)
            {
                return prism;
            }
        }

        return null;
    }

    private bool BlocksWater(Vector2Int position)
    {
        return GetRockAt(position) != null
            || IsWallAt(position)
            || GetLightSourceAt(position) != null
            || GetPrismAt(position) != null
            || GetFlowerAt(position) != null;
    }

    private bool BlocksLight(Vector2Int position)
    {
        return GetRockAt(position) != null || IsWallAt(position);
    }

    private bool BlocksRock(Vector2Int position)
    {
        return GetRockAt(position) != null
            || IsWallAt(position)
            || GetFlowerAt(position) != null
            || GetWaterSourceAt(position) != null
            || GetLightSourceAt(position) != null
            || GetPrismAt(position) != null;
    }

    public bool BlocksCursor(Vector2Int position)
    {
        return IsWallAt(position);
    }

    public bool IsInsideBounds(Vector2Int position)
    {
        if (!useManualBounds)
        {
            CalculateAutoBounds(out Vector2Int min, out Vector2Int max);
            return position.x >= min.x && position.x <= max.x && position.y >= min.y && position.y <= max.y;
        }

        return position.x >= boundsMin.x && position.x <= boundsMax.x && position.y >= boundsMin.y && position.y <= boundsMax.y;
    }

    private void CalculateAutoBounds(out Vector2Int min, out Vector2Int max)
    {
        min = boundsMin;
        max = boundsMax;

        foreach (GridPiece piece in FindObjectsOfType<GridPiece>())
        {
            min = Vector2Int.Min(min, piece.GridPosition);
            max = Vector2Int.Max(max, piece.GridPosition);
        }

        foreach (WallTile wall in FindObjectsOfType<WallTile>())
        {
            foreach (Vector2Int cell in wall.GetOccupiedCells())
            {
                min = Vector2Int.Min(min, cell);
                max = Vector2Int.Max(max, cell);
            }
        }

        min += new Vector2Int(-1, -1);
        max += new Vector2Int(1, 1);
    }

    private static Vector2Int ToVector(PuzzleDirection direction)
    {
        switch (direction)
        {
            case PuzzleDirection.Up:
                return Vector2Int.up;
            case PuzzleDirection.Down:
                return Vector2Int.down;
            case PuzzleDirection.Left:
                return Vector2Int.left;
            default:
                return Vector2Int.right;
        }
    }

    private static void RecordNearestDistance(Dictionary<Vector2Int, int> distances, Vector2Int cell, int distance)
    {
        if (!distances.TryGetValue(cell, out int currentDistance) || distance < currentDistance)
        {
            distances[cell] = distance;
        }
    }

    private static void RecordNearestDirection(Dictionary<Vector2Int, Vector2Int> directions, Dictionary<Vector2Int, int> distances, Vector2Int cell, Vector2Int direction, int distance)
    {
        if (!distances.TryGetValue(cell, out int currentDistance) || distance <= currentDistance || !directions.ContainsKey(cell))
        {
            directions[cell] = direction;
        }
    }

    private void RefreshFlowVisuals()
    {
        if (!showFlowVisuals)
        {
            ClearFlowVisuals();
            return;
        }

        EnsureFlowVisualRoot();
        ClearFlowVisuals();

        HashSet<Vector2Int> visualCells = new HashSet<Vector2Int>(waterCells.Keys);
        visualCells.UnionWith(litCells);

        foreach (Vector2Int cell in visualCells)
        {
            if ((waterCells.ContainsKey(cell) && BlocksWater(cell)) || (litCells.Contains(cell) && BlocksLight(cell)))
            {
                continue;
            }

            bool hasWater = waterCells.ContainsKey(cell);
            bool hasLight = litCells.Contains(cell);
            flowVisuals.Add(CreateFlowVisual(cell, hasWater, hasLight, GetWaterSourceAt(cell) != null));
        }
    }

    private float GetDistanceAlpha(Dictionary<Vector2Int, int> distances, Vector2Int cell, float baseAlpha, float fadePerCell)
    {
        if (!distances.TryGetValue(cell, out int distance))
        {
            return baseAlpha;
        }

        return Mathf.Max(minimumFlowAlpha, baseAlpha * Mathf.Pow(1f - Mathf.Clamp01(fadePerCell), distance));
    }

    private float GetSpriteAlpha(Dictionary<Vector2Int, int> distances, Vector2Int cell, float baseAlpha, float fadePerCell, float multiplier)
    {
        float alpha = GetDistanceAlpha(distances, cell, baseAlpha, fadePerCell) * multiplier;
        return Mathf.Clamp(alpha, spriteMinimumAlpha, 1f);
    }

    private void EnsureFlowVisualRoot()
    {
        if (flowVisualRoot != null)
        {
            return;
        }

        GameObject root = new GameObject("Flow Visuals");
        root.transform.SetParent(transform);
        flowVisualRoot = root.transform;
    }

    private GameObject CreateFlowVisual(Vector2Int cell, bool hasWater, bool hasLight, bool isWaterSource)
    {
        GameObject visual = new GameObject("Flow Visual");
        visual.transform.SetParent(flowVisualRoot);
        visual.transform.position = new Vector3(cell.x + 0.5f, cell.y + 0.5f, flowVisualZ);

        if (hasLight)
        {
            CreateLightVisual(visual.transform, cell, hasWater);
        }

        if (hasWater)
        {
            CreateWaterVisual(visual.transform, cell, hasLight, isWaterSource);
        }

        return visual;
    }

    private void CreateWaterVisual(Transform parent, Vector2Int cell, bool isMixedWithLight, bool isWaterSource)
    {
        WaterKind waterKind = waterCells.TryGetValue(cell, out WaterKind foundKind) ? foundKind : WaterKind.Clean;
        Color color = waterKind == WaterKind.Muddy ? muddyWaterVisualColor : waterVisualColor;
        color.a = GetSpriteAlpha(waterDistances, cell, color.a, waterSpriteFadePerCell, waterSpriteAlphaMultiplier);
        if (isMixedWithLight)
        {
            float alpha = color.a;
            color = Color.Lerp(color, mixedVisualColor, 0.25f);
            color.a = alpha;
        }

        float phase = GetCellPhase(cell);
        float speed = waterKind == WaterKind.Muddy ? muddyWaterMotionSpeed : waterMotionSpeed;
        Sprite sprite = waterKind == WaterKind.Muddy ? GetMuddyWaterSprite() : GetWaterSprite();
        float visualScale = GetFlowVisualScale();

        CreateLiquidBodyVisual(parent, cell, waterKind, color, visualScale, isWaterSource);
        if (!isWaterSource)
        {
            CreateLiquidSurfaceFlowVisuals(parent, cell, waterKind, color, visualScale, sprite, speed, phase);
        }
    }

    private void CreateLiquidBodyVisual(Transform parent, Vector2Int cell, WaterKind waterKind, Color flowColor, float visualScale, bool isWaterSource)
    {
        Color bodyColor = flowColor;
        bodyColor.a = Mathf.Clamp01(flowColor.a * liquidBodyAlphaMultiplier);

        if (!isWaterSource)
        {
            float bodySize = liquidBodySize * visualScale;
            GameObject body = CreateSpriteVisual("Liquid Body", parent, GetLiquidBodySprite(), bodyColor, bodySize, bodySize, Vector2Int.zero);
            SpriteRenderer bodyRenderer = body.GetComponent<SpriteRenderer>();
            bodyRenderer.sortingOrder = flowVisualSortingOrder + liquidBodySortingOffset;
        }

        foreach (Vector2Int direction in CardinalDirections)
        {
            Vector2Int neighbor = cell + direction;
            if (waterCells.TryGetValue(neighbor, out WaterKind neighborKind) && neighborKind == waterKind)
            {
                if (direction == Vector2Int.left || direction == Vector2Int.down)
                {
                    continue;
                }

                bool neighborIsWaterSource = GetWaterSourceAt(neighbor) != null;
                float connectorLength = liquidConnectorLength * visualScale;
                float connectorOffset = 0.5f;
                if (isWaterSource || neighborIsWaterSource)
                {
                    connectorLength *= 0.68f;
                    connectorOffset = isWaterSource ? 0.68f : 0.32f;
                }

                CreateLiquidConnector(parent, bodyColor, visualScale, direction, connectorLength, connectorOffset);
                continue;
            }

            if (GetFlowerAt(neighbor) != null)
            {
                float connectorLength = liquidConnectorLength * visualScale * 0.64f;
                float connectorOffset = isWaterSource ? 0.66f : 0.36f;
                CreateLiquidConnector(parent, bodyColor, visualScale, direction, connectorLength, connectorOffset);
            }
        }
    }

    private void CreateLiquidConnector(Transform parent, Color bodyColor, float visualScale, Vector2Int direction, float connectorLength, float connectorOffset)
    {
        GameObject connector = CreateSpriteVisual("Liquid Connector", parent, GetLiquidBodySprite(), bodyColor, liquidConnectorWidth * visualScale, connectorLength, direction);
        connector.transform.localPosition = (Vector3)((Vector2)direction * connectorOffset);
        connector.GetComponent<SpriteRenderer>().sortingOrder = flowVisualSortingOrder + liquidBodySortingOffset;
    }

    private void CreateLiquidSurfaceFlowVisuals(Transform parent, Vector2Int cell, WaterKind waterKind, Color flowColor, float visualScale, Sprite sprite, float speed, float phase)
    {
        bool hasConnection = false;
        foreach (Vector2Int direction in CardinalDirections)
        {
            Vector2Int neighbor = cell + direction;
            if (!waterCells.TryGetValue(neighbor, out WaterKind neighborKind) || neighborKind != waterKind)
            {
                continue;
            }

            if (direction == Vector2Int.left || direction == Vector2Int.down)
            {
                continue;
            }

            hasConnection = true;
            CreateLiquidSurfaceFlowSegment(parent, sprite, flowColor, visualScale, waterKind, direction, speed, phase, GetWaterSourceAt(neighbor) != null);
        }

        foreach (Vector2Int direction in CardinalDirections)
        {
            if (GetFlowerAt(cell + direction) == null)
            {
                continue;
            }

            hasConnection = true;
            CreateLiquidSurfaceFlowSegment(parent, sprite, flowColor, visualScale, waterKind, direction, speed, phase, true);
        }

        if (!hasConnection && CountSameLiquidNeighbors(cell, waterKind) == 0)
        {
            CreateLiquidSurfaceFlowSegment(parent, sprite, flowColor, visualScale, waterKind, Vector2Int.zero, speed, phase, false);
        }
    }

    private void CreateLiquidSurfaceFlowSegment(Transform parent, Sprite sprite, Color flowColor, float visualScale, WaterKind waterKind, Vector2Int direction, float speed, float phase, bool endsAtWaterSource)
    {
        Color surfaceColor = flowColor;
        surfaceColor.a = Mathf.Clamp01(flowColor.a * liquidSurfaceFlowAlphaMultiplier);

        float width = (waterKind == WaterKind.Muddy ? muddyWaterVisualSize * 0.72f : waterStreamWidth) * visualScale;
        float height = waterStreamLength * visualScale;
        float offset = 0.5f;
        if (direction == Vector2Int.zero)
        {
            width = (waterKind == WaterKind.Muddy ? muddyWaterVisualSize : waterStreamWidth) * visualScale;
            height = (waterKind == WaterKind.Muddy ? muddyWaterVisualSize : waterStreamLength) * visualScale;
        }
        else if (endsAtWaterSource)
        {
            height *= 0.68f;
            offset = 0.32f;
        }

        GameObject surface = CreateSpriteVisual("Liquid Surface Flow", parent, sprite, surfaceColor, width, height, direction);
        surface.transform.localPosition = (Vector3)((Vector2)direction * offset);
        FlowVisualAnimator animator = surface.AddComponent<FlowVisualAnimator>();
        animator.Configure(surface.GetComponent<SpriteRenderer>(), surfaceColor, (Vector2)direction * waterMotionDistance, speed, 0.05f, phase);
    }

    private int CountSameLiquidNeighbors(Vector2Int cell, WaterKind waterKind)
    {
        int count = 0;
        foreach (Vector2Int direction in CardinalDirections)
        {
            if (waterCells.TryGetValue(cell + direction, out WaterKind neighborKind) && neighborKind == waterKind)
            {
                count++;
            }
        }

        return count;
    }

    private void CreateLightVisual(Transform parent, Vector2Int cell, bool isMixedWithWater)
    {
        Color color = isMixedWithWater ? Color.Lerp(lightVisualColor, mixedVisualColor, 0.35f) : lightVisualColor;
        color.a = GetSpriteAlpha(lightDistances, cell, lightVisualColor.a, lightSpriteFadePerCell, lightSpriteAlphaMultiplier);
        if (useAdditiveLightBlend)
        {
            color.r *= additiveLightStrength;
            color.g *= additiveLightStrength;
            color.b *= additiveLightStrength;
        }

        Vector2Int direction = lightDirections.TryGetValue(cell, out Vector2Int foundDirection) ? foundDirection : Vector2Int.right;
        float visualScale = GetFlowVisualScale();
        GameObject light = CreateSpriteVisual("Light Beam", parent, GetLightBeamSprite(), color, lightBeamWidth * visualScale, lightBeamLength * visualScale, direction);
        ApplyLightMaterial(light.GetComponent<SpriteRenderer>());
        FlowVisualAnimator animator = light.AddComponent<FlowVisualAnimator>();
        animator.Configure(light.GetComponent<SpriteRenderer>(), color, Vector2.zero, lightPulseSpeed, 0.12f, GetCellPhase(cell));
    }

    private float GetFlowVisualScale()
    {
        return Mathf.Max(0.01f, flowVisualSize / 0.64f);
    }

    private GameObject CreateSpriteVisual(string visualName, Transform parent, Sprite sprite, Color color, float width, float height, Vector2Int direction)
    {
        GameObject visual = new GameObject(visualName);
        visual.transform.SetParent(parent);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.Euler(0f, 0f, GetDirectionAngle(direction));

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = flowVisualSortingOrder;

        Vector2 spriteSize = sprite != null ? sprite.bounds.size : Vector2.one;
        float scaleX = spriteSize.x > 0f ? width / spriteSize.x : width;
        float scaleY = spriteSize.y > 0f ? height / spriteSize.y : height;
        visual.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        return visual;
    }

    private void ApplyLightMaterial(SpriteRenderer renderer)
    {
        if (!useAdditiveLightBlend || renderer == null)
        {
            return;
        }

        Material material = GetAdditiveLightMaterial();
        if (material != null)
        {
            renderer.sharedMaterial = material;
        }
    }

    private Material GetAdditiveLightMaterial()
    {
        if (additiveLightMaterial != null)
        {
            return additiveLightMaterial;
        }

        Shader shader = Shader.Find("Bloom Rock Puzzle/Additive Sprite");
        if (shader == null)
        {
            shader = Shader.Find("Legacy Shaders/Particles/Additive");
        }

        if (shader == null)
        {
            shader = Shader.Find("Particles/Additive");
        }

        if (shader == null)
        {
            Debug.LogWarning("Additive light shader was not found. Light beams will use the default sprite material.");
            return null;
        }

        additiveLightMaterial = new Material(shader)
        {
            name = "Generated Additive Light Material"
        };
        additiveLightMaterial.hideFlags = HideFlags.HideAndDontSave;
        return additiveLightMaterial;
    }

    private Sprite GetWaterSprite()
    {
        if (waterFlowSprite != null)
        {
            return waterFlowSprite;
        }

        if (defaultWaterFlowSprite == null)
        {
            defaultWaterFlowSprite = CreateDefaultWaterFlowSprite();
        }

        return defaultWaterFlowSprite;
    }

    private Sprite GetMuddyWaterSprite()
    {
        if (muddyWaterFlowSprite != null)
        {
            return muddyWaterFlowSprite;
        }

        if (defaultMuddyWaterFlowSprite == null)
        {
            defaultMuddyWaterFlowSprite = CreateDefaultMuddyWaterFlowSprite();
        }

        return defaultMuddyWaterFlowSprite;
    }

    private Sprite GetLiquidBodySprite()
    {
        if (liquidBodySprite != null)
        {
            return liquidBodySprite;
        }

        if (defaultLiquidBodySprite == null)
        {
            defaultLiquidBodySprite = CreateDefaultLiquidBodySprite();
        }

        return defaultLiquidBodySprite;
    }

    private Sprite GetLightBeamSprite()
    {
        if (lightBeamSprite != null)
        {
            return lightBeamSprite;
        }

        if (defaultLightBeamSprite == null)
        {
            defaultLightBeamSprite = CreateDefaultLightBeamSprite();
        }

        return defaultLightBeamSprite;
    }

    private static float GetDirectionAngle(Vector2Int direction)
    {
        if (direction == Vector2Int.zero)
        {
            return 0f;
        }

        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
    }

    private static float GetCellPhase(Vector2Int cell)
    {
        int hash = cell.x * 73856093 ^ cell.y * 19349663;
        return Mathf.Abs(hash % 1000) / 1000f;
    }

    private static Sprite CreateDefaultWaterFlowSprite()
    {
        const int size = 64;
        Texture2D texture = CreateTransparentTexture(size, size);
        Color[] pixels = texture.GetPixels();

        for (int y = 0; y < size; y++)
        {
            float vertical = y / (float)(size - 1);
            float centerX = 32f + Mathf.Sin(vertical * Mathf.PI * 2f) * 6f;
            DrawSoftLine(pixels, size, centerX, y, 3.5f, 0.8f);
            DrawSoftLine(pixels, size, centerX - 13f, y, 2f, 0.45f);
            DrawSoftLine(pixels, size, centerX + 14f, y, 2f, 0.35f);
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite CreateDefaultMuddyWaterFlowSprite()
    {
        const int size = 64;
        Texture2D texture = CreateTransparentTexture(size, size);
        Color[] pixels = texture.GetPixels();

        DrawSoftBlob(pixels, size, new Vector2(32f, 32f), 24f, 0.65f);
        DrawSoftBlob(pixels, size, new Vector2(20f, 40f), 10f, 0.35f);
        DrawSoftBlob(pixels, size, new Vector2(45f, 24f), 12f, 0.4f);
        DrawSoftBlob(pixels, size, new Vector2(28f, 18f), 7f, 0.3f);

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite CreateDefaultLiquidBodySprite()
    {
        const int size = 64;
        Texture2D texture = CreateTransparentTexture(size, size);
        Color[] pixels = texture.GetPixels();

        DrawSoftBlob(pixels, size, new Vector2(32f, 32f), 28f, 0.82f);
        DrawSoftBlob(pixels, size, new Vector2(22f, 41f), 17f, 0.35f);
        DrawSoftBlob(pixels, size, new Vector2(44f, 23f), 16f, 0.28f);

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite CreateDefaultLightBeamSprite()
    {
        const int width = 24;
        const int height = 96;
        Texture2D texture = CreateTransparentTexture(width, height);
        Color[] pixels = texture.GetPixels();

        for (int y = 0; y < height; y++)
        {
            float lengthFade = Mathf.Sin((y / (float)(height - 1)) * Mathf.PI);
            for (int x = 0; x < width; x++)
            {
                float centerDistance = Mathf.Abs(x - (width - 1) * 0.5f) / ((width - 1) * 0.5f);
                float alpha = Mathf.Pow(1f - Mathf.Clamp01(centerDistance), 1.7f) * lengthFade;
                pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), height);
    }

    private static Texture2D CreateTransparentTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        texture.SetPixels(pixels);
        return texture;
    }

    private static void DrawSoftLine(Color[] pixels, int size, float centerX, int y, float radius, float alpha)
    {
        int xMin = Mathf.Max(0, Mathf.FloorToInt(centerX - radius * 2f));
        int xMax = Mathf.Min(size - 1, Mathf.CeilToInt(centerX + radius * 2f));

        for (int x = xMin; x <= xMax; x++)
        {
            float distance = Mathf.Abs(x - centerX);
            float lineAlpha = Mathf.Clamp01(1f - distance / radius) * alpha;
            int index = y * size + x;
            pixels[index] = new Color(1f, 1f, 1f, Mathf.Max(pixels[index].a, lineAlpha));
        }
    }

    private static void DrawSoftBlob(Color[] pixels, int size, Vector2 center, float radius, float alpha)
    {
        int xMin = Mathf.Max(0, Mathf.FloorToInt(center.x - radius));
        int xMax = Mathf.Min(size - 1, Mathf.CeilToInt(center.x + radius));
        int yMin = Mathf.Max(0, Mathf.FloorToInt(center.y - radius));
        int yMax = Mathf.Min(size - 1, Mathf.CeilToInt(center.y + radius));

        for (int y = yMin; y <= yMax; y++)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float blobAlpha = Mathf.Clamp01(1f - distance / radius) * alpha;
                int index = y * size + x;
                pixels[index] = new Color(1f, 1f, 1f, Mathf.Max(pixels[index].a, blobAlpha));
            }
        }
    }

    private void ClearFlowVisuals()
    {
        foreach (GameObject visual in flowVisuals)
        {
            if (visual != null)
            {
                Destroy(visual);
            }
        }

        flowVisuals.Clear();
    }

    private void RefreshClearVisual(bool isClear)
    {
        bool showClearTextFallback = isClear && showNextArrowOnClear && string.IsNullOrEmpty(ResolveNextSceneName());
        if (!showClearText && !showClearTextFallback)
        {
            if (clearTextMesh != null)
            {
                clearTextMesh.gameObject.SetActive(false);
            }

            return;
        }

        EnsureClearText();
        clearTextMesh.gameObject.SetActive(isClear);
    }

    private void RefreshNextArrow(bool isClear)
    {
        if (!showNextArrowOnClear || string.IsNullOrEmpty(ResolveNextSceneName()))
        {
            if (nextArrowButton != null)
            {
                nextArrowButton.gameObject.SetActive(false);
            }

            return;
        }

        EnsureNextArrowButton();
        nextArrowButton.gameObject.SetActive(isClear && !isTransitioning);
    }

    private static void SetFlowerBloomStatusUiHidden(bool hidden)
    {
        foreach (FlowerBloomStatusUI statusUi in FindObjectsOfType<FlowerBloomStatusUI>())
        {
            if (statusUi != null)
            {
                statusUi.SetTransitionHidden(hidden);
            }
        }
    }

    private string ResolveNextSceneName()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            return CanLoadScene(nextSceneName) ? nextSceneName : "";
        }

        if (!autoResolveNextSceneName)
        {
            return "";
        }

        string activeSceneName = SceneManager.GetActiveScene().name;
        if (activeSceneName == "Tutorial")
        {
            return CanLoadScene(firstNumberedStageSceneName) ? firstNumberedStageSceneName : "";
        }

        string numberedNextSceneName = GetNextNumberedStageSceneName(activeSceneName);
        if (!string.IsNullOrEmpty(numberedNextSceneName) && CanLoadScene(numberedNextSceneName))
        {
            return numberedNextSceneName;
        }

        if (IsFinalNumberedStageScene(activeSceneName))
        {
            return CanLoadScene(clearSceneName) ? clearSceneName : "";
        }

        int activeBuildIndex = SceneManager.GetActiveScene().buildIndex;
        int nextBuildIndex = activeBuildIndex + 1;
        if (activeBuildIndex >= 0 && nextBuildIndex < SceneManager.sceneCountInBuildSettings)
        {
            string nextBuildScenePath = SceneUtility.GetScenePathByBuildIndex(nextBuildIndex);
            string nextBuildSceneName = System.IO.Path.GetFileNameWithoutExtension(nextBuildScenePath);
            return CanLoadScene(nextBuildSceneName) ? nextBuildSceneName : "";
        }

        return "";
    }

    private string GetNextNumberedStageSceneName(string sceneName)
    {
        if (!TryParseNumberedSceneName(sceneName, out string prefix, out int stageNumber)
            || !TryParseNumberedSceneName(firstNumberedStageSceneName, out string stagePrefix, out _)
            || prefix != stagePrefix)
        {
            return "";
        }

        int nextStageNumber = int.MaxValue;
        string nextStageSceneName = "";
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string buildScenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string buildSceneName = System.IO.Path.GetFileNameWithoutExtension(buildScenePath);
            if (TryParseNumberedSceneName(buildSceneName, out string buildPrefix, out int buildStageNumber)
                && buildPrefix == prefix
                && buildStageNumber > stageNumber
                && buildStageNumber < nextStageNumber)
            {
                nextStageNumber = buildStageNumber;
                nextStageSceneName = buildSceneName;
            }
        }

        return nextStageSceneName;
    }

    private bool IsFinalNumberedStageScene(string sceneName)
    {
        return TryParseNumberedSceneName(sceneName, out string prefix, out _)
            && TryParseNumberedSceneName(firstNumberedStageSceneName, out string stagePrefix, out _)
            && prefix == stagePrefix
            && CanLoadScene(clearSceneName);
    }

    private static bool TryParseNumberedSceneName(string sceneName, out string prefix, out int stageNumber)
    {
        prefix = "";
        stageNumber = 0;
        int numberStartIndex = sceneName.Length;
        while (numberStartIndex > 0 && char.IsDigit(sceneName[numberStartIndex - 1]))
        {
            numberStartIndex--;
        }

        if (numberStartIndex == sceneName.Length)
        {
            return false;
        }

        prefix = sceneName.Substring(0, numberStartIndex);
        string numberText = sceneName.Substring(numberStartIndex);
        return int.TryParse(numberText, out stageNumber);
    }

    private static bool CanLoadScene(string sceneName)
    {
        return !string.IsNullOrEmpty(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName);
    }

    private void EnsureNextArrowButton()
    {
        if (nextArrowButton != null)
        {
            return;
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        EnsureEventSystem();

        GameObject buttonObject = new GameObject("Next Stage Arrow");
        buttonObject.transform.SetParent(canvas.transform, false);
        RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 0.5f);
        buttonRect.anchorMax = new Vector2(1f, 0.5f);
        buttonRect.pivot = new Vector2(1f, 0.5f);
        buttonRect.anchoredPosition = EffectiveNextArrowAnchoredPosition;
        buttonRect.sizeDelta = EffectiveNextArrowSize;

        Image image = buttonObject.AddComponent<Image>();
        image.sprite = EffectiveNextArrowSprite != null ? EffectiveNextArrowSprite : CreateDefaultNextArrowSprite();
        image.color = EffectiveNextArrowColor;
        image.preserveAspect = true;

        nextArrowButton = buttonObject.AddComponent<Button>();
        nextArrowButton.targetGraphic = image;
        nextArrowButton.onClick.AddListener(SeManager.PlayButtonClick);
        nextArrowButton.onClick.AddListener(LoadConfiguredNextSceneWithScroll);

        nextArrowButton.gameObject.SetActive(false);
    }

    private static BloomPuzzleTransitionSettings TransitionSettings
    {
        get
        {
            if (transitionSettings == null)
            {
                transitionSettings = Resources.Load<BloomPuzzleTransitionSettings>(TransitionSettingsResourcePath);
            }

            return transitionSettings;
        }
    }

    private Sprite EffectiveNextArrowSprite
    {
        get
        {
            BloomPuzzleTransitionSettings settings = TransitionSettings;
            return settings != null && settings.NextArrowSprite != null ? settings.NextArrowSprite : nextArrowSprite;
        }
    }

    private Color EffectiveNextArrowColor => TransitionSettings != null ? TransitionSettings.NextArrowColor : nextArrowColor;
    private Vector2 EffectiveNextArrowAnchoredPosition => TransitionSettings != null ? TransitionSettings.NextArrowAnchoredPosition : nextArrowAnchoredPosition;
    private Vector2 EffectiveNextArrowSize => TransitionSettings != null ? TransitionSettings.NextArrowSize : nextArrowSize;

    private static Sprite CreateDefaultNextArrowSprite()
    {
        const int width = 96;
        const int height = 48;
        const int scale = 4;
        Color clear = Color.clear;
        Color ink = Color.white;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = clear;
        }

        void FillRect(int xMin, int yMin, int rectWidth, int rectHeight)
        {
            for (int y = yMin; y < yMin + rectHeight; y++)
            {
                for (int x = xMin; x < xMin + rectWidth; x++)
                {
                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        pixels[y * width + x] = ink;
                    }
                }
            }
        }

        FillRect(0, 12, 64, scale);
        FillRect(0, 32, 64, scale);
        FillRect(0, 12, scale, 24);
        FillRect(16, 20, scale, 8);
        FillRect(32, 20, scale, 8);
        FillRect(48, 20, scale, 8);

        for (int i = 0; i < 28; i += scale)
        {
            FillRect(64 + i, 12 + i / 2, scale, scale);
            FillRect(64 + i, 32 - i / 2, scale, scale);
        }

        FillRect(88, 22, scale, scale);
        FillRect(88, 26, scale, scale);

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private IEnumerator ScrollThenLoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            yield break;
        }

        isTransitioning = true;
        SetFlowerBloomStatusUiHidden(true);
        if (nextArrowButton != null)
        {
            nextArrowButton.gameObject.SetActive(false);
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            QueueIncomingScrollTransition(sceneName);
            SceneManager.LoadScene(sceneName);
            yield break;
        }

        Vector3 start = mainCamera.transform.position;
        Vector3 end = start + Vector3.right * transitionScrollDistance;
        EnsureTransitionBackground(mainCamera, start, end, transitionBackgroundSortingOrder, TransitionBackgroundPlacement.Outgoing);
        SetTransitionBackgroundAlpha(1f);
        float duration = Mathf.Max(0.01f, transitionScrollDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            mainCamera.transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        QueueIncomingScrollTransition(sceneName, mainCamera);
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator PlayIncomingScrollTransitionIfNeeded()
    {
        if (!playIncomingScrollTransition || isTransitioning)
        {
            yield break;
        }

        if (PlayerPrefs.GetString(IncomingTransitionSceneKey, "") != SceneManager.GetActiveScene().name)
        {
            yield break;
        }

        PlayerPrefs.DeleteKey(IncomingTransitionSceneKey);
        PlayerPrefs.Save();

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            yield break;
        }

        isTransitioning = true;
        SetFlowerBloomStatusUiHidden(true);
        Vector3 end = mainCamera.transform.position;
        Vector3 start = end - Vector3.right * transitionScrollDistance;
        EnsureTransitionBackground(mainCamera, start, end, incomingTransitionBackgroundSortingOrder, TransitionBackgroundPlacement.Incoming);
        SetTransitionBackgroundAlpha(1f);
        float duration = Mathf.Max(0.01f, transitionScrollDuration);
        float elapsed = 0f;

        mainCamera.transform.position = start;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            mainCamera.transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        mainCamera.transform.position = end;
        yield return FadeTransitionBackground(1f, 0f);
        ClearTransitionBackground();
        SetFlowerBloomStatusUiHidden(false);
        isTransitioning = false;
    }

    private void EnsureTransitionBackground(Camera targetCamera, Vector3 fromPosition, Vector3 toPosition, int sortingOrder, TransitionBackgroundPlacement placement)
    {
        ClearTransitionBackground();
        transitionBackgroundRenderers.Clear();

        if (transitionBackgroundSprite == null || targetCamera == null || !targetCamera.orthographic)
        {
            return;
        }

        Bounds spriteBounds = transitionBackgroundSprite.bounds;
        if (spriteBounds.size.x <= 0f || spriteBounds.size.y <= 0f)
        {
            return;
        }

        float viewHeight = targetCamera.orthographicSize * 2f;
        float viewWidth = viewHeight * targetCamera.aspect;
        float spriteScale = (viewHeight / spriteBounds.size.y) * Mathf.Max(1f, transitionBackgroundScalePadding);
        float tileWidth = spriteBounds.size.x * spriteScale;
        float minX = Mathf.Min(fromPosition.x, toPosition.x) - viewWidth * 0.5f - tileWidth;
        float maxX = Mathf.Max(fromPosition.x, toPosition.x) + viewWidth * 0.5f + tileWidth;
        float centerY = (fromPosition.y + toPosition.y) * 0.5f;
        float z = Mathf.Max(fromPosition.z, toPosition.z) + Mathf.Abs(targetCamera.transform.position.z);

        transitionBackgroundRoot = new GameObject("Transition Background");

        if (stretchTransitionBackgroundToScrollPath)
        {
            float edgePadding = Mathf.Max(0f, transitionBackgroundOverlap);
            float requiredWidth = Mathf.Abs(toPosition.x - fromPosition.x) + viewWidth + edgePadding;
            float coverScale = Mathf.Max(spriteScale, requiredWidth / spriteBounds.size.x);
            float centerX = (Mathf.Min(fromPosition.x, toPosition.x) + Mathf.Max(fromPosition.x, toPosition.x)) * 0.5f;
            float scale = coverScale;

            if (placement == TransitionBackgroundPlacement.Incoming && TryGetQueuedTransitionBackground(targetCamera.transform.position, out Vector3 queuedPosition, out float queuedScale))
            {
                centerX = queuedPosition.x;
                centerY = queuedPosition.y;
                scale = queuedScale;
            }

            GameObject tile = CreateTransitionBackgroundTile("Transition Background Cover", new Vector3(centerX, centerY, z), sortingOrder);
            tile.transform.localScale = Vector3.one * scale;
            return;
        }

        float step = Mathf.Max(0.01f, tileWidth - Mathf.Max(0f, transitionBackgroundOverlap));

        int tileCount = Mathf.CeilToInt((maxX - minX) / step) + 1;
        for (int i = 0; i < tileCount; i++)
        {
            GameObject tile = CreateTransitionBackgroundTile("Transition Background Tile", new Vector3(minX + tileWidth * 0.5f + step * i, centerY, z), sortingOrder);
            tile.transform.localScale = Vector3.one * spriteScale;
        }
    }

    private GameObject CreateTransitionBackgroundTile(string tileName, Vector3 position, int sortingOrder)
    {
        GameObject tile = new GameObject(tileName);
        tile.transform.SetParent(transitionBackgroundRoot.transform);
        tile.transform.position = position;

        SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
        renderer.sprite = transitionBackgroundSprite;
        renderer.color = transitionBackgroundColor;
        renderer.sortingOrder = sortingOrder;
        transitionBackgroundRenderers.Add(renderer);
        return tile;
    }

    private IEnumerator FadeTransitionBackground(float fromAlpha, float toAlpha)
    {
        if (transitionBackgroundRoot == null || transitionBackgroundRenderers.Count == 0)
        {
            yield break;
        }

        float duration = Mathf.Max(0f, transitionBackgroundFadeDuration);
        if (duration <= 0f)
        {
            SetTransitionBackgroundAlpha(toAlpha);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetTransitionBackgroundAlpha(Mathf.Lerp(fromAlpha, toAlpha, t));
            yield return null;
        }

        SetTransitionBackgroundAlpha(toAlpha);
    }

    private void SetTransitionBackgroundAlpha(float alpha)
    {
        float clampedAlpha = Mathf.Clamp01(alpha);
        foreach (SpriteRenderer renderer in transitionBackgroundRenderers)
        {
            if (renderer == null)
            {
                continue;
            }

            Color color = transitionBackgroundColor;
            color.a *= clampedAlpha;
            renderer.color = color;
        }
    }

    private void ClearTransitionBackground()
    {
        if (transitionBackgroundRoot == null)
        {
            return;
        }

        Destroy(transitionBackgroundRoot);
        transitionBackgroundRoot = null;
        transitionBackgroundRenderers.Clear();
    }

    private enum TransitionBackgroundPlacement
    {
        Outgoing,
        Incoming
    }

    private static void QueueIncomingScrollTransition(string sceneName)
    {
        PlayerPrefs.SetString(IncomingTransitionSceneKey, sceneName);
        PlayerPrefs.Save();
    }

    private void QueueIncomingScrollTransition(string sceneName, Camera targetCamera)
    {
        QueueIncomingScrollTransition(sceneName);

        if (targetCamera == null || transitionBackgroundRenderers.Count == 0 || transitionBackgroundRenderers[0] == null)
        {
            ClearQueuedTransitionBackground();
            return;
        }

        Transform backgroundTransform = transitionBackgroundRenderers[0].transform;
        Vector3 cameraPosition = targetCamera.transform.position;
        PlayerPrefs.SetFloat(IncomingTransitionBackgroundOffsetXKey, backgroundTransform.position.x - cameraPosition.x);
        PlayerPrefs.SetFloat(IncomingTransitionBackgroundOffsetYKey, backgroundTransform.position.y - cameraPosition.y);
        PlayerPrefs.SetFloat(IncomingTransitionBackgroundScaleKey, backgroundTransform.localScale.x);
        PlayerPrefs.Save();
    }

    private static bool TryGetQueuedTransitionBackground(Vector3 cameraPosition, out Vector3 position, out float scale)
    {
        if (!PlayerPrefs.HasKey(IncomingTransitionBackgroundOffsetXKey)
            || !PlayerPrefs.HasKey(IncomingTransitionBackgroundOffsetYKey)
            || !PlayerPrefs.HasKey(IncomingTransitionBackgroundScaleKey))
        {
            position = Vector3.zero;
            scale = 1f;
            return false;
        }

        position = new Vector3(
            cameraPosition.x + PlayerPrefs.GetFloat(IncomingTransitionBackgroundOffsetXKey),
            cameraPosition.y + PlayerPrefs.GetFloat(IncomingTransitionBackgroundOffsetYKey),
            cameraPosition.z);
        scale = PlayerPrefs.GetFloat(IncomingTransitionBackgroundScaleKey);
        ClearQueuedTransitionBackground();
        return true;
    }

    private static void ClearQueuedTransitionBackground()
    {
        PlayerPrefs.DeleteKey(IncomingTransitionBackgroundOffsetXKey);
        PlayerPrefs.DeleteKey(IncomingTransitionBackgroundOffsetYKey);
        PlayerPrefs.DeleteKey(IncomingTransitionBackgroundScaleKey);
    }

    private void EnsureClearText()
    {
        if (clearTextMesh != null)
        {
            return;
        }

        GameObject textObject = new GameObject("Clear Text");
        textObject.transform.SetParent(transform);
        textObject.transform.position = clearTextPosition;
        clearTextMesh = textObject.AddComponent<TextMesh>();
        clearTextMesh.text = clearText;
        clearTextMesh.anchor = TextAnchor.MiddleCenter;
        clearTextMesh.alignment = TextAlignment.Center;
        clearTextMesh.characterSize = 0.5f;
        clearTextMesh.fontSize = 96;
        clearTextMesh.color = clearTextColor;
        clearTextMesh.gameObject.SetActive(false);
    }

    private struct FlowNode
    {
        public readonly Vector2Int Position;
        public readonly WaterKind WaterKind;
        public readonly int Distance;

        public FlowNode(Vector2Int position, WaterKind waterKind, int distance)
        {
            Position = position;
            WaterKind = waterKind;
            Distance = distance;
        }
    }

    private struct LightRay
    {
        public readonly Vector2Int Position;
        public readonly Vector2Int Direction;
        public readonly int Distance;

        public LightRay(Vector2Int position, Vector2Int direction, int distance)
        {
            Position = position;
            Direction = direction;
            Distance = distance;
        }
    }

    private struct LightRayKey
    {
        public readonly Vector2Int Position;
        public readonly Vector2Int Direction;

        public LightRayKey(Vector2Int position, Vector2Int direction)
        {
            Position = position;
            Direction = direction;
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        Gizmos.color = new Color(1f, 0.9f, 0.15f, 0.35f);
        foreach (Vector2Int cell in litCells)
        {
            Gizmos.DrawCube(new Vector3(cell.x, cell.y, -0.2f), Vector3.one * 0.35f);
        }

        Gizmos.color = new Color(0.2f, 0.65f, 1f, 0.35f);
        foreach (KeyValuePair<Vector2Int, WaterKind> waterCell in waterCells)
        {
            Vector2Int cell = waterCell.Key;
            Gizmos.color = waterCell.Value == WaterKind.Muddy ? new Color(0.45f, 0.25f, 0.1f, 0.35f) : new Color(0.2f, 0.65f, 1f, 0.35f);
            Gizmos.DrawCube(new Vector3(cell.x, cell.y, -0.3f), Vector3.one * 0.55f);
        }
    }
}
