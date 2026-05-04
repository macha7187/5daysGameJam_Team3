using UnityEngine;
using UnityEngine.Serialization;

[AddComponentMenu("Bloom Rock Puzzle/Light Source Tile")]
public class LightSourceTile : GridPiece
{
    [Header("Initial Settings")]
    [FormerlySerializedAs("direction")]
    [SerializeField] private PuzzleDirection initialDirection = PuzzleDirection.Right;

    private PuzzleDirection currentDirection;

    public PuzzleDirection Direction => currentDirection;

    protected override void Awake()
    {
        base.Awake();
        currentDirection = initialDirection;
        RefreshRotation();
    }

    private void Start()
    {
        RefreshRotation();
    }

    private void OnValidate()
    {
        currentDirection = initialDirection;
        RefreshRotation();
    }

    public void RotateClockwise()
    {
        currentDirection = GetClockwiseDirection(currentDirection);
        RefreshRotation();
    }

    private void RefreshRotation()
    {
        transform.rotation = Quaternion.Euler(0f, 0f, GetZRotation(currentDirection));
    }

    private static PuzzleDirection GetClockwiseDirection(PuzzleDirection currentDirection)
    {
        switch (currentDirection)
        {
            case PuzzleDirection.Up:
                return PuzzleDirection.Right;
            case PuzzleDirection.Right:
                return PuzzleDirection.Down;
            case PuzzleDirection.Down:
                return PuzzleDirection.Left;
            default:
                return PuzzleDirection.Up;
        }
    }

    private static float GetZRotation(PuzzleDirection currentDirection)
    {
        switch (currentDirection)
        {
            case PuzzleDirection.Up:
                return 90f;
            case PuzzleDirection.Down:
                return -90f;
            case PuzzleDirection.Left:
                return 180f;
            default:
                return 0f;
        }
    }
}
