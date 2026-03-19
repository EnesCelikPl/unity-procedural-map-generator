using UnityEngine;

public class RuntimeLevelController : MonoBehaviour
{
    [Header("References")]
    public IslandWalkerGenerator mapGenerator;
    public PropSpawnerOneTilemap landSpawner;
    public PropSpawnerOneTilemap waterSpawner;
    public PropSpawnerOneTilemap cloudSpawner; // varsa bağla, yoksa boş kalabilir

    [Header("Level")]
    public int level = 1;

    [Header("Keys")]
    public KeyCode nextLevelKey = KeyCode.N;
    public KeyCode prevLevelKey = KeyCode.B;
    public KeyCode regenerateKey = KeyCode.R;

    void Start()
    {
        GenerateAll();
    }

    void Update()
    {
        if (Input.GetKeyDown(nextLevelKey))
        {
            level++;
            GenerateAll();
        }

        if (Input.GetKeyDown(prevLevelKey))
        {
            level = Mathf.Max(1, level - 1);
            GenerateAll();
        }

        if (Input.GetKeyDown(regenerateKey))
        {
            GenerateAll();
        }
    }

    void GenerateAll()
    {
        // overlap reset (LAND+WATER aynı hücre olmasın)
        PropSpawnerOneTilemap.ResetGlobalOverlap();

        // 1) Map üret
        if (mapGenerator != null)
        {
            mapGenerator.LevelNumber = level;
            mapGenerator.GenerateNow();
        }

        // 2) Props üret
        if (landSpawner != null) landSpawner.SpawnNow(level);
        if (waterSpawner != null) waterSpawner.SpawnNow(level);
        if (cloudSpawner != null) cloudSpawner.SpawnNow(level);

        Debug.Log($"[RuntimeLevelController] Level generated: {level}");
    }
}