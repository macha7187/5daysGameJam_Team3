using UnityEngine;

[AddComponentMenu("Bloom Rock Puzzle/Flower Tile")]
public class FlowerTile : GridPiece
{
    private enum LightRequirement
    {
        Required,
        Forbidden,
        Ignored
    }

    [Header("Sprites")]
    [SerializeField] private Sprite dormantSprite;
    [SerializeField] private Sprite bloomingSprite;

    [Header("Bloom Conditions")]
    [SerializeField] private LightRequirement lightRequirement = LightRequirement.Required;

    [Header("Colors")]
    [SerializeField] private Color dormantColor = new Color(0.6f, 0.25f, 0.55f);
    [SerializeField] private Color bloomingColor = new Color(1f, 0.55f, 0.8f);
    [SerializeField] private bool tintSprite = true;

    public bool IsLit { get; private set; }
    public bool HasAdjacentWater { get; private set; }
    public bool IsBlooming { get; private set; }

    private Renderer cachedRenderer;
    private SpriteRenderer cachedSpriteRenderer;

    protected override void Awake()
    {
        base.Awake();
        cachedRenderer = GetComponent<Renderer>();
        cachedSpriteRenderer = GetComponent<SpriteRenderer>();
        RefreshVisual();
    }

    public void SetConditions(bool isLit, bool hasAdjacentWater)
    {
        IsLit = isLit;
        HasAdjacentWater = hasAdjacentWater;
        IsBlooming = HasAdjacentWater && IsLightConditionSatisfied();
        RefreshVisual();
    }

    private bool IsLightConditionSatisfied()
    {
        switch (lightRequirement)
        {
            case LightRequirement.Forbidden:
                return !IsLit;
            case LightRequirement.Ignored:
                return true;
            default:
                return IsLit;
        }
    }

    private void RefreshVisual()
    {
        if (cachedRenderer == null)
        {
            cachedRenderer = GetComponent<Renderer>();
        }

        if (cachedSpriteRenderer == null)
        {
            cachedSpriteRenderer = GetComponent<SpriteRenderer>();
        }

        Color color = IsBlooming ? bloomingColor : dormantColor;
        Sprite sprite = IsBlooming ? bloomingSprite : dormantSprite;

        if (cachedRenderer != null)
        {
            cachedRenderer.material.color = color;
        }

        if (cachedSpriteRenderer != null)
        {
            if (sprite != null)
            {
                cachedSpriteRenderer.sprite = sprite;
            }

            cachedSpriteRenderer.color = tintSprite ? color : Color.white;
        }
    }
}
