using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Tilemaps;
using TMPro;
using UniRx;
using Sirenix.OdinInspector;
using Unity.Cinemachine;

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

    //Event
    [SerializeField] private EventManager eventManager;

    [Header("Particle Value")]
    [SerializeField] private long baseParticleValue = 1;
    [SerializeField] private long progressPerValueStep = 50;
    [SerializeField] private long valueIncreasePerStep = 1;
    private float currentParticleWeightingRate = 1f;
    private readonly HashSet<ParticleController> activeParticles = new HashSet<ParticleController>();

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
    // 全クライアントが取得したパーティクルの総数（Room Custom Properties で管理）
    //private int totalCollected = 0;
    public ReactiveProperty<long> totalCollected = new ReactiveProperty<long>(0);
    // 自分が取得したパーティクルの数（Player Custom Properties で管理）
    //private int myCollected = 0;
    public ReactiveProperty<long> myCollected = new ReactiveProperty<long>(0);

    // Custom Properties のキー
    private const string TOTAL_COLLECTED_KEY = "totalCollected";
    private const string MY_COLLECTED_KEY = "myCollected";

    // マスターが各プレイヤーのカウントをローカルで追跡（複数リクエスト同時処理対策）
    private Dictionary<int, long> masterPlayerCollectCounts = new Dictionary<int, long>();


    void Start()
    {
        PhotonNetwork.LogLevel = PunLogLevel.ErrorsOnly;
        pv = GetComponent<PhotonView>();

        // RequestSpawnStream を購読して、外部要求をマスターに伝達
        RequestSpawnStream.Subscribe(pos => RequestSpawn(pos)).AddTo(this);
        totalCollected.Subscribe(_ => RefreshActiveParticleValues()).AddTo(this);

        if (eventManager != null)
        {
            currentParticleWeightingRate = eventManager.CurrentParticleWeightingRate;
            eventManager.OnEventStart.Subscribe(HandleEventStart).AddTo(this);
            eventManager.OnEventEnd.Subscribe(_ => ResetParticleWeighting()).AddTo(this);
        }

        if (precomputeFreePositions)
        {
            PrecomputeFreePositions();
        }

        //SetupPool();
        //UpdateCountUI();
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        pv = GetComponent<PhotonView>();
        
        // Room Properties から totalCollected を取得
        if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(TOTAL_COLLECTED_KEY, out var totalObj))
        {
            totalCollected.Value = (long)totalObj;
        }
        else
        {
            totalCollected.Value = 0;
        }

        // Player Properties から myCollected を取得
        if (PhotonNetwork.LocalPlayer != null && PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(MY_COLLECTED_KEY, out var myObj))
        {
            myCollected.Value = (long)myObj;
        }
        else
        {
            myCollected.Value = 0;
        }

        //UpdateCountUI();

        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("OnJoinedRoom: I'm master, starting periodic spawn.");
            SetupPool();
            SpawnParticlePeriodically(spawnIntervalSeconds);
            
            // マスター側で各プレイヤーのカウントを初期化
            masterPlayerCollectCounts.Clear();
            foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
            {
                long count = 0;
                if (player.CustomProperties.TryGetValue(MY_COLLECTED_KEY, out var countObj))
                {
                    count = (long)countObj;
                }
                masterPlayerCollectCounts[player.ActorNumber] = count;
            }
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
            
            // 新しいマスターが引き継ぐため、プレイヤーカウント辞書を初期化
            masterPlayerCollectCounts.Clear();
            foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
            {
                long count = 0;
                if (player.CustomProperties.TryGetValue(MY_COLLECTED_KEY, out var countObj))
                {
                    count = (long)countObj;
                }
                masterPlayerCollectCounts[player.ActorNumber] = count;
            }
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
    /// 新しいプレイヤーが入室したときに呼ばれる
    /// </summary>
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        
        // マスターなら、新規プレイヤーをカウント辞書に追加
        if (PhotonNetwork.IsMasterClient)
        {
            if (!masterPlayerCollectCounts.ContainsKey(newPlayer.ActorNumber))
            {
                masterPlayerCollectCounts[newPlayer.ActorNumber] = 0;
            }
        }
    }

    /// <summary>
    /// プレイヤーが退室したときに呼ばれる
    /// </summary>
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        
        // マスターなら、退室プレイヤーをカウント辞書から削除
        if (PhotonNetwork.IsMasterClient)
        {
            if (masterPlayerCollectCounts.ContainsKey(otherPlayer.ActorNumber))
            {
                masterPlayerCollectCounts.Remove(otherPlayer.ActorNumber);
            }
        }
    }

    /// <summary>
    /// Room Custom Properties が更新されたときに呼ばれる（totalCollected の同期）
    /// </summary>
    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        base.OnRoomPropertiesUpdate(propertiesThatChanged);

        if (propertiesThatChanged.ContainsKey(TOTAL_COLLECTED_KEY))
        {
            totalCollected.Value = (long)propertiesThatChanged[TOTAL_COLLECTED_KEY];
            //UpdateCountUI();
        }
    }

    /// <summary>
    /// Player Custom Properties が更新されたときに呼ばれる（myCollected の同期）
    /// </summary>
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        base.OnPlayerPropertiesUpdate(targetPlayer, changedProps);

        // 自分のプロパティが更新されたかチェック
        if (targetPlayer == PhotonNetwork.LocalPlayer && changedProps.ContainsKey(MY_COLLECTED_KEY))
        {
            myCollected.Value = (long)changedProps[MY_COLLECTED_KEY];
            //UpdateCountUI();
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

    void HandleEventStart(EventData eventData)
    {
        currentParticleWeightingRate = eventData != null ? eventData.particleWeightingRate : 1f;
        RefreshActiveParticleValues();
    }

    void ResetParticleWeighting()
    {
        currentParticleWeightingRate = 1f;
        RefreshActiveParticleValues();
    }

    /*
    private void UpdateCountUI()
    {
        if (CountText != null)
        {
            CountText.text = "Total: " + totalCollected.Value + "  My: " + myCollected;
        }
    }
    */

    public void RequestCollectFromMaster(int viewID, int n, int requesterActor = -1)
    {
        int actorToCredit = requesterActor;
        if (n == 0 && actorToCredit <= 0 && PhotonNetwork.LocalPlayer != null)
        {
            actorToCredit = PhotonNetwork.LocalPlayer.ActorNumber;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            MasterHandleCollect(viewID, actorToCredit, n);
        }
        else
        {
            pv.RPC(nameof(RPC_RequestCollect),
                RpcTarget.MasterClient,
                viewID,
                actorToCredit,
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
    [Header("パーティクル出現範囲")]
    [SerializeField, MinMaxRangeSlider(-38f, 38f)] Vector2 rangeX = new Vector2(-38f, 38f);
    [SerializeField, MinMaxRangeSlider(-15f, 20f)] Vector2 rangeY = new Vector2(-15f, 20f);
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
                    if (world.x >= rangeX.x && world.x <= rangeX.y && world.y >= rangeY.x && world.y <= rangeY.y)
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
            var w = freePositions[UnityEngine.Random.Range(0, freePositions.Count)];
            //タイルの範囲でランダムに
            w += new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), UnityEngine.Random.Range(-0.5f, 0.5f));
            return new Vector2(w.x, w.y);
        }

        // Otherwise, try a limited number of attempts to avoid placing on tiles.
        for (int attempt = 0; attempt < maxDecideAttempts; attempt++)
        {
            float x = UnityEngine.Random.Range(-38f, 38f);
            float y = UnityEngine.Random.Range(-15f, 10f);
            Vector2 pos = new Vector2(x, y);

            if (groundTilemap == null) return pos;

            Vector3Int cell = groundTilemap.WorldToCell(pos);
            if (!groundTilemap.HasTile(cell))
            {
                return pos;
            }
        }

        // Fallback: return a random position if no free spot found within attempts.
        float xf = UnityEngine.Random.Range(-38f, 38f);
        float yf = UnityEngine.Random.Range(-15f, 10f);
        return new Vector2(xf, yf);
    }

    /*
    private Color DecideRandomColor()
    {
        float h = UnityEngine.Random.Range(0f, 1f);
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
            //instance.SetVisible(false);
            instance.SetActive(false, 1);
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
            ActivateParticle(nextParticle);
            return nextParticle;
        }
        else
        {
            ParticleController instance = InstantiateParicle(prefabType);
            ActivateParticle(instance);
            return instance;
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
        ApplyValueToParticle(instance);
        
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

    private void ActivateParticle(ParticleController particle)
    {
        if (particle == null)
        {
            return;
        }

        ApplyValueToParticle(particle);
        particle.SetActive(true);
        activeParticles.Add(particle);
    }

    private void ApplyValueToParticle(ParticleController particle)
    {
        if (particle == null)
        {
            return;
        }

        particle.Value = CalculateCurrentParticleValue();
    }

    private long CalculateCurrentParticleValue()
    {
        long scaledBaseValue = CalculateScaledBaseParticleValue();
        double weightedValue = scaledBaseValue * currentParticleWeightingRate;

        if (weightedValue <= 0d)
        {
            return 0;
        }

        return Math.Max(1L, (long)Math.Ceiling(weightedValue));
    }

    private long CalculateScaledBaseParticleValue()
    {
        if (progressPerValueStep <= 0)
        {
            return baseParticleValue;
        }

        long progressSteps = totalCollected.Value / progressPerValueStep;
        return baseParticleValue + (progressSteps * valueIncreasePerStep);
    }

    private void RefreshActiveParticleValues()
    {
        if (activeParticles.Count == 0)
        {
            return;
        }

        List<ParticleController> staleParticles = null;
        foreach (ParticleController particle in activeParticles)
        {
            if (particle == null || !particle.IsVisible)
            {
                staleParticles ??= new List<ParticleController>();
                staleParticles.Add(particle);
                continue;
            }

            ApplyValueToParticle(particle);
        }

        if (staleParticles == null)
        {
            return;
        }

        foreach (ParticleController staleParticle in staleParticles)
        {
            activeParticles.Remove(staleParticle);
        }
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

        long collectedValue = particle.Value;
        totalCollected.Value += collectedValue;
        particle.SetActive(false);
        activeParticles.Remove(particle);
        pool.Push(particle);
        currentParticleCount--;

        // Room Custom Properties に totalCollected を同期
        var roomProps = new ExitGames.Client.Photon.Hashtable { { TOTAL_COLLECTED_KEY, totalCollected.Value } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

        // requesterActor に該当するプレイヤーを取得して myCollected を更新
        if (n == 0)
        {
            Player requester = PhotonNetwork.CurrentRoom.GetPlayer(requesterActor);
            if (requester != null)
            {
                // ローカル辞書から読むことで、複数リクエスト同時処理でも正確にカウント可能
                if (!masterPlayerCollectCounts.ContainsKey(requesterActor))
                {
                    masterPlayerCollectCounts[requesterActor] = 0;
                }
                
                masterPlayerCollectCounts[requesterActor] += collectedValue;
                long newCount = masterPlayerCollectCounts[requesterActor];
                
                // Player Custom Properties に同期
                var playerProps = new ExitGames.Client.Photon.Hashtable { { MY_COLLECTED_KEY, newCount } };
                requester.SetCustomProperties(playerProps);
            }
        }

        //UpdateCountUI();
    }

    #endregion
}
