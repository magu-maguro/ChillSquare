using UnityEngine;

public class ForGround : MonoBehaviour
{
    public bool IsGrounded { get; private set; }

    private int groundContactCount = 0;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ground") || other.CompareTag("Player/Head"))
        {
            groundContactCount++;
            IsGrounded = groundContactCount > 0;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Ground") || other.CompareTag("Player/Head"))
        {
            groundContactCount = Mathf.Max(0, groundContactCount - 1);
            IsGrounded = groundContactCount > 0;
        }
    }
}
