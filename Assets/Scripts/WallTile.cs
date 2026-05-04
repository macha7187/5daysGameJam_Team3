using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Bloom Rock Puzzle/Wall Tile")]
public class WallTile : GridPiece
{
    [SerializeField] private bool includeSelf = false;
    [SerializeField] private bool includeChildren = true;

    public bool ContainsCell(Vector2Int position)
    {
        foreach (Vector2Int cell in GetOccupiedCells())
        {
            if (cell == position)
            {
                return true;
            }
        }

        return false;
    }

    public IEnumerable<Vector2Int> GetOccupiedCells()
    {
        if (includeSelf || transform.childCount == 0)
        {
            yield return GridPosition;
        }

        if (!includeChildren)
        {
            yield break;
        }

        foreach (Transform child in transform)
        {
            foreach (Vector2Int childCell in GetChildCells(child))
            {
                yield return childCell;
            }
        }
    }

    private static IEnumerable<Vector2Int> GetChildCells(Transform current)
    {
        yield return WorldToGridPosition(current.position);

        foreach (Transform child in current)
        {
            foreach (Vector2Int childCell in GetChildCells(child))
            {
                yield return childCell;
            }
        }
    }

    private static Vector2Int WorldToGridPosition(Vector3 position)
    {
        return new Vector2Int(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));
    }
}
