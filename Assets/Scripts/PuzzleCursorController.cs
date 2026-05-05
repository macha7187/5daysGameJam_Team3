using UnityEngine;

[AddComponentMenu("Bloom Rock Puzzle/Puzzle Cursor Controller")]
public class PuzzleCursorController : GridPiece
{
    [Header("Visual Assets")]
    [SerializeField] private Sprite idleSprite;      
    [SerializeField] private Sprite grabbingSprite;
    [Header("Scale Settings")]
    [SerializeField] private Vector3 idleScale = Vector3.one;          
    [SerializeField] private Vector3 grabbingScale = new Vector3(1.2f, 1.2f, 1.2f); 
    [SerializeField] private BloomPuzzleLevel level;
    [SerializeField] private Color idleColor = new Color(0.65f, 0.9f, 1f, 0.45f);
    [SerializeField] private Color grabbingColor = new Color(1f, 0.35f, 0.3f, 0.75f);

    private PushableRock grabbedRock;
    private SpriteRenderer spriteRenderer;

    protected override void Awake()
    {
        base.Awake();
        spriteRenderer = GetComponent<SpriteRenderer>();
        RefreshVisual();
    }

    private void Start()
    {
        if (level == null)
        {
            level = FindObjectOfType<BloomPuzzleLevel>();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            ToggleGrab();
        }

        Vector2Int direction = ReadMoveInput();
        if (direction != Vector2Int.zero)
        {
            TryMoveCursor(direction);
        }
    }

    private static Vector2Int ReadMoveInput()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            return Vector2Int.up;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            return Vector2Int.down;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            return Vector2Int.left;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            return Vector2Int.right;
        }

        return Vector2Int.zero;
    }

    private void ToggleGrab()
    {
        if (grabbedRock != null)
        {
            grabbedRock = null;
            SeManager.PlayRockDrop();
            RefreshVisual();
            return;
        }

        grabbedRock = level != null ? level.GetRockAt(GridPosition) : null;
        if (grabbedRock != null)
        {
            SeManager.PlayRockLift();
        }

        if (grabbedRock == null)
        {
            bool rotatedLight = level != null && level.TryRotateLightSourceAt(GridPosition);
            if (!rotatedLight)
            {
                level?.TryRotatePrismAt(GridPosition);
            }
        }

        RefreshVisual();
    }

    private void TryMoveCursor(Vector2Int direction)
    {
        if (level == null)
        {
            GridPosition += direction;
            return;
        }

        Vector2Int target = GridPosition + direction;
        if (!level.IsInsideBounds(target) || level.BlocksCursor(target))
        {
            return;
        }

        if (grabbedRock == null)
        {
            GridPosition = target;
            return;
        }

        if (level.TryMoveRock(grabbedRock, direction))
        {
            GridPosition = target;
        }
    }

    private void RefreshVisual()
    {
        if (spriteRenderer == null) return;

        if (grabbedRock != null)
        {
            spriteRenderer.sprite = grabbingSprite;
            spriteRenderer.color = grabbingColor;
            transform.localScale = grabbingScale;
        }
        else
        {
            spriteRenderer.sprite = idleSprite;
            spriteRenderer.color = idleColor;
            transform.localScale = idleScale;
        }
    }
}
