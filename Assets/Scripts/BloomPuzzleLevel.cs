using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public sealed class BloomPuzzleLevel : MonoBehaviour
{
    [Header("Board")]
    [SerializeField] private bool useManualBounds = false;
    [SerializeField] private Vector2Int boundsMin = new Vector2Int(-4, -3);
    [SerializeField] private Vector2Int boundsMax = new Vector2Int(4, 3);

    [Header("Rules")]
    [SerializeField] private bool waterNeedsAdjacentFlower = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onLevelCleared = new UnityEvent();

    private readonly HashSet<Vector2Int> litCells = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> waterCells = new HashSet<Vector2Int>();
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

    public bool TryMoveRock(PushableRock rock, Vector2Int direction)
    {
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

    public void RefreshAll()
    {
        foreach (GridPiece piece in FindObjectsOfType<GridPiece>())
        {
            piece.SnapToGrid();
        }

        RebuildLight();
        RebuildWater();
        RefreshFlowers();
    }

    private void RebuildLight()
    {
        litCells.Clear();

        foreach (LightSourceTile source in FindObjectsOfType<LightSourceTile>())
        {
            Vector2Int direction = ToVector(source.Direction);
            Vector2Int cursor = source.GridPosition + direction;

            while (IsInsideBounds(cursor))
            {
                if (GetRockAt(cursor) != null)
                {
                    break;
                }

                litCells.Add(cursor);
                cursor += direction;
            }
        }
    }

    private void RebuildWater()
    {
        waterCells.Clear();
        Queue<Vector2Int> frontier = new Queue<Vector2Int>();

        foreach (WaterSourceTile source in FindObjectsOfType<WaterSourceTile>())
        {
            waterCells.Add(source.GridPosition);
            frontier.Enqueue(source.GridPosition);
        }

        while (frontier.Count > 0)
        {
            Vector2Int current = frontier.Dequeue();

            foreach (Vector2Int direction in CardinalDirections)
            {
                Vector2Int next = current + direction;

                if (!IsInsideBounds(next) || waterCells.Contains(next) || GetRockAt(next) != null)
                {
                    continue;
                }

                waterCells.Add(next);
                frontier.Enqueue(next);
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
            onLevelCleared?.Invoke();
        }
        else if (!allBlooming)
        {
            wasCleared = false;
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

        if (GetRockAt(position) != null)
        {
            return false;
        }

        return true;
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
