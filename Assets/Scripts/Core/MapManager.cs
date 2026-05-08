using UnityEngine;

/// <summary>
/// Manages the game map: boundaries, obstacles, and decorations.
/// Creates boundary EdgeCollider2D walls, spawns obstacle objects with colliders,
/// and places visual-only decorations. Obstacles block the player and projectiles.
/// Enemies (kinematic) may clip through; Ghost explicitly ignores obstacles.
/// </summary>
public class MapManager : MonoBehaviour
{
    [Header("Map Size")]
    [SerializeField] private float _mapHalfSize = 100f;

    [Header("Obstacle Sprites")]
    [SerializeField] private Sprite _treeSprite;
    [SerializeField] private Sprite _rockSprite;
    [SerializeField] private Sprite _wallSprite;

    [Header("Obstacle Counts")]
    [SerializeField] private int _treeCount = 50;
    [SerializeField] private int _rockCount = 35;
    [SerializeField] private int _wallCount = 12;
    [SerializeField] private float _obstacleClearRadius = 10f;

    [Header("Fence")]
    [SerializeField] private Sprite _fenceSprite;
    [SerializeField] private float _fenceSpacing = 4f;

    [Header("Ground Material")]
    [SerializeField] private Material _groundMaterial;

    public float MapHalfSize => _mapHalfSize;

    private void Start()
    {
        CreateBoundaries();
        CreateGround();
        CreateObstacles();
        CreateFence();
        CreateDecorations();
    }

    // ── Boundaries ──────────────────────────────────────────────

    private void CreateBoundaries()
    {
        float h = _mapHalfSize;

        // Parent for all boundary objects
        var boundaryParent = new GameObject("Boundaries");
        boundaryParent.transform.SetParent(transform);

        // Four walls: Top, Bottom, Left, Right
        CreateBoundaryWall(boundaryParent, "Wall_Top", new Vector2(-h, h), new Vector2(h, h));
        CreateBoundaryWall(boundaryParent, "Wall_Bottom", new Vector2(-h, -h), new Vector2(h, -h));
        CreateBoundaryWall(boundaryParent, "Wall_Left", new Vector2(-h, -h), new Vector2(-h, h));
        CreateBoundaryWall(boundaryParent, "Wall_Right", new Vector2(h, -h), new Vector2(h, h));
    }

