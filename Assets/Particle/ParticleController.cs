using UnityEngine;
using UnityEngine.Rendering.Universal;
using Photon.Pun;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// ParticleSystemは用いていない
/// あくまでGameObjectをparticleと呼んでいるだけ
/// 各パーティクルの挙動を管理
/// </summary>
public class ParticleController : MonoBehaviour, IPunObservable
{
    private ParticleManager particleManager;
    public ParticleManager ParticleManager { get => particleManager; set => particleManager = value; }
    [SerializeField] private long value = 1;
    public long Value { get => value; set => this.value = value; }
    // 同期用ID（マスターが発行する）
    public int ParticleId { get; set; } = -1;

    private PhotonView pv;

    private bool isActiveState = false;
    private Vector3 lastSentPosition;
    private bool lastSentActiveState;
    private bool hasSentInitialState = false;
    private const float positionSyncThresholdSqr = 0.0001f; // 0.01f^2

    private Renderer rend;
    private Collider2D col;
    private Light2D light2d;

    private List<ParticleSparkleController> sparkles = new List<ParticleSparkleController>();

    public bool IsVisible { get; private set; } = true;

    [Header("Light2D Intensity")]
    [SerializeField] private float intensityOffset = 0.5f;
    [SerializeField] private float intensityAmplitude = 0.5f;
    [SerializeField] private float intensityFrequency = 0.7f;
    [Range(0f, 10f)] private float intensityTimeOffset = 0f;

    private Vector3 defaultLocalScale;
    private Tween spawnTween;

    void Awake()
    {
        defaultLocalScale = transform.localScale;

        pv = GetComponent<PhotonView>();
        rend = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        light2d = GetComponent<Light2D>();
        lastSentPosition = transform.position;
        lastSentActiveState = isActiveState;
        if (particleManager == null)
        {
            particleManager = FindFirstObjectByType<ParticleManager>();
            this.transform.SetParent(particleManager.transform);
        }

        // 子オブジェクトから ParticleSparkleController を取得してリストに追加
        foreach (Transform child in transform)
        {
            ParticleSparkleController sparkle = child.GetComponent<ParticleSparkleController>();
            if (sparkle != null)
            {
                sparkles.Add(sparkle);
            }
        }
        intensityTimeOffset = Random.Range(0f, 10f); // 各パーティクルで時間オフセットをランダムにしてちらつきにバリエーションを持たせる
    }

    void Update()
    {
        //光の明るさを増減させる
        light2d.intensity = Mathf.PingPong((Time.time + intensityTimeOffset) * intensityFrequency, intensityAmplitude) + intensityOffset;
    }

    void OnEnable()
    {
        Color myColor = DecideRandomColor();
        light2d.color = myColor;

        //ParticleManagerから呼ぶことに
        //PlaySpawnEffect(myColor);
    }

    public void SetVisible(bool visible, int n = 0)
    {
        IsVisible = visible;

        if (rend != null)
        {
            rend.enabled = visible;
        }
        if (col != null)
        {
            col.enabled = visible;
        }
        if (light2d != null)
        {
            light2d.enabled = visible;
        }
        //取得演出
        if (!visible && n == 0)
        {
            CollectEffect();
        }
        // 子オブジェクトのスパークルも表示/非表示を切り替える
        foreach (var sparkle in sparkles)
        {
            if (visible)
            {
                sparkle.Appear();
            }
            else
            {
                sparkle.Disappear();
            }
        }
    }

    /// <summary>
    /// アクティブ状態を設定し、ネットワークで同期する（SetVisible()も含む）
    /// </summary>
    public void SetActive(bool active, int n = 0)
    {
        isActiveState = active;
        SetVisible(active, n);
    }

    private void CollectEffect()
    {
        GetEffectManager.Instance.PlayGetEffect(transform.position);
    }

    public void Initialize(int id, Vector2 pos, Color color)
    {
        ParticleId = id;
        transform.position = pos;
        SetColor(color);

        isActiveState = true;
        gameObject.SetActive(true);
    }

