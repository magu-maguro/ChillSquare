using UnityEngine;

public class CPUPlayerSensor : MonoBehaviour
{
    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.5f;
    [SerializeField] private LayerMask groundLayer;

    public bool IsGroundBelow { get; private set; }
    public bool IsGroundFront { get; private set; }
    public bool IsGroundBack { get; private set; }

    [Header("Wall Check")]
    [SerializeField] private float wallCheckDistance = 0.5f;
    public bool IsWallFront { get; private set; }

    [Header("Particle Check")]
    [SerializeField] private float particleCheckRadius = 15f;
    public Transform NearestParticle { get; private set; }
    public float NearestParticleDistance { get; private set; }
    public Vector2 NearestParticleDirection { get; private set; }
    [Header("Player Check")]
    [SerializeField] private float playerCheckRadius = 20f;
    public Vector2 DirectionToPlayer { get; private set; }
    public Transform NearestPlayer { get; private set; }
    public float NearestPlayerDistance { get; private set; }

    private void Update()
    {
        CheckGround();
        CheckWall();
        CheckNearestParticle();
        CheckPlayerDirection();
    }

    private void CheckGround()
    {
        Vector2 origin = transform.position;

        IsGroundBelow =
            Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);

        IsGroundFront =
            Physics2D.Raycast(origin + Vector2.right * 0.3f, Vector2.down, groundCheckDistance, groundLayer);

        IsGroundBack =
            Physics2D.Raycast(origin + Vector2.left * 0.3f, Vector2.down, groundCheckDistance, groundLayer);
    }

    private void CheckWall()
    {
        Vector2 origin = transform.position;

        IsWallFront =
            Physics2D.Raycast(origin, Vector2.right, wallCheckDistance, groundLayer);
    }

    private void CheckNearestParticle()
    {
        //円コライダー内の最も近いコライダーとの座標の差を取得
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, particleCheckRadius);
        NearestParticle = null;
        NearestParticleDistance = float.MaxValue;
        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag("Particle"))
            {
                float distance = Vector2.Distance(transform.position, collider.transform.position);
                if (distance < NearestParticleDistance)
                {
                    NearestParticleDistance = distance;
                    NearestParticle = collider.transform;
                    NearestParticleDirection = collider.transform.position - transform.position;

                    Debug.DrawLine(transform.position, collider.transform.position, Color.green);
                }
            }
        }
    }

    private void CheckPlayerDirection()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, playerCheckRadius);
        NearestPlayer = null;
        NearestPlayerDistance = float.MaxValue;
        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag("Player"))
            {
                float distance = Vector2.Distance(transform.position, collider.transform.position);
                if (distance < NearestPlayerDistance)
                {
                    NearestPlayerDistance = distance;
                    NearestPlayer = collider.transform;
                    DirectionToPlayer = collider.transform.position - transform.position;
                    Debug.DrawLine(transform.position, collider.transform.position, Color.red);
                }
                return;
            }
        }
    }
}
