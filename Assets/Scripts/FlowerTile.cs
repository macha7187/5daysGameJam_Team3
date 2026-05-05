using UnityEngine;

[AddComponentMenu("Bloom Rock Puzzle/Flower Tile")]
public class FlowerTile : GridPiece
{
    public enum FlowerKind
    {
        Normal,
        Hydrangea,
        Lotus,
        Other
    }

    private enum LightRequirement
    {
        Required,
        Forbidden,
        Ignored
    }

    private enum WaterRequirement
    {
        CleanOnly,
        MuddyOnly,
        AnyWater
    }

    [Header("Sprites")]
    [SerializeField] private Sprite dormantSprite;
    [SerializeField] private Sprite bloomingSprite;

    [Header("Type")]
    [SerializeField] private FlowerKind flowerKind = FlowerKind.Normal;

    [Header("Bloom Conditions")]
    [SerializeField] private LightRequirement lightRequirement = LightRequirement.Required;
    [SerializeField] private WaterRequirement waterRequirement = WaterRequirement.CleanOnly;
    [SerializeField] private bool failsWhenAdjacentMuddyWater = false;

    [Header("Colors")]
    [SerializeField] private Color dormantColor = new Color(0.6f, 0.25f, 0.55f);
    [SerializeField] private Color bloomingColor = new Color(1f, 0.55f, 0.8f);
    [SerializeField] private bool tintSprite = true;

    public bool IsLit { get; private set; }
    public bool HasAdjacentWater { get; private set; }
    public bool HasAdjacentCleanWater { get; private set; }
    public bool HasAdjacentMuddyWater { get; private set; }
    public bool IsBlooming { get; private set; }
    public FlowerKind Kind => flowerKind;

    private Renderer cachedRenderer;
    private SpriteRenderer cachedSpriteRenderer;

    protected override void Awake()
    {
        base.Awake();
        cachedRenderer = GetComponent<Renderer>();
        cachedSpriteRenderer = GetComponent<SpriteRenderer>();
        RefreshVisual();
    }

    public void SetConditions(bool isLit, bool hasAdjacentCleanWater, bool hasAdjacentMuddyWater)
    {
        IsLit = isLit;
        HasAdjacentCleanWater = hasAdjacentCleanWater;
        HasAdjacentMuddyWater = hasAdjacentMuddyWater;
        HasAdjacentWater = HasAdjacentCleanWater || HasAdjacentMuddyWater;
        IsBlooming = IsWaterConditionSatisfied() && IsLightConditionSatisfied();
        RefreshVisual();
    }

    public void SetConditions(bool isLit, bool hasAdjacentWater)
    {
        SetConditions(isLit, hasAdjacentWater, false);
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

    private bool IsWaterConditionSatisfied()
    {
        if (failsWhenAdjacentMuddyWater && HasAdjacentMuddyWater)
        {
            return false;
        }

        switch (waterRequirement)
        {
            case WaterRequirement.MuddyOnly:
                return HasAdjacentMuddyWater;
            case WaterRequirement.AnyWater:
                return HasAdjacentWater;
            default:
                return HasAdjacentCleanWater;
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
