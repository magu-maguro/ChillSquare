using Photon.Pun;
using UnityEngine;
using UniRx;

public class PlayerSkinController : MonoBehaviourPun
{
    SpriteRenderer spriteRenderer;
    private bool isLocalOwner = false;

    private SkinChangeManager skinChangeManager;
    public void Initialize(SkinChangeManager manager)
    {
        skinChangeManager = manager;
    }
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        // 所有判定
        isLocalOwner = (photonView == null) || photonView.IsMine;

        // 初期スキン適用
        ApplySavedSkin();

        // SkinChangeManager の Save 通知を購読
        //SkinChangeManager skinChangeManager = FindAnyObjectByType<SkinChangeManager>();
        if (skinChangeManager != null)
        {
            skinChangeManager.OnSkinSaved
                .Subscribe(data =>
                {
                    // スキンデータをJSONに変換
                    string json = JsonUtility.ToJson(data);
                    // RPC(AllBuffered)でスキンを全クライアントに反映
                    if (photonView != null && isLocalOwner)
                    {
                        photonView.RPC(nameof(RPC_ApplySkin), RpcTarget.AllBuffered, json);
                    }
                    else if (isLocalOwner)
                    {
                        // PhotonViewがない場合は直接適用
                        skinChangeManager.ApplySkin(spriteRenderer, data);
                    }
                })
                .AddTo(this);
        }
    }

    void ApplySavedSkin()
    {
        if (!isLocalOwner) return;

        if (PlayerPrefs.HasKey("SkinData"))
        {
            string json = PlayerPrefs.GetString("SkinData");
            if (photonView != null)
            {
                photonView.RPC(nameof(RPC_ApplySkin), RpcTarget.AllBuffered, json);
            }
            else
            {
                // PhotonViewがない場合は直接適用
                SkinChangeManager manager = FindAnyObjectByType<SkinChangeManager>();
                if (manager != null)
                {
                    var data = JsonUtility.FromJson<SkinData>(json);
                    manager.ApplySkin(spriteRenderer, data);
                }
            }
        }
    }

    [PunRPC]
    void RPC_ApplySkin(string json)
    {
        SkinChangeManager manager = FindAnyObjectByType<SkinChangeManager>();
        if (manager != null)
        {
            var data = JsonUtility.FromJson<SkinData>(json);
            manager.ApplySkin(spriteRenderer, data);
        }
    }
}
