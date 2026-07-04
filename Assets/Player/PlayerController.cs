using Photon.Pun;
using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using UniRx;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviourPunCallbacks
{
    private Rigidbody2D rb;
    private ForGround forGround;
    // このオブジェクトがこのクライアントの所有か
    private bool isLocalOwner = false;

    #region input
    //---入力関係
    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private bool isJumpPressed;//ジャンプキー押した瞬間かどうか
    private bool isJumpPressing;//ジャンプキー押している間かどうか
    private bool isJumping;//isJumpPressed~着地の間かどうか
    private bool isDoubleJumped;//二段ジャンプしたかどうか
    private bool isDownPressing;//急降下中かどうか

    protected virtual Vector2 GetMoveInput()
    {
        return moveInput;
    }
    protected virtual bool GetJumpPressed()
    {
        return isJumpPressed;
    }
    protected virtual bool GetJumpPressing()
    {
        return isJumpPressing;
    }
    #endregion
    //子オブジェクトのコライダーで接地判定
    private Collider2D groundCollider;
    private bool isGrounded;
    // コヨーテタイムを使わなかったときに、空中で1回だけジャンプを許可するフラグ
    private bool allowAirJumpAfterCoyoteMiss = false;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float jumpInitialSpeed = 24f;
    [SerializeField] private bool allowDoubleJump = true;
    [Header("gravity")]
    [SerializeField] private float jumpingGravity = 5f;
    [SerializeField] private float normalGravity = 10f;
    [SerializeField] private float maxFallSpeed = 20f;
    [SerializeField] private float maxFastFallSpeed = 40f;
    [Header("time")]
    [SerializeField] private float coyoteTime = 0.2f;
    private float coyoteTimer = 0f;
    //---
    //CPUかどうか
    protected virtual bool IsCPU => false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        forGround = GetComponentInChildren<ForGround>();
        groundCollider = transform.GetChild(0).GetComponent<Collider2D>();
    }

    protected virtual void Start()
    {
        // 所有判定（Start時点での判定）
        isLocalOwner = (photonView == null) || photonView.IsMine;

        // ローカル所有者のみで入力を登録する（InputActionsはシングルトンの可能性があるため、
        // 非所有者でDisableすると同クライアントの他インスタンスに影響する）
        if (isLocalOwner)
        {
            if (InputManager.Instance != null)
            {
                inputActions = InputManager.Instance.GetInputActions();
                if (inputActions != null)
                {
                    inputActions.Player.Enable();
                    //横移動
                    inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
                    inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
                    //ジャンプ
                    inputActions.Player.Jump.performed += ctx => { isJumpPressed = true; isJumpPressing = true; };
                    inputActions.Player.Jump.canceled += ctx => isJumpPressing = false;
                    //急降下
                    inputActions.Player.FastFall.performed += ctx => isDownPressing = true;
                    inputActions.Player.FastFall.canceled += ctx => isDownPressing = false;
                }
            }
        }
        else
        {
            // 非オーナーはローカル物理を止めて受信座標に従う
            //rb.simulated = false;
            rb.bodyType = RigidbodyType2D.Kinematic;
            //foreach (var col in GetComponents<Collider2D>()) col.isTrigger = true;
        }


    }

    public override void OnDisable()
    {
        if (!isLocalOwner) return;
        if (inputActions == null) return;

        inputActions.Player.Disable();
    }

    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        base.OnMasterClientSwitched(newMasterClient);
        RefreshOwnershipState();
    }



    private void FixedUpdate()
    {
        //if(!IsCPU) Debug.Log(CanJump());
        //GameManagerのbool確認
        RefreshOwnershipState();
        // 自分が所有していないプレイヤーは処理をスキップ（位置同期はPlayerNetworkSyncが担当）
        if (photonView != null && !photonView.IsMine)
        {
            return;
        }

        if (!GameManager.Instance.IsPlayerInputAllowed())
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }
        //横移動
        //rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
        //moveInputなどを間接的に取る(CPU対応)
        var input = GetMoveInput();
        rb.linearVelocity = new Vector2(input.x * moveSpeed, rb.linearVelocity.y);

        //接地判定
        isGrounded = forGround.IsGrounded;
        if(isGrounded && forGround.LastGroundCollider != null && forGround.LastGroundCollider.gameObject.layer == LayerMask.NameToLayer("HalfCollision"))
        {
            //半透明床の上にいる場合は、下方向の入力があるときだけ接地判定を無効化する
            if (isDownPressing)
            {
                isGrounded = false;
                StartCoroutine(Fallthrough(forGround.LastGroundCollider, 0.2f));
            }
        }
        if (isGrounded)
        {
            isJumping = false;
            isDoubleJumped = false;
            allowAirJumpAfterCoyoteMiss = false;
        }
        jumpPressed = GetJumpPressed();
        jumpPressing = GetJumpPressing();
        canJump = CanJump();
        CoyoteControll();
        Jump();
    }

    private IEnumerator Fallthrough(Collider2D platformCollider, float duration)
    {
        Physics2D.IgnoreCollision(GetComponent<Collider2D>(), platformCollider, true);
        yield return new WaitForSeconds(duration);
        Physics2D.IgnoreCollision(GetComponent<Collider2D>(), platformCollider, false);
    }

    private void RefreshOwnershipState()
    {
        bool nowLocalOwner = (photonView == null) || photonView.IsMine;
        if (nowLocalOwner == isLocalOwner) return;

        isLocalOwner = nowLocalOwner;
        if (isLocalOwner)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    private bool jumpPressed, jumpPressing;
    private bool canJump;


    private void Jump()
    {
        //ジャンプ(初速度与えるだけ)
        if (jumpPressed && canJump)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpInitialSpeed);
            // 空中でのジャンプ（isJumping==false）や二段ジャンプの扱い
            if (isJumping)
            {
                // すでに空中ジャンプ済み -> これが二段ジャンプ
                isDoubleJumped = true;
            }
            else
            {
                // isJumping==false のまま空中ジャンプが起きた場合（コヨーテ未使用で地面離脱後など）
                // それを二段ジャンプ扱いにして、以降の追加ジャンプを防ぐ
                if (allowAirJumpAfterCoyoteMiss)
                {
                    isDoubleJumped = true;
                }
            }

            // ジャンプが発生したので isJumping を true にし、フラグをリセット
            isJumping = true;
            allowAirJumpAfterCoyoteMiss = false;
        }

        //ジャンプキー押していて上昇中のときだけ重力弱め
        if (jumpPressing && rb.linearVelocity.y > 0)
        {
            rb.gravityScale = jumpingGravity;
        }
        else if (isDownPressing && rb.linearVelocity.y < 0) //急降下に対応
        {
            rb.gravityScale = normalGravity * 2f;
        }
        else
        {
            rb.gravityScale = normalGravity;
        }

        //落下速度制限
        if (rb.linearVelocity.y < -maxFallSpeed)
        {
            if (isDownPressing) rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFastFallSpeed);
            else rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
        }

        isJumpPressed = false;
    }

    private void CoyoteControll()
    {
        if (!IsCPU)
        {
            if (isGrounded) coyoteTimer = 0f;
            else
            {
                coyoteTimer += Time.deltaTime;
            }
            // コヨーテタイムを過ぎたがジャンプしていない場合、空中で1回だけジャンプを許可する
            if (!isGrounded && coyoteTimer >= coyoteTime && !isJumping)
            {
                allowAirJumpAfterCoyoteMiss = true;
            }
        }
    }

    private bool CanJump()
    {
        if (isGrounded) return true;
        // Ground を離れている場合のジャンプ判定
        // - allowDoubleJump が有効なら、すでにジャンプ中かつ二段ジャンプ未使用であれば二段ジャンプを許可
        // - コヨーテタイム未使用で地面を離れた場合は空中で1回だけジャンプを許可
        // - それ以外はコヨーテタイム内の初回ジャンプのみ許可
        if (allowDoubleJump)
        {
            // 空中で既にジャンプ（最初のジャンプ）していて、二段ジャンプ未使用なら許可
            if (isJumping && !isDoubleJumped) return true;

            // コヨーテタイムを過ぎた後に地面離脱時の特別な空中ジャンプ
            if (allowAirJumpAfterCoyoteMiss && !isDoubleJumped) return true;

            // 地面を離れた直後（コヨーテタイム）に初回ジャンプを許可
            if (!IsCPU && coyoteTimer < coyoteTime && !isJumping) return true;

            return false;
        }

        // 二段ジャンプを許可しない場合は、コヨーテタイム内でまだジャンプしていなければ許可
        if (!IsCPU && coyoteTimer < coyoteTime && !isJumping) return true;
        return false;
    }
}
