using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapPropSpawner : MonoBehaviour
{
    public enum SpawnMode
    {
        LandOnly,
        WaterOnly
    }

    [Header("Tilemap References")]
    public Tilemap tilemap;
    public TileBase landTile;
    public TileBase waterTile;

    [Header("Spawn Settings")]
    public SpawnMode mode = SpawnMode.LandOnly;
    public List<GameObject> prefabs = new List<GameObject>();
    public int count = 20;

    [Header("Placement")]
    public Vector2 offset = new Vector2(0.5f, 0.5f);
    public bool randomFlipX = true;
    public bool randomFlipY = false;
    public float minDistanceBetweenProps = 1.0f;

    [Header("Edge Bias (0 = center, 1 = edges)")]
    [Range(0f, 1f)]
    public float edgeBias = 0.85f;

    [Header("Rendering")]
    public string sortingLayerName = "Props";
    public int orderInLayer = 10;

    [Header("Cleanup")]
    public Transform spawnedParent;
    public bool clearPreviousOnSpawn = true;

    private List<Vector3> usedPositions = new List<Vector3>();

    void Start()
    {
        Spawn();
    }

    public void Spawn()
    {
        if (tilemap == null || prefabs.Count == 0)
        {
            Debug.LogWarning("Tilemap veya prefab listesi boş.");
            return;
        }

        if (clearPreviousOnSpawn && spawnedParent != null)
        {
            for (int i = spawnedParent.childCount - 1; i >= 0; i--)
                Destroy(spawnedParent.GetChild(i).gameObject);
        }

        usedPositions.Clear();

        BoundsInt bounds = tilemap.cellBounds;
        List<Vector3Int> validCells = new List<Vector3Int>();

        Vector3 center = bounds.center;

        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            TileBase tile = tilemap.GetTile(cell);
            if (tile == null) continue;

            bool valid =
                (mode == SpawnMode.LandOnly && tile == landTile) ||
                (mode == SpawnMode.WaterOnly && tile == waterTile);

            if (!valid) continue;

            float distToCenter = Vector3.Distance(cell, center);
            float maxDist = Mathf.Max(bounds.size.x, bounds.size.y) * 0.5f;
            float t = distToCenter / maxDist;

            if (Random.value < Mathf.Lerp(1f - edgeBias, 1f, t))
                validCells.Add(cell);
        }

        if (validCells.Count == 0)
        {
            Debug.LogWarning("Uygun hücre bulunamadı. Mode=" + mode);
            return;
        }

        int attempts = 0;
        int spawned = 0;

        while (spawned < count && attempts < count * 20)
        {
            attempts++;

            Vector3Int cell = validCells[Random.Range(0, validCells.Count)];
            Vector3 worldPos = tilemap.CellToWorld(cell) + (Vector3)offset;

            bool tooClose = false;
            foreach (var p in usedPositions)
            {
                if (Vector3.Distance(p, worldPos) < minDistanceBetweenProps)
                {
                    tooClose = true;
                    break;
                }
            }

            if (tooClose) continue;

            GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
            GameObject go = Instantiate(prefab, worldPos, Quaternion.identity);

            if (spawnedParent != null)
                go.transform.SetParent(spawnedParent);

            ApplyRenderSettings(go);
            ApplyRandomFlip(go);

            usedPositions.Add(worldPos);
            spawned++;
        }
    }

    void ApplyRandomFlip(GameObject go)
    {
        SpriteRenderer sr = go.GetComponentInChildren<SpriteRenderer>();
        if (sr == null) return;

        sr.flipX = randomFlipX && Random.value > 0.5f;
        sr.flipY = randomFlipY && Random.value > 0.5f;
    }

    void ApplyRenderSettings(GameObject go)
    {
        SpriteRenderer sr = go.GetComponentInChildren<SpriteRenderer>();
        if (sr == null) return;

        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = orderInLayer;
    }
}