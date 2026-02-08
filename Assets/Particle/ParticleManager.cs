using Photon.Pun;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Timeline;
using UnityEngine.Tilemaps;
using TMPro;
using UniRx;
//using System.Diagnostics;

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
    private Stack<ParticleController> stack;
    private bool isSpawning = false;
    private float spawnInterval = 0f;
    private PhotonView pv;

    // マスターが発行する一意ID
    private int nextParticleId = 0;

    // ローカルで動作中のパーティクルをIDで参照するマップ
    private Dictionary<int, ParticleController> activeById = new Dictionary<int, ParticleController>();

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

        SetupPool();
        // Start 時点で既にルーム内かつマスターなら定期スポーンを開始
        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
        {
            Debug.Log("You are master on Start, starting spawn.");
            SpawnParticlePeriodically(0.1f);
        }
        else
        {
            Debug.Log("Spawn will start when (if) you become master or on OnJoinedRoom.");
        }
        UpdateCountUI();
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        pv = GetComponent<PhotonView>();
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("OnJoinedRoom: I'm master, starting periodic spawn.");
            SpawnParticlePeriodically(0.1f);
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
            SpawnParticlePeriodically(0.1f);
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

    void OnDisable()
    {
        if (isSpawning)
        {
            CancelInvoke(nameof(TrySpawnParticle));
            isSpawning = false;
        }
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
            instance.ParticleId = -1;
            instance.transform.SetParent(this.transform);
            instance.SetColor();
            
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

    /// <summary>
    /// 外部（プレイヤー等）がパーティクルの生成を要求するときに呼ぶ。
    /// マスターが判断して、全員へスポーン命令をブロードキャストする。
    /// </summary>
    public void RequestSpawn(Vector2 desiredPos, int prefabIndex = 0)
    {
        if (pv == null)
        {
            pv = GetComponent<PhotonView>();
        }

        if (PhotonNetwork.IsMasterClient)
        {
            // 直接マスターとして処理
            MasterSpawnAndBroadcast(desiredPos, prefabIndex);
        }
        else
        {
            // マスターへリクエストを送る（RPC）
            if (pv != null)
            {
                pv.RPC(nameof(RPC_RequestSpawn), RpcTarget.MasterClient, desiredPos.x, desiredPos.y, prefabIndex, PhotonNetwork.LocalPlayer.ActorNumber);
            }
        }
    }

    private void UpdateCountUI()
    {
        if (CountText != null)
        {
            CountText.text = "Total: " + totalCollected + "  My: " + myCollected;
        }
    }

    public void ReturnToPool(ParticleController instance)
    {
        // IDマッピングを解除
        if (instance != null && instance.ParticleId != -1)
        {
            activeById.Remove(instance.ParticleId);
            instance.ParticleId = -1;
        }

        instance.gameObject.SetActive(false);
        stack.Push(instance);

        // スポーンが停止しているなら再開する
        if (!isSpawning && spawnInterval > 0f)
        {
            InvokeRepeating(nameof(TrySpawnParticle), spawnInterval, spawnInterval);
            isSpawning = true;
        }
    }

    /// <summary>
    /// クライアントがパーティクルに触れたとき（回収したとき）に呼ばれる。
    /// 実際の破棄（ReturnToPool）はマスターの判断で全員へ通知される。
    /// </summary>
    public void NotifyCollected(ParticleController instance, int n)
    {
        if (instance == null) return;
        

        if (instance.ParticleId == -1)
        {
            // IDが無ければローカルでそのまま戻す（プールは各クライアントで初期化されるため）
            ReturnToPool(instance);
            return;
        }

        if (pv == null) pv = GetComponent<PhotonView>();

        if (PhotonNetwork.IsMasterClient)
        {
            // マスター自身が回収した場合はグローバルカウントを更新して全体へ通知
            totalCollected++;
            if (pv != null) pv.RPC(nameof(RPC_DestroyParticle), RpcTarget.AllBuffered, instance.ParticleId, PhotonNetwork.LocalPlayer.ActorNumber, totalCollected, n);
        }
        else
        {
            // マスターへ破棄要求を送る
            if (pv != null) pv.RPC(nameof(RPC_RequestDestroy), RpcTarget.MasterClient, instance.ParticleId, PhotonNetwork.LocalPlayer.ActorNumber, n);
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
        // マスターだけが定期的なスポーンを決定する
        if (!PhotonNetwork.IsMasterClient) return;

        // マスターはスポーン判定を行い、全員へ通知する
        Vector2 randomPos = DecideRandomPos();
        MasterSpawnAndBroadcast(randomPos, 0);
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
    #endregion

    #region PUN RPCs and local spawn/destroy helpers
    [PunRPC]
    private void RPC_RequestSpawn(float x, float y, int prefabIndex, int requesterActor)
    {
        // この RPC は MasterClient のみに届く
        if (!PhotonNetwork.IsMasterClient) return;
        MasterSpawnAndBroadcast(new Vector2(x, y), prefabIndex);
    }

    private void MasterSpawnAndBroadcast(Vector2 pos, int prefabIndex)
    {
        // マスターがスポーンの可否を判断してから全員へ通知する
        int id = ++nextParticleId;
        // 色はマスターが決める
        Color color = Color.HSVToRGB(UnityEngine.Random.Range(0f, 1f), 1f, 1f);

        if (pv != null)
        {
            pv.RPC(nameof(RPC_SpawnParticle), RpcTarget.AllBuffered, pos.x, pos.y, prefabIndex, color.r, color.g, color.b, id);
        }
        else
        {
            // ローカルのみ
            LocalSpawn(id, pos, color, prefabIndex);
        }
    }

    [PunRPC]
    private void RPC_SpawnParticle(float x, float y, int prefabIndex, float r, float g, float b, int id)
    {
        Color color = new Color(r, g, b);
        LocalSpawn(id, new Vector2(x, y), color, prefabIndex);
    }

    private void LocalSpawn(int id, Vector2 pos, Color color, int prefabIndex)
    {
        var p = Get();
        if (p == null) return;
        p.ParticleId = id;
        p.transform.position = pos;
        p.SetColor(color);
        activeById[id] = p;
    }

    [PunRPC]
    private void RPC_RequestDestroy(int id, int requesterActor, int n)
    {
        // Master に届く
        if (!PhotonNetwork.IsMasterClient) return;
        // マスター側でグローバルカウントを更新して、収集者情報と共に全員へ通知する
        totalCollected++;
        if (pv != null) pv.RPC(nameof(RPC_DestroyParticle), RpcTarget.AllBuffered, id, requesterActor, totalCollected, n);
    }

    [PunRPC]
    private void RPC_DestroyParticle(int id, int collectorActor, int newTotal, int n)
    {
        LocalDestroy(id, collectorActor, newTotal, n);
    }

    private void LocalDestroy(int id, int collectorActor, int newTotal, int n)
    {
        if (activeById.TryGetValue(id, out var p))
        {
            ReturnToPool(p);
            // カウントを同期
            totalCollected = newTotal;
            if (PhotonNetwork.LocalPlayer != null && PhotonNetwork.LocalPlayer.ActorNumber == collectorActor && n == 0)
            {
                myCollected++;
            }
            UpdateCountUI();
        }
    }
    #endregion
}
