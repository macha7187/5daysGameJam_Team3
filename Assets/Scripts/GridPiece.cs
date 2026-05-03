using UnityEngine;

public abstract class GridPiece : MonoBehaviour
{
    [SerializeField] private Vector2Int gridPosition;

    public Vector2Int GridPosition
    {
        get => gridPosition;
        set
        {
            gridPosition = value;
            transform.position = new Vector3(value.x, value.y, transform.position.z);
        }
    }

    protected virtual void Awake()
    {
        SnapToGrid();
    }

    protected virtual void OnValidate()
    {
        SnapToGrid();
    }

    public void SnapToGrid()
    {
        gridPosition = Vector2Int.RoundToInt(transform.position);
        transform.position = new Vector3(gridPosition.x, gridPosition.y, transform.position.z);
    }
}
