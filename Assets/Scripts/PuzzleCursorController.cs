using UnityEngine;

[AddComponentMenu("Bloom Rock Puzzle/Puzzle Cursor Controller")]
public class PuzzleCursorController : GridPiece
{
    [SerializeField] private BloomPuzzleLevel level;
    [SerializeField] private Color idleColor = new Color(0.65f, 0.9f, 1f, 0.45f);
    [SerializeField] private Color grabbingColor = new Color(1f, 0.35f, 0.3f, 0.75f);

    private PushableRock grabbedRock;
    private Renderer cachedRenderer;

    protected override void Awake()
    {
        base.Awake();
        cachedRenderer = GetComponent<Renderer>();
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
            RefreshVisual();
            return;
        }

        grabbedRock = level != null ? level.GetRockAt(GridPosition) : null;
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
        if (cachedRenderer == null)
        {
            cachedRenderer = GetComponent<Renderer>();
        }

        if (cachedRenderer != null)
        {
            cachedRenderer.material.color = grabbedRock != null ? grabbingColor : idleColor;
        }
    }
}
