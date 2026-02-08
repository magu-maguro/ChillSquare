using UnityEngine;
using Photon.Pun;

public class CPUPlayerController : PlayerController
{
    //------Horizontal------
    private HorizontalState horizontalState;
    public enum HorizontalState
    {
        Idle,
        WalkLeft,
        WalkRight
    }
    private float horizontalTimer;
    //------Jump------

    private JumpState jumpState;
    public enum JumpState
    {
        Idle,
        Jump
    }
    private float JumpTimer;
    //------Input------
    private Vector2 cpuMoveInput;
    private bool cpuJumpPressed;
    private bool cpuJumpPressing;
    //------Rates------
    //

    //------CPU Flag------
    protected override bool IsCPU => true;
    public bool canMove = true;

    private void Update()
    {
        if(!canMove) return;
        UpdateHorizontalMovement();
        UpdateJump();
    }

    private void UpdateHorizontalMovement()
    {
        horizontalTimer -= Time.deltaTime;
        if (horizontalTimer <= 0f)
        {
            // 次の行動を決定
            horizontalState = (HorizontalState)Random.Range(0, 3);//重みを付けたい
            horizontalTimer = Random.Range(0.4f, 1.5f); // 数秒続ける
        }

        switch (horizontalState)
        {
            case HorizontalState.Idle:
                cpuMoveInput = Vector2.zero;
                break;
            case HorizontalState.WalkLeft:
                cpuMoveInput = Vector2.left;
                break;
            case HorizontalState.WalkRight:
                cpuMoveInput = Vector2.right;
                break;
        }
    }

    private void UpdateJump()
    {
        if (cpuJumpPressed)
            cpuJumpPressed = false; // 次のフレームでリセット
        
        JumpTimer -= Time.deltaTime;
        if (JumpTimer <= 0f)
        {
            // 次の行動を決定
            jumpState = (JumpState)Random.Range(0, 2);
            JumpTimer = Random.Range(0.1f, 1f); // 1～3秒続ける
        }

        switch (jumpState)
        {
            case JumpState.Idle:
                cpuJumpPressed = false;
                cpuJumpPressing = false;
                break;
            case JumpState.Jump:
                cpuJumpPressed = true;
                cpuJumpPressing = true;
                break;
        }
    }

    //------------------------------
    protected override Vector2 GetMoveInput()
    {
        return cpuMoveInput;
    }

    protected override bool GetJumpPressed()
    {
        return cpuJumpPressed;
    }

    protected override bool GetJumpPressing()
    {
        return cpuJumpPressing;
    }

    //------------------------------

    private bool Dice(int percentage)
    {
        int n = Random.Range(1, 101);//1~100のランダムな整数
        if (n <= percentage) return true;
        else return false;
    }

    protected override void ApplyPlayerSkin()
    {
        // ランダムな色を生成して、マスターが生成している場合は RPC で全クライアントに反映する
        SkinData randomSkinData = new SkinData();
        for (int i = 0; i < 16; i++)
        {
            randomSkinData.colors[i] = new Color(
                Random.value,
                Random.value,
                Random.value,
                1f
            );
        }

        SkinChangeManager skinChangeManager = FindAnyObjectByType<SkinChangeManager>();
        if (skinChangeManager == null) return;

        string json = JsonUtility.ToJson(randomSkinData);
        // PhotonView を持ち、かつオーナー（通常はマスター）であれば RPC(AllBuffered) で配信
        if (photonView != null && photonView.IsMine)
        {
            photonView.RPC("RPC_ApplyPlayerSkin", RpcTarget.AllBuffered, json);
        }
        else if (photonView == null)
        {
            // Photon 未使用のローカル実行時
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            skinChangeManager.ApplySkin(renderer, randomSkinData);
        }
    }

    [PunRPC]

    void RPC_ApplyPlayerSkin(string skinDataJson)
    {
        SkinChangeManager skinChangeManager = FindAnyObjectByType<SkinChangeManager>();
        if (skinChangeManager != null)
        {
            SkinData data = JsonUtility.FromJson<SkinData>(skinDataJson);
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            skinChangeManager.ApplySkin(renderer, data);
        }
    }
}
