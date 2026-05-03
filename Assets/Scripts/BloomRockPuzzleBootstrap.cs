using UnityEngine;

public static class BloomRockPuzzleBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateSampleLevelIfSceneIsEmpty()
    {
        if (Object.FindObjectOfType<BloomPuzzleLevel>() != null)
        {
            return;
        }

        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = new Color(0.08f, 0.12f, 0.16f);
        }

        GameObject levelObject = new GameObject("Bloom Rock Puzzle Prototype Level");
        levelObject.AddComponent<BloomPuzzleLevel>();

        PuzzleCursorController cursor = CreatePiece<PuzzleCursorController>("Cursor", new Vector2Int(-3, 0), new Color(0.65f, 0.9f, 1f, 0.45f), 1.1f);
        cursor.transform.position = new Vector3(cursor.GridPosition.x, cursor.GridPosition.y, -0.2f);
        CreatePiece<PushableRock>("Rock", new Vector2Int(-1, 0), new Color(0.45f, 0.42f, 0.38f), 0.92f);
        CreatePiece<PushableRock>("Rock", new Vector2Int(1, -1), new Color(0.45f, 0.42f, 0.38f), 0.92f);
        CreatePiece<FlowerTile>("Flower", new Vector2Int(2, 0), new Color(0.6f, 0.25f, 0.55f), 0.72f);
        CreatePiece<LightSourceTile>("Light Source", new Vector2Int(-4, 0), new Color(1f, 0.82f, 0.2f), 0.76f);
        CreatePiece<WaterSourceTile>("Water Source", new Vector2Int(2, -2), new Color(0.2f, 0.65f, 1f), 0.76f);

        Debug.Log("Created a playable Bloom Rock Puzzle prototype. Move the cursor with WASD or arrow keys, and grab rocks with Space or Enter.");
    }

    private static T CreatePiece<T>(string objectName, Vector2Int position, Color color, float scale) where T : GridPiece
    {
        GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Quad);
        piece.name = objectName;
        piece.transform.localScale = Vector3.one * scale;
        piece.transform.position = new Vector3(position.x, position.y, 0f);

        Renderer renderer = piece.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }

        return piece.AddComponent<T>();
    }
}
