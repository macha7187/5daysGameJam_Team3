using UnityEngine;

[CreateAssetMenu(fileName = "BloomPuzzleTransitionSettings", menuName = "Bloom Rock Puzzle/Transition Settings")]
public class BloomPuzzleTransitionSettings : ScriptableObject
{
    [Header("Next Arrow")]
    [SerializeField] private Sprite nextArrowSprite;
    [SerializeField] private Color nextArrowColor = Color.white;
    [SerializeField] private Vector2 nextArrowAnchoredPosition = new Vector2(-36f, 0f);
    [SerializeField] private Vector2 nextArrowSize = new Vector2(180f, 120f);

    public Sprite NextArrowSprite => nextArrowSprite;
    public Color NextArrowColor => nextArrowColor;
    public Vector2 NextArrowAnchoredPosition => nextArrowAnchoredPosition;
    public Vector2 NextArrowSize => nextArrowSize;
}