    public void Release(int n, int requesterActor = -1)
    {
        // 即座にローカルで非表示にして重複取得を防止（楽観的ロック）
        if (!IsVisible) return; // すでに取得リクエスト送信済み

        // マスター自身はこの直後に同インスタンスで判定するため、先に非表示にしない
        // （先に非表示にすると MasterHandleCollect 側の IsVisible ガードで弾かれる）
        if (!PhotonNetwork.IsMasterClient)
        {
            // 受信側の状態判定と矛盾しないよう、表示だけでなく状態も下げる
            SetActive(false);
        }

        if (particleManager != null) particleManager.RequestCollectFromMaster(pv.ViewID, n, requesterActor);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 非表示状態では衝突判定を無視
        if (!IsVisible) return;

        if (collision.CompareTag("Player"))
        {
            PhotonView playerView = collision.GetComponentInParent<PhotonView>();

            // 自分が所有していないプレイヤーの当たり判定では取得処理を走らせない
            if (playerView != null && !playerView.IsMine)
            {
                return;
            }

            int requesterActor = playerView != null
                ? playerView.OwnerActorNr
                : (PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : -1);

            Release(0, requesterActor);
        }
        else if (collision.CompareTag("CPU"))
        {
            Release(1, -1);
        }
    }

    public void Deactivate()
    {
        isActiveState = false;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// HSVのHをランダムに選択
    /// </summary>
    /*
    public void SetColor()
    {
        Light2D light = GetComponent<Light2D>();
        float h = Random.Range(0f, 1f);
        Color color = Color.HSVToRGB(h, 1f, 1f);
        light.color = color;
    }
    */

    private Color DecideRandomColor()
    {
        float h = Random.Range(0f, 1f);
        Color color = Color.HSVToRGB(h, 1f, 1f);
        return color;
    }

    public void SetColor(Color color)
    {
        Light2D light = GetComponent<Light2D>();
        light.color = color;
    }

    public void PlaySpawnEffect(ParticleEventEffect effect)
    {
        spawnTween?.Kill();

        // プール再利用時に前回の演出状態を残さない
        transform.localScale = defaultLocalScale;
        transform.localRotation = Quaternion.identity;

        if (effect == null)
            return;

        // VFX
        if (effect.spawnVfxPrefab != null)
        {
            Instantiate(
                effect.spawnVfxPrefab,
                transform.position,
                Quaternion.identity
            );
        }

        // 収集粒子本体
        ParticleBehavior behavior = effect.behavior;
        if (behavior == null)
            return;

        transform.localScale = Vector3.Scale(defaultLocalScale, behavior.startScale);
        transform.localRotation = Quaternion.Euler(0f, 0f, behavior.startRotationZ);

        Sequence sequence = DOTween.Sequence()
            .SetDelay(behavior.delay)
            .Append(transform
                .DOScale(
                    Vector3.Scale(defaultLocalScale, behavior.endScale),
                    behavior.scaleDuration)
                .SetEase(behavior.ease))
            .Join(transform
                .DORotate(
                    new Vector3(0f, 0f, behavior.startRotationZ + behavior.rotateAmountZ),
                    behavior.rotationDuration,
                    RotateMode.FastBeyond360)
                .SetEase(behavior.ease));

        spawnTween = sequence;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 変化があったときのみ送信して帯域を節約する
            Vector3 currentPosition = transform.position;
            bool positionChanged = (currentPosition - lastSentPosition).sqrMagnitude > positionSyncThresholdSqr;
            bool activeStateChanged = isActiveState != lastSentActiveState;

            if (!hasSentInitialState || positionChanged || activeStateChanged)
            {
                stream.SendNext(currentPosition);
                stream.SendNext(isActiveState);

                lastSentPosition = currentPosition;
                lastSentActiveState = isActiveState;
                hasSentInitialState = true;
            }
        }
        else
        {
            // 他クライアントが受信：位置と表示状態を適用
            Vector3 receivedPos = (Vector3)stream.ReceiveNext();
            bool receivedActiveState = (bool)stream.ReceiveNext();

            // 位置を同期
            if (Vector3.Distance(transform.position, receivedPos) > 0.01f)
            {
                transform.position = receivedPos;
            }

            // 表示状態を同期（ローカル楽観ロックで見た目だけズレた場合も補正）
            if (isActiveState != receivedActiveState || IsVisible != receivedActiveState)
            {
                isActiveState = receivedActiveState;
                SetVisible(isActiveState);
            }
        }
    }
}
