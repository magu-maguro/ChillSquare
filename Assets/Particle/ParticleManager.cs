using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Timeline;
using UnityEngine.Tilemaps;

/// <summary>
/// Object Poolを使用してParticleControllerを管理
/// パーティクル全体を管理
/// </summary>
public class ParticleManager : MonoBehaviour
{
    [SerializeField] private List<ParticleController> prefabs = new List<ParticleController>();

    [Header("Tilemap Avoidance")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private bool precomputeFreePositions = true;
    [SerializeField] private int maxDecideAttempts = 50;
    private List<Vector3> freePositions = new List<Vector3>();

    //pool
    [SerializeField] private uint initPoolSize = 10;
    private Stack<ParticleController> stack;
    private bool isSpawning = false;
    private float spawnInterval = 0f;

    void Start()
    {
        if (precomputeFreePositions)
        {
            PrecomputeFreePositions();
        }

        SetupPool();
        SpawnParticlePeriodically(0.1f);
    }

    private void SetupPool()
    {
        stack = new Stack<ParticleController>((int)initPoolSize);
        ParticleController instance = null;
        for (int i = 0; i < initPoolSize; i++)
        {
            int index = 0;
            instance = Instantiate(prefabs[index]);
            instance.ParticleManager = this;
            
            instance.transform.position = DecideRandomPos();
            // プレインスタンスは非アクティブでプールへ入れる
            instance.gameObject.SetActive(false);
            stack.Push(instance);
        }
    }

    public ParticleController Get()
    {
        ParticleController instance = null;
        if (stack.Count > 0)
        {
            instance = stack.Pop();
            instance.gameObject.SetActive(true);
            instance.transform.position = DecideRandomPos();
            return instance;
        }

        // プールが空の場合は新規生成しない（呼び続けられるのを防ぐ）
        return null;
    }

    public void ReturnToPool(ParticleController instance)
    {
        instance.gameObject.SetActive(false);
        stack.Push(instance);

        // スポーンが停止しているなら再開する
        if (!isSpawning && spawnInterval > 0f)
        {
            InvokeRepeating(nameof(TrySpawnParticle), spawnInterval, spawnInterval);
            isSpawning = true;
        }
    }

    #region Function===================================================
    // 定期的な出現
    public void SpawnParticlePeriodically(float interval)
    {
        // スポーン用ラッパーで呼び出す。プールが枯渇したら自動停止する。
        spawnInterval = interval;
        if (!isSpawning)
        {
            InvokeRepeating(nameof(TrySpawnParticle), interval, interval);
            isSpawning = true;
        }
    }

    private void TrySpawnParticle()
    {
        var p = Get();
        if (p == null)
        {
            // プールに空きが無ければ繰り返し呼び出しを停止する
            CancelInvoke(nameof(TrySpawnParticle));
            isSpawning = false;
        }
    }
    #endregion

    #region Helper===================================================
    private void PrecomputeFreePositions()
    {
        freePositions.Clear();
        if (groundTilemap == null) return;

        BoundsInt bounds = groundTilemap.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (!groundTilemap.HasTile(cell))
                {
                    Vector3 world = groundTilemap.GetCellCenterWorld(cell);
                    if (world.x >= -38f && world.x <= 38f && world.y >= -15f && world.y <= 10f)
                    {
                        freePositions.Add(world);
                    }
                }
            }
        }
    }

    private Vector2 DecideRandomPos()
    {
        // If we precomputed free positions and have entries, sample one.
        if (precomputeFreePositions && freePositions != null && freePositions.Count > 0)
        {
            var w = freePositions[Random.Range(0, freePositions.Count)];
            //タイルの範囲でランダムに
            w += new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f));
            return new Vector2(w.x, w.y);
        }

        // Otherwise, try a limited number of attempts to avoid placing on tiles.
        for (int attempt = 0; attempt < maxDecideAttempts; attempt++)
        {
            float x = Random.Range(-38f, 38f);
            float y = Random.Range(-15f, 10f);
            Vector2 pos = new Vector2(x, y);

            if (groundTilemap == null) return pos;

            Vector3Int cell = groundTilemap.WorldToCell(pos);
            if (!groundTilemap.HasTile(cell))
            {
                return pos;
            }
        }

        // Fallback: return a random position if no free spot found within attempts.
        float xf = Random.Range(-38f, 38f);
        float yf = Random.Range(-15f, 10f);
        return new Vector2(xf, yf);
    }
    #endregion
}
