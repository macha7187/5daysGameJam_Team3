using UnityEngine;

[AddComponentMenu("Bloom Rock Puzzle/Water Source Tile")]
public class WaterSourceTile : GridPiece
{
    [SerializeField] private WaterKind waterKind = WaterKind.Clean;

    public WaterKind WaterKind => waterKind;
}
