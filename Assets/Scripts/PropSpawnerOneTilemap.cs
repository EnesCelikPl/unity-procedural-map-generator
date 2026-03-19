using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PropSpawnerOneTilemap : MonoBehaviour
{
    public enum Mode { LandOnly, WaterOnly }

    // LAND + WATER spawner aynı hücreye spawn etmesin diye ortak set
    private static HashSet<Vector3Int> GlobalUsedCells = new HashSet<Vector3Int>();

    [Header("Tilemap (ONE)")]
    public Tilemap tilemap;
    public TileBase landTile;
    public TileBase waterTile;

    [Header("Mode")]
    public Mode mode = Mode.LandOnly;

    [Header("Prefabs")]
    public List<GameObject> prefabs = new List<GameObject>();
    public int count = 20;

    [Header("Rules")]
    public bool preventOverlap = true;

    [Tooltip("WaterOnly: su prop'ları karadan minimum kaç tile uzakta olsun? (DEFAULT 2)")]
    public int minWaterDistanceFromLand = 2;

    [Header("Edge Bias (Land)")]
    [Range(0f, 1f)]
    public float edgeBias = 0.85f; // 0 random, 1 kıyı ağırlıklı

    [Header("Placement")]
    public Vector2 offset = new Vector2(0.5f, 0.5f);

    [Header("Sorting (optional)")]
    [Tooltip("Boş bırakırsan prefab kendi sorting layer'ı kalır. Bulut için Clouds yazabilirsin.")]
    public string forceSortingLayerName = "";
    public int forceSortingOrder = 0;

    [Header("Cleanup")]
    public Transform spawnedParent;
    public bool clearPreviousOnSpawn = true;

    [Header("Deterministic")]
    public bool deterministicPerLevel = true;

    // cached
    private readonly List<Vector3Int> candidates = new List<Vector3Int>();
    private readonly List<Vector3Int> edgeCells = new List<Vector3Int>();
    private readonly List<Vector3Int> innerCells = new List<Vector3Int>();

    // 4 yön komşu
    private static readonly Vector3Int[] N4 =
    {
        new Vector3Int(1,0,0),
        new Vector3Int(-1,0,0),
        new Vector3Int(0,1,0),
        new Vector3Int(0,-1,0)
    };

    public void SpawnNow(int levelNumber)
    {
        if (tilemap == null || landTile == null || waterTile == null)
        {
            Debug.LogWarning($"[{name}] Tilemap/Land/Water boş!");
            return;
        }

        if (prefabs == null || prefabs.Count == 0)
        {
            Debug.LogWarning($"[{name}] Prefabs listesi boş!");
            return;
        }

        if (spawnedParent == null)
            spawnedParent = transform;

        if (clearPreviousOnSpawn)
            ClearPrevious();

        // Level bazlı seed
        if (deterministicPerLevel)
            Random.InitState(levelNumber * 10007 + (int)mode * 97);

        BuildCandidateLists();

        if (candidates.Count == 0)
        {
            Debug.LogWarning($"[{name}] Uygun hücre bulunamadı. Mode={mode}");
            return;
        }

        int spawned = 0;
        int safety = 0;

        while (spawned < count && safety < count * 300)
        {
            safety++;

            Vector3Int cell = PickCell();

            // Overlap global
            if (preventOverlap && GlobalUsedCells.Contains(cell))
                continue;

            // WaterOnly ise: karadan minDistance kadar uzaklık kontrolü
            if (mode == Mode.WaterOnly)
            {
                if (!IsWaterFarFromLand(cell, minWaterDistanceFromLand))
                    continue;
            }

            GameObject pf = prefabs[Random.Range(0, prefabs.Count)];
            if (pf == null) continue;

            Vector3 world = tilemap.CellToWorld(cell) + new Vector3(offset.x, offset.y, 0f);

            GameObject go = Instantiate(pf, world, Quaternion.identity, spawnedParent);
            go.name = pf.name;

            ApplySorting(go);

            GlobalUsedCells.Add(cell);
            spawned++;
        }

        if (spawned == 0)
            Debug.LogWarning($"[{name}] Spawn 0 oldu. (Su uzaklık filtresi çok sert olabilir)");
    }

    // Level üretmeden önce çağıracağız
    public static void ResetGlobalOverlap()
    {
        GlobalUsedCells.Clear();
    }

    void ClearPrevious()
    {
        for (int i = spawnedParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(spawnedParent.GetChild(i).gameObject);
        }
    }

    void BuildCandidateLists()
    {
        candidates.Clear();
        edgeCells.Clear();
        innerCells.Clear();

        BoundsInt b = tilemap.cellBounds;

        for (int x = b.xMin; x < b.xMax; x++)
        {
            for (int y = b.yMin; y < b.yMax; y++)
            {
                Vector3Int c = new Vector3Int(x, y, 0);
                TileBase t = tilemap.GetTile(c);
                if (t == null) continue;

                bool ok = (mode == Mode.LandOnly && t == landTile) ||
                          (mode == Mode.WaterOnly && t == waterTile);

                if (!ok) continue;

                candidates.Add(c);

                if (mode == Mode.LandOnly)
                {
                    if (IsCoastLand(c)) edgeCells.Add(c);
                    else innerCells.Add(c);
                }
                else
                {
                    innerCells.Add(c);
                }
            }
        }

        if (edgeCells.Count == 0 && innerCells.Count > 0)
            edgeCells.AddRange(innerCells);
    }

    Vector3Int PickCell()
    {
        if (mode == Mode.LandOnly && edgeBias > 0.001f && edgeCells.Count > 0)
        {
            bool pickEdge = Random.value < edgeBias;
            if (pickEdge)
                return edgeCells[Random.Range(0, edgeCells.Count)];
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    bool IsCoastLand(Vector3Int landCell)
    {
        foreach (var d in N4)
        {
            TileBase nt = tilemap.GetTile(landCell + d);
            if (nt == null) return true;
            if (nt == waterTile) return true;
        }
        return false;
    }

    bool IsWaterFarFromLand(Vector3Int waterCell, int minDist)
    {
        int d = Mathf.Max(0, minDist);

        for (int dx = -d; dx <= d; dx++)
        {
            for (int dy = -d; dy <= d; dy++)
            {
                Vector3Int p = new Vector3Int(waterCell.x + dx, waterCell.y + dy, 0);
                TileBase t = tilemap.GetTile(p);
                if (t == landTile)
                    return false;
            }
        }
        return true;
    }

    void ApplySorting(GameObject go)
    {
        if (string.IsNullOrEmpty(forceSortingLayerName) && forceSortingOrder == 0)
            return;

        var renderers = go.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in renderers)
        {
            if (!string.IsNullOrEmpty(forceSortingLayerName))
                sr.sortingLayerName = forceSortingLayerName;

            if (forceSortingOrder != 0)
                sr.sortingOrder = forceSortingOrder;
        }
    }
}