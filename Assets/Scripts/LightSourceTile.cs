using UnityEngine;

[AddComponentMenu("Bloom Rock Puzzle/Light Source Tile")]
public class LightSourceTile : GridPiece
{
    [SerializeField] private PuzzleDirection direction = PuzzleDirection.Right;

    public PuzzleDirection Direction => direction;
}
