using UnityEngine;

/// <summary>
/// ParticleSystemは用いていない
/// あくまでGameObjectをparticleと呼んでいるだけ
/// 各パーティクルの挙動を管理
/// </summary>
public class ParticleController : MonoBehaviour
{
    private ParticleManager particleManager;
    public ParticleManager ParticleManager { get => particleManager; set => particleManager = value; }

    public void Release()
    {
        particleManager.ReturnToPool(this);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Release();
        }
    }
}
