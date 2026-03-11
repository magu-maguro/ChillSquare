using UnityEngine;
using Photon.Pun;
using Sirenix.OdinInspector;

public class CPUPlayerController : PlayerController
{
    [Space(10)]
    [Header("------以下CPU------")]

    [ShowInInspector, LabelText("行動パターン")] private MovementState movementState = MovementState.Idle;
    /// <summary>
    /// 行動パターン
    /// </summary>
    public enum MovementState
    {
        Idle, LightSeeking, ChasePlayer, Wander
    }

    private int[][] stateTransitionRates = new int[][]
    {
        // Idle, LightSeeking, ChasePlayer, Wander
        new int[] { 10, 30, 30, 30 }, // Idleからの遷移率
        new int[] {  1, 80,  5, 14 }, // LightSeekingからの遷移率
        new int[] {  1, 14, 80,  5 }, // ChasePlayerからの遷移率
        new int[] {  1,  5, 14, 80 }  // Wanderからの遷移率
    };

    [ShowInInspector, LabelText("行動パターン変化まであと")] private float movementStateTimer;
    private float movementStateDecisionInterval = 3f;
    //------Horizontal------
    [ShowInInspector, LabelText("横移動状態")] private HorizontalState horizontalState;
    public enum HorizontalState
    {
        Idle,
        WalkLeft,
        WalkRight
    }
    [ShowInInspector, LabelText("横移動状態遷移まであと")] private float horizontalTimer;
    private float horizontalDecisionInterval = 3f;
    //------Jump------

    [ShowInInspector, LabelText("ジャンプ状態")] private JumpState jumpState;
    public enum JumpState
    {
        Idle,
        Jump
    }
    [ShowInInspector, LabelText("ジャンプ状態遷移まであと")] private float JumpTimer;
    //------Input------
    private Vector2 cpuMoveInput;
    private bool cpuJumpPressed;
    private bool cpuJumpPressing;
    //------Rates------
    //

    //------CPU Flag------
    protected override bool IsCPU => true;
    public bool canMove = true;

    //------Sensor------
    private CPUPlayerSensor sensor;
    protected override void Start()
    {
        base.Start();
        sensor = GetComponent<CPUPlayerSensor>();
        ApplyCPUSkin();
    }

    private void Update()
    {
        if (!canMove) return;
        DecideMovementState();
        UpdateHorizontalMovement();
        UpdateJump();
    }

    // movementStateTimerごとにマルコフ連鎖的に状態決定
    private void DecideMovementState()
    {
        movementStateTimer -= Time.deltaTime;
        if (movementStateTimer > 0f) return;

        int current = (int)movementState;
        int[] rates = stateTransitionRates[current];

        int randomValue = Random.Range(0, 100);
        int cumulative = 0;

        for (int i = 0; i < rates.Length; i++)
        {
            cumulative += rates[i];
            if (randomValue < cumulative)
            {
                movementState = (MovementState)i;
                break;
            }
        }
        movementStateDecisionInterval = Random.Range(2f, 5f);
        movementStateTimer = movementStateDecisionInterval;
    }

    private void UpdateHorizontalMovement()
    {
        horizontalTimer -= Time.deltaTime;
        if (horizontalTimer > 0f) return;


        switch (DecideHorizontalMovement())
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

    /// <summary>
    /// horizontalStateTimerごとにMovementStateに応じて横移動の状態を決定
    /// </summary>
    /// <returns></returns>
    private HorizontalState DecideHorizontalMovement()
    {
        switch (movementState)
        {
            case MovementState.Idle:
                horizontalState = HorizontalState.Idle;
                break;
            case MovementState.LightSeeking:
                horizontalState = HorizontalLightSeeking();
                break;
            case MovementState.ChasePlayer:
                horizontalState = HorizontalChasePlayer();
                break;
            case MovementState.Wander:
                horizontalState = HorizontalWandering();
                break;
        }
        horizontalDecisionInterval = Random.Range(1f, 2f);
        horizontalTimer = horizontalDecisionInterval;
        return horizontalState;
    }
    private HorizontalState HorizontalLightSeeking()
    {
        // 近くの光源を探す
        if (sensor.NearestParticle != null)
        {
            if (sensor.NearestParticleDirection.x < -0.2f)
                return HorizontalState.WalkLeft;
            else if (sensor.NearestParticleDirection.x > 0.2f)
                return HorizontalState.WalkRight;
        }
        return HorizontalWandering();
    }
    private HorizontalState HorizontalChasePlayer()
    {
        // プレイヤーの方向を探す
        if (sensor.NearestPlayer != null)
        {
            if (sensor.DirectionToPlayer.x < -0.2f)
                return HorizontalState.WalkLeft;
            else if (sensor.DirectionToPlayer.x > 0.2f)
                return HorizontalState.WalkRight;
        }
        return HorizontalWandering();
    }
    private HorizontalState HorizontalWandering()
    {
        int randomValue = Random.Range(0, 100);
        switch (horizontalState)
        {
            case HorizontalState.Idle:
                if (randomValue < 20)
                    return HorizontalState.Idle;
                else if (randomValue < 60)
                    return HorizontalState.WalkLeft;
                else
                    return HorizontalState.WalkRight;
            case HorizontalState.WalkLeft:
                if (randomValue < 70)
                    return HorizontalState.WalkLeft;
                else if (randomValue < 80)
                    return HorizontalState.Idle;
                else
                    return HorizontalState.WalkRight;
            case HorizontalState.WalkRight:
                if (randomValue < 70)
                    return HorizontalState.WalkRight;
                else if (randomValue < 80)
                    return HorizontalState.Idle;
                else
                    return HorizontalState.WalkLeft;
            default:
                return HorizontalState.Idle;
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
            JumpTimer = Random.Range(0.3f, 0.7f);
        }

        switch (DecideJump())
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

    private JumpState DecideJump()
    {
        switch (movementState)
        {
            case MovementState.Idle:
                jumpState = JumpState.Idle;
                break;
            case MovementState.LightSeeking:
                jumpState = JumpLightSeeking();
                break;
            case MovementState.ChasePlayer:
                jumpState = JumpChasePlayer();
                break;
            case MovementState.Wander:
                jumpState = JumpWandering();
                break;
        }
        return jumpState;
    }
    private JumpState JumpLightSeeking()
    {
        if (sensor.NearestParticle != null)
        {
            if (sensor.NearestParticleDirection.y > 0.5f)
                return JumpState.Jump;
        }
        return JumpState.Idle;
    }
    private JumpState JumpChasePlayer()
    {
        if (sensor.NearestPlayer != null)
        {
            if (sensor.DirectionToPlayer.y > 0.5f)
                return JumpState.Jump;
        }
        return JumpState.Idle;
    }
    private JumpState JumpWandering()
    {
        if (Dice(70))
            return JumpState.Jump;
        return JumpState.Idle;
    }

    #region GetInput Override
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

    #endregion

    private bool Dice(int percentage)
    {
        int n = Random.Range(1, 101);//1~100のランダムな整数
        if (n <= percentage) return true;
        else return false;
    }

    #region Skin

    void ApplyCPUSkin()
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
        Debug.Log("CPU, applySkin!!!!!!!!!!!!!!!!!!!");

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
    #endregion
}
