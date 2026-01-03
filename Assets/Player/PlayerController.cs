using UnityEngine;
using UnityEngine.InputSystem;
using UniRx;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;

    #region input
    //---入力関係
    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private bool isJumpPressed;//ジャンプキー押した瞬間かどうか
    private bool isJumpPressing;//ジャンプキー押している間かどうか
    private bool isJumping;//isJumpPressed~着地の間かどうか
    private bool isDoubleJumped;//二段ジャンプしたかどうか

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
    [Header("time")]
    [SerializeField] private float coyoteTime = 0.2f;
    private float coyoteTimer = 0f;
    //---
    //CPUかどうか
    protected virtual bool IsCPU => false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        inputActions = new PlayerInputActions();
        groundCollider = transform.GetChild(0).GetComponent<Collider2D>();
    }

    private void Start()
    {
        ApplyPlayerSkin();

        // SkinChangeManager の Save 通知を購読（CPUは無視）
        if (!IsCPU)
        {
            SkinChangeManager skinChangeManager = FindAnyObjectByType<SkinChangeManager>();
            if (skinChangeManager != null)
            {
                skinChangeManager.OnSkinSaved
                    .Subscribe(data =>
                    {
                        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
                        skinChangeManager.ApplySkin(renderer, data);
                    })
                    .AddTo(this);
            }
        }
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();

        //横移動
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        //ジャンプ
        // ジャンプ入力時は押下フラグのみ立てる。isJumping は実際にジャンプしたときに true にする。
        inputActions.Player.Jump.performed += ctx => { isJumpPressed = true; isJumpPressing = true; };
        inputActions.Player.Jump.canceled += ctx => isJumpPressing = false;
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

    protected virtual void ApplyPlayerSkin()
    {
        SkinChangeManager skinChangeManager = FindAnyObjectByType<SkinChangeManager>();
        if (skinChangeManager != null && PlayerPrefs.HasKey("SkinData"))
        {
            string json = PlayerPrefs.GetString("SkinData");
            SkinData data = JsonUtility.FromJson<SkinData>(json);
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            skinChangeManager.ApplySkin(renderer, data);
        }
    }

    private void FixedUpdate()
    {
        if(!IsCPU) Debug.Log(CanJump());
        //GameManagerのbool確認
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
        isGrounded = GetComponentInChildren<ForGround>().IsGrounded;
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
        Junp();
    }

    private bool jumpPressed, jumpPressing;
    private bool canJump;


    private void Junp()
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
        else
        {
            rb.gravityScale = normalGravity;
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
