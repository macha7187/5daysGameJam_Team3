using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

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
    [SerializeField] private Color mixedVisualColor = new Color(0.25f, 1f, 0.45f, 0.45f);

    [Header("Clear Visual")]
    [SerializeField] private bool showClearText = true;
    [SerializeField] private string clearText = "CLEAR!";
    [SerializeField] private Vector3 clearTextPosition = new Vector3(0f, 3f, -0.5f);
    [SerializeField] private Color clearTextColor = new Color(1f, 0.95f, 0.25f, 1f);

    private readonly HashSet<Vector2Int> litCells = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> waterCells = new HashSet<Vector2Int>();
    private readonly Dictionary<Vector2Int, int> lightDistances = new Dictionary<Vector2Int, int>();
    private readonly Dictionary<Vector2Int, int> waterDistances = new Dictionary<Vector2Int, int>();
    private readonly List<GameObject> flowVisuals = new List<GameObject>();
    private Transform flowVisualRoot;
    private TextMesh clearTextMesh;
    private bool wasCleared;

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

        foreach (LightSourceTile source in FindObjectsOfType<LightSourceTile>())
        {
            Vector2Int direction = ToVector(source.Direction);
            Vector2Int cursor = source.GridPosition + direction;
            int distance = 1;

            while (IsInsideBounds(cursor))
            {
                if (BlocksLight(cursor))
                {
                    break;
                }

                litCells.Add(cursor);
                RecordNearestDistance(lightDistances, cursor, distance);
                cursor += direction;
                distance++;
            }
        }
    }

    private void RebuildWater()
    {
        waterCells.Clear();
        waterDistances.Clear();
        Queue<FlowNode> frontier = new Queue<FlowNode>();

        foreach (WaterSourceTile source in FindObjectsOfType<WaterSourceTile>())
        {
            waterCells.Add(source.GridPosition);
            waterDistances[source.GridPosition] = 0;
            frontier.Enqueue(new FlowNode(source.GridPosition, 0));
        }

        while (frontier.Count > 0)
        {
            FlowNode current = frontier.Dequeue();

            foreach (Vector2Int direction in CardinalDirections)
            {
                Vector2Int next = current.Position + direction;
                if (waterCells.Count >= maxWaterCells)
                {
                    return;
                }

                if (!IsInsideBounds(next) || waterCells.Contains(next) || BlocksWater(next))
                {
                    continue;
                }

                int nextDistance = current.Distance + 1;
                waterCells.Add(next);
                waterDistances[next] = nextDistance;
                frontier.Enqueue(new FlowNode(next, nextDistance));
            }
        }
    }

    private void RefreshFlowers()
    {
        FlowerTile[] flowers = FindObjectsOfType<FlowerTile>();
        bool allBlooming = flowers.Length > 0;

        foreach (FlowerTile flower in flowers)
        {
            bool hasWater = waterNeedsAdjacentFlower ? HasAdjacentWater(flower.GridPosition) : waterCells.Contains(flower.GridPosition);
            bool isLit = litCells.Contains(flower.GridPosition);
            flower.SetConditions(isLit, hasWater);
            allBlooming &= flower.IsBlooming;
        }

        if (allBlooming && !wasCleared)
        {
            wasCleared = true;
            Debug.Log("Stage clear: all flowers are blooming.");
            RefreshClearVisual(true);
            onLevelCleared?.Invoke();
        }
        else if (!allBlooming)
        {
            wasCleared = false;
            RefreshClearVisual(false);
        }
    }

    private bool HasAdjacentWater(Vector2Int position)
    {
        foreach (Vector2Int direction in CardinalDirections)
        {
            if (waterCells.Contains(position + direction))
            {
                return true;
            }
        }

        return false;
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

    private bool BlocksWater(Vector2Int position)
    {
        return GetRockAt(position) != null
            || IsWallAt(position)
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
            || GetLightSourceAt(position) != null;
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

        HashSet<Vector2Int> visualCells = new HashSet<Vector2Int>(waterCells);
        visualCells.UnionWith(litCells);

        foreach (Vector2Int cell in visualCells)
        {
            if ((waterCells.Contains(cell) && BlocksWater(cell)) || (litCells.Contains(cell) && BlocksLight(cell)))
            {
                continue;
            }

            bool hasWater = waterCells.Contains(cell);
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
            Color color = waterVisualColor;
            color.a = GetDistanceAlpha(waterDistances, cell, waterVisualColor.a, waterFadePerCell);
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
        if (!showClearText)
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
        public readonly int Distance;

        public FlowNode(Vector2Int position, int distance)
        {
            Position = position;
            Distance = distance;
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
        foreach (Vector2Int cell in waterCells)
        {
            Gizmos.DrawCube(new Vector3(cell.x, cell.y, -0.3f), Vector3.one * 0.55f);
        }
    }
}
