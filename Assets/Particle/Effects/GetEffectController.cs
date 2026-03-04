using UnityEngine;

/// <summary>
/// lifetimeが尽きたら破棄
/// </summary>
public class GetEffectController : MonoBehaviour
{
    public GetEffectManager getEffectManager;
    private ParticleSystem ps;
    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        getEffectManager = GetEffectManager.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        if (!ps.IsAlive())
        {
            getEffectManager.ReturnEffectToPool(this.gameObject);
        }
    }

    public void PlayEffect(Vector3 position)
    {
        gameObject.SetActive(true);
        transform.position = position;
        ps.Play();
    }
}
