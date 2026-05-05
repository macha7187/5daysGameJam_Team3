using System.Collections.Generic;
using System.Collections;
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
    [SerializeField] private float waterFadePerCell = 0.65f;
    [SerializeField] private float lightFadePerCell = 0.2f;
    [SerializeField] private Color lightVisualColor = new Color(1f, 0.92f, 0.1f, 0.45f);
    [SerializeField] private Color waterVisualColor = new Color(0.1f, 0.55f, 1f, 0.7f);
    [SerializeField] private Color muddyWaterVisualColor = new Color(0.45f, 0.28f, 0.12f, 0.7f);
    [SerializeField] private Color mixedVisualColor = new Color(0.25f, 1f, 0.45f, 0.45f);

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
    [SerializeField] private bool stretchTransitionBackgroundToScrollPath = false;
    [SerializeField] private float transitionBackgroundOverlap = 1f;
    [SerializeField] private float transitionBackgroundScalePadding = 1.08f;
    [SerializeField] private float transitionBackgroundFadeDuration = 0.12f;
    [SerializeField] private Color nextArrowColor = new Color(0.62f, 0.55f, 0.82f, 1f);

    private readonly HashSet<Vector2Int> litCells = new HashSet<Vector2Int>();
    private readonly Dictionary<Vector2Int, WaterKind> waterCells = new Dictionary<Vector2Int, WaterKind>();
    private readonly Dictionary<Vector2Int, int> lightDistances = new Dictionary<Vector2Int, int>();
    private readonly Dictionary<Vector2Int, int> waterDistances = new Dictionary<Vector2Int, int>();
    private readonly List<GameObject> flowVisuals = new List<GameObject>();
    private Transform flowVisualRoot;
    private TextMesh clearTextMesh;
    private Button nextArrowButton;
    private GameObject transitionBackgroundRoot;
    private readonly List<SpriteRenderer> transitionBackgroundRenderers = new List<SpriteRenderer>();
    private bool isTransitioning;
    private bool wasCleared;
    private bool hasBuiltWaterOnce;

    private const string IncomingTransitionSceneKey = "BloomPuzzleLevel.IncomingTransitionScene";

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
        Queue<FlowNode> frontier = new Queue<FlowNode>();

        foreach (WaterSourceTile source in FindObjectsOfType<WaterSourceTile>())
        {
            RecordWater(source.GridPosition, source.WaterKind);
            waterDistances[source.GridPosition] = 0;
            frontier.Enqueue(new FlowNode(source.GridPosition, source.WaterKind, 0));
        }

        while (frontier.Count > 0)
        {
            FlowNode current = frontier.Dequeue();

            foreach (Vector2Int direction in CardinalDirections)
            {
                Vector2Int next = current.Position + direction;
                if (waterCells.Count >= maxWaterCells)
                {
                    frontier.Clear();
                    break;
                }

                if (!IsInsideBounds(next) || waterCells.ContainsKey(next) || BlocksWater(next))
                {
                    continue;
                }

                int nextDistance = current.Distance + 1;
                RecordWater(next, current.WaterKind);
                waterDistances[next] = nextDistance;
                frontier.Enqueue(new FlowNode(next, current.WaterKind, nextDistance));
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

        foreach (FlowerTile flower in flowers)
        {
            bool hasCleanWater = waterNeedsAdjacentFlower ? HasAdjacentWater(flower.GridPosition, WaterKind.Clean) : HasWater(flower.GridPosition, WaterKind.Clean);
            bool hasMuddyWater = waterNeedsAdjacentFlower ? HasAdjacentWater(flower.GridPosition, WaterKind.Muddy) : HasWater(flower.GridPosition, WaterKind.Muddy);
            bool isLit = litCells.Contains(flower.GridPosition);
            flower.SetConditions(isLit, hasCleanWater, hasMuddyWater);
            allBlooming &= flower.IsBlooming;
        }

        if (allBlooming && !wasCleared)
        {
            wasCleared = true;
            Debug.Log("Stage clear: all flowers are blooming.");
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

    private void RecordWater(Vector2Int position, WaterKind waterKind)
    {
        waterCells[position] = waterKind;
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
            Color color = GetFlowVisualColor(cell, hasWater, hasLight);
            flowVisuals.Add(CreateFlowVisual(cell, color));
        }
    }

    private Color GetFlowVisualColor(Vector2Int cell, bool hasWater, bool hasLight)
    {
        if (hasWater && hasLight)
        {
            Color color = mixedVisualColor;
            color.a = Mathf.Max(GetDistanceAlpha(waterDistances, cell, waterVisualColor.a, waterFadePerCell), GetDistanceAlpha(lightDistances, cell, lightVisualColor.a, lightFadePerCell));
            return color;
        }

        if (hasWater)
        {
            Color color = waterCells.TryGetValue(cell, out WaterKind waterKind) && waterKind == WaterKind.Muddy ? muddyWaterVisualColor : waterVisualColor;
            color.a = GetDistanceAlpha(waterDistances, cell, color.a, waterFadePerCell);
            return color;
        }

        Color lightColor = lightVisualColor;
        lightColor.a = GetDistanceAlpha(lightDistances, cell, lightVisualColor.a, lightFadePerCell);
        return lightColor;
    }

    private float GetDistanceAlpha(Dictionary<Vector2Int, int> distances, Vector2Int cell, float baseAlpha, float fadePerCell)
    {
        if (!distances.TryGetValue(cell, out int distance))
        {
            return baseAlpha;
        }

        return Mathf.Max(minimumFlowAlpha, baseAlpha * Mathf.Pow(1f - Mathf.Clamp01(fadePerCell), distance));
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

    private GameObject CreateFlowVisual(Vector2Int cell, Color color)
    {
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
        visual.name = "Flow Visual";
        visual.transform.SetParent(flowVisualRoot);
        visual.transform.position = new Vector3(cell.x + 0.5f, cell.y + 0.5f, flowVisualZ);
        visual.transform.localScale = Vector3.one * flowVisualSize;

        Collider collider = visual.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = CreateTransparentMaterial(color);
        }

        return visual;
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

        string numberedNextSceneName = GetNextNumberedSceneName(activeSceneName);
        if (!string.IsNullOrEmpty(numberedNextSceneName) && CanLoadScene(numberedNextSceneName))
        {
            return numberedNextSceneName;
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

    private static string GetNextNumberedSceneName(string sceneName)
    {
        int numberStartIndex = sceneName.Length;
        while (numberStartIndex > 0 && char.IsDigit(sceneName[numberStartIndex - 1]))
        {
            numberStartIndex--;
        }

        if (numberStartIndex == sceneName.Length)
        {
            return "";
        }

        string prefix = sceneName.Substring(0, numberStartIndex);
        string numberText = sceneName.Substring(numberStartIndex);
        if (!int.TryParse(numberText, out int stageNumber))
        {
            return "";
        }

        return prefix + (stageNumber + 1);
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
        buttonRect.anchoredPosition = nextArrowAnchoredPosition;
        buttonRect.sizeDelta = nextArrowSize;

        Image image = buttonObject.AddComponent<Image>();
        image.sprite = nextArrowSprite != null ? nextArrowSprite : CreateDefaultNextArrowSprite();
        image.color = nextArrowColor;
        image.preserveAspect = true;

        nextArrowButton = buttonObject.AddComponent<Button>();
        nextArrowButton.targetGraphic = image;
        nextArrowButton.onClick.AddListener(SeManager.PlayButtonClick);
        nextArrowButton.onClick.AddListener(LoadConfiguredNextSceneWithScroll);

        nextArrowButton.gameObject.SetActive(false);
    }

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
        EnsureTransitionBackground(mainCamera, start, end, transitionBackgroundSortingOrder);
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

        QueueIncomingScrollTransition(sceneName);
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
        Vector3 end = mainCamera.transform.position;
        Vector3 start = end - Vector3.right * transitionScrollDistance;
        EnsureTransitionBackground(mainCamera, start, end, incomingTransitionBackgroundSortingOrder);
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
        isTransitioning = false;
    }

    private void EnsureTransitionBackground(Camera targetCamera, Vector3 fromPosition, Vector3 toPosition, int sortingOrder)
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
            GameObject tile = CreateTransitionBackgroundTile("Transition Background Stretch", new Vector3((minX + maxX) * 0.5f, centerY, z), sortingOrder);
            tile.transform.localScale = new Vector3((maxX - minX) / spriteBounds.size.x, viewHeight / spriteBounds.size.y, 1f);
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

    private static void QueueIncomingScrollTransition(string sceneName)
    {
        PlayerPrefs.SetString(IncomingTransitionSceneKey, sceneName);
        PlayerPrefs.Save();
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

    private static Material CreateTransparentMaterial(Color color)
    {
        Shader shader = Shader.Find("Sprites/Default");
        Material material = new Material(shader);
        material.color = color;
        return material;
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
