using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;
    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private bool isJumpPressed;//ジャンプキー押した瞬間かどうか
    private bool isJumpPressing;//ジャンプキー押している間かどうか
    //子オブジェクトのコライダーで接地判定
    private Collider2D groundCollider;
    private bool isGrounded;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float jumpInitialSpeed = 24f;
    [Header("gravity")]
    [SerializeField] private float jumpingGravity = 5f;
    [SerializeField] private float normalGravity = 10f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        inputActions = new PlayerInputActions();
        groundCollider = transform.GetChild(0).GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();

        //横移動
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        //ジャンプ
        inputActions.Player.Jump.performed += ctx => { isJumpPressed = true; isJumpPressing = true; };
        inputActions.Player.Jump.canceled += ctx => isJumpPressing = false;
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

    private void FixedUpdate()
    {
        //横移動
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);

        //接地判定
        isGrounded = GetComponentInChildren<ForGround>().IsGrounded;

        //ジャンプ(初速度与えるだけ)
        if (isJumpPressed && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpInitialSpeed);
        }

        //ジャンプキー押していて上昇中のときだけ重力弱め
        if (isJumpPressing && rb.linearVelocity.y > 0)
        {
            rb.gravityScale = jumpingGravity;
        }
        else
        {
            rb.gravityScale = normalGravity;
        }

        isJumpPressed = false;
    }
}
