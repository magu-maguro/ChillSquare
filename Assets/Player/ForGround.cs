using UnityEngine;
using System.Collections.Generic;

public class ForGround : MonoBehaviour
{
    public bool IsGrounded { get; private set; }

    private int groundContactCount = 0;
    //最後に触れたコライダーのレイヤーを取得する
    private List<Collider2D> groundColliders = new List<Collider2D>();
    public Collider2D LastGroundCollider => groundColliders.Count > 0 ? groundColliders[groundColliders.Count - 1] : null;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ground") || other.CompareTag("Player/Head"))
        {
            groundContactCount++;
            IsGrounded = groundContactCount > 0;
            groundColliders.Add(other);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Ground") || other.CompareTag("Player/Head"))
        {
            groundContactCount = Mathf.Max(0, groundContactCount - 1);
            IsGrounded = groundContactCount > 0;
            groundColliders.Remove(other);
        }
    }
}
