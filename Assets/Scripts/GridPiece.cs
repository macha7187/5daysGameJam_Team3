using UnityEngine;

public abstract class GridPiece : MonoBehaviour
{
    private Vector3 worldOffset;
    private bool hasWorldOffset;

    public Vector2Int GridPosition
    {
        get
        {
            Vector3 position = transform.position;
            return new Vector2Int(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));
        }
        set
        {
            EnsureWorldOffset();
            transform.position = new Vector3(value.x + worldOffset.x, value.y + worldOffset.y, transform.position.z);
        }
    }

    protected virtual void Awake()
    {
        EnsureWorldOffset();
    }

    private void EnsureWorldOffset()
    {
        if (hasWorldOffset)
        {
            return;
        }

        Vector3 position = transform.position;
        worldOffset = new Vector3(position.x - Mathf.Floor(position.x), position.y - Mathf.Floor(position.y), 0f);
        hasWorldOffset = true;
    }
}
