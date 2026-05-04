using UnityEngine;

[AddComponentMenu("Bloom Rock Puzzle/Light Source Tile")]
public class LightSourceTile : GridPiece
{
    [SerializeField] private PuzzleDirection direction = PuzzleDirection.Right;

    public PuzzleDirection Direction => direction;

    private void Start()
    {
        RefreshRotation();
    }

    private void OnValidate()
    {
        RefreshRotation();
    }

    public void RotateClockwise()
    {
        direction = GetClockwiseDirection(direction);
        RefreshRotation();
    }

    private void RefreshRotation()
    {
        transform.rotation = Quaternion.Euler(0f, 0f, GetZRotation(direction));
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
