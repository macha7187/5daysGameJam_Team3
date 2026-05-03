using UnityEngine;

public sealed class LightSourceTile : GridPiece
{
    [SerializeField] private PuzzleDirection direction = PuzzleDirection.Right;

    public PuzzleDirection Direction => direction;
}