    private void CreateBoundaryWall(GameObject parent, string name, Vector2 pointA, Vector2 pointB)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent.transform);
        var ec = obj.AddComponent<EdgeCollider2D>();
        ec.points = new Vector2[] { pointA, pointB };
    }

    // ── Ground ─────────────────────────────────────────────────

    private void CreateGround()
    {
        // Find or create ground quad
        var existingGround = GameObject.Find("Ground");
        if (existingGround != null)
        {
            // Apply material to existing ground
            var mr = existingGround.GetComponent<MeshRenderer>();
            if (mr != null && _groundMaterial != null)
                mr.sharedMaterial = _groundMaterial;
            return;
        }

        // Create a new ground quad
        var groundObj = new GameObject("Ground");
        groundObj.transform.SetParent(transform);
        groundObj.transform.position = new Vector3(0f, 0f, 1f);

        var mf = groundObj.AddComponent<MeshFilter>();
        var groundRenderer = groundObj.AddComponent<MeshRenderer>();

        // Create a simple quad mesh
        float size = _mapHalfSize * 2f;
        var mesh = new Mesh
        {
            vertices = new Vector3[]
            {
                new Vector3(-size / 2f, -size / 2f, 0f),
                new Vector3(size / 2f, -size / 2f, 0f),
                new Vector3(-size / 2f, size / 2f, 0f),
                new Vector3(size / 2f, size / 2f, 0f)
            },
            triangles = new int[] { 0, 2, 1, 2, 3, 1 },
            uv = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(size / 10f, 0f),
                new Vector2(0f, size / 10f),
                new Vector2(size / 10f, size / 10f)
            }
        };
        mesh.RecalculateNormals();
        mf.mesh = mesh;

        if (_groundMaterial != null)
            groundRenderer.sharedMaterial = _groundMaterial;
    }

    // ── Obstacles ───────────────────────────────────────────────

    private void CreateObstacles()
    {
        var obstacleParent = new GameObject("Obstacles");
        obstacleParent.transform.SetParent(transform);

        // Spawn trees (small, below characters)
        for (int i = 0; i < _treeCount; i++)
        {
            var pos = GetRandomPosition(_obstacleClearRadius);
            CreateObstacle(obstacleParent, $"Tree_{i}", _treeSprite, pos,
                new Vector2(1.2f, 1.6f), 1.8f, -2, 0.04f);
        }

        // Spawn rocks (small, below characters)
        for (int i = 0; i < _rockCount; i++)
        {
            var pos = GetRandomPosition(_obstacleClearRadius);
            CreateObstacle(obstacleParent, $"Rock_{i}", _rockSprite, pos,
                new Vector2(0.9f, 0.9f), 1.2f, -2, 0.04f);
        }

        // Spawn walls (small, below characters)
        for (int i = 0; i < _wallCount; i++)
        {
            var pos = GetRandomPosition(_obstacleClearRadius);
            CreateObstacle(obstacleParent, $"Wall_{i}", _wallSprite, pos,
                new Vector2(3f, 0.8f), 3.5f, -2, 0.04f);
        }
    }

    private void CreateObstacle(GameObject parent, string name, Sprite sprite, Vector2 position,
        Vector2 colliderSize, float collisionRadius, int sortingOrder, float scaleMultiplier = 1f)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent.transform);
        obj.transform.position = new Vector3(position.x, position.y, 0f);

        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;

        var bc = obj.AddComponent<BoxCollider2D>();
        bc.size = colliderSize * scaleMultiplier;

        // Random slight rotation for variety (except walls)
        if (!name.StartsWith("Wall"))
        {
            obj.transform.Rotate(0f, 0f, Random.Range(-15f, 15f));
        }

        // Random scale variation
        float scale = Random.Range(0.85f, 1.15f) * scaleMultiplier;
        obj.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private Vector2 GetRandomPosition(float clearRadius)
    {
        // Keep trying until we find a position outside the clear zone
        for (int attempt = 0; attempt < 50; attempt++)
        {
            float x = Random.Range(-_mapHalfSize + 5f, _mapHalfSize - 5f);
            float y = Random.Range(-_mapHalfSize + 5f, _mapHalfSize - 5f);

            // Keep away from center spawn point
            if (new Vector2(x, y).sqrMagnitude < clearRadius * clearRadius)
                continue;

            return new Vector2(x, y);
        }

        return new Vector2(Random.Range(-_mapHalfSize * 0.5f, _mapHalfSize * 0.5f),
                           Random.Range(-_mapHalfSize * 0.5f, _mapHalfSize * 0.5f));
    }

    // ── Fence ──────────────────────────────────────────────────

    private void CreateFence()
    {
        if (_fenceSprite == null) return;

        var fenceParent = new GameObject("Fence");
        fenceParent.transform.SetParent(transform);

        float h = _mapHalfSize - 0.5f; // Slightly inside the boundary
        int countPerSide = Mathf.CeilToInt(_mapHalfSize * 2f / _fenceSpacing);

        // Top and bottom
        for (int i = 0; i < countPerSide; i++)
        {
            float x = -h + i * _fenceSpacing;
            CreateFencePost(fenceParent, $"FenceT_{i}", new Vector2(x, h));
            CreateFencePost(fenceParent, $"FenceB_{i}", new Vector2(x, -h));
        }

        // Left and right
        for (int i = 0; i < countPerSide; i++)
        {
            float y = -h + i * _fenceSpacing;
            CreateFencePost(fenceParent, $"FenceL_{i}", new Vector2(-h, y));
            CreateFencePost(fenceParent, $"FenceR_{i}", new Vector2(h, y));
        }
    }

    private void CreateFencePost(GameObject parent, string name, Vector2 position)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent.transform);
        obj.transform.position = new Vector3(position.x, position.y, 0f);

        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = _fenceSprite;
        sr.sortingOrder = 0;
    }

    // ── Decorations ────────────────────────────────────────────

    private void CreateDecorations()
    {
        var decoParent = new GameObject("Decorations");
        decoParent.transform.SetParent(transform);

        // Scattered grass patches (small colored quads)
        for (int i = 0; i < 80; i++)
        {
            var pos = GetRandomPosition(5f);
            CreateGrassPatch(decoParent, $"Grass_{i}", pos);
        }
    }

    private void CreateGrassPatch(GameObject parent, string name, Vector2 position)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent.transform);
        obj.transform.position = new Vector3(position.x, position.y, 0f);

        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateColorSprite(1, 1, new Color(0.25f, 0.45f, 0.15f, 0.4f));
        sr.sortingOrder = -1; // Behind obstacles

        float scale = Random.Range(0.8f, 2.5f);
        obj.transform.localScale = new Vector3(scale, scale, 1f);
        obj.transform.Rotate(0f, 0f, Random.Range(0f, 360f));
    }

    /// <summary>Creates a small 1×1 solid-color sprite for decoration purposes.</summary>
    private static Sprite CreateColorSprite(int width, int height, Color color)
    {
        var tex = new Texture2D(width, height);
        var pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 1f);
    }
}