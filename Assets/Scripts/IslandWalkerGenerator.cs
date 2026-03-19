using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class IslandWalkerGenerator : MonoBehaviour
{
    [Header("References")]
    public Tilemap tilemap;
    public TileBase waterTile;
    public TileBase landTile;

    [Header("Level (Seed)")]
    public int LevelNumber = 1;
    public bool deterministicPerLevel = true;

    [Header("Map Size")]
    public int width = 60;
    public int height = 40;
    public Vector2Int origin = new Vector2Int(0, 0);

    [Header("Walker Settings")]
    public int steps = 800;
    [Range(0f, 1f)] public float turnChance = 0.55f;
    public int brushRadius = 1;

    [Header("Land Amount Control")]
    public int minLandCells = 0;
    public int retriesIfTooSmall = 3;

    [Header("Generate")]
    public bool generateOnStart = true;

    // ✅ PROP BÖLÜMÜ GERİ GELDİ (Inspector’da görünecek)
    [Header("Prop Spawners (Inspector'dan bağla)")]
    public PropSpawnerOneTilemap landSpawner;
    public PropSpawnerOneTilemap waterSpawner;

    // Internal
    private HashSet<Vector3Int> landCells = new HashSet<Vector3Int>();

    void Start()
    {
        if (generateOnStart)
            GenerateNow();
    }

    public void GenerateNow()
    {
        if (tilemap == null || waterTile == null || landTile == null)
        {
            Debug.LogError("[IslandWalkerGenerator] Tilemap / WaterTile / LandTile boş!");
            return;
        }

        int tries = Mathf.Max(1, retriesIfTooSmall);
        bool success = false;

        for (int attempt = 0; attempt < tries; attempt++)
        {
            InitSeed(attempt);

            tilemap.ClearAllTiles();
            FillWater();

            landCells.Clear();
            DoRandomWalk();

            if (minLandCells <= 0 || landCells.Count >= minLandCells)
            {
                success = true;
                break;
            }
        }

        if (!success)
            Debug.LogWarning($"[IslandWalkerGenerator] Ada küçük kaldı. LandCells={landCells.Count}");

        // ✅ Map bitti → prop spawn otomatik
        SpawnProps();
    }

    void InitSeed(int attempt)
    {
        if (deterministicPerLevel)
        {
            int seed = LevelNumber * 10007 + attempt * 97;
            Random.InitState(seed);
        }
        else
        {
            Random.InitState(System.Environment.TickCount);
        }
    }

    void FillWater()
    {
        for (int x = origin.x; x < origin.x + width; x++)
        {
            for (int y = origin.y; y < origin.y + height; y++)
            {
                tilemap.SetTile(new Vector3Int(x, y, 0), waterTile);
            }
        }
    }

    void DoRandomWalk()
    {
        Vector3Int pos = new Vector3Int(origin.x + width / 2, origin.y + height / 2, 0);
        Vector2Int dir = Vector2Int.right;

        for (int i = 0; i < steps; i++)
        {
            PaintLand(pos);

            if (Random.value < turnChance)
            {
                int r = Random.Range(0, 4);
                dir = r switch
                {
                    0 => Vector2Int.right,
                    1 => Vector2Int.left,
                    2 => Vector2Int.up,
                    _ => Vector2Int.down
                };
            }

            pos.x += dir.x;
            pos.y += dir.y;

            pos.x = Mathf.Clamp(pos.x, origin.x + 1, origin.x + width - 2);
            pos.y = Mathf.Clamp(pos.y, origin.y + 1, origin.y + height - 2);
        }
    }

    void PaintLand(Vector3Int center)
    {
        int r = Mathf.Max(0, brushRadius);

        for (int dx = -r; dx <= r; dx++)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                int x = center.x + dx;
                int y = center.y + dy;

                if (x < origin.x || x >= origin.x + width) continue;
                if (y < origin.y || y >= origin.y + height) continue;

                Vector3Int p = new Vector3Int(x, y, 0);

                tilemap.SetTile(p, landTile);
                landCells.Add(p);
            }
        }
    }

    void SpawnProps()
    {
        // Spawner’ların tile/tiles referansını buradan garanti ediyoruz
        if (landSpawner != null)
        {
            landSpawner.tilemap = tilemap;
            landSpawner.landTile = landTile;
            landSpawner.waterTile = waterTile;
            landSpawner.mode = PropSpawnerOneTilemap.Mode.LandOnly;
            landSpawner.SpawnNow(LevelNumber);
        }

        if (waterSpawner != null)
        {
            waterSpawner.tilemap = tilemap;
            waterSpawner.landTile = landTile;
            waterSpawner.waterTile = waterTile;
            waterSpawner.mode = PropSpawnerOneTilemap.Mode.WaterOnly;
            waterSpawner.SpawnNow(LevelNumber);
        }
    }
}