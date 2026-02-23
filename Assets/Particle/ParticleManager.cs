using Photon.Pun;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using TMPro;
using UniRx;

/// <summary>
/// Object Poolを使用してParticleControllerを管理
/// パーティクル全体を管理
/// </summary>
public class ParticleManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private List<ParticleController> prefabs = new List<ParticleController>();

    [Header("Tilemap Avoidance")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private bool precomputeFreePositions = true;
    [SerializeField] private int maxDecideAttempts = 50;
    private List<Vector3> freePositions = new List<Vector3>();

    //pool
    [SerializeField] private uint initPoolSize = 10;
    [SerializeField] private uint maxParticleCount = 300;
    [SerializeField] private float spawnIntervalSeconds = 0.1f;
    private Stack<ParticleController> pool;
    private int currentParticleCount = 0;
    private bool isSpawning = false;
    private float spawnInterval = 0f;
    private PhotonView pv;

    // マスターが発行する一意ID
    //private int nextParticleId = 0;

    // ローカルで動作中のパーティクルをIDで参照するマップ
    //private Dictionary<int, ParticleController> activeById = new Dictionary<int, ParticleController>();

    // 外部からのスポーン要求を受け取るストリーム
    public Subject<Vector2> RequestSpawnStream { get; } = new Subject<Vector2>();

    //UI
    [SerializeField] private TextMeshProUGUI CountText;
    // 全クライアントが取得したパーティクルの総数（マスターが管理して配布）
    private int totalCollected = 0;
    // 自分が取得したパーティクルの数（ローカル）
    private int myCollected = 0;

    void Start()
    {
        pv = GetComponent<PhotonView>();

        // RequestSpawnStream を購読して、外部要求をマスターに伝達
        RequestSpawnStream.Subscribe(pos => RequestSpawn(pos)).AddTo(this);

        if (precomputeFreePositions)
        {
            PrecomputeFreePositions();
        }

        //SetupPool();
        UpdateCountUI();
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        pv = GetComponent<PhotonView>();
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("OnJoinedRoom: I'm master, starting periodic spawn.");
            SetupPool();
            SpawnParticlePeriodically(spawnIntervalSeconds);
        }
    }

    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        base.OnMasterClientSwitched(newMasterClient);
        if (newMasterClient == null) return;

        // このクライアントが新しいマスターになったら開始、そうでなければ停止
        if (PhotonNetwork.LocalPlayer != null && newMasterClient.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            Debug.Log("OnMasterClientSwitched: I became master, start spawning.");
            // マスターになったのでプール管理を開始
            if (pool == null)
            {
                SetupPool();
            }
            SpawnParticlePeriodically(spawnIntervalSeconds);
        }
        else
        {
            Debug.Log("OnMasterClientSwitched: Another master, stop spawning if running.");
            if (isSpawning)
            {
                CancelInvoke(nameof(TrySpawnParticle));
                isSpawning = false;
            }
        }
    }

    public override void OnDisable()
    {
        if (isSpawning)
        {
            CancelInvoke(nameof(TrySpawnParticle));
            isSpawning = false;
        }
    }

    /// <summary>
    /// 外部（プレイヤー等）がパーティクルの生成を要求するときに呼ぶ。
    /// マスターが判断して、全員へスポーン命令をブロードキャストする。
    /// </summary>
    public void RequestSpawn(Vector2 desiredPos, int prefabIndex = 0)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // 直接マスターとして処理
            //MasterSpawnAndBroadcast(desiredPos, prefabIndex);
            InstantiateOrActivate(0);
        }
        else
        {
            //pv.RPC(nameof(RPC_RequestSpawn), RpcTarget.MasterClient, desiredPos.x, desiredPos.y, prefabIndex/*, PhotonNetwork.LocalPlayer.ActorNumber*/);
        }
    }

    private void UpdateCountUI()
    {
        if (CountText != null)
        {
            CountText.text = "Total: " + totalCollected + "  My: " + myCollected;
        }
    }

    public void RequestCollectFromMaster(int viewID, int n)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            MasterHandleCollect(viewID, PhotonNetwork.LocalPlayer.ActorNumber, n);
        }
        else
        {
            pv.RPC(nameof(RPC_RequestCollect),
                RpcTarget.MasterClient,
                viewID,
                PhotonNetwork.LocalPlayer.ActorNumber,
                n);
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
        if (!PhotonNetwork.IsMasterClient) return;

        // マスターはスポーン判定を行い、全員へ通知する


        InstantiateOrActivate(0);
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
                    if (world.x >= -38f && world.x <= 38f && world.y >= -15f && world.y <= 20f)
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

    /*
    private Color DecideRandomColor()
    {
        float h = Random.Range(0f, 1f);
        Color color = Color.HSVToRGB(h, 1f, 1f);
        return color;
    }
    */

    private void SetupPool()
    {
        pool = new Stack<ParticleController>();
        ParticleController instance = null;

        for (int i = 0; i < initPoolSize; i++)
        {
            instance = InstantiateParicle();
            instance.SetVisible(false);
            instance.SetActive(false);
            pool.Push(instance);
        }
        currentParticleCount = 0;
    }

    private ParticleController GetParticle(int prefabType = 0)
    {
        if(currentParticleCount >= maxParticleCount)
        {
            Debug.LogWarning($"パーティクル最大数({maxParticleCount})に達しました");
            return null;
        }

        if (pool.Count > 0)
        {
            ParticleController nextParticle = pool.Pop();
            currentParticleCount++;
            nextParticle.transform.position = DecideRandomPos();
            nextParticle.SetActive(true);
            return nextParticle;
        }
        else
        {
            return InstantiateParicle(prefabType);
        }
    }

    private ParticleController InstantiateParicle(int prefabType = 0)
    {
        Vector2 randomPos = DecideRandomPos();
        GameObject obj = PhotonNetwork.InstantiateRoomObject(
            prefabs[prefabType].name,
            randomPos,
            Quaternion.identity
        );
        ParticleController instance = obj.GetComponent<ParticleController>();

        instance.ParticleManager = this;
        obj.GetComponent<Transform>().SetParent(this.GetComponent<Transform>());
        currentParticleCount++;
        
        return instance;
    }

    //int initializedParticleNum = 0;
    private void InstantiateOrActivate(int prefabType = 0)
    {

        ParticleController nextParticle = GetParticle(prefabType);
        if(nextParticle == null)
        {
            return;
        }

        //nextParticle.SetColor(DecideRandomColor());
    }

    #endregion

    #region PUN RPCs and local spawn/destroy helpers

    /*
    [PunRPC]
    private void RPC_RequestSpawn(float x, float y, int prefabIndex)
    {
        // この RPC は MasterClient のみに届く
        if (!PhotonNetwork.IsMasterClient) return;

        InstantiateOrActivate(0);
        obj.GetComponent<ParticleController>().particleManager = this;
    }
    */

    [PunRPC]
    private void RPC_RequestCollect(int viewID, int requesterActor, int n)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        MasterHandleCollect(viewID, requesterActor, n);
    }
    private void MasterHandleCollect(int viewID, int requesterActor, int n)
    {
        PhotonView targetView = PhotonView.Find(viewID);
        if (targetView == null) return;

        var particle = targetView.GetComponent<ParticleController>();
        if (particle == null) return;

        // すでに無効なら拒否（同時取得防止）
        if (!particle.IsVisible) return;

        totalCollected++;
        particle.SetActive(false);
        pool.Push(particle);
        currentParticleCount--;

        //Debug.LogErrorFormat($">>>currentPool={pool.Count}, currentParticleCount={currentParticleCount}<<<");

        if (PhotonNetwork.LocalPlayer.ActorNumber == requesterActor && n == 0)
        {
            myCollected++;
        }

        UpdateCountUI();
    }

    #endregion
}
