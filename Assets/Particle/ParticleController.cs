using UnityEngine;
using UnityEngine.Rendering.Universal;

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
        particleManager.CountParticle(1);
        particleManager.ReturnToPool(this);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Release();
        }
    }

    /// <summary>
    /// HSVのHをランダムに選択
    /// </summary>
    public void SetColor()
    {
        Light2D light = GetComponent<Light2D>();
        float h = Random.Range(0f, 1f);
        Color color = Color.HSVToRGB(h, 1f, 1f);
        light.color = color;
    }
}
