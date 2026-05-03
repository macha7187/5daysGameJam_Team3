using UnityEngine;

[AddComponentMenu("Bloom Rock Puzzle/Flower Tile")]
public class FlowerTile : GridPiece
{
    [SerializeField] private Color dormantColor = new Color(0.6f, 0.25f, 0.55f);
    [SerializeField] private Color bloomingColor = new Color(1f, 0.55f, 0.8f);

    public bool IsLit { get; private set; }
    public bool HasAdjacentWater { get; private set; }
    public bool IsBlooming { get; private set; }

    private Renderer cachedRenderer;

    protected override void Awake()
    {
        base.Awake();
        cachedRenderer = GetComponent<Renderer>();
        RefreshVisual();
    }

    public void SetConditions(bool isLit, bool hasAdjacentWater)
    {
        IsLit = isLit;
        HasAdjacentWater = hasAdjacentWater;
        IsBlooming = IsLit && HasAdjacentWater;
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (cachedRenderer == null)
        {
            cachedRenderer = GetComponent<Renderer>();
        }

        if (cachedRenderer != null)
        {
            cachedRenderer.material.color = IsBlooming ? bloomingColor : dormantColor;
        }
    }
}
