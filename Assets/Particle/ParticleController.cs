using UnityEngine;
using UnityEngine.Rendering.Universal;
using Photon.Pun;
using System.Collections.Generic;

/// <summary>
/// ParticleSystemは用いていない
/// あくまでGameObjectをparticleと呼んでいるだけ
/// 各パーティクルの挙動を管理
/// </summary>
public class ParticleController : MonoBehaviour, IPunObservable
{
    private ParticleManager particleManager;
    public ParticleManager ParticleManager { get => particleManager; set => particleManager = value; }
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
    [SerializeField] private ParticleSystem effectPrefab;

    public bool IsVisible {get; private set;} = true;

    void Awake()
    {
        pv = GetComponent<PhotonView>();
        rend = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        light2d = GetComponent<Light2D>();
        lastSentPosition = transform.position;
        lastSentActiveState = isActiveState;
        if(particleManager == null)
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
    }

    void OnEnable()
    {
        light2d.color = DecideRandomColor();
    }

    public void SetVisible(bool visible, int n = 0)
    {
        IsVisible = visible;

        if(rend != null)
        {
            rend.enabled = visible;
        }
        if(col != null)
        {
            col.enabled = visible;
        }
        if(light2d != null)
        {
            light2d.enabled = visible;
        }
        //取得演出
        if(!visible && n == 0)
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
        var effect = Instantiate(effectPrefab, transform.position, Quaternion.identity);
        effect.Play();
    }

    public void Initialize(int id, Vector2 pos, Color color)
    {
        ParticleId = id;
        transform.position = pos;
        SetColor(color);

        isActiveState = true;
        gameObject.SetActive(true);
    }

    public void Release(int n)
    {
        // 即座にローカルで非表示にして重複取得を防止（楽観的ロック）
        if (!IsVisible) return; // すでに取得リクエスト送信済み

        // マスター自身はこの直後に同インスタンスで判定するため、先に非表示にしない
        // （先に非表示にすると MasterHandleCollect 側の IsVisible ガードで弾かれる）
        if (!PhotonNetwork.IsMasterClient)
        {
            SetVisible(false);
        }
        
        if(particleManager != null) particleManager.RequestCollectFromMaster(pv.ViewID, n);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 非表示状態では衝突判定を無視
        if (!IsVisible) return;

        if (collision.CompareTag("Player"))
        {
            Release(0);
        }
        else if (collision.CompareTag("CPU"))
        {
            Release(1);
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

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if(stream.IsWriting)
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
            
            // 表示状態を同期
            if (isActiveState != receivedActiveState)
            {
                isActiveState = receivedActiveState;
                SetVisible(isActiveState);
            }
        }
    }
}
